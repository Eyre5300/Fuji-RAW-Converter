using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color=System.Drawing.Color;
using PixelFormat=System.Drawing.Imaging.PixelFormat;


namespace S5Raw {
 sealed class PreviewImage:IDisposable {
  sealed class Pixels {public Bitmap Full,Small;public int References=1;}
  Pixels pixels;public string Description;
  public Bitmap Bitmap {get{return pixels.Full;}set{pixels=new Pixels{Full=value};}}
  public Bitmap Overview {get{return pixels.Small;}set{pixels.Small=value;}}
  public long Bytes {get{return (long)Bitmap.Width*Bitmap.Height*4+(Overview==null?0:(long)Overview.Width*Overview.Height*4);}}
  public PreviewImage Retain(){System.Threading.Interlocked.Increment(ref pixels.References);return new PreviewImage{pixels=pixels,Description=Description};}
  public void Dispose(){var old=System.Threading.Interlocked.Exchange(ref pixels,null);if(old!=null&&System.Threading.Interlocked.Decrement(ref old.References)==0){old.Full.Dispose();if(old.Small!=null)old.Small.Dispose();}}
 }
 static class PreviewReader {
  public static bool Supported(string path){string ext=Path.GetExtension(path).ToLowerInvariant();if(ext==".raf"){string reason;return RafHeader.Accept(path,out reason);}return new[]{".jpg",".jpeg",".tif",".tiff",".png"}.Contains(ext);}
  static uint BE(byte[] b,int i){return ((uint)b[i]<<24)|((uint)b[i+1]<<16)|((uint)b[i+2]<<8)|b[i+3];}
  public static PreviewImage Read(string path,bool alreadySrgb=false){
   bool raf=Path.GetExtension(path).Equals(".raf",StringComparison.OrdinalIgnoreCase);
   using(var file=File.OpenRead(path)){
    Stream source=file;MemoryStream preview=null;
    try {
     if(raf){
      byte[] header=new byte[108];if(file.Read(header,0,108)!=108||Encoding.ASCII.GetString(header,0,16)!="FUJIFILMCCD-RAW ")throw new IOException(L.T("不是有效的 RAF 文件"));
      uint offset=BE(header,84),length=BE(header,88);if(length<4||(long)offset+length>file.Length)throw new IOException(L.T("此 RAF 没有可用的内嵌预览，请先转换 TIFF"));
      file.Position=offset;byte[] data=new BinaryReader(file).ReadBytes(checked((int)length));preview=new MemoryStream(data,false);source=preview;
     }
     var frame=BitmapFrame.Create(source,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
     return Render(frame,frame,alreadySrgb);
    }finally{if(preview!=null)preview.Dispose();}
   }
  }
  public static PreviewImage ReadOfficial(string bitmap,string raf){
   using(var file=File.OpenRead(bitmap))using(var raw=File.OpenRead(raf)){
    var frame=BitmapFrame.Create(file,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
    var reader=new BinaryReader(raw);byte[] header=reader.ReadBytes(108);raw.Position=BE(header,84);
    using(var jpeg=new MemoryStream(reader.ReadBytes(checked((int)BE(header,88))),false)){
     var metadataFrame=BitmapFrame.Create(jpeg,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
     return Render(frame,metadataFrame,false);
    }
   }
  }
  static PreviewImage Render(BitmapFrame frame,BitmapFrame metadataFrame,bool alreadySrgb){
     BitmapSource rendered=frame;bool managed=metadataFrame.ColorContexts!=null&&metadataFrame.ColorContexts.Count>0;
     if(rendered.Format!=PixelFormats.Bgra32)rendered=new FormatConvertedBitmap(rendered,PixelFormats.Bgra32,null,0);
     // WIC's color transform does not accept this camera's RGB48 TIFF directly.
     // Reduce only the display buffer to BGRA32, then map its embedded profile to sRGB.
     if(managed&&!alreadySrgb)rendered=new ColorConvertedBitmap(rendered,metadataFrame.ColorContexts[0],new ColorContext(PixelFormats.Bgra32),PixelFormats.Bgra32);
     rendered=new FormatConvertedBitmap(rendered,PixelFormats.Pbgra32,null,0);
     var bitmap=new Bitmap(rendered.PixelWidth,rendered.PixelHeight,PixelFormat.Format32bppPArgb);
     var locked=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
     try{rendered.CopyPixels(System.Windows.Int32Rect.Empty,locked.Scan0,locked.Stride*bitmap.Height,locked.Stride);}finally{bitmap.UnlockBits(locked);}
     var metadata=metadataFrame.Metadata as BitmapMetadata;int orientation=1;
     if(metadata!=null){foreach(var query in new[]{"/app1/ifd/{ushort=274}","/ifd/{ushort=274}"}){try{object v=metadata.GetQuery(query);if(v!=null){orientation=Convert.ToInt32(v);break;}}catch(NotSupportedException){}}}
     var rotations=new[]{RotateFlipType.RotateNoneFlipNone,RotateFlipType.RotateNoneFlipNone,RotateFlipType.RotateNoneFlipX,RotateFlipType.Rotate180FlipNone,RotateFlipType.Rotate180FlipX,RotateFlipType.Rotate90FlipX,RotateFlipType.Rotate90FlipNone,RotateFlipType.Rotate270FlipX,RotateFlipType.Rotate270FlipNone};
     if(orientation>1&&orientation<rotations.Length)bitmap.RotateFlip(rotations[orientation]);
     Bitmap overview=null;if(Math.Max(bitmap.Width,bitmap.Height)>2048){float ratio=2048f/Math.Max(bitmap.Width,bitmap.Height);overview=new Bitmap((int)(bitmap.Width*ratio),(int)(bitmap.Height*ratio),PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(overview)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(bitmap,0,0,overview.Width,overview.Height);}}
     return new PreviewImage{Bitmap=bitmap,Overview=overview,Description=bitmap.Width+" × "+bitmap.Height};
  }
 }
 sealed class ViewerForm:Form {
  readonly string caption;readonly List<string> files;ListBox list=new ListBox();ImageCanvas canvas=new ImageCanvas();Label details=new Label();ToolStripLabel scale=new ToolStripLabel();PreviewImage current;int generation;string loadError;
  public ViewerForm(IEnumerable<string> paths,string selected,string title="图片浏览器"){
   caption=title;
   SuspendLayout();files=paths.Where(PreviewReader.Supported).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
   Text=L.T("Fuji S3/S5 Pro RAW Converter · 图片浏览器");ClientSize=new Size(1120,760);MinimumSize=new Size(760,500);StartPosition=FormStartPosition.CenterParent;Font=new Font("Microsoft YaHei UI",10);KeyPreview=true;
   var bar=new ToolStrip{GripStyle=ToolStripGripStyle.Hidden,Dock=DockStyle.Top,Padding=new Padding(6)};
   Add(bar,L.T("上一张 ←"),delegate{Navigate(-1);});Add(bar,L.T("下一张 →"),delegate{Navigate(1);});bar.Items.Add(new ToolStripSeparator());
   Add(bar,L.T("适应窗口"),delegate{canvas.Fit();});Add(bar,L.T("原始大小"),delegate{canvas.Actual();});Add(bar,"−",delegate{canvas.ScaleBy(.8f);});Add(bar,"＋",delegate{canvas.ScaleBy(1.25f);});bar.Items.Add(scale);
   Add(bar,L.T("系统打开"),delegate{if(list.SelectedIndex>=0)System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(files[list.SelectedIndex]){UseShellExecute=true});});
   var split=new SplitContainer{Dock=DockStyle.Fill,FixedPanel=FixedPanel.Panel1,SplitterDistance=235};list.Dock=DockStyle.Fill;list.HorizontalScrollbar=true;foreach(var p in files)list.Items.Add(Path.GetFileName(p));split.Panel1.Controls.Add(list);canvas.Dock=DockStyle.Fill;split.Panel2.Controls.Add(canvas);
   details.AutoEllipsis=true;details.Dock=DockStyle.Bottom;details.Height=32;details.Padding=new Padding(8,0,8,0);details.TextAlign=ContentAlignment.MiddleLeft;details.BackColor=Color.FromArgb(237,240,242);Controls.Add(split);Controls.Add(details);Controls.Add(bar);
   list.SelectedIndexChanged+=delegate{LoadSelected();};canvas.ZoomChanged+=delegate{scale.Text=(canvas.Zoom*100).ToString("0")+"%";};
   bool sizingList=false;
   Shown+=delegate{
    float dpi;using(var g=CreateGraphics())dpi=g.DpiX/96f;
    int measured=files.Count==0?0:files.Max(p=>TextRenderer.MeasureText(Path.GetFileName(p),list.Font,Size.Empty,TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix).Width);
    object saved=Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\S5Raw","ViewerFileListWidth",0);
    int preferred=saved is int?(int)saved:0;
    int width=preferred>0?(int)(preferred*dpi):Math.Max((int)(300*dpi),measured+SystemInformation.VerticalScrollBarWidth+(int)(24*dpi));
    sizingList=true;split.Panel1MinSize=(int)(180*dpi);split.Panel2MinSize=(int)(320*dpi);
    split.SplitterDistance=Math.Max(split.Panel1MinSize,Math.Min(width,split.Width-split.Panel2MinSize-split.SplitterWidth));sizingList=false;
    int i=files.FindIndex(x=>x.Equals(selected,StringComparison.OrdinalIgnoreCase));if(files.Count>0)list.SelectedIndex=Math.Max(0,i);
   };
   split.SplitterMoved+=delegate{
    if(sizingList||!Visible)return;
    float dpi;using(var g=CreateGraphics())dpi=g.DpiX/96f;
    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\S5Raw","ViewerFileListWidth",(int)(split.SplitterDistance/dpi));
   };
   L.Changed+=ApplyLanguage;
   FormClosed+=delegate{L.Changed-=ApplyLanguage;generation++;canvas.SetImage(null);if(current!=null)current.Dispose();};
   AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ResumeLayout(true);ApplyLanguage();
  }
  void ApplyLanguage(){L.Apply(this);UpdateCaption();if(current==null&&list.SelectedIndex>=0)details.Text=loadError==null?L.T("加载中…"):L.T("无法预览：")+L.Error(loadError);}
  void UpdateCaption(){int i=list.SelectedIndex;Text="Fuji S3/S5 Pro RAW Converter · "+L.T(caption)+(i<0?"":" — "+Path.GetFileName(files[i])+" ["+(i+1)+"/"+files.Count+"]");}
  static void Add(ToolStrip strip,string label,Action action){var b=new ToolStripButton(label);b.Click+=delegate{action();};strip.Items.Add(b);}
  void Navigate(int delta){if(files.Count>0)list.SelectedIndex=Math.Max(0,Math.Min(files.Count-1,list.SelectedIndex+delta));}
  protected override bool ProcessCmdKey(ref Message msg,Keys key){if(key==Keys.Right||key==Keys.PageDown){Navigate(1);return true;}if(key==Keys.Left||key==Keys.PageUp){Navigate(-1);return true;}if(key==Keys.Escape){Close();return true;}return base.ProcessCmdKey(ref msg,key);}
  void LoadSelected(){
   int i=list.SelectedIndex;if(i<0)return;int token=++generation;string path=files[i];loadError=null;UpdateCaption();details.Text=L.T("加载中…");
   canvas.SetImage(null);if(current!=null){current.Dispose();current=null;}
   FullPreview.Load(this,path,delegate(PreviewImage image,Exception error){if(token!=generation){if(image!=null)image.Dispose();return;}if(error!=null){loadError=error.Message;details.Text=L.T("无法预览：")+L.Error(error.Message);return;}current=image;canvas.SetPreview(current);details.Text=current.Description+"   ·   "+Path.GetDirectoryName(path);},delegate{return token==generation;});
   foreach(int neighbor in new[]{i+1,i+2,i+3,i-1})if(neighbor>=0&&neighbor<files.Count)FullPreview.Prefetch(files[neighbor],delegate{return token==generation;});
  }
  public static List<string> InFolder(string folder,bool recursive){return Directory.EnumerateFiles(folder,"*",recursive?SearchOption.AllDirectories:SearchOption.TopDirectoryOnly).Where(PreviewReader.Supported).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();}
 }
}

