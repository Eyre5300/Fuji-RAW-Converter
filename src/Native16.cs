using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace S5Raw {
 static class Native16 {
  [DllImport("kernel32",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibraryEx(string f,IntPtr h,uint flags);
  [DllImport("kernel32",CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr h,string s);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate short One(IntPtr a);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate short None();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate short Two(IntPtr a,IntPtr b);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate short Render(IntPtr raw,IntPtr param,IntPtr output,NativeWorker.Progress cb);
  static IntPtr library;
  static T Fn<T>(string name){return (T)(object)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,name),typeof(T));}
  static void Check(string operation,short code){if(code!=0)throw new IOException("原厂转换内核 "+operation+" 失败，code="+code);}
  internal static string Run(string engine,string input,string output,string color,string film,int bits,NativeWorker.Progress cb){
   var info=RafHeader.Read(input,engine);
   if(!(bool)info["supported"])throw new IOException("当前原厂内核不支持："+info["model"]+"（"+info["processorCode"]+"）");
   string module=(string)info["nativeModule"];
   string selectedProfile=FilmSimulation.Resolve(engine,module,film,color);
   library=LoadLibraryEx(Path.Combine(engine,module),IntPtr.Zero,8);
   if(library==IntPtr.Zero)throw new IOException("加载原厂转换内核 "+module+" 失败，Win32="+Marshal.GetLastWin32Error());
   var allocations=new List<IntPtr>();
   Func<int,IntPtr> alloc=delegate(int length){var p=Marshal.AllocHGlobal(length);allocations.Add(p);Marshal.Copy(new byte[length],0,p,length);return p;};
   Func<string,IntPtr> str=delegate(string text){var p=Marshal.StringToHGlobalAnsi(text);allocations.Add(p);return p;};
   bool initialized=false,profile=false,opened=false;IntPtr descriptor=IntPtr.Zero;
   try {
    Check("初始化",Fn<One>("FFRafInit")(IntPtr.Zero));initialized=true;
    Check("色彩空间",Fn<One>("FFRafDescent")(str(selectedProfile)));profile=true;
    var size=alloc(4);Check("参数大小",Fn<One>("FFRafBlenny")(size));
    var parameters=alloc(Marshal.ReadInt32(size));Check("原厂默认参数",Fn<One>("FFRafMandarin")(parameters));
    byte[] bytes=File.ReadAllBytes(input);var raw=alloc(bytes.Length);Marshal.Copy(bytes,0,raw,bytes.Length);
    descriptor=alloc(32);Marshal.WriteByte(descriptor,0,1);Marshal.WriteInt32(descriptor,4,bytes.Length);Marshal.WriteIntPtr(descriptor,8,raw);
    Check("打开 RAF",Fn<Two>("FFRafKudagombe")(raw,descriptor));opened=true;
    // Reconstructed from RAFCNVX's native output descriptor. Low format bit selects 16-bit TIFF.
    var options=alloc(0x128);Marshal.WriteIntPtr(options,0,str("output.tif"));Marshal.WriteIntPtr(options,4,str(output.TrimEnd('\\')+"\\"));
    var software=Encoding.ASCII.GetBytes("S5Raw "+Shared.Version+" / FUJIFILM native "+bits+"-bit / "+film+"\0");Marshal.Copy(software,0,IntPtr.Add(options,8),software.Length);
    Marshal.WriteInt32(options,0x108,1);Marshal.WriteInt32(options,0x10c,72);Marshal.WriteInt32(options,0x110,1);Marshal.WriteInt32(options,0x114,72);Marshal.WriteInt32(options,0x120,bits==16?0x40000001:0x40000000);
    Check("转换",Fn<Render>("FFRafMantarayEx")(descriptor,parameters,options,cb));GC.KeepAlive(cb);
    string target=Path.Combine(output,"output.tif");
    EmbedProfile(target,Path.Combine(engine,color=="adobe-rgb"?"Adobe.ICC":"sRGB.ICC"));
    return target;
   } finally {
    if(opened)Fn<One>("FFRafOyabiccha")(descriptor);
    if(profile)Fn<None>("FFRafAscent")();
    if(initialized)Fn<One>("FFRafExit")(IntPtr.Zero);
    foreach(var p in allocations)Marshal.FreeHGlobal(p);
   }
  }
  // Append an ICC tag and a replacement first IFD. All native pixels, EXIF and strip offsets remain intact.
  static void EmbedProfile(string file,string profile){
   byte[] icc=File.ReadAllBytes(profile);
   using(var stream=new FileStream(file,FileMode.Open,FileAccess.ReadWrite)){
    var reader=new BinaryReader(stream);var writer=new BinaryWriter(stream);
    if(reader.ReadUInt16()!=0x4949||reader.ReadUInt16()!=42)throw new IOException("无法给原厂 TIFF 写入色彩配置");
    stream.Position=reader.ReadUInt32();int count=reader.ReadUInt16();var entries=new List<byte[]>();
    for(int i=0;i<count;i++){var entry=reader.ReadBytes(12);if(BitConverter.ToUInt16(entry,0)!=34675)entries.Add(entry);}
    uint next=reader.ReadUInt32();stream.Position=stream.Length;if((stream.Position&1)!=0)writer.Write((byte)0);
    uint profileOffset=(uint)stream.Position;writer.Write(icc);if((stream.Position&1)!=0)writer.Write((byte)0);
    var tag=new byte[12];Array.Copy(BitConverter.GetBytes((ushort)34675),0,tag,0,2);Array.Copy(BitConverter.GetBytes((ushort)7),0,tag,2,2);Array.Copy(BitConverter.GetBytes(icc.Length),0,tag,4,4);Array.Copy(BitConverter.GetBytes(profileOffset),0,tag,8,4);entries.Add(tag);
    entries.Sort(delegate(byte[] a,byte[] b){return BitConverter.ToUInt16(a,0).CompareTo(BitConverter.ToUInt16(b,0));});
    uint directory=(uint)stream.Position;writer.Write((ushort)entries.Count);foreach(var entry in entries)writer.Write(entry);writer.Write(next);stream.Position=4;writer.Write(directory);
   }
  }
 }
}
