using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace S5Raw {
 static class NativeWorker {
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibraryEx(string file,IntPtr h,uint flags);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern bool SetDllDirectory(string directory);
  [DllImport("kernel32.dll",CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr h,string name);
  [DllImport("kernel32.dll")] static extern uint SetErrorMode(uint mode);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate short Init(int reserved);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate short Exit();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate short Progress(int value);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl,CharSet=CharSet.Ansi)]
  delegate short Convert(int mode,[MarshalAs(UnmanagedType.LPStr)] string source,[MarshalAs(UnmanagedType.LPStr)] string directory,[Out] StringBuilder actualOutput,Progress callback);
  internal static int Run(string[] a) {
   if(a.Length!=10)return 2;
   SetErrorMode(0x0001|0x0002|0x8000);
   string engine=a[1], input=a[2], output=a[3], cancel=a[4];
   var modules=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
   int previous=-1;
   Progress cb=delegate(int value){
    try {
     int percent=Math.Max(0,Math.Min(100,value&65535));
     if(percent!=previous){Console.WriteLine(Shared.Json(new {type="progress",percent=percent}));previous=percent;}
     foreach(ProcessModule m in Process.GetCurrentProcess().Modules)
      if(m.ModuleName.StartsWith("FG",StringComparison.OrdinalIgnoreCase)||m.ModuleName.StartsWith("FE",StringComparison.OrdinalIgnoreCase)||m.ModuleName.Equals("RAFCNVX.dll",StringComparison.OrdinalIgnoreCase))modules[m.ModuleName]=m.FileName;
     return File.Exists(cancel)?(short)-1:(short)0;
    } catch {return -1;}
   };
   try {
    SetDllDirectory(engine);
    if(a[5]=="16"||a[9]!="standard") {
     string target16=Native16.Run(engine,input,output,a[6],a[9],int.Parse(a[5]),cb);
     TiffOrientation.Normalize(target16);
     if(a[7]=="jpeg"){
      if(File.Exists(cancel))throw new OperationCanceledException();
      string jpeg=Path.Combine(output,"full.jpg");JpegExport.Write(target16,input,jpeg,int.Parse(a[8]),a[9]);target16=jpeg;
     }
     Console.WriteLine(Shared.Json(new {type="worker_result",code=0,output=target16,modules=modules}));
     return 0;
    }
    var lib=LoadLibraryEx(Path.Combine(engine,"RAFCNVX.dll"),IntPtr.Zero,8);
    if(lib==IntPtr.Zero)throw new IOException("加载原厂内核失败，Win32="+Marshal.GetLastWin32Error());
    var init=(Init)Marshal.GetDelegateForFunctionPointer(GetProcAddress(lib,"Init"),typeof(Init));
    var run=(Convert)Marshal.GetDelegateForFunctionPointer(GetProcAddress(lib,"DoProcess"),typeof(Convert));
    var exit=(Exit)Marshal.GetDelegateForFunctionPointer(GetProcAddress(lib,"Exit"),typeof(Exit));
    int start=init(0);if(start!=0)throw new IOException("原厂初始化失败，code="+start);
    short rc;var target=new StringBuilder(1040);
    try { rc=run(0,input,output.TrimEnd('\\')+"\\",target,cb);GC.KeepAlive(cb); } finally {exit();}
    if(rc==0)TiffOrientation.Normalize(target.ToString());
    Console.WriteLine(Shared.Json(new {type="worker_result",code=rc,output=target.ToString(),modules=modules}));
    return rc==0?0:5;
   } catch(Exception e){Console.WriteLine(Shared.Json(new {type="worker_error",message=e.Message}));return 5;}
  }
 }
}
