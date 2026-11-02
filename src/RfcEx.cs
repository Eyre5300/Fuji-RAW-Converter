using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace S5Raw {
 static class RfcEx {
  internal const string DllHash="afdac23b0107aed6e5454af7c810a9d4363ac4e3e99df1b14f9d5825cdf28ad8";
  internal static string EngineDirectory {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"engine","RFCEX");}}
  internal static readonly string[] Keys={"standard","provia","velvia","astia","classic-chrome","pro-neg-hi","pro-neg-std","classic-neg","eterna","bleach-bypass","nostalgic-neg","reala-ace","acros","acros-ye","acros-r","acros-g","mono","mono-ye","mono-r","mono-g","sepia"};
  internal static readonly string[] Labels={"默认 / 拍摄设置","PROVIA / 标准","Velvia / 鲜艳","ASTIA / 柔和","Classic Chrome","PRO Neg. Hi","PRO Neg. Std","Classic Neg.","ETERNA","ETERNA Bleach Bypass","Nostalgic Neg.","REALA ACE","ACROS（黑白）","ACROS + Ye（黑白）","ACROS + R（黑白）","ACROS + G（黑白）","黑白","黑白 + Ye","黑白 + R","黑白 + G","棕褐色"};
  internal static readonly int[] Modes={25,26,27,28,29,30,31,42,41,43,44,45,37,38,39,40,32,33,34,35,36};
  static readonly object gate=new object();static HashSet<string> models;
  internal static bool Supports(string model){
   lock(gate){if(models==null){string file=Path.Combine(EngineDirectory,"models.txt");models=new HashSet<string>(File.Exists(file)?File.ReadAllLines(file):new string[0],StringComparer.OrdinalIgnoreCase);}}
   return Environment.Is64BitOperatingSystem&&File.Exists(Path.Combine(EngineDirectory,"Library","SILKYPIX64.dll"))&&models.Contains(model);
  }
  // Menus for the pinned RFC EX build. These select vendor-native modes;
  // this application does not implement film LUTs or color transforms.
  static readonly Dictionary<string,string[]> cameraFilms=CreateCameraFilms();
  static Dictionary<string,string[]> CreateCameraFilms(){
   var result=new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase);
   string basic="standard,provia,velvia,astia,mono,sepia";
   string filters=basic+",mono-ye,mono-r,mono-g";
   string portrait=filters+",pro-neg-hi,pro-neg-std";
   string chrome=portrait+",classic-chrome";
   string acros=chrome+",acros,acros-ye,acros-r,acros-g";
   string cinema=acros+",eterna";
   string negative=cinema+",classic-neg";
   string bleach=negative+",bleach-bypass";
   string nostalgic=bleach+",nostalgic-neg";
   AddFilms(result,"X-A1|X-M1",basic);
   AddFilms(result,"X-A2|X-A10|X-A20",basic+",classic-chrome");
   AddFilms(result,"FinePix X100|X10|XF1|X-S1|XQ1",filters);
   AddFilms(result,"XQ2",filters+",classic-chrome");
   AddFilms(result,"X-Pro1|X-E1|X100S|X20",portrait);
   AddFilms(result,"X-T1|X-T10|X-E2|X-E2S|X100T|X30|X70|X-A3|X-A5|X-A7|X-T100|X-T200|XF10",chrome);
   AddFilms(result,"X-Pro2|X-T2|X-T20|X-E3|X100F",acros);
   AddFilms(result,"X-H1|X-T3|X-T30",cinema);
   AddFilms(result,"X-Pro3|X100V|GFX 50S|GFX 50R",negative);
   AddFilms(result,"X-T4|X-S10|X-E4|X-T30 II|GFX 100",bleach);
   AddFilms(result,"GFX100S|GFX50S II",nostalgic);
   AddFilms(result,"X-H2|X-H2S|X-T5|X-S20|X-T50|X-M5|X-E5|X100VI|GFX100 II|GFX100S II|GFX100RF|X-T30 III",nostalgic+",reala-ace");
   return result;
  }
  static void AddFilms(Dictionary<string,string[]> result,string names,string keys){
   var allowed=new HashSet<string>(keys.Split(','),StringComparer.Ordinal);
   string[] ordered=Keys.Where(allowed.Contains).ToArray();
   foreach(string name in names.Split('|'))result.Add(name,ordered);
  }
  internal static string[] Films(string model){string[] keys;return model!=null&&cameraFilms.TryGetValue(model,out keys)?(string[])keys.Clone():new[]{"standard"};}
  internal static string Label(string film){int i=Array.IndexOf(Keys,film);return i>=0?Labels[i]:film;}
  internal static void Verify(string root){
   string dll=Path.Combine(root,"Library","SILKYPIX64.dll");
   using(var f=File.OpenRead(dll))using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").ToLowerInvariant()!=DllHash)throw new IOException("RFC EX 内核版本或校验不匹配，请恢复随程序提供的内核。");
   foreach(string file in new[]{"SPD50.ini","Library/DefaultParameters.spd","ICCProfiles/ISL-sRGB.icc","ICCProfiles/ISL-ARGB1998.icc"})if(!File.Exists(Path.Combine(root,file)))throw new IOException("缺少 RFC EX 文件："+file);
   if(!Directory.Exists(Path.Combine(root,"ColorModeData")))throw new IOException("缺少 RFC EX ColorModeData");
  }
 }
}
