using System;
using System.IO;
using System.Collections.Generic;

namespace S5Raw {
 // Fuji emits uncompressed RGB strips, with the rotation on the main IFD only.
 // Rotate the integer samples and its thumbnail in place; keep ICC/EXIF offsets intact.
 static class TiffOrientation {
  sealed class Tag {public long Position;public ushort Type;public uint Count,Value;}
  static Dictionary<ushort,Tag> ReadDirectory(BinaryReader reader,uint offset,out uint next){
   reader.BaseStream.Position=offset;int count=reader.ReadUInt16();var tags=new Dictionary<ushort,Tag>();
   for(int i=0;i<count;i++){
    long position=reader.BaseStream.Position;ushort id=reader.ReadUInt16();
    tags.Add(id,new Tag{Position=position,Type=reader.ReadUInt16(),Count=reader.ReadUInt32(),Value=reader.ReadUInt32()});
   }
   next=reader.ReadUInt32();return tags;
  }
  static uint[] Values(BinaryReader reader,Tag tag){
   reader.BaseStream.Position=tag.Count*(tag.Type==3?2:4)<=4?tag.Position+8:tag.Value;
   var values=new uint[tag.Count];for(int i=0;i<values.Length;i++)values[i]=tag.Type==3?reader.ReadUInt16():reader.ReadUInt32();return values;
  }
  static void Set(BinaryWriter writer,Tag tag,uint value,ushort type=4){
   writer.BaseStream.Position=tag.Position+2;writer.Write(type);writer.Write((uint)1);writer.Write(value);
  }
  internal static void ValidateRgb(string path,int expectedBits,string profile){
   using(var stream=File.OpenRead(path))using(var reader=new BinaryReader(stream)){
    if(reader.ReadUInt16()!=0x4949||reader.ReadUInt16()!=42)throw new IOException("RFC EX 未产生有效 TIFF");
    uint next;var tags=ReadDirectory(reader,reader.ReadUInt32(),out next);
    uint[] bits=Values(reader,tags[258]);
    if(bits.Length!=3||bits[0]!=expectedBits||bits[1]!=expectedBits||bits[2]!=expectedBits||Values(reader,tags[277])[0]!=3||Values(reader,tags[262])[0]!=2)
     throw new IOException("RFC EX TIFF 实际位深或颜色通道与所选设置不符");
    if(Values(reader,tags[256])[0]==0||Values(reader,tags[257])[0]==0)throw new IOException("RFC EX TIFF 尺寸无效");
    if(!tags.ContainsKey(34675))throw new IOException("RFC EX TIFF 缺少 ICC 色彩配置");
    byte[] expected=File.ReadAllBytes(profile);Tag icc=tags[34675];
    if(icc.Count!=expected.Length)throw new IOException("RFC EX TIFF 色彩空间与所选设置不符");
    stream.Position=icc.Value;byte[] actual=reader.ReadBytes(expected.Length);
    if(actual.Length!=expected.Length)throw new IOException("RFC EX TIFF ICC 不完整");
    for(int i=0;i<expected.Length;i++)if(actual[i]!=expected[i])throw new IOException("RFC EX TIFF 色彩空间与所选设置不符");
   }
  }
  internal static void Normalize(string path){
   using(var stream=new FileStream(path,FileMode.Open,FileAccess.ReadWrite)){
    var reader=new BinaryReader(stream);var writer=new BinaryWriter(stream);
    if(reader.ReadUInt16()!=0x4949||reader.ReadUInt16()!=42)throw new IOException("原厂 TIFF 格式无法校正方向");
    uint next;var tags=ReadDirectory(reader,reader.ReadUInt32(),out next);
    int orientation=tags.ContainsKey(274)?(int)Values(reader,tags[274])[0]:1;
    if(orientation<=1||orientation>8)return;
    while(true){
     Rotate(reader,writer,tags,orientation);
     if(next==0)break;
     tags=ReadDirectory(reader,next,out next);
    }
   }
  }
  static void Rotate(BinaryReader reader,BinaryWriter writer,Dictionary<ushort,Tag> tags,int orientation){
   int width=(int)Values(reader,tags[256])[0],height=(int)Values(reader,tags[257])[0];
   uint[] bits=Values(reader,tags[258]),offsets=Values(reader,tags[273]),counts=Values(reader,tags[279]);
   if(Values(reader,tags[259])[0]!=1||Values(reader,tags[262])[0]!=2||Values(reader,tags[277])[0]!=3||
      (tags.ContainsKey(284)&&Values(reader,tags[284])[0]!=1)||bits.Length!=3||
      (bits[0]!=8&&bits[0]!=16)||bits[1]!=bits[0]||bits[2]!=bits[0])
    throw new IOException("原厂 TIFF 像素格式无法校正方向");
   int pixelBytes=(int)bits[0]/8*3,length=checked(width*height*pixelBytes);long size=0;
   for(int i=0;i<offsets.Length;i++){
    if(offsets[i]!=offsets[0]+size)throw new IOException("原厂 TIFF 像素条带不连续");
    size+=counts[i];
   }
   if(size!=length)throw new IOException("原厂 TIFF 像素长度不匹配");
   reader.BaseStream.Position=offsets[0];byte[] source=reader.ReadBytes(length),rotated=new byte[length];
   int targetWidth=orientation>=5?height:width,targetHeight=orientation>=5?width:height;
   for(int y=0;y<height;y++)for(int x=0;x<width;x++){
    int dx=x,dy=y;
    switch(orientation){
     case 2:dx=width-1-x;break;
     case 3:dx=width-1-x;dy=height-1-y;break;
     case 4:dy=height-1-y;break;
     case 5:dx=y;dy=x;break;
     case 6:dx=height-1-y;dy=x;break;
     case 7:dx=height-1-y;dy=width-1-x;break;
     case 8:dx=y;dy=width-1-x;break;
    }
    int from=(y*width+x)*pixelBytes,to=(dy*targetWidth+dx)*pixelBytes;
    for(int b=0;b<pixelBytes;b++)rotated[to+b]=source[from+b];
   }
   writer.BaseStream.Position=offsets[0];writer.Write(rotated);
   Set(writer,tags[256],(uint)targetWidth);Set(writer,tags[257],(uint)targetHeight);
   Set(writer,tags[273],offsets[0]);Set(writer,tags[278],(uint)targetHeight);Set(writer,tags[279],(uint)length);
   if(tags.ContainsKey(274))Set(writer,tags[274],1,3);
   if(tags.ContainsKey(34665)){
    uint unused;var exif=ReadDirectory(reader,tags[34665].Value,out unused);
    if(exif.ContainsKey(40962))Set(writer,exif[40962],(uint)targetWidth);
    if(exif.ContainsKey(40963))Set(writer,exif[40963],(uint)targetHeight);
   }
  }
 }
}
