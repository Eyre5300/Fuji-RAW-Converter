using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace S5Raw {
 static class FilmSimulation {
  internal static readonly string[] Keys={"standard","f1","f1a","f1b","f1c","f2"};
  internal static readonly string[] Labels={"标准","F1","F1a","F1b","F1c","F2"};
  internal static string EngineDirectory {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"engine","RAFCNV");}}
  // HS-V3 uses the older F3/F4 identifiers internally for its F1/F2 film profiles.
  static string Profile(string film,string color){
   if(film=="standard")return color=="adobe-rgb"?"FUJIFILM Adobe RGB (1998)/0/":"sRGB IEC61966-2.1/0/";
   string mode=film=="f2"?"F4":"F3"+film.Substring(2);
   return mode+(color=="adobe-rgb"?" Adobe RGB/0/":" sRGB/0/");
  }
  static readonly Dictionary<string,string[]> cache=new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase);
  static string[] Profiles(string engine,string module){
   string file=Path.Combine(engine,Path.ChangeExtension(module,".FCC"));
   if(!File.Exists(file)&&module.StartsWith("FG",StringComparison.OrdinalIgnoreCase))file=Path.Combine(engine,"FE"+Path.ChangeExtension(module.Substring(2),".FCC"));
   lock(cache){
    string[] found;if(cache.TryGetValue(file,out found))return found;
    var names=new List<string>();
    if(File.Exists(file))using(var reader=new BinaryReader(File.OpenRead(file))){
     int count=reader.ReadInt32();
     if(count<0||4L+count*256L>reader.BaseStream.Length)throw new IOException("原厂胶片配置无效："+file);
     for(int i=0;i<count;i++)names.Add(Encoding.ASCII.GetString(reader.ReadBytes(256)).TrimEnd('\0'));
    }
    return cache[file]=names.ToArray();
   }
  }
  internal static string[] Available(string engine,string module,string color){
   var profiles=Profiles(engine,module);
   return Keys.Where(k=>k=="standard"||profiles.Contains(Profile(k,color))).ToArray();
  }
  internal static string Resolve(string engine,string module,string film,string color){
   string profile=Profile(film,color);
   if(film!="standard"&&!Profiles(engine,module).Contains(profile))throw new IOException("此照片的原厂内核不支持 "+film.ToUpperInvariant()+" / "+color+"："+module);
   return profile;
  }
 }
}
