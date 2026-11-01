using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

namespace S5Raw {
 class Cli {
  static volatile bool cancelled;
  static bool json;
  static string cancelFile;
  static readonly JavaScriptSerializer serializer=new JavaScriptSerializer();
  sealed class Options {
   public List<string> Inputs=new List<string>();
   public string Output,OutputDir,TempDir,EngineDir,Existing="error";
   public bool Recursive,ExplicitColor;
   public int Timeout=180, Bits=8, Quality=92, Jobs=Environment.ProcessorCount;
   public string Format,JobList;
   public string ColorSpace="srgb",Film="standard";
  }
  sealed class Job {public string Input,Relative,Output,Film,ColorSpace,Backend,Engine;public bool ExplicitColor;}
  sealed class UserError:Exception {public int Code;public UserError(string s,int c):base(s){Code=c;}}
  [STAThread] static int Main(string[] args) {
   Console.OutputEncoding=new UTF8Encoding(false);
   if(args.Length>0&&args[0]=="--worker")return NativeWorker.Run(args);
   json=Array.IndexOf(args,"--json")>=0;
   Console.CancelKeyPress+=delegate(object sender,ConsoleCancelEventArgs e){e.Cancel=true;cancelled=true;};
   try {
    if(args.Length==0||args[0]=="--help"||args[0]=="help"||Array.IndexOf(args,"--help")>=0){Help();return 0;}
    if(args[0]=="--version"||args[0]=="version"){Console.WriteLine("s5raw "+Shared.Version);return 0;}
    var o=Parse(args);
    if(args[0]=="doctor")return Doctor(o);
    if(args[0]=="info")return Info(o);
    if(args[0]=="preview")return Preview(o);
    if(args[0]=="convert")return Batch(o);
    throw new UserError("未知命令："+args[0],2);
   }catch(UserError e){Emit(new {type="error",code=e.Code,message=e.Message},e.Message);return e.Code;}
   catch(Exception e){Emit(new {type="error",code=5,message=e.Message},e.Message);return 5;}
  }
  static void Help(){Console.WriteLine(@"Fuji S3/S5 Pro RAW Converter 1.7.5 — FUJIFILM 原厂 RAW 转换命令行

s5raw convert -i photo.RAF -o photo.tif --bits 16
s5raw convert -i folder --output-dir output --recursive --json
s5raw info -i photo.RAF --json
s5raw preview -i photo.RAF -o preview.jpg
s5raw doctor --json

输入：-i / --input 可重复；目录只扫描 .RAF，默认保留相对目录结构。
输出：默认存入各源文件夹；-o 指定单文件，--output-dir 指定批量目录。
选项：--format tiff|jpeg       默认 TIFF；单文件也可由 .jpg 扩展名识别
      --quality 1–100          JPEG 质量，默认 92，全尺寸 sRGB
      --jobs / -j 数量         并行数，默认 CPU 可用逻辑核心数；无机型隐藏上限
                              按 90% 内存预算与任务峰值确定稳定并行数
      --job-list 文件          JSON 数组，每项含 input、relative，可单独指定 film、colorSpace
      --bits 8|16              默认 8；16 为原厂原生 16 位/通道
      --color-space srgb|adobe-rgb  16 位或胶片模拟 TIFF 的输出空间，默认 srgb
      --film 模式名           旧机型：standard/f1/f1a/f1b/f1c/f2
                              X-T50：standard/provia/velvia/astia/classic-chrome/
                              reala-ace/acros/mono 等；用 info 查看完整列表
      --recursive / -r
      --existing error|skip|rename|overwrite  （默认 error）
      --json                   stdout 输出 UTF-8 JSON Lines 事件
      --timeout 秒             单张转换超时，默认 180
      --temp-dir 目录           临时工作区根目录
      --engine-dir 目录         替代原厂内核目录
      --cancel-file 文件        外部程序创建该文件可请求取消

标准 8 位使用 LE 流程；选择胶片模拟时使用原厂高级内核。
胶片模拟适用于 JPEG、8 位及 16 位 TIFF，实际可选模式由该照片的原厂配置决定。
JPEG 使用原厂完整 16 位 sRGB 渲染后编码为 8 位 JPEG。
preview 仅提取内嵌 JPEG。Ctrl+C 可取消，原始 RAF 始终只读。
退出码：0 成功/跳过；2 参数错误；3 输入错误；4 内核错误；5 转换失败；130 取消。
默认 stdout 每行一个成功输出路径，进度及错误写 stderr。");}
  static Options Parse(string[] args){
   var o=new Options();
   for(int i=1;i<args.Length;i++){
    string k=args[i];if(k=="--json")continue;if(k=="-r"||k=="--recursive"){o.Recursive=true;continue;}
    if(i+1>=args.Length)throw new UserError("缺少参数值："+k,2);string v=args[++i];
    switch(k){
     case "-i":case "--input":o.Inputs.Add(Path.GetFullPath(v));break;
     case "-o":case "--output":o.Output=Path.GetFullPath(v);break;
     case "--output-dir":o.OutputDir=Path.GetFullPath(v);break;
     case "--temp-dir":o.TempDir=Path.GetFullPath(v);break;
     case "--engine-dir":o.EngineDir=Path.GetFullPath(v);break;
     case "--existing":o.Existing=v;break;
     case "--format":o.Format=v.ToLowerInvariant();break;
     case "--quality":if(!int.TryParse(v,out o.Quality)||o.Quality<1||o.Quality>100)throw new UserError("quality 必须为 1–100",2);break;
     case "--jobs":case "-j":if(!int.TryParse(v,out o.Jobs)||o.Jobs<1)throw new UserError("jobs 必须为正整数",2);break;
     case "--job-list":o.JobList=Path.GetFullPath(v);break;
     case "--bits":if(!int.TryParse(v,out o.Bits)||(o.Bits!=8&&o.Bits!=16))throw new UserError("bits 必须为 8 或 16",2);break;
     case "--color-space":o.ColorSpace=v;o.ExplicitColor=true;break;
     case "--film":o.Film=v.ToLowerInvariant();break;
     case "--timeout":if(!int.TryParse(v,out o.Timeout)||o.Timeout<1||o.Timeout>86400)throw new UserError("timeout 必须为 1–86400 秒",2);break;
     case "--cancel-file":cancelFile=Path.GetFullPath(v);break;
     default:throw new UserError("未知选项："+k,2);
    }
   }
   if(!new[]{"error","skip","rename","overwrite"}.Contains(o.Existing))throw new UserError("无效 existing 策略",2);
   if(o.Format==null)o.Format=o.Output!=null&&new[]{".jpg",".jpeg"}.Contains(Path.GetExtension(o.Output).ToLowerInvariant())?"jpeg":"tiff";
   if(o.Format!="tiff"&&o.Format!="jpeg")throw new UserError("format 必须为 tiff 或 jpeg",2);
   if(!FilmSimulation.Keys.Concat(RfcEx.Keys).Contains(o.Film))throw new UserError("不支持此胶片名称；请用 info 查看该相机可用模式",2);
   if(o.Format=="jpeg"&&o.ColorSpace!="srgb")throw new UserError("JPEG 输出使用 sRGB；Adobe RGB 请选 TIFF",2);
   if(o.ColorSpace!="srgb"&&o.ColorSpace!="adobe-rgb")throw new UserError("color-space 必须为 srgb 或 adobe-rgb",2);
   if(o.Output!=null&&o.OutputDir!=null)throw new UserError("-o 与 --output-dir 不能同时使用",2);
   return o;
  }
  static void Emit(object value,string human){if(json)Console.WriteLine(Shared.Json(value));else if(human!=null)Console.Error.WriteLine(human);}
  static bool Cancelled(){return cancelled||(cancelFile!=null&&File.Exists(cancelFile));}
  static string Hash(string file){using(var f=File.OpenRead(file))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
  static string VerifyEngine(Options o,string film=null){
   film=film??o.Film;
   string dir=o.EngineDir??(film=="standard"?RafHeader.EngineDirectory:FilmSimulation.EngineDirectory);
   if(!Directory.Exists(dir)||(film=="standard"&&!File.Exists(Path.Combine(dir,"RAFCNVX.dll"))))throw new UserError("缺少原厂内核："+dir,4);
   return dir;
  }
  static void CheckManifest(string dir){
   string mf=Path.Combine(dir,"manifest.json");if(!File.Exists(mf))return;
   var rows=serializer.Deserialize<List<Dictionary<string,object>>>(File.ReadAllText(mf));
   if(rows==null||rows.Count<2)throw new UserError("内核清单无效",4);
   var allowed=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach(var row in rows){string name=Convert.ToString(row["file"]);if(Path.GetFileName(name)!=name)throw new UserError("内核清单路径无效",4);string f=Path.Combine(dir,name);allowed.Add(name);if(!File.Exists(f)||Hash(f)!=Convert.ToString(row["sha256"]))throw new UserError("内核文件缺失或校验失败："+name,4);}
   foreach(var f in Directory.GetFiles(dir,"*.dll"))if(!allowed.Contains(Path.GetFileName(f)))throw new UserError("内核目录存在清单外 DLL："+Path.GetFileName(f),4);
   if(!allowed.Contains("RAFCNVX.dll"))throw new UserError("缺少 RAFCNVX.dll",4);
  }
  static int Doctor(Options o){
   string dir=VerifyEngine(o);CheckManifest(dir);RfcEx.Verify(RfcEx.EngineDirectory);
   if(!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"s5raw-rfcex.exe")))throw new UserError("缺少 RFC EX 工作进程",4);
   Emit(new {type="doctor",ok=true,version=Shared.Version,engine=dir,rfcex=RfcEx.EngineDirectory,rfcexAvailable=Environment.Is64BitOperatingSystem,processBits=IntPtr.Size*8,formats=new[]{"tiff8","tiff16","jpeg"},cleanMachineValidated=false},"内核检查通过。旧机型使用 32 位内核，RFC EX 使用 64 位内核。尚未进行全新 Windows 系统验证。");return 0;
  }
  static uint BE(byte[] b,int n){return ((uint)b[n]<<24)|((uint)b[n+1]<<16)|((uint)b[n+2]<<8)|b[n+3];}
  static Dictionary<string,object> ReadRaf(string file,string engine=null){
   try{return RafHeader.Read(file,engine);}catch(IOException e){throw new UserError(e.Message+"："+file,3);}
  }
  static string Single(Options o){if(o.Inputs.Count!=1||Directory.Exists(o.Inputs[0]))throw new UserError("此命令需要一个 -i 文件",2);return o.Inputs[0];}
  static int Info(Options o){var r=ReadRaf(Single(o),o.EngineDir);r["type"]="info";if((bool)r["supported"])r["filmSimulations"]=(string)r["backend"]=="rfcex"?RfcEx.Films((string)r["model"]):FilmSimulation.Available(o.EngineDir??FilmSimulation.EngineDirectory,(string)r["nativeModule"],o.ColorSpace);if(json)Console.WriteLine(Shared.Json(r));else foreach(var kv in r)Console.WriteLine(kv.Key+": "+kv.Value);return 0;}
  static string SelectTarget(string requested,string policy,HashSet<string> reserved){
   if(Directory.Exists(requested))throw new IOException("输出路径是文件夹："+requested);
   bool exists=File.Exists(requested)||(reserved!=null&&reserved.Contains(requested));
   if(!exists)return requested;
   if(policy=="skip")return null;
   if(policy=="overwrite"&&(reserved==null||!reserved.Contains(requested)))return requested;
   if(policy!="rename")throw new IOException("输出文件已存在或批量名称冲突："+requested);
   string dir=Path.GetDirectoryName(requested),name=Path.GetFileNameWithoutExtension(requested),ext=Path.GetExtension(requested);
   for(int i=2;i<int.MaxValue;i++){string p=Path.Combine(dir,name+"_"+i+ext);if(!File.Exists(p)&&!Directory.Exists(p)&&(reserved==null||!reserved.Contains(p)))return p;}
   throw new IOException("无法分配输出文件名");
  }
  static void Publish(string staged,string target,string policy){
   Directory.CreateDirectory(Path.GetDirectoryName(target));
   string pending=Path.Combine(Path.GetDirectoryName(target),".s5raw-"+Guid.NewGuid().ToString("N")+".partial");
   try{
    File.Copy(staged,pending,false);
    if(Cancelled())throw new OperationCanceledException();
    if(File.Exists(target)&&policy=="overwrite")File.Replace(pending,target,null);else File.Move(pending,target);
   }finally{if(File.Exists(pending))File.Delete(pending);}
  }
  static int Preview(Options o){
   string src=Single(o);var meta=ReadRaf(src);if(o.Output==null)throw new UserError("preview 需要 -o 输出 .jpg 文件",2);
   if(!new[]{".jpg",".jpeg"}.Contains(Path.GetExtension(o.Output).ToLowerInvariant()))throw new UserError("preview 输出扩展名必须为 .jpg 或 .jpeg",2);
   string dst=SelectTarget(o.Output,o.Existing,null);if(dst==null){Emit(new {type="skipped",input=src,output=o.Output},"跳过："+o.Output);return 0;}
   Directory.CreateDirectory(Path.GetDirectoryName(dst));string temp=Path.Combine(Path.GetDirectoryName(dst),".s5raw-"+Guid.NewGuid().ToString("N")+".preview");
   try{using(var f=File.OpenRead(src))using(var w=File.Create(temp)){f.Position=Convert.ToUInt32(meta["previewOffset"]);long left=Convert.ToUInt32(meta["previewBytes"]);var b=new byte[65536];while(left>0){if(Cancelled())return 130;int n=f.Read(b,0,(int)Math.Min(b.Length,left));if(n==0)throw new IOException("RAF 提前结束");w.Write(b,0,n);left-=n;}}
    Publish(temp,dst,o.Existing);Emit(new {type="completed",operation="preview",input=src,output=dst},"预览已提取："+dst);if(!json)Console.WriteLine(dst);return 0;
   }finally{if(File.Exists(temp))File.Delete(temp);}
  }
  static IEnumerable<string> Walk(string dir,bool recursive){
   foreach(string f in Directory.GetFiles(dir).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))if(Path.GetExtension(f).Equals(".raf",StringComparison.OrdinalIgnoreCase))yield return f;
   if(recursive)foreach(string sub in Directory.GetDirectories(dir).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))if((File.GetAttributes(sub)&FileAttributes.ReparsePoint)==0)foreach(string f in Walk(sub,true))yield return f;
  }
  static List<Job> Plan(Options o){
   if(o.Inputs.Count==0&&o.JobList==null)throw new UserError("需要 -i 或 --job-list",2);
   var jobs=new List<Job>();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach(var input in o.Inputs){
    if(Directory.Exists(input)){string root=input.TrimEnd('\\','/')+Path.DirectorySeparatorChar;foreach(var f in Walk(input,o.Recursive))if(seen.Add(f))jobs.Add(new Job{Input=f,Relative=f.Substring(root.Length)});}
    else if(File.Exists(input)){if(seen.Add(input))jobs.Add(new Job{Input=input,Relative=Path.GetFileName(input)});}
    else throw new UserError("输入不存在："+input,3);
   }
   if(o.JobList!=null){
    var rows=new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Deserialize<List<Dictionary<string,string>>>(File.ReadAllText(o.JobList,Encoding.UTF8));
    foreach(var row in rows){string input=Path.GetFullPath(row["input"]),relative=row.ContainsKey("relative")?row["relative"]:Path.GetFileName(input);
     if(Path.IsPathRooted(relative)||relative.Split('\\','/').Contains(".."))throw new UserError("job-list 的 relative 必须为输出目录内的相对路径",2);
     if(!File.Exists(input))throw new UserError("输入不存在："+input,3);
     if(seen.Add(input))jobs.Add(new Job{Input=input,Relative=relative,
      Film=row.ContainsKey("film")&&row["film"]!=null?row["film"].ToLowerInvariant():null,
      ColorSpace=row.ContainsKey("colorSpace")?row["colorSpace"]:null,
      ExplicitColor=row.ContainsKey("colorSpace")&&row["colorSpace"]!=null});
    }
   }
   foreach(var j in jobs){
    j.Film=j.Film??o.Film;j.ColorSpace=j.ColorSpace??o.ColorSpace;j.ExplicitColor=j.ExplicitColor||o.ExplicitColor;
    if(!FilmSimulation.Keys.Concat(RfcEx.Keys).Contains(j.Film))throw new UserError("job-list 中的胶片名称无效："+j.Film,2);
    if(j.ColorSpace!="srgb"&&j.ColorSpace!="adobe-rgb")throw new UserError("job-list 中的 colorSpace 无效",2);
    if(o.Format=="jpeg"&&j.ColorSpace!="srgb")throw new UserError("JPEG 输出使用 sRGB",2);
   }
   if(jobs.Count==0)throw new UserError("没有找到 RAF 文件",3);
   if(o.Output!=null&&jobs.Count!=1)throw new UserError("多个输入请使用 --output-dir",2);
   foreach(var j in jobs){j.Output=o.Output??(o.OutputDir==null?Path.ChangeExtension(j.Input,o.Format=="jpeg"?".jpg":".tif"):Path.Combine(o.OutputDir,Path.ChangeExtension(j.Relative,o.Format=="jpeg"?".jpg":".tif")));if(!(o.Format=="jpeg"?new[]{".jpg",".jpeg"}:new[]{".tif",".tiff"}).Contains(Path.GetExtension(j.Output).ToLowerInvariant()))throw new UserError("输出扩展名与 format 不一致",2);if(j.Input.Equals(j.Output,StringComparison.OrdinalIgnoreCase))throw new UserError("禁止覆盖输入",2);}
   return jobs;
  }
  static int Batch(Options o){
   var jobs=Plan(o);
   foreach(var j in jobs){try{j.Backend=(string)RafHeader.Read(j.Input,o.EngineDir)["backend"];}catch(IOException){}
    if(j.Backend=="rafcnv")j.Engine=VerifyEngine(o,j.Film);
   }
   bool hasModern=jobs.Any(j=>j.Backend=="rfcex");if(hasModern)RfcEx.Verify(RfcEx.EngineDirectory);
   var engineDirs=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
   string parent=o.TempDir??Path.GetTempPath();Directory.CreateDirectory(parent);
   string work=Path.Combine(parent,"s5raw-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);
   int completed=0,failed=0,skipped=0;var used=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   try{
    string nativeWork=Shared.NativePath(work);
    // A batch can contain different legacy films as well as RFC EX files.
    foreach(string source in jobs.Where(j=>j.Engine!=null).Select(j=>j.Engine).Distinct(StringComparer.OrdinalIgnoreCase)){
     string dest=Path.Combine(nativeWork,"engine"+engineDirs.Count);Directory.CreateDirectory(dest);
     foreach(var file in Directory.GetFiles(source))File.Copy(file,Path.Combine(dest,Path.GetFileName(file)));
     engineDirs.Add(source,dest);
    }
    int lanes=Math.Min(o.Jobs,jobs.Count);
    var scheduler=new AdaptiveParallelism(o.Jobs,jobs.Count,(hasModern?1024L:256L)*1024*1024,MemoryPressure.Read,delegate(ParallelState state){Emit(state,"并行：设置 "+state.requested+"，运行 "+state.active+"，内存预算并行数 "+state.target+"，状态 "+state.reason+"；内存预算：90%，按任务峰值确定稳定并行数");});
    Emit(new {type="batch_started",total=jobs.Count,parallel=lanes,requestedParallel=o.Jobs,version=Shared.Version},"开始转换 "+jobs.Count+" 个文件");
    scheduler.Tick();var threads=new List<Thread>();
    for(int lane=0;lane<lanes;lane++){var thread=new Thread(delegate(){while(!Cancelled()){
     int i=scheduler.Take(Cancelled);if(i<0)break;Job j=jobs[i];string actual=null;
     try{
      var info=ReadRaf(j.Input,o.EngineDir);if(!(bool)info["supported"])throw new IOException("当前原厂内核不支持："+info["model"]+"（"+info["processorCode"]+"）");
      bool modern=(string)info["backend"]=="rfcex";if(modern&&!RfcEx.Films((string)info["model"]).Contains(j.Film))throw new IOException("此相机不支持所选胶片模式："+j.Film);
      if(!modern&&!FilmSimulation.Available(j.Engine,(string)info["nativeModule"],j.ColorSpace).Contains(j.Film))throw new IOException("此相机不支持所选胶片模式："+j.Film);
      if(!modern&&o.Format=="tiff"&&o.Bits==8&&j.Film=="standard"&&j.ExplicitColor)throw new IOException("标准 8 位 TIFF 使用 LE 相机色彩空间；指定色彩空间请选 16 位或胶片模拟");
      lock(used){actual=SelectTarget(j.Output,o.Existing,used);if(actual!=null)used.Add(actual);}
      if(actual==null){Interlocked.Increment(ref skipped);Emit(new {type="skipped",index=i+1,total=jobs.Count,input=j.Input,output=j.Output},"跳过："+j.Output);continue;}
      string one=Path.Combine(nativeWork,"f"+i.ToString("D6"));Directory.CreateDirectory(one);
      string input=Path.Combine(one,"input.RAF"),outdir=Path.Combine(one,"out");Directory.CreateDirectory(outdir);File.Copy(j.Input,input,false);
      Emit(new {type="file_started",index=i+1,total=jobs.Count,input=j.Input,output=actual},"["+(i+1)+"/"+jobs.Count+"] "+j.Input);
      var result=RunWorker(modern?RfcEx.EngineDirectory:engineDirs[j.Engine],input,outdir,Path.Combine(one,"cancel"),o.Timeout,o.Format=="jpeg"?16:o.Bits,j.ColorSpace,i+1,jobs.Count,j.Input,o.Format,o.Quality,j.Film,modern,scheduler);
      if(Cancelled())break;
      string generated=Convert.ToString(result["output"]);
      if(!generated.StartsWith(outdir+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!File.Exists(generated))throw new IOException("内核未产生有效输出路径");
      if(o.Format=="tiff")ValidateTiff(generated);
      Publish(generated,actual,o.Existing);Interlocked.Increment(ref completed);
      Emit(new {type="completed",index=i+1,total=jobs.Count,input=j.Input,output=actual,bytes=new FileInfo(actual).Length,format=o.Format,bits=o.Format=="jpeg"?8:o.Bits,colorSpace=o.Format=="jpeg"?"srgb":(modern||o.Bits==16||j.Film!="standard")?j.ColorSpace:"camera",film=j.Film,backend=modern?"rfcex":"rafcnv",modules=result.ContainsKey("modules")?result["modules"]:null},"完成："+actual);
      if(!json)Console.WriteLine(actual);
      Directory.Delete(one,true);
     }catch(OperationCanceledException){cancelled=true;break;}
     catch(Exception ex){Interlocked.Increment(ref failed);Emit(new {type="file_error",index=i+1,total=jobs.Count,input=j.Input,output=actual??j.Output,message=ex.Message},"失败："+j.Input+" — "+ex.Message);}
     finally{scheduler.Complete();}
    }});thread.SetApartmentState(ApartmentState.STA);threads.Add(thread);thread.Start();}
    while(threads.Any(t=>t.IsAlive)){scheduler.Tick();Thread.Sleep(250);}
    foreach(var thread in threads)thread.Join();
    bool stop=Cancelled();Emit(new {type="summary",total=jobs.Count,completed=completed,skipped=skipped,failed=failed,cancelled=stop},"完成 "+completed+"，跳过 "+skipped+"，失败 "+failed+(stop?"，已取消":""));
    return stop?130:failed>0?5:0;
   }finally{try{Shared.DeleteOwnedDirectory(work);}catch(Exception ex){Emit(new {type="cleanup_warning",path=work,message=ex.Message},"临时文件清理失败："+work);}}
  }
  static Dictionary<string,object> RunWorker(string engine,string input,string output,string localCancel,int timeout,int bits,string colorSpace,int index,int total,string original,string format,int quality,string film,bool modern,AdaptiveParallelism scheduler){
   var psi=new ProcessStartInfo(modern?Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"s5raw-rfcex.exe"):System.Reflection.Assembly.GetExecutingAssembly().Location,Shared.Arguments("--worker",engine,input,output,localCancel,bits.ToString(),colorSpace,format,quality.ToString(),film));
   psi.UseShellExecute=false;psi.CreateNoWindow=true;psi.RedirectStandardOutput=true;psi.RedirectStandardError=true;psi.StandardOutputEncoding=Encoding.UTF8;psi.StandardErrorEncoding=Encoding.UTF8;psi.WorkingDirectory=engine;
   Dictionary<string,object> result=null;string error=null;object sync=new object();
   using(var p=new Process()){bool succeeded=false;try{p.StartInfo=psi;
    p.OutputDataReceived+=delegate(object s,DataReceivedEventArgs e){if(e.Data==null)return;try{
     var row=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(e.Data);string type=Convert.ToString(row["type"]);
     if(type=="progress"){int v=Convert.ToInt32(row["percent"]);Emit(new {type="progress",index=index,total=total,input=original,percent=v},"  "+v+"%");}
     else lock(sync){if(type=="worker_result")result=row;else if(type=="worker_error")error=Convert.ToString(row["message"]);}
    }catch(Exception ex){lock(sync)error="内核协议错误："+ex.Message;}};
    p.ErrorDataReceived+=delegate(object s,DataReceivedEventArgs e){if(!string.IsNullOrEmpty(e.Data))lock(sync)error=e.Data;};
    p.Start();scheduler.Track(p);p.BeginOutputReadLine();p.BeginErrorReadLine();var timer=Stopwatch.StartNew();long cancelAt=-1;
    while(!p.WaitForExit(100)){
     if(Cancelled()){
      if(cancelAt<0){File.WriteAllText(localCancel,"cancel");cancelAt=timer.ElapsedMilliseconds;}
      if(timer.ElapsedMilliseconds-cancelAt>2000){p.Kill();p.WaitForExit();break;}
     }
     if(timer.Elapsed.TotalSeconds>timeout){p.Kill();p.WaitForExit();throw new IOException("单张转换超时（"+timeout+" 秒）");}
    }
    p.WaitForExit();if(Cancelled())throw new OperationCanceledException();
    lock(sync){if(p.ExitCode!=0||result==null||Convert.ToInt32(result["code"])!=0)throw new IOException(error??("原厂内核失败，exit="+p.ExitCode+", native="+(result==null?"unknown":Convert.ToString(result["code"]))));succeeded=true;return result;}
    }finally{scheduler.Untrack(p,succeeded);}
   }
  }
  static void ValidateTiff(string file){using(var f=File.OpenRead(file)){byte[] b=new byte[4];if(f.Length<1024||f.Read(b,0,4)!=4||!((b[0]==73&&b[1]==73&&b[2]==42&&b[3]==0)||(b[0]==77&&b[1]==77&&b[2]==0&&b[3]==42)))throw new IOException("输出不是有效 TIFF");}}
 }
}

