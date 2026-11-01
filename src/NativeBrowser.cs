using System;
using System.IO;
using System.Runtime.InteropServices;

namespace S5Raw {
 static class NativeBrowser {
  static int Main(string[] args){return Run(args);}
  [DllImport("kernel32",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibraryEx(string path,IntPtr reserved,uint flags);
  [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern bool SetDllDirectory(string path);
  [DllImport("kernel32",CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module,string name);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl,CharSet=CharSet.Ansi)]
  delegate int ReadImage([MarshalAs(UnmanagedType.LPStr)]string path,IntPtr pixels,ref int bytes);
  internal static int Run(string[] args){
   if(args.Length!=4)return 2;
   try {
    string root=args[1];SetDllDirectory(root);Directory.SetCurrentDirectory(root);
    var library=LoadLibraryEx(Path.Combine(root,"PLUGINS","PIR030RAF.dll"),IntPtr.Zero,8);
    if(library==IntPtr.Zero)throw new IOException("加载原厂浏览插件失败，Win32="+Marshal.GetLastWin32Error());
    var read=(ReadImage)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"ReadImage"),typeof(ReadImage));
    int bytes=0;if(read(args[2],IntPtr.Zero,ref bytes)!=1||bytes<40)throw new IOException("原厂浏览插件无法读取此 RAF");
    var pixels=Marshal.AllocHGlobal(bytes);
    try {
     if(read(args[2],pixels,ref bytes)!=1)throw new IOException("原厂浏览插件读取预览失败");
     byte[] dib=new byte[bytes];Marshal.Copy(pixels,dib,0,bytes);
     // PIR030RAF ReadImage returns a 24-bit DIB, already decoded from the in-camera preview.
     using(var target=new BinaryWriter(File.Create(args[3]))){target.Write((ushort)0x4d42);target.Write(bytes+14);target.Write(0);target.Write(54);target.Write(dib);}
    }finally{Marshal.FreeHGlobal(pixels);}
    return 0;
   }catch(Exception error){Console.Error.WriteLine(error.Message);return 5;}
  }
 }
}
