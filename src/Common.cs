using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;

namespace S5Raw {
 public static class Shared {
  public const string Version = "1.8.0";
  public static string Json(object value) { return new JavaScriptSerializer().Serialize(value); }
  public static string Quote(string value) {
   var b=new StringBuilder("\"");int slashes=0;
   foreach(char c in value) { if(c=='\\'){slashes++;continue;} if(c=='\"'){b.Append('\\',slashes*2+1);b.Append(c);}else{b.Append('\\',slashes);b.Append(c);}slashes=0; }
   b.Append('\\',slashes*2);return b.Append('"').ToString();
  }
  public static string Arguments(params string[] values) { var a=new List<string>();foreach(var v in values)a.Add(Quote(v));return string.Join(" ",a.ToArray()); }
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern uint GetShortPathName(string path,StringBuilder result,uint size);
  public static string NativePath(string path) {
   var b=new StringBuilder(1024);uint n=GetShortPathName(path,b,1024);
   string p=n>0&&n<1024?b.ToString():path;
   if(Encoding.Default.GetString(Encoding.Default.GetBytes(p))!=p||Encoding.Default.GetByteCount(p)>200)
    throw new IOException("原厂内核无法访问此临时路径。请用 --temp-dir 指定较短的本地路径（例如 C:\\S5Temp）。");
   return p;
  }
  public static void DeleteOwnedDirectory(string path) {
   // Only callers' randomly named, newly created private job directories are accepted.
   string full=Path.GetFullPath(path);
   if(!Path.GetFileName(full).StartsWith("s5raw-",StringComparison.Ordinal))throw new IOException("Unsafe cleanup path");
   if(Directory.Exists(full))Directory.Delete(full,true);
  }
 }
 static class RafHeader {
  // Match RAFCNVX: the six decimal IOP digits at RAF offset 22 select FG%06X.dll.
  public static string EngineDirectory {get{return Path.Combine(Path.GetDirectoryName(typeof(Shared).Assembly.Location),"engine","RAFCNVX");}}
  static uint BE(byte[] b,int p){return ((uint)b[p]<<24)|((uint)b[p+1]<<16)|((uint)b[p+2]<<8)|b[p+3];}
  public static Dictionary<string,object> Read(string file,string engine=null){
   using(var stream=File.OpenRead(file)){
    byte[] header=new BinaryReader(stream).ReadBytes(108);
    if(header.Length!=108||Encoding.ASCII.GetString(header,0,16)!="FUJIFILMCCD-RAW ")throw new IOException("不是有效的富士 RAF 文件");
    string model=Encoding.ASCII.GetString(header,28,32).Trim('\0',' ');uint offset=BE(header,84),length=BE(header,88);
    string code=Encoding.ASCII.GetString(header,20,8),module=null;
    int processor;
    if((code.StartsWith("FF")||code.StartsWith("FA"))&&int.TryParse(code.Substring(2),System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out processor))module="FG"+processor.ToString("X6")+".dll";
    if(offset<108||length<4||(long)offset+length>stream.Length)throw new IOException("RAF 预览范围无效");
    stream.Position=offset;if(stream.ReadByte()!=255||stream.ReadByte()!=216)throw new IOException("RAF 预览数据无效");
    bool legacy=module!=null&&File.Exists(Path.Combine(engine??EngineDirectory,module));bool modern=!legacy&&RfcEx.Supports(model);
    return new Dictionary<string,object>{{"input",file},{"model",model},{"processorCode",code},{"nativeModule",module},{"bytes",stream.Length},{"previewOffset",offset},{"previewBytes",length},{"supported",legacy||modern},{"backend",legacy?"rafcnv":modern?"rfcex":"unsupported"},{"legacySupported",legacy}};
   }
  }
  public static bool Accept(string file,out string reason){
   try{var info=Read(file);if((bool)info["supported"]){reason=null;return true;}reason="当前原厂内核不支持："+info["model"]+"（"+info["processorCode"]+"）";}
   catch(IOException error){reason=error.Message;}
   catch(UnauthorizedAccessException error){reason=error.Message;}
   return false;
  }
 }
}
