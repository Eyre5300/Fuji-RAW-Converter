using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace S5Raw {
 static class FullPreview {
  sealed class Entry {public Task<PreviewImage> Work;public int Readers;public long Used;public readonly List<Func<bool>> Requests=new List<Func<bool>>();}
  static readonly object gate=new object();
  static readonly Dictionary<string,Entry> cache=new Dictionary<string,Entry>(StringComparer.OrdinalIgnoreCase);
  static readonly SemaphoreSlim slots=new SemaphoreSlim(Environment.ProcessorCount);
  static readonly string disk=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"S5Raw","Preview-v4-official");
  const long MemoryLimit=768L*1024*1024,DiskLimit=1024L*1024*1024;
  static long clock;static bool closing;
  static string Key(string path){var f=new FileInfo(path);return f.FullName+"|"+f.Length+"|"+f.LastWriteTimeUtc.Ticks;}
  public static PreviewImage Read(string path,Func<bool> wanted=null){
   if(wanted!=null&&!wanted())return null;string key=Key(path);Entry entry;TaskCompletionSource<PreviewImage> pending=null;
   lock(gate){
    if(closing)return null;
    if(!cache.TryGetValue(key,out entry)){
     pending=new TaskCompletionSource<PreviewImage>();entry=new Entry{Work=pending.Task};cache.Add(key,entry);
    }
    entry.Readers++;entry.Used=++clock;entry.Requests.Add(wanted);
   }
   // The calling thread is already a background worker. Do not queue another task and block the pool waiting for it.
   if(pending!=null){
    slots.Wait();try{
     bool needed;lock(gate){needed=!closing&&entry.Requests.Any(request=>request==null||request());if(!needed)cache.Remove(key);}
     pending.SetResult(needed?Decode(path,key):null);
    }catch(Exception error){pending.SetException(error);}finally{slots.Release();}
   }
   try{var image=entry.Work.GetAwaiter().GetResult();return image==null||(wanted!=null&&!wanted())?null:image.Retain();}
   finally{lock(gate){entry.Readers--;entry.Requests.Remove(wanted);Entry current;if(entry.Work.IsFaulted&&cache.TryGetValue(key,out current)&&object.ReferenceEquals(current,entry))cache.Remove(key);TrimMemory();}}
  }
  static void TrimMemory(){
   long bytes=cache.Values.Where(e=>e.Work.Status==TaskStatus.RanToCompletion).Sum(e=>e.Work.Result.Bytes);
   foreach(var item in cache.OrderBy(e=>e.Value.Used).ToArray()){
    if(!closing&&bytes<=MemoryLimit)break;
    if(item.Value.Readers!=0||item.Value.Work.Status!=TaskStatus.RanToCompletion)continue;
    bytes-=item.Value.Work.Result.Bytes;item.Value.Work.Result.Dispose();cache.Remove(item.Key);
   }
  }
  static PreviewImage Decode(string path,string key){
   if(!Path.GetExtension(path).Equals(".raf",StringComparison.OrdinalIgnoreCase))return PreviewReader.Read(path);
   string name;using(var sha=SHA256.Create())name=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-","");
   string saved=Path.Combine(disk,name+".bmp");
   if(File.Exists(saved)){try{var hit=PreviewReader.Read(saved,true);File.SetLastWriteTimeUtc(saved,DateTime.UtcNow);return hit;}catch(IOException){}catch(NotSupportedException){}}
   string work=Path.Combine(Path.GetTempPath(),"s5raw-preview-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);
   try{
    string native=Shared.NativePath(work),source;
    try{source=Shared.NativePath(path);}catch(IOException){source=Path.Combine(native,"input.RAF");File.Copy(path,source);}
    string engine=Shared.NativePath(Path.Combine(Path.GetDirectoryName(typeof(Shared).Assembly.Location),"engine","browser"));
    var psi=new ProcessStartInfo(Path.Combine(engine,"s5raw-browser.exe"),Shared.Arguments("--browser-worker",engine,source,Path.Combine(native,"preview.bmp"))){
     UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8,WorkingDirectory=engine
    };
    using(var process=Process.Start(psi)){
     string errors="";var report=new StringBuilder();process.ErrorDataReceived+=delegate(object sender,DataReceivedEventArgs e){if(e.Data!=null)errors+=e.Data+"\n";};process.OutputDataReceived+=delegate(object sender,DataReceivedEventArgs e){if(e.Data!=null)report.AppendLine(e.Data);};process.BeginErrorReadLine();process.BeginOutputReadLine();
     if(!process.WaitForExit(180000)){process.Kill();process.WaitForExit();throw new IOException(L.T("RAW 预览解码超时"));}
     process.WaitForExit();if(process.ExitCode!=0)throw new IOException(string.IsNullOrWhiteSpace(errors)?report.ToString():errors);
    }
    var image=PreviewReader.ReadOfficial(Path.Combine(native,"preview.bmp"),path);
    // Save a lossless display cache independently; the full image can be shown immediately.
    var snapshot=(System.Drawing.Bitmap)image.Bitmap.Clone();ThreadPool.QueueUserWorkItem(delegate{
     string pending=saved+"."+Guid.NewGuid().ToString("N")+".partial";
     try{Directory.CreateDirectory(disk);snapshot.Save(pending,ImageFormat.Bmp);if(File.Exists(saved))File.Delete(saved);File.Move(pending,saved);TrimDisk();}
     catch(Exception){}
     finally{snapshot.Dispose();try{if(File.Exists(pending))File.Delete(pending);}catch(IOException){}catch(UnauthorizedAccessException){}}
    });return image;
   }finally{try{Shared.DeleteOwnedDirectory(work);}catch(IOException){}catch(UnauthorizedAccessException){}}
  }
  static readonly object diskGate=new object();
  static void TrimDisk(){lock(diskGate){var files=new DirectoryInfo(disk).GetFiles("*.bmp").Where(f=>f.Name.Length==68).OrderByDescending(f=>f.LastWriteTimeUtc).ToArray();long bytes=0;foreach(var file in files){bytes+=file.Length;if(bytes>DiskLimit)try{file.Delete();}catch(IOException){}}}}
  public static void Load(Form owner,string path,Action<PreviewImage,Exception> done,Func<bool> wanted=null){
   PreviewImage ready=null;
   string key;try{key=Key(path);}catch(IOException error){done(null,error);return;}
   lock(gate){Entry entry;if(!closing&&cache.TryGetValue(key,out entry)&&entry.Work.Status==TaskStatus.RanToCompletion){entry.Used=++clock;ready=entry.Work.Result.Retain();}}
   if(ready!=null){if(wanted!=null&&!wanted())ready.Dispose();else done(ready,null);return;}
   var worker=new BackgroundWorker();worker.DoWork+=delegate(object s,DoWorkEventArgs e){e.Result=Read(path,wanted);};
   worker.RunWorkerCompleted+=delegate(object s,RunWorkerCompletedEventArgs e){var image=e.Error==null?(PreviewImage)e.Result:null;
    if(owner.IsDisposed||(wanted!=null&&!wanted())){if(image!=null)image.Dispose();}else if(image!=null||e.Error!=null)done(image,e.Error);worker.Dispose();
   };worker.RunWorkerAsync();
  }
  public static void Prefetch(string path,Func<bool> wanted){ThreadPool.QueueUserWorkItem(delegate{if(!wanted())return;try{using(var image=Read(path,wanted)){} }catch(Exception){} });}
  public static void Cleanup(){lock(gate){closing=true;TrimMemory();}}
 }
}
