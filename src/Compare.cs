using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace S5Raw {
 sealed class PhotoPair {public string Raw;public List<string> Results=new List<string>();public override string ToString(){return Path.GetFileName(Raw)+(Results.Count==0?L.T(" · 未匹配"):"");}}
 sealed class ResultChoice {public string Path;public override string ToString(){return (System.IO.Path.GetExtension(Path).StartsWith(".ti",StringComparison.OrdinalIgnoreCase)?"TIFF":"JPEG")+" · "+System.IO.Path.GetFileName(Path);}}
 sealed class CompareForm:Form {
  readonly List<PhotoPair> pairs;CompareCanvas canvas=new CompareCanvas();PreviewImage before,after;int generation,resultGeneration;bool changing;string preferredFormat;
  ComboBox choices=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=230},results=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};
  Label leftTitle=new Label(),rightTitle=new Label(),counter=new Label{Width=85,Height=34,TextAlign=ContentAlignment.MiddleCenter};Button previous,next;
  CheckBox linked=new CheckBox{Text=L.T("同步"),Checked=true,AutoSize=false,Size=new Size(64,34),Margin=new Padding(8,3,3,3)};
  PhotoInfo rawInfo,outputInfo;string leftError,rightError;ToolTip headerTips=new ToolTip();
  Label scale=new Label{AutoSize=false,Size=new Size(105,34),TextAlign=ContentAlignment.MiddleLeft};
  public CompareForm(IEnumerable<PhotoPair> photos,string selected){
   SuspendLayout();pairs=photos.ToList();Text=L.T("Fuji RAW Converter · 对照");Font=new Font("Microsoft YaHei UI",10);ClientSize=new Size(1180,720);MinimumSize=new Size(800,480);StartPosition=FormStartPosition.CenterParent;
   var top=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(4,5,4,5),WrapContents=true};
   previous=Add(top,L.T("上一组"),80,delegate{Navigate(-1);});next=Add(top,L.T("下一组"),80,delegate{Navigate(1);});top.Controls.Add(counter);
   var selector=new TableLayoutPanel{Width=236,Height=40,ColumnCount=1,RowCount=1,Margin=Padding.Empty};choices.Anchor=AnchorStyles.Left|AnchorStyles.Right;selector.Controls.Add(choices);top.Controls.Add(selector);foreach(var pair in pairs)choices.Items.Add(pair);
   Add(top,L.T("适应窗口"),104,delegate{canvas.Fit();});Add(top,L.T("原始大小"),104,delegate{canvas.Actual();});Add(top,"−",40,delegate{canvas.ScaleBy(.8f);});Add(top,"＋",40,delegate{canvas.ScaleBy(1.25f);});top.Controls.Add(linked);top.Controls.Add(scale);
   var headers=new TableLayoutPanel{Dock=DockStyle.Top,Height=88,ColumnCount=2,RowCount=1,Margin=Padding.Empty,BackColor=Color.FromArgb(238,241,243)};headers.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));headers.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
   leftTitle.Dock=DockStyle.Fill;leftTitle.Padding=new Padding(8,0,8,0);leftTitle.TextAlign=ContentAlignment.MiddleLeft;leftTitle.AutoEllipsis=true;headers.Controls.Add(leftTitle,0,0);
   var resultHeader=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty,Padding=new Padding(6,0,6,0)};resultHeader.RowStyles.Add(new RowStyle(SizeType.Absolute,34));resultHeader.RowStyles.Add(new RowStyle(SizeType.Percent,100));results.Anchor=AnchorStyles.Left|AnchorStyles.Right;resultHeader.Controls.Add(results,0,0);rightTitle.Dock=DockStyle.Fill;rightTitle.TextAlign=ContentAlignment.MiddleLeft;rightTitle.AutoEllipsis=true;resultHeader.Controls.Add(rightTitle,0,1);headers.Controls.Add(resultHeader,1,0);
   canvas.Dock=DockStyle.Fill;Controls.Add(canvas);Controls.Add(headers);Controls.Add(top);
   canvas.ZoomChanged+=delegate{scale.Text=after==null?"":L.T("缩放 ")+(canvas.Zoom*100).ToString("0")+"%";};linked.CheckedChanged+=delegate{canvas.Linked=linked.Checked;};
   choices.SelectedIndexChanged+=delegate{if(!changing)LoadPair();};results.SelectedIndexChanged+=delegate{if(!changing)LoadResult(true);};
   Shown+=delegate{if(pairs.Count>0)choices.SelectedIndex=Math.Max(0,pairs.FindIndex(p=>p.Raw.Equals(selected,StringComparison.OrdinalIgnoreCase)));};
   L.Changed+=ApplyLanguage;
   FormClosed+=delegate{L.Changed-=ApplyLanguage;generation++;resultGeneration++;ReleaseImages();headerTips.Dispose();};
   AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ResumeLayout(true);ApplyLanguage();
  }
  void ApplyLanguage(){
   L.Apply(this);int i=choices.SelectedIndex;Text=L.T("Fuji RAW Converter · 对照")+(i<0?"":" — "+Path.GetFileName(pairs[i].Raw)+" ["+counter.Text+"]");
   if(i<0)return;
   // Refresh display strings without changing the selected pair, zoom or loaded pixels.
   changing=true;var selected=choices.SelectedItem;for(int n=0;n<choices.Items.Count;n++)choices.Items[n]=pairs[n];choices.SelectedItem=selected;changing=false;
   leftTitle.Text="RAW · "+Path.GetFileName(pairs[i].Raw)+(before!=null?"\n"+rawInfo.Details(before.Description):leftError==null?L.T(" · 加载中…"):" · "+L.Error(leftError));
   rightTitle.Text=after!=null?outputInfo.Details(after.Description):rightError!=null?L.Error(rightError):results.SelectedItem==null?L.T("未找到转换结果"):L.T("加载中…");
   scale.Text=after==null?"":L.T("缩放 ")+(canvas.Zoom*100).ToString("0")+"%";headerTips.SetToolTip(leftTitle,leftTitle.Text);headerTips.SetToolTip(rightTitle,rightTitle.Text);
  }
  static string Format(string path){return Path.GetExtension(path).StartsWith(".ti",StringComparison.OrdinalIgnoreCase)?"TIFF":"JPEG";}
  void ReleaseImages(){canvas.Clear();if(before!=null){before.Dispose();before=null;}if(after!=null){after.Dispose();after=null;}}
  void Navigate(int delta){if(pairs.Count>0)choices.SelectedIndex=Math.Max(0,Math.Min(pairs.Count-1,choices.SelectedIndex+delta));}
  void LoadPair(){
   int i=choices.SelectedIndex;if(i<0)return;leftError=null;var pair=pairs[i];int token=++generation;resultGeneration++;ReleaseImages();scale.Text="";
   counter.Text=(i+1)+" / "+pairs.Count;previous.Enabled=i>0;next.Enabled=i<pairs.Count-1;Text=L.T("Fuji RAW Converter · 对照 — ")+Path.GetFileName(pair.Raw)+" ["+counter.Text+"]";
   leftTitle.Text="RAW · "+Path.GetFileName(pair.Raw)+L.T(" · 加载中…");
   rawInfo=PhotoInfo.Read(pair.Raw);
   changing=true;results.Items.Clear();foreach(string path in pair.Results)results.Items.Add(new ResultChoice{Path=path});results.Enabled=pair.Results.Count>0;
   if(pair.Results.Count>0)results.SelectedIndex=Math.Max(0,pair.Results.FindIndex(p=>Format(p)==preferredFormat));changing=false;
   FullPreview.Load(this,pair.Raw,delegate(PreviewImage image,Exception error){if(error!=null){leftError=error.Message;leftTitle.Text="RAW · "+L.Error(error.Message);return;}before=image;canvas.SetBefore(before);leftTitle.Text="RAW · "+Path.GetFileName(pair.Raw)+"\n"+rawInfo.Details(before.Description);headerTips.SetToolTip(leftTitle,leftTitle.Text);},delegate{return token==generation;});
   LoadResult(false);
   foreach(int neighbor in new[]{i+1,i+2,i+3,i-1})if(neighbor>=0&&neighbor<pairs.Count){var adjacent=pairs[neighbor];FullPreview.Prefetch(adjacent.Raw,delegate{return token==generation;});if(Math.Abs(neighbor-i)==1){var result=adjacent.Results.FirstOrDefault(p=>Format(p)==preferredFormat)??adjacent.Results.FirstOrDefault();if(result!=null)FullPreview.Prefetch(result,delegate{return token==generation;});}}
   var alternative=pair.Results.FirstOrDefault(p=>Format(p)!=preferredFormat);if(alternative!=null)FullPreview.Prefetch(alternative,delegate{return token==generation;});
  }
  void LoadResult(bool preserve){
   rightError=null;int token=++resultGeneration;canvas.SetAfter(null,preserve);if(after!=null){after.Dispose();after=null;}scale.Text="";
   var choice=results.SelectedItem as ResultChoice;if(choice==null){rightTitle.Text=L.T("未找到转换结果");return;}preferredFormat=Format(choice.Path);rightTitle.Text=L.T("加载中…");
   var info=PhotoInfo.Read(choice.Path);outputInfo=info;if(string.IsNullOrEmpty(info.Camera))info.Camera=rawInfo.Camera;if(string.IsNullOrEmpty(info.Shot))info.Shot=rawInfo.Shot;
   FullPreview.Load(this,choice.Path,delegate(PreviewImage image,Exception error){if(error!=null){rightError=error.Message;rightTitle.Text=L.Error(error.Message);return;}after=image;canvas.SetAfter(after,preserve);rightTitle.Text=info.Details(after.Description);headerTips.SetToolTip(rightTitle,choice.Path+"\n"+rightTitle.Text);},delegate{return token==resultGeneration;});
  }
  static Button Add(Control panel,string text,int width,Action action){var button=new Button{Text=text,Width=width,Height=34,FlatStyle=FlatStyle.Flat,BackColor=Color.White};button.FlatAppearance.BorderColor=Color.FromArgb(195,201,205);button.Click+=delegate{action();};panel.Controls.Add(button);return button;}
  protected override bool ProcessCmdKey(ref Message msg,Keys key){if(key==Keys.Escape){Close();return true;}if(!choices.DroppedDown&&!results.DroppedDown&&(key==Keys.Right||key==Keys.PageDown)){Navigate(1);return true;}if(!choices.DroppedDown&&!results.DroppedDown&&(key==Keys.Left||key==Keys.PageUp)){Navigate(-1);return true;}return base.ProcessCmdKey(ref msg,key);}
 }
}
