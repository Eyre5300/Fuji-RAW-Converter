using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace S5Raw {
 // Private ABI for the exact SHA-256 pinned in RfcEx. Never use these RVAs with
 // another DLL build. One x64 process per photo contains native faults/leaks.
 static class RfcExWorker {
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr LoadLibraryEx(string path,IntPtr reserved,uint flags);
  [DllImport("kernel32.dll",CharSet=CharSet.Ansi)]static extern IntPtr GetProcAddress(IntPtr h,string name);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern bool SetDllDirectory(string path);
  [DllImport("kernel32.dll")]static extern uint SetErrorMode(uint mode);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Init(IntPtr path);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate IntPtr Unary(IntPtr obj);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Delete(IntPtr obj);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Finish();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate IntPtr Ctor(IntPtr obj,int flag);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate short SetInt(IntPtr obj,int value);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Assign(IntPtr obj,IntPtr text);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate IntPtr Cast(IntPtr src,int delta,IntPtr sourceType,IntPtr targetType,int flag);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate short DevelopParam(IntPtr fd,int kind,IntPtr settings,IntPtr result,int flag);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate short SetParam(IntPtr fd,IntPtr param,int a,int b);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate short WriteImage(IntPtr fd,IntPtr path,IntPtr settings,IntPtr callback,IntPtr progress,IntPtr crop,int flag,IntPtr subSettings,int format,IntPtr other,int n,int flag2);
  static IntPtr h;
  static T Fn<T>(int rva){return (T)(object)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(h,rva),typeof(T));}
  static T Export<T>(string name){IntPtr p=GetProcAddress(h,name);if(p==IntPtr.Zero)throw new IOException("RFC EX 导出缺失："+name);return (T)(object)Marshal.GetDelegateForFunctionPointer(p,typeof(T));}
  static IntPtr Mem(int bytes){IntPtr p=Marshal.AllocHGlobal(bytes);Marshal.Copy(new byte[bytes],0,p,bytes);return p;}
  static IntPtr Utf8(string value){byte[] b=Encoding.UTF8.GetBytes(value+"\0");IntPtr p=Marshal.AllocHGlobal(b.Length);Marshal.Copy(b,0,p,b.Length);return p;}
  static void AssignPath(IntPtr p,string value){IntPtr text=Utf8(value);try{Fn<Assign>(0x2babb0)(p,text);}finally{Marshal.FreeHGlobal(text);}}
  static void Check(short code,string operation){if(code!=0)throw new IOException(operation+"失败，RFC EX code="+code);}
  static void Progress(int value){Console.WriteLine(Shared.Json(new {type="progress",percent=value}));}
  [STAThread] static int Main(string[] a){
   Console.OutputEncoding=new UTF8Encoding(false);SetErrorMode(0x8003);
   if(a.Length!=10||a[0]!="--worker"||IntPtr.Size!=8)return 2;
   // Preserve the parent's short-path spelling for its output containment check.
   string root=Path.GetFullPath(a[1]),input=Path.GetFullPath(a[2]),output=a[3];
   IntPtr obj=IntPtr.Zero;bool initialized=false;
   try{
    RfcEx.Verify(root);var meta=RafHeader.Read(input);string model=(string)meta["model"];
    if(Array.IndexOf(RfcEx.Films(model),a[9])<0)throw new IOException("此机型没有所选胶片模式："+model+" / "+a[9]);
    if(File.Exists(a[4]))return 130;
    SetDllDirectory(Path.Combine(root,"Library"));h=LoadLibraryEx(Path.Combine(root,"Library","SILKYPIX64.dll"),IntPtr.Zero,8);
    if(h==IntPtr.Zero)throw new IOException("加载 RFC EX 失败，Win32="+Marshal.GetLastWin32Error());
    string work=Path.GetDirectoryName(output),cfg=Path.Combine(work,"rfcex.ini");
    string ini=File.ReadAllText(Path.Combine(root,"SPD50.ini"))+"\r\n[SILKYPIX]\r\nINSTALLFOLDER="+root+"\r\nDLLFOLDER="+Path.Combine(root,"Library")+"\r\nLOCALSETTINGFOLDER="+work+"\r\nTEMPFOLDER="+work+"\r\nISLSETTINGFOLDER="+work+"\r\n";
    File.WriteAllText(cfg,ini,new UTF8Encoding(false));IntPtr text=Utf8(cfg);int status;try{status=Export<Init>("Initialize")(text);}finally{Marshal.FreeHGlobal(text);}
    if(status!=1&&status!=2)throw new IOException("RFC EX 初始化失败："+status);initialized=true;Progress(5);
    text=Utf8(input);try{obj=Export<Unary>("CreateImageFile")(text);}finally{Marshal.FreeHGlobal(text);}
    if(obj==IntPtr.Zero)throw new IOException("RFC EX 无法读取此 RAF："+model);
    IntPtr fd=Fn<Cast>(0xfd31cc)(Marshal.ReadIntPtr(obj,0x80),0,IntPtr.Add(h,0x199cd98),IntPtr.Add(h,0x19a15f8),0);
    if(fd==IntPtr.Zero)throw new IOException("RFC EX 未返回 RAW 开发对象");
    // Materialize camera defaults, then use the official setters for overrides.
    // Text-sidecar import alone is insufficient: it can silently leave defaults.
    if(a[9]!="standard"){
     IntPtr holder=Mem(0x78);Fn<Unary>(0x030910)(holder);
     Check(Fn<DevelopParam>(0xce24b0)(fd,0,IntPtr.Zero,holder,1),"读取相机参数");
     IntPtr param=Marshal.ReadIntPtr(holder,0x70);if(param==IntPtr.Zero)throw new IOException("RFC EX 参数对象为空");
     Check(Fn<SetInt>(0x9d3870)(param,6),"设置相机胶片色彩类别");
     Check(Fn<SetInt>(0x9d35c0)(param,RfcEx.Modes[Array.IndexOf(RfcEx.Keys,a[9])]),"设置胶片参数");
     // SetDevelopParam returns 1 when values already match; negative is failure.
     short applied=Fn<SetParam>(0xa64810)(fd,param,0,0);if(applied<0)Check(applied,"设置胶片模拟");
     // The reference remains alive through development; the process owns its lifetime.
    }
    Progress(20);IntPtr settings=Mem(0xc000),path=Mem(0x1000);
    Fn<Ctor>(0x9e8680)(settings,1);Fn<Unary>(0x2a5480)(path);
    string tiff=Path.Combine(output,"native.tif");AssignPath(path,tiff);
    try{
     int type=a[5]=="16"?2:1;Marshal.WriteInt32(settings,0x1408,type);Marshal.WriteInt32(settings,0x1410,type);
     Marshal.WriteByte(settings,0x1540,1);
     // SaveColorSpace: 0 = development parameter, 1 = sRGB, 2 = Adobe RGB.
     Marshal.WriteInt32(settings,0x22dc,a[6]=="adobe-rgb"?2:1);
     AssignPath(IntPtr.Add(settings,0x1548),Path.Combine(root,"ICCProfiles","ISL-sRGB.icc"));
     AssignPath(IntPtr.Add(settings,0x22e0),Path.Combine(root,"ICCProfiles",a[6]=="adobe-rgb"?"ISL-ARGB1998.icc":"ISL-sRGB.icc"));
     Check(Fn<WriteImage>(0xa66200)(fd,path,settings,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,IntPtr.Add(settings,0x7df0),0,IntPtr.Zero,-1,1),"渲染 TIFF");
    }finally{Fn<Unary>(0x9eb0d0)(settings);Fn<Unary>(0x2a7180)(path);Marshal.FreeHGlobal(settings);Marshal.FreeHGlobal(path);}
    if(File.Exists(a[4]))return 130;Progress(90);
    TiffOrientation.ValidateRgb(tiff,int.Parse(a[5]),Path.Combine(root,"ICCProfiles",a[6]=="adobe-rgb"?"ISL-ARGB1998.icc":"ISL-sRGB.icc"));
    TiffOrientation.Normalize(tiff);
    string target=tiff;if(a[7]=="jpeg"){target=Path.Combine(output,"full.jpg");JpegExport.Write(tiff,input,target,int.Parse(a[8]),a[9]);}
    Progress(100);Console.WriteLine(Shared.Json(new {type="worker_result",code=0,output=target,modules=new[]{"RFCEX/SILKYPIX64.dll"}}));return 0;
   }catch(Exception e){Console.WriteLine(Shared.Json(new {type="worker_error",message=e.Message}));return 5;}
   finally{if(obj!=IntPtr.Zero)Export<Delete>("DeleteImageFile")(obj);if(initialized)Export<Finish>("Finalize")();}
  }
 }
}
