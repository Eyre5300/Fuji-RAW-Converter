using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace S5Raw {
 static class Launcher {
  [STAThread] static int Main(string[] args){
   string executable=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data","Fuji-RAW-Converter.exe");
   try {Process.Start(new ProcessStartInfo(executable,Shared.Arguments(args)){UseShellExecute=false});return 0;}
   catch(Exception error){MessageBox.Show("无法启动 Fuji RAW Converter："+error.Message,"Fuji RAW Converter",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
  }
 }
}
