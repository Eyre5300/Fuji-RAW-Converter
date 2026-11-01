using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace S5Raw {
 static class PhotoFiles {
  // Exact stems and the converter's numeric suffixes only, within the corresponding folder.
  public static string Match(string raw,string relative,string outputFolder,string known){return MatchAll(raw,relative,outputFolder,known).FirstOrDefault();}
  public static List<string> MatchAll(string raw,string relative,string outputFolder,string known,Dictionary<string,string[]> catalog=null){
   var results=new List<string>();if(known!=null&&File.Exists(known))results.Add(known);
   string source=Path.GetDirectoryName(raw),stem=Path.GetFileNameWithoutExtension(raw);
   var folders=new List<string>();
   if(!string.IsNullOrWhiteSpace(outputFolder)){
    folders.Add(Path.GetDirectoryName(Path.Combine(outputFolder,relative)));folders.Add(outputFolder);
    folders.Add(Path.Combine(outputFolder,new DirectoryInfo(source).Name));
    folders.Add(Path.GetDirectoryName(Path.Combine(outputFolder,raw.Substring(Path.GetPathRoot(raw).Length))));
   }
   folders.Add(source);
   string parent=Path.GetDirectoryName(source);
   if(parent!=null)folders.Add(Path.Combine(parent,new DirectoryInfo(parent).Name,new DirectoryInfo(source).Name));
   folders.Add(Path.Combine(source,"S5Raw-输出"));folders.Add(Path.Combine(source,"S5Raw-TIFF"));
   foreach(string folder in folders.Distinct(StringComparer.OrdinalIgnoreCase)){
    if(!Directory.Exists(folder))continue;
    string[] files;if(catalog==null||!catalog.TryGetValue(folder,out files)){files=Directory.GetFiles(folder);if(catalog!=null)catalog[folder]=files;}
    var matches=files.Where(p=>new[]{".tif",".tiff",".jpg",".jpeg"}.Contains(Path.GetExtension(p).ToLowerInvariant()))
     .Where(p=>MatchesStem(Path.GetFileNameWithoutExtension(p),stem));
    // TIFF takes priority over the camera's same-name JPEG; newest result wins within a format.
    var ordered=matches.OrderBy(p=>Path.GetExtension(p).StartsWith(".ti",StringComparison.OrdinalIgnoreCase)?0:1).ThenByDescending(File.GetLastWriteTimeUtc);
    results.AddRange(ordered);
   }
   return results.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
  }
  static bool MatchesStem(string candidate,string stem){int number;return candidate.Equals(stem,StringComparison.OrdinalIgnoreCase)||(candidate.StartsWith(stem+"_",StringComparison.OrdinalIgnoreCase)&&int.TryParse(candidate.Substring(stem.Length+1),out number)&&number>0);}
 }
}
