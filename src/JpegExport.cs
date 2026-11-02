using System;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace S5Raw {
 static class JpegExport {
  static uint BE(byte[] b,int p){return ((uint)b[p]<<24)|((uint)b[p+1]<<16)|((uint)b[p+2]<<8)|b[p+3];}
  internal static void Write(string tiff,string raf,string output,int quality,string film){
   // Rendered TIFF pixels, never the RAF's small preview, supply the JPEG image.
   using(var source=File.OpenRead(tiff)){
    var frame=BitmapFrame.Create(source,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
    var rgb=new FormatConvertedBitmap(frame,PixelFormats.Bgr24,null,0);
    BitmapMetadata metadata=new BitmapMetadata("jpg");
    using(var raw=File.OpenRead(raf)){
     var reader=new BinaryReader(raw);byte[] header=reader.ReadBytes(108);raw.Position=BE(header,84);
     using(var jpeg=new MemoryStream(reader.ReadBytes(checked((int)BE(header,88))),false)){
      var preview=PreviewJpeg.Read(jpeg,BitmapCreateOptions.None);
      if(preview.Metadata is BitmapMetadata)metadata=(BitmapMetadata)preview.Metadata.Clone();
     }
     metadata.SetQuery("/app1/ifd/{ushort=271}","FUJIFILM");
     metadata.SetQuery("/app1/ifd/{ushort=272}",Encoding.ASCII.GetString(header,28,32).Trim('\0',' '));
    }
    metadata.SetQuery("/app1/ifd/{ushort=305}","S5Raw "+Shared.Version+" / FUJIFILM native / "+film);
    metadata.SetQuery("/app1/ifd/{ushort=274}",(ushort)1);
    metadata.SetQuery("/app1/ifd/exif/{ushort=40961}",(ushort)1);
    metadata.SetQuery("/app1/ifd/exif/{ushort=40962}",(uint)frame.PixelWidth);
    metadata.SetQuery("/app1/ifd/exif/{ushort=40963}",(uint)frame.PixelHeight);
    var encoder=new JpegBitmapEncoder{QualityLevel=quality};
    encoder.Frames.Add(BitmapFrame.Create(rgb,null,metadata,frame.ColorContexts));
    using(var target=File.Create(output))encoder.Save(target);
   }
  }
}
}
