using System;
using Microsoft.Win32;

namespace S5Raw {
 static class Preferences {
  const string Key=@"HKEY_CURRENT_USER\Software\S5Raw";
  internal static string Get(string name,string fallback=""){return Convert.ToString(Registry.GetValue(Key,name,fallback));}
  internal static void Set(string name,string value){Registry.SetValue(Key,name,value);}
 }
}
