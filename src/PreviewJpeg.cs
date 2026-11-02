using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace S5Raw {
 static class PreviewJpeg {
  internal static BitmapFrame Read(Stream source,BitmapCreateOptions options){
   long start=source.Position;
   try{return BitmapFrame.Create(source,options,BitmapCacheOption.OnLoad);}
   catch(ArgumentException){}catch(NotSupportedException){}catch(FileFormatException){}catch(COMException){}
   // Some real X-H2S/X-S20/X-T5 embedded JPEGs are readable by GDI+ but
   // rejected by WIC. Normalize an in-memory preview copy, retaining EXIF.
   // RAW development and exported image pixels never use this copy.
   source.Position=start;
   using(var image=Image.FromStream(source,false,true))using(var copy=new MemoryStream()){
    ImageCodecInfo jpeg=Array.Find(ImageCodecInfo.GetImageEncoders(),c=>c.FormatID==ImageFormat.Jpeg.Guid);
    using(var settings=new EncoderParameters(1)){
     settings.Param[0]=new EncoderParameter(Encoder.Quality,100L);
     image.Save(copy,jpeg,settings);
    }
    copy.Position=0;
    return BitmapFrame.Create(copy,options,BitmapCacheOption.OnLoad);
   }
  }
 }
}
