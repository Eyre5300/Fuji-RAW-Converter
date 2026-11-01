using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace S5Raw {
 sealed class PhotoInfo {
  public string Camera="",Shot="";
  static object Query(BitmapMetadata metadata,string path){return metadata==null?null:metadata.GetQuery(path);}
  static double Rational(object value){
   if(value==null)return 0;
   ulong packed=Convert.ToUInt64(value);uint denominator=(uint)(packed>>32);
   return denominator==0?0:(double)(uint)packed/denominator;
  }
  internal static PhotoInfo Read(string path){
   var info=new PhotoInfo();
   try{using(var file=File.OpenRead(path)){
    Stream source=file;MemoryStream embedded=null;
    try{
     if(Path.GetExtension(path).Equals(".raf",StringComparison.OrdinalIgnoreCase)){
      var reader=new BinaryReader(file);byte[] header=reader.ReadBytes(108);
      Func<int,uint> be=p=>((uint)header[p]<<24)|((uint)header[p+1]<<16)|((uint)header[p+2]<<8)|header[p+3];
      file.Position=be(84);embedded=new MemoryStream(reader.ReadBytes((int)be(88)),false);source=embedded;
     }
     var decoder=BitmapDecoder.Create(source,BitmapCreateOptions.DelayCreation,BitmapCacheOption.None);
     var metadata=decoder.Frames[0].Metadata as BitmapMetadata;
     string prefix=decoder is JpegBitmapDecoder?"/app1/ifd/":"/ifd/";
     info.Camera=Convert.ToString(Query(metadata,prefix+"{ushort=272}")).Trim();
     string exif=prefix+"exif/";var parts=new List<string>();
     object iso=Query(metadata,exif+"{ushort=34855}");if(iso!=null)parts.Add("ISO "+iso);
     double aperture=Rational(Query(metadata,exif+"{ushort=33437}"));if(aperture>0)parts.Add("f/"+aperture.ToString("0.#"));
     double exposure=Rational(Query(metadata,exif+"{ushort=33434}"));if(exposure>0)parts.Add(exposure<1?"1/"+Math.Round(1/exposure)+" s":exposure.ToString("0.##")+" s");
     double focal=Rational(Query(metadata,exif+"{ushort=37386}"));if(focal>0)parts.Add(focal.ToString("0.#")+" mm");
     info.Shot=string.Join(" · ",parts.ToArray());
    }finally{if(embedded!=null)embedded.Dispose();}
   }}catch(IOException){}catch(NotSupportedException){}
   return info;
  }
  internal string Details(string dimensions){return dimensions+(string.IsNullOrEmpty(Camera)?"":" · "+Camera)+(string.IsNullOrEmpty(Shot)?"":"\n"+(L.English?Shot:Shot.Replace(" s"," 秒")));}
 }
}
