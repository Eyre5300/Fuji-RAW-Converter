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
  // Expose film overrides only for the camera whose native pipeline is validated.
  internal static string[] Films(string model){return model.Equals("X-T50",StringComparison.OrdinalIgnoreCase)?Keys:new[]{"standard"};}
  internal static string Label(string film){int i=Array.IndexOf(Keys,film);return i>=0?Labels[i]:film;}
  internal static void Verify(string root){
   string dll=Path.Combine(root,"Library","SILKYPIX64.dll");
   using(var f=File.OpenRead(dll))using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").ToLowerInvariant()!=DllHash)throw new IOException("RFC EX 内核版本或校验不匹配，请恢复随程序提供的内核。");
   foreach(string file in new[]{"SPD50.ini","Library/DefaultParameters.spd","ICCProfiles/ISL-sRGB.icc","ICCProfiles/ISL-ARGB1998.icc"})if(!File.Exists(Path.Combine(root,file)))throw new IOException("缺少 RFC EX 文件："+file);
   if(!Directory.Exists(Path.Combine(root,"ColorModeData")))throw new IOException("缺少 RFC EX ColorModeData");
  }
 }
}
