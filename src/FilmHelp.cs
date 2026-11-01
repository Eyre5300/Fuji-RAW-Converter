using System;
using System.Text;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace S5Raw {
 static class FilmHelp {
  // Concise paraphrases of Fujifilm's X-T50 manual (Film Simulation) and
  // FinePix S5 Pro manual p.111. Keep every selectable key documented.
  internal const string XManual="https://fujifilm-dsc.com/en/manual/x-t50/menu_shooting/image_quality_setting/#film_simulation";
  internal const string SManual="https://dl.fujifilm-x.com/support/manual/s/finepix_s5pro_manual_01.pdf";
  static readonly Dictionary<string,string[]> descriptions=new Dictionary<string,string[]>{
   {"f1",new[]{"彩色。柔和肤色过渡，适合闪光灯棚拍人像。","Color. Smooth skin transitions for studio portraits."}},
   {"f1a",new[]{"彩色。比 F1 略鲜艳。","Color. Slightly stronger saturation than F1."}},
   {"f1b",new[]{"彩色。柔和肤色与鲜明自然色，适合日光人像。","Color. Smooth skin, vivid natural colors; daylight portraits."}},
   {"f1c",new[]{"彩色。比 F1 更锐利，适合时尚摄影。","Color. Sharper than F1; fashion photography."}},
   {"f2",new[]{"彩色。强调鲜艳自然色，适合风光。","Color. Vivid natural colors for landscapes."}},
   {"provia",new[]{"彩色。通用、均衡，适合日常题材。","Color. Balanced, general-purpose rendering."}},
   {"velvia",new[]{"彩色。鲜艳浓郁，适合自然风景。","Color. Vibrant landscapes and nature."}},
   {"astia",new[]{"彩色。色彩与反差较柔和。","Color. Softer color and contrast."}},
   {"classic-chrome",new[]{"彩色。色彩克制，暗部反差较强。","Color. Muted palette, stronger shadow contrast."}},
   {"pro-neg-hi",new[]{"彩色。人像风格，反差略高。","Color. Portraits with increased contrast."}},
   {"pro-neg-std",new[]{"彩色。肤色层次柔和，便于后期。","Color. Gentle skin gradations; editing latitude."}},
   {"classic-neg",new[]{"彩色。色彩与明暗对比鲜明。","Color. Strong color and tonal separation."}},
   {"eterna",new[]{"彩色。柔和色彩、丰富暗部，偏电影观感。","Color. Subdued cinematic palette, rich shadows."}},
   {"bleach-bypass",new[]{"彩色。低饱和、高反差；并非黑白。","Color. Low saturation, high contrast; not monochrome."}},
   {"nostalgic-neg",new[]{"彩色。高光偏琥珀暖色，暗部浓郁。","Color. Amber highlights, rich shadows."}},
   {"reala-ace",new[]{"彩色。自然色彩与较硬朗的影调。","Color. Faithful colors, firm tonality."}},
   {"acros",new[]{"黑白。强调细节与层次。","B&W. Detailed tonal rendering."}},
   {"acros-ye",new[]{"黑白黄滤镜。略增反差、压暗天空。","B&W yellow filter. Mild contrast, darker skies."}},
   {"acros-r",new[]{"黑白红滤镜。反差更强、天空更暗。","B&W red filter. Strong contrast, darker skies."}},
   {"acros-g",new[]{"黑白绿滤镜。适合人像肤色层次。","B&W green filter. Portrait skin tones."}},
   {"mono",new[]{"黑白。普通单色渲染。","B&W. Conventional monochrome rendering."}},
   {"mono-ye",new[]{"黑白黄滤镜。略增反差、压暗天空。","B&W yellow filter. Mild contrast, darker skies."}},
   {"mono-r",new[]{"黑白红滤镜。反差更强、天空更暗。","B&W red filter. Strong contrast, darker skies."}},
   {"mono-g",new[]{"黑白绿滤镜。适合人像肤色层次。","B&W green filter. Portrait skin tones."}},
   {"sepia",new[]{"棕褐单色。不是自然彩色。","Sepia monochrome; not natural color."}}
  };
  internal static string Description(string model,string key,bool english){
   if(key=="standard")return model=="X-T50"?(english?"Uses the RAF shooting settings; a monochrome capture may remain monochrome. Choose PROVIA explicitly for color.":"依据 RAF 拍摄设置；若拍摄时用了黑白模式，输出也可能是黑白。需要彩色可明确选择 PROVIA。"):(english?"Default rendering from the camera's conversion engine; use as a comparison baseline.":"使用该相机转换内核的默认渲染，可作为效果对比基准。");
   string[] value;if(!descriptions.TryGetValue(key,out value))throw new ArgumentException("Missing film description: "+key);
   return value[english?1:0];
  }
  internal static string Label(string key){int i=Array.IndexOf(FilmSimulation.Keys,key);return L.T(key=="standard"?RfcEx.Label(key):i>=0?FilmSimulation.Labels[i]:RfcEx.Label(key));}
  internal static string BuildText(string model,string[] keys,string selected,bool english){
   var text=new StringBuilder();
   text.AppendLine(english?"These settings affect exported pixels. RAW previews stay unchanged. Reading this guide does not change your selection.":"这些设置影响导出图像，RAW 预览不会随之改变。查看说明不会更改当前选择。");
   text.AppendLine();
   foreach(string key in keys){text.AppendLine((key==selected?(english?"[Selected] ":"【当前选择】 "):"")+Label(key));text.AppendLine(Description(model,key,english));text.AppendLine();}
   text.AppendLine(english?"Style summaries follow the camera manuals; software output may differ from in-camera JPEGs.":"风格特点依据相机手册概括，软件导出与相机直出 JPEG 可能不同。");
   text.AppendLine(english?"Official reference:":"官方参考：");text.AppendLine(keys.Any(k=>k.StartsWith("f",StringComparison.Ordinal))?SManual:XManual);
   return text.ToString();
  }
  internal static FlowLayoutPanel CreateFooter(bool english,out Button close){
   // AutoSize reserves space for the actual font/button height, including padding.
   var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.RightToLeft,WrapContents=false,Margin=Padding.Empty,Padding=new Padding(0,12,0,0)};
   close=new Button{Text=english?"Close":"关闭",AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(96,36),Padding=new Padding(8,4,8,4),Margin=Padding.Empty,DialogResult=DialogResult.OK};footer.Controls.Add(close);return footer;
  }
  internal static void Show(IWin32Window owner,string model,string[] keys,string selected){
   bool english=L.English;
   using(var form=new Form{Text=(english?"Film simulation help — ":"胶片模拟说明 — ")+model,StartPosition=FormStartPosition.CenterParent,ClientSize=new Size(700,560),MinimumSize=new Size(520,400),ShowInTaskbar=false,Font=new Font("Microsoft YaHei UI",10),AutoScaleDimensions=new SizeF(96,96),AutoScaleMode=AutoScaleMode.Dpi}){
    var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=3};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,40));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));form.Controls.Add(layout);
    layout.Controls.Add(new Label{Text=model+(english?" · Available film simulations":" · 可用胶片模拟"),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);
    var body=new RichTextBox{Dock=DockStyle.Fill,ReadOnly=true,BackColor=Color.White,BorderStyle=BorderStyle.FixedSingle,WordWrap=true,DetectUrls=true,Margin=Padding.Empty,Text=BuildText(model,keys,selected,english)};
    body.LinkClicked+=delegate(object sender,LinkClickedEventArgs e){try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.LinkText){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(form,ex.Message);}};layout.Controls.Add(body,0,1);
    Button close;var footer=CreateFooter(english,out close);layout.Controls.Add(footer,0,2);form.AcceptButton=form.CancelButton=close;
    form.Shown+=delegate{int at=body.Text.IndexOf(english?"[Selected]":"【当前选择】",StringComparison.Ordinal);body.Select(Math.Max(0,at),0);body.ScrollToCaret();close.Focus();};
    form.ShowDialog(owner);
   }
  }
 }
}
