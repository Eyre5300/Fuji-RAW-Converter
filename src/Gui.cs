using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8")]
namespace S5Raw {
 class MainForm:Form {
  sealed class Photo {public string Input,Relative,Output,NativeModule,Model,Backend,Key=Guid.NewGuid().ToString("N");public ListViewItem Item;}
  ListView inputs=new ListView();ImageList thumbnails=new ImageList();TextBox output=new TextBox(),log=new TextBox();
  CheckBox sameFolder=new CheckBox{Text=L.T("源文件夹"),Checked=true,AutoSize=true,Anchor=AnchorStyles.Left};
  ComboBox language=new ComboBox();ComboBox format=new ComboBox(),color=new ComboBox(),existing=new ComboBox();NumericUpDown quality=new NumericUpDown{Minimum=1,Maximum=100,Value=92,Width=58};
  CheckBox recursive=new CheckBox{Text=L.T("包含子文件夹"),Checked=true,AutoSize=true};Label qualityLabel=new Label{Text=L.T("JPEG 质量"),AutoSize=false,Width=80,Height=32,TextAlign=ContentAlignment.MiddleLeft,Margin=new Padding(8,0,0,0)},selection=new Label(),status=new Label();
  NumericUpDown parallel=new NumericUpDown{Minimum=1,Maximum=Math.Max(256,Environment.ProcessorCount),Value=Environment.ProcessorCount};int[] fileProgress;int finishedFiles;
  Label parallelStatus=new Label{AutoSize=true,Dock=DockStyle.Top,Margin=new Padding(3,4,3,4)};Dictionary<string,object> parallelState;string parallelReason;
  Button start=new Button(),cancel=new Button();ProgressBar progress=new ProgressBar();FlowLayoutPanel actions=new FlowLayoutPanel();TableLayoutPanel settings=new TableLayoutPanel();
  sealed class CameraFilmRow {public ComboBox Choice;public Button Help;public Label Caption;public string[] Keys=new string[0];}
  readonly Dictionary<string,CameraFilmRow> filmControls=new Dictionary<string,CameraFilmRow>();
  readonly Dictionary<string,string> cameraFilms=new Dictionary<string,string>();
  TableLayoutPanel layout,filmRows=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=2,Margin=Padding.Empty};
  Panel filmViewport=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Margin=Padding.Empty};Label filmHeading;
  string filmLayoutKey=null;
  Process process;string controlFolder,cancelPath;bool closeWhenDone,thumbnailRunning;Queue<Photo> thumbnailQueue=new Queue<Photo>();
  public MainForm(){
   sameFolder.Checked=Preferences.Get("OutputInSource","1")!="0";output.Text=Preferences.Get("OutputFolder");
   SuspendLayout();Text="Fuji S3/S5 Pro RAW Converter "+Shared.Version;StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(940,716);MinimumSize=new Size(900,636);Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(246,248,250);
   layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),Margin=Padding.Empty,ColumnCount=1,RowCount=9};
   layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,28));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));foreach(float h in new[]{192f,48f,12f,0f,32f,0f})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,h));Controls.Add(layout);
   actions.Dock=DockStyle.Fill;actions.AutoSize=true;actions.AutoSizeMode=AutoSizeMode.GrowAndShrink;actions.WrapContents=true;actions.Padding=Padding.Empty;actions.Margin=Padding.Empty;var topBar=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};topBar.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles[0].SizeType=SizeType.Absolute;layout.RowStyles[0].Height=50;topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,144));topBar.Controls.Add(actions,0,0);language.DropDownStyle=ComboBoxStyle.DropDownList;language.Anchor=AnchorStyles.Top|AnchorStyles.Right;language.Width=136;language.MinimumSize=new Size(136,0);language.Margin=new Padding(8,4,0,8);language.AccessibleName="Language";language.Items.AddRange(new object[]{"简体中文","English"});language.SelectedIndex=L.English?1:0;language.SelectedIndexChanged+=delegate{L.SetEnglish(language.SelectedIndex==1);};topBar.Controls.Add(language,1,0);layout.Controls.Add(topBar,0,0);
   AddButton(actions,L.T("添加照片"),delegate{using(var d=new OpenFileDialog{Filter=L.T("富士 RAW (*.RAF)|*.RAF"),Multiselect=true,InitialDirectory=OpenFolder()})if(d.ShowDialog(this)==DialogResult.OK)AddInputs(d.FileNames);});
   AddButton(actions,L.T("添加文件夹"),delegate{using(var d=new FolderBrowserDialog{SelectedPath=OpenFolder()})if(d.ShowDialog(this)==DialogResult.OK)AddInputs(new[]{d.SelectedPath});});
   AddButton(actions,L.T("全选"),delegate{foreach(ListViewItem i in inputs.Items)i.Checked=true;});AddButton(actions,L.T("全不选"),delegate{foreach(ListViewItem i in inputs.Items)i.Checked=false;});
   AddButton(actions,L.T("移除"),delegate{foreach(ListViewItem i in inputs.SelectedItems.Cast<ListViewItem>().ToArray())inputs.Items.Remove(i);UpdateSelection();});
   AddButton(actions,L.T("清空"),delegate{inputs.Items.Clear();thumbnails.Images.Clear();Placeholder();lock(thumbnailQueue)thumbnailQueue.Clear();UpdateSelection();});recursive.AutoSize=false;recursive.Size=new Size(156,36);recursive.Margin=new Padding(8,0,0,8);actions.Controls.Add(recursive);
   selection.Margin=Padding.Empty;selection.Dock=DockStyle.Fill;selection.TextAlign=ContentAlignment.MiddleLeft;selection.Text=L.T("0 张");layout.Controls.Add(selection,0,1);
   using(var g=Graphics.FromHwnd(IntPtr.Zero)){int width=Math.Min(256,(int)(156*g.DpiX/96));thumbnails.ImageSize=new Size(width,width*2/3);}thumbnails.ColorDepth=ColorDepth.Depth32Bit;Placeholder();
   inputs.Margin=new Padding(0,4,0,0);inputs.Dock=DockStyle.Fill;inputs.View=View.LargeIcon;inputs.LargeImageList=thumbnails;inputs.CheckBoxes=true;inputs.MultiSelect=true;inputs.HideSelection=false;inputs.ShowItemToolTips=true;inputs.AllowDrop=true;
   inputs.ItemChecked+=delegate{UpdateSelection();};inputs.DoubleClick+=delegate{BrowseInput();};inputs.KeyDown+=delegate(object o,KeyEventArgs e){if(e.KeyCode==Keys.Enter){e.Handled=true;BrowseInput();}};
   inputs.DragEnter+=delegate(object o,DragEventArgs e){if(process==null&&e.Data.GetDataPresent(DataFormats.FileDrop))e.Effect=DragDropEffects.Copy;};inputs.DragDrop+=delegate(object o,DragEventArgs e){AddInputs((string[])e.Data.GetData(DataFormats.FileDrop));};layout.Controls.Add(inputs,0,2);
   settings.Dock=DockStyle.Fill;settings.Margin=Padding.Empty;settings.ColumnCount=2;settings.RowCount=4;settings.Padding=new Padding(0,12,0,4);settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(int i=0;i<4;i++)settings.RowStyles.Add(new RowStyle(SizeType.Absolute,44));layout.Controls.Add(settings,0,3);
   settings.Controls.Add(FieldLabel(L.T("输出位置")),0,0);
   var destinationRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};destinationRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,184));destinationRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));destinationRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,88));
   sameFolder.Margin=new Padding(0,0,8,0);destinationRow.Controls.Add(sameFolder,0,0);output.Anchor=AnchorStyles.Left|AnchorStyles.Right;output.Margin=new Padding(0,0,8,0);output.Enabled=!sameFolder.Checked;destinationRow.Controls.Add(output,1,0);sameFolder.CheckedChanged+=delegate{output.Enabled=!sameFolder.Checked;SaveOutput();};output.Leave+=delegate{SaveOutput();};
   var browse=new Button{Text=L.T("浏览…"),Anchor=AnchorStyles.Left|AnchorStyles.Right,Height=36,Margin=Padding.Empty};browse.Click+=delegate{using(var d=new FolderBrowserDialog{SelectedPath=output.Text})if(d.ShowDialog(this)==DialogResult.OK){output.Text=d.SelectedPath;sameFolder.Checked=false;SaveOutput();}};destinationRow.Controls.Add(browse,2,0);settings.Controls.Add(destinationRow,1,0);
   settings.Controls.Add(FieldLabel(L.T("输出格式")),0,1);var formats=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=5,RowCount=1,Margin=Padding.Empty};formats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,184));formats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,184));formats.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));formats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,80));formats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   format.DropDownStyle=ComboBoxStyle.DropDownList;format.Anchor=AnchorStyles.Left|AnchorStyles.Right;format.Margin=new Padding(0,0,8,0);format.Items.AddRange(new object[]{L.T("TIFF · 16 位"),L.T("TIFF · 8 位"),"JPEG"});format.SelectedIndex=0;
   color.DropDownStyle=ComboBoxStyle.DropDownList;color.Anchor=AnchorStyles.Left|AnchorStyles.Right;color.Margin=new Padding(0,0,8,0);color.Items.AddRange(new object[]{"sRGB","Adobe RGB (1998)"});color.SelectedIndex=0;qualityLabel.AutoSize=true;qualityLabel.Anchor=AnchorStyles.Left;qualityLabel.Margin=new Padding(8,0,8,0);quality.Anchor=AnchorStyles.Left|AnchorStyles.Right;quality.Margin=Padding.Empty;formats.Controls.Add(format,0,0);formats.Controls.Add(color,1,0);formats.Controls.Add(qualityLabel,2,0);formats.Controls.Add(quality,3,0);format.SelectedIndexChanged+=delegate{UpdateFormat();};UpdateFormat();settings.Controls.Add(formats,1,1);
   settings.Controls.Add(FieldLabel(L.T("同名文件")),0,2);existing.DropDownStyle=ComboBoxStyle.DropDownList;existing.Anchor=AnchorStyles.Left|AnchorStyles.Right;existing.Margin=new Padding(0,0,8,0);existing.Items.AddRange(new object[]{L.T("自动加序号"),L.T("跳过"),L.T("报错"),L.T("覆盖")});existing.SelectedIndex=0;
   var options=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=1,Margin=Padding.Empty};options.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,184));options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));options.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,80));options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));options.Controls.Add(existing,0,0);var parallelLabel=FieldLabel(L.T("并行数"));parallelLabel.AutoSize=true;parallelLabel.Margin=new Padding(0,0,8,0);options.Controls.Add(parallelLabel,1,0);parallel.Anchor=AnchorStyles.Left|AnchorStyles.Right;parallel.Margin=Padding.Empty;parallel.Width=80;options.Controls.Add(parallel,2,0);settings.Controls.Add(options,1,2);
   filmHeading=FieldLabel(L.T("胶片模拟"));filmHeading.Dock=DockStyle.Top;filmHeading.Height=44;settings.Controls.Add(filmHeading,0,3);
   filmRows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,184));filmRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));filmViewport.Controls.Add(filmRows);settings.Controls.Add(filmViewport,1,3);color.SelectedIndexChanged+=delegate{UpdateFormat();};
   var run=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Margin=Padding.Empty,Padding=new Padding(0,8,0,0)};layout.RowStyles[4].SizeType=SizeType.AutoSize;start.Text=L.T("转换选中项 (0)");start.AutoSize=true;start.AutoSizeMode=AutoSizeMode.GrowAndShrink;start.MinimumSize=new Size(160,36);start.Padding=new Padding(12,0,12,0);start.Margin=new Padding(0,0,8,8);start.BackColor=Color.FromArgb(28,103,84);start.ForeColor=Color.White;start.FlatStyle=FlatStyle.Flat;start.Click+=delegate{StartBatch();};cancel.Text=L.T("取消");cancel.AutoSize=true;cancel.AutoSizeMode=AutoSizeMode.GrowAndShrink;cancel.MinimumSize=new Size(72,36);cancel.Padding=new Padding(12,0,12,0);cancel.Margin=new Padding(0,0,16,8);cancel.Enabled=false;cancel.Click+=delegate{CancelBatch();};run.Controls.Add(start);run.Controls.Add(cancel);
   AddButton(run,L.T("预览"),delegate{BrowseInput();});AddButton(run,L.T("对照"),delegate{CompareSelected();});AddButton(run,L.T("浏览转换结果"),delegate{BrowseOutput();});AddButton(run,L.T("打开输出文件夹"),delegate{OpenOutputFolder();});AddButton(run,L.T("日志"),delegate{layout.RowStyles[8].Height=log.Visible?0:100*CurrentScale();log.Visible=!log.Visible;});layout.Controls.Add(run,0,4);
   progress.Dock=DockStyle.Fill;progress.Margin=new Padding(0,2,0,2);layout.Controls.Add(progress,0,5);status.Margin=Padding.Empty;status.Text=L.T("就绪");status.Dock=DockStyle.Fill;status.TextAlign=ContentAlignment.MiddleLeft;layout.Controls.Add(status,0,7);
   layout.RowStyles[6].SizeType=SizeType.AutoSize;layout.Controls.Add(parallelStatus,0,6);RenderParallel();
   log.Margin=new Padding(0,4,0,0);log.Dock=DockStyle.Fill;log.Multiline=true;log.ReadOnly=true;log.ScrollBars=ScrollBars.Vertical;log.BackColor=Color.White;log.Visible=false;layout.Controls.Add(log,0,8);
   FormClosing+=delegate(object o,FormClosingEventArgs e){SaveOutput();if(process!=null){e.Cancel=true;closeWhenDone=true;CancelBatch();}};
   Action fitToolbar=delegate{if(topBar.ClientSize.Width<=0)return;var first=actions.Controls[0];int alignedTop=actions.Top+first.Top+(first.Height-language.Height)/2;language.Margin=new Padding(language.Margin.Left,Math.Max(0,alignedTop),language.Margin.Right,language.Margin.Bottom);int width=Math.Max(1,topBar.ClientSize.Width-(int)(144*CurrentScale()));int height=actions.GetPreferredSize(new Size(width,0)).Height;layout.RowStyles[0].Height=Math.Max(height,language.Height+language.Margin.Vertical)+4*CurrentScale();};SizeChanged+=delegate{fitToolbar();};FormClosed+=delegate{L.Changed-=fitToolbar;};
   Shown+=delegate{fitToolbar();using(var g=CreateGraphics())Line(L.T("显示 DPI：")+g.DpiX+L.T("；界面：")+ClientSize.Width+" × "+ClientSize.Height);};
   L.Changed+=ApplyLanguage;L.Changed+=fitToolbar;
   FormClosed+=delegate{L.Changed-=ApplyLanguage;lock(thumbnailQueue)thumbnailQueue.Clear();thumbnails.Dispose();};
   AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ResumeLayout(true);ApplyLanguage();
  }
  static string OpenFolder(){string path=Preferences.Get("OpenFolder");return Directory.Exists(path)?path:"";}
  void SaveOutput(){Preferences.Set("OutputFolder",output.Text);Preferences.Set("OutputInSource",sameFolder.Checked?"1":"0");}
  void ApplyLanguage(){SuspendLayout();L.Apply(this);settings.ColumnStyles[0].Width=(L.English?128:100)*CurrentScale();UpdateSelection();RenderParallel();if(process==null)status.Text=L.T("就绪");ResumeLayout(true);}
  float CurrentScale(){using(var g=CreateGraphics())return g.DpiX/96;}
  static Label FieldLabel(string text){return new Label{Text=text,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Margin=new Padding(0,0,12,0)};}
  static void AddButton(Control parent,string text,Action action){var b=new Button{Text=text,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(72,36),Padding=new Padding(12,0,12,0),Margin=new Padding(0,0,8,8),FlatStyle=FlatStyle.Flat,BackColor=Color.White};b.FlatAppearance.BorderColor=Color.FromArgb(195,201,205);b.Click+=delegate{action();};parent.Controls.Add(b);}
  void Placeholder(){var imageListHandle=thumbnails.Handle;using(var b=new Bitmap(thumbnails.ImageSize.Width,thumbnails.ImageSize.Height))using(var g=Graphics.FromImage(b)){g.Clear(Color.FromArgb(225,231,235));TextRenderer.DrawText(g,"RAF",Font,new Rectangle(Point.Empty,thumbnails.ImageSize),Color.DimGray,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);thumbnails.Images.Add("pending",b);}}
  string FilmFor(Photo photo){string value;return cameraFilms.TryGetValue(photo.Model,out value)?value:"standard";}
  bool UsesColorSpace(Photo photo){return format.SelectedIndex==0||(format.SelectedIndex==1&&(photo.Backend=="rfcex"||FilmFor(photo)!="standard"));}
  void UpdateFormat(){RefreshFilms();color.Enabled=process==null&&(format.SelectedIndex==0||(format.SelectedIndex==1&&inputs.CheckedItems.Cast<ListViewItem>().Any(i=>UsesColorSpace((Photo)i.Tag))));quality.Visible=qualityLabel.Visible=format.SelectedIndex==2;}
  bool refreshingFilms;
  void RefreshFilms(){
   if(refreshingFilms||filmViewport.Parent==null)return;refreshingFilms=true;
   try{
    var photos=inputs.Items.Cast<ListViewItem>().Select(i=>(Photo)i.Tag).ToArray();
    var groups=photos.GroupBy(p=>p.Model).OrderBy(g=>g.Key,StringComparer.OrdinalIgnoreCase).ToArray();
    string signature=string.Join("|",groups.Select(g=>g.Key).ToArray());float scale=CurrentScale();int rowHeight=(int)(44*scale);
    if(signature!=filmLayoutKey){
     filmRows.SuspendLayout();foreach(Control child in filmRows.Controls.Cast<Control>().ToArray())child.Dispose();filmRows.Controls.Clear();filmRows.RowStyles.Clear();filmControls.Clear();
     filmRows.RowCount=Math.Max(1,groups.Length);
     if(groups.Length==0){var hint=FieldLabel(L.T("勾选照片后显示对应机型模式"));filmRows.Controls.Add(hint,0,0);filmRows.SetColumnSpan(hint,2);filmRows.RowStyles.Add(new RowStyle(SizeType.Absolute,rowHeight));}
     for(int i=0;i<groups.Length;i++){
      string model=groups[i].Key;var row=new CameraFilmRow{Caption=FieldLabel(model),Choice=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Anchor=AnchorStyles.Top|AnchorStyles.Left,Width=(int)(320*scale),Margin=Padding.Empty,AccessibleName=model+" "+L.T("胶片模拟")}};
      filmRows.RowStyles.Add(new RowStyle(SizeType.Absolute,rowHeight));filmRows.Controls.Add(row.Caption,0,i);
      row.Help=new Button{Text=L.T("帮助"),AccessibleName=model+" "+L.T("胶片模拟说明"),FlatStyle=FlatStyle.Flat,BackColor=Color.White};row.Help.FlatAppearance.BorderColor=Color.FromArgb(195,201,205);
      row.Help.Click+=delegate{string current;cameraFilms.TryGetValue(model,out current);FilmHelp.Show(this,model,row.Keys,current??"standard");};
      var cell=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};cell.Controls.Add(row.Choice);cell.Controls.Add(row.Help);cell.Layout+=delegate{float dpi=CurrentScale();int gap=(int)(8*dpi);row.Help.Size=new Size(Math.Max(row.Help.MinimumSize.Width,(int)(80*dpi)),Math.Max(row.Choice.Height,(int)(32*dpi)));row.Choice.Width=Math.Max(1,Math.Min((int)(320*dpi),cell.ClientSize.Width-row.Help.Width-gap));row.Choice.Location=new Point(0,Math.Max(0,(cell.ClientSize.Height-row.Choice.Height)/2));row.Help.Location=new Point(row.Choice.Right+gap,Math.Max(0,(cell.ClientSize.Height-row.Help.Height)/2));};filmRows.Controls.Add(cell,1,i);filmControls.Add(model,row);
      row.Choice.SelectedIndexChanged+=delegate{if(refreshingFilms||row.Choice.SelectedIndex<0)return;cameraFilms[model]=row.Keys[row.Choice.SelectedIndex];UpdateFormat();};
     }
     filmLayoutKey=signature;filmRows.ResumeLayout(true);
    }
    // Equal-height table cells center captions and selectors on the same axis.
    filmHeading.Height=rowHeight;filmRows.ColumnStyles[0].Width=184*scale;
    settings.RowStyles[3].Height=Math.Max(1,Math.Min(3,groups.Length))*rowHeight;
    layout.RowStyles[3].Height=(192+44*(Math.Max(1,Math.Min(3,groups.Length))-1))*scale;
    string space=format.SelectedIndex==2?"srgb":color.SelectedIndex==1?"adobe-rgb":"srgb";
    foreach(var group in groups){
     CameraFilmRow row=filmControls[group.Key];var allowed=new HashSet<string>(FilmSimulation.Keys.Concat(RfcEx.Keys));
     foreach(var photo in group)allowed.IntersectWith(photo.Backend=="rfcex"?RfcEx.Films(photo.Model):FilmSimulation.Available(FilmSimulation.EngineDirectory,photo.NativeModule,space));
     string[] next=FilmSimulation.Keys.Concat(RfcEx.Keys).Distinct().Where(allowed.Contains).ToArray();string selected;
     if(!cameraFilms.TryGetValue(group.Key,out selected)||!next.Contains(selected))selected="standard";
     cameraFilms[group.Key]=selected;row.Choice.Enabled=group.Any(p=>p.Item.Checked);
     row.Choice.AccessibleName=group.Key+" "+L.T("胶片模拟");
     string[] labels=next.Select(k=>L.T(k=="standard"?RfcEx.Label(k):FilmSimulation.Keys.Contains(k)?FilmSimulation.Labels[Array.IndexOf(FilmSimulation.Keys,k)]:RfcEx.Label(k))).ToArray();
     if(!next.SequenceEqual(row.Keys)||!labels.SequenceEqual(row.Choice.Items.Cast<string>())){row.Keys=next;row.Choice.BeginUpdate();row.Choice.Items.Clear();row.Choice.Items.AddRange(labels);row.Choice.SelectedIndex=Array.IndexOf(next,selected);row.Choice.EndUpdate();}
    }
   }finally{refreshingFilms=false;}
  }
  void UpdateSelection(){int count=inputs.CheckedItems.Count;selection.Text=L.F("共 {0} 张，已勾选 {1} 张",inputs.Items.Count,count);start.Text=L.F("转换选中项 ({0})",count);UpdateFormat();}
  IEnumerable<string> Scan(string dir){foreach(var f in Directory.GetFiles(dir).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))if(Path.GetExtension(f).Equals(".raf",StringComparison.OrdinalIgnoreCase))yield return f;if(recursive.Checked)foreach(var sub in Directory.GetDirectories(dir).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))if((File.GetAttributes(sub)&FileAttributes.ReparsePoint)==0)foreach(var f in Scan(sub))yield return f;}
  public void AddInputs(string[] paths){
   int added=0,rejected=0;
   var known=new HashSet<string>(inputs.Items.Cast<ListViewItem>().Select(i=>((Photo)i.Tag).Input),StringComparer.OrdinalIgnoreCase);inputs.BeginUpdate();
   try{foreach(var p in paths){string full=Path.GetFullPath(p);bool directory=Directory.Exists(full);string root=directory?full.TrimEnd('\\','/')+Path.DirectorySeparatorChar:Path.GetDirectoryName(full)+Path.DirectorySeparatorChar;
    if(directory||File.Exists(full))Preferences.Set("OpenFolder",directory?full:Path.GetDirectoryName(full));
    foreach(string f in directory?Scan(full):new[]{full}){if(!File.Exists(f)||!Path.GetExtension(f).Equals(".raf",StringComparison.OrdinalIgnoreCase)||!known.Add(f))continue;
     string reason;if(!RafHeader.Accept(f,out reason)){rejected++;Line(L.T("跳过 ")+Path.GetFileName(f)+"："+L.Error(reason));continue;}
     var meta=RafHeader.Read(f);var photo=new Photo{Input=f,Model=(string)meta["model"],Backend=(string)meta["backend"],NativeModule=(string)meta["nativeModule"],Relative=directory?f.Substring(root.Length):Path.GetFileName(f)};var item=new ListViewItem(Path.GetFileName(f),"pending"){Tag=photo,Checked=true,ToolTipText=f};photo.Item=item;inputs.Items.Add(item);QueueThumbnail(photo);
     added++;
    }
   }}catch(Exception e){MessageBox.Show(this,L.Error(e.Message),L.T("添加照片"));}finally{inputs.EndUpdate();UpdateSelection();if(rejected>0)status.Text=L.F("已添加 {0} 张，跳过 {1} 张不支持或无效的 RAW",added,rejected);}
  }
  void QueueThumbnail(Photo photo){lock(thumbnailQueue){thumbnailQueue.Enqueue(photo);if(thumbnailRunning)return;thumbnailRunning=true;}System.Threading.ThreadPool.QueueUserWorkItem(delegate{while(true){Photo p;lock(thumbnailQueue){if(thumbnailQueue.Count==0){thumbnailRunning=false;return;}p=thumbnailQueue.Dequeue();}Bitmap thumb=null;string error=null;try{using(var image=PreviewReader.Read(p.Input)){thumb=new Bitmap(thumbnails.ImageSize.Width,thumbnails.ImageSize.Height);using(var g=Graphics.FromImage(thumb)){g.Clear(Color.FromArgb(35,37,39));g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;float ratio=Math.Min((float)thumb.Width/image.Bitmap.Width,(float)thumb.Height/image.Bitmap.Height);float w=image.Bitmap.Width*ratio,h=image.Bitmap.Height*ratio;g.DrawImage(image.Bitmap,(thumb.Width-w)/2,(thumb.Height-h)/2,w,h);}}}catch(Exception e){error=L.Error(e.Message);}
    Bitmap ready=thumb;string failure=error;
    if(IsDisposed||!IsHandleCreated){if(ready!=null)ready.Dispose();continue;}
    try{BeginInvoke((Action)delegate{try{if(p.Item.ListView!=inputs)return;if(ready!=null){thumbnails.Images.Add(p.Key,ready);p.Item.ImageKey=p.Key;}else p.Item.ToolTipText=p.Input+L.T("\n预览不可用：")+L.Error(failure);}finally{if(ready!=null)ready.Dispose();}});}catch(InvalidOperationException){if(ready!=null)ready.Dispose();}
   }});}
  Photo Current(){return inputs.SelectedItems.Count>0?(Photo)inputs.SelectedItems[0].Tag:inputs.Items.Count>0?(Photo)inputs.Items[0].Tag:null;}
  void BrowseInput(){try{var p=Current();if(p==null)return;new ViewerForm(inputs.Items.Cast<ListViewItem>().Select(i=>((Photo)i.Tag).Input),p.Input).Show(this);}catch(Exception e){MessageBox.Show(this,L.Error(e.Message),L.T("无法预览"));}}
  string Result(Photo p){return PhotoFiles.Match(p.Input,p.Relative,sameFolder.Checked?null:output.Text,p.Output);}
  void BrowseOutput(){try{var catalog=new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase);var files=inputs.Items.Cast<ListViewItem>().SelectMany(i=>{var p=(Photo)i.Tag;return PhotoFiles.MatchAll(p.Input,p.Relative,sameFolder.Checked?null:output.Text,p.Output,catalog);}).Distinct(StringComparer.OrdinalIgnoreCase).ToList();if(files.Count>0){var p=Current();string selected=p==null?null:Result(p);new ViewerForm(files,selected??files[0],"转换结果").Show(this);status.Text=L.T("已匹配 ")+files.Count+" / "+inputs.Items.Count;}else status.Text=L.T("未找到转换结果");}catch(Exception e){MessageBox.Show(this,L.Error(e.Message));}}
  void CompareSelected(){try{var p=Current();if(p==null)return;var catalog=new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase);var pairs=inputs.Items.Cast<ListViewItem>().Select(i=>{var photo=(Photo)i.Tag;return new PhotoPair{Raw=photo.Input,Results=PhotoFiles.MatchAll(photo.Input,photo.Relative,sameFolder.Checked?null:output.Text,photo.Output,catalog)};}).ToList();new CompareForm(pairs,p.Input).Show(this);}catch(Exception e){MessageBox.Show(this,L.Error(e.Message),L.T("无法对照"));}}
  void OpenOutputFolder(){try{var p=Current();string folder=sameFolder.Checked?(p==null?null:Path.GetDirectoryName(p.Input)):output.Text;if(string.IsNullOrWhiteSpace(folder))return;folder=Path.GetFullPath(folder);Directory.CreateDirectory(folder);Process.Start(new ProcessStartInfo(folder){UseShellExecute=true,Verb="open"});}catch(Exception e){MessageBox.Show(this,L.Error(e.Message),L.T("无法打开文件夹"));}}
  void Line(string text){log.AppendText(text+Environment.NewLine);}
  void OnUi(Action action){if(!IsDisposed&&IsHandleCreated)BeginInvoke(action);}
  void Busy(bool busy){actions.Enabled=settings.Enabled=inputs.Enabled=language.Enabled=!busy;start.Enabled=!busy;cancel.Enabled=busy;}
  void CancelBatch(){if(process==null)return;try{File.WriteAllText(cancelPath,"cancel");status.Text=L.T("正在取消…");cancel.Enabled=false;}catch(Exception e){Line(L.Error(e.Message));}}
  void StartBatch(){
   var selected=inputs.CheckedItems.Cast<ListViewItem>().Select(i=>(Photo)i.Tag).ToList();
   if(selected.Count==0){MessageBox.Show(this,L.T("请勾选要转换的照片。"));return;}if(!sameFolder.Checked&&string.IsNullOrWhiteSpace(output.Text)){MessageBox.Show(this,L.T("请选择输出文件夹。"));return;}
   SaveOutput();
   string exe=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"s5raw.exe");
   controlFolder=Path.Combine(Path.GetTempPath(),"s5raw-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(controlFolder);cancelPath=Path.Combine(controlFolder,"cancel");string jobs=Path.Combine(controlFolder,"jobs.json");
   var rows=selected.Select(p=>new {input=p.Input,relative=p.Relative,film=FilmFor(p),colorSpace=UsesColorSpace(p)?(color.SelectedIndex==0?"srgb":"adobe-rgb"):null}).ToArray();File.WriteAllText(jobs,new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(rows),new UTF8Encoding(false));
   var args=new List<string>{"convert","--json","--job-list",jobs,"--existing",new[]{"rename","skip","error","overwrite"}[existing.SelectedIndex],"--cancel-file",cancelPath,"--jobs",parallel.Value.ToString()};
   if(!sameFolder.Checked)args.AddRange(new[]{"--output-dir",output.Text});
   if(format.SelectedIndex==2){args.AddRange(new[]{"--format","jpeg","--quality",quality.Value.ToString()});}else{args.AddRange(new[]{"--bits",format.SelectedIndex==0?"16":"8"});}
   fileProgress=new int[selected.Count];finishedFiles=0;parallelState=null;parallelReason=null;RenderParallel();log.Clear();Line(L.English?"Requested parallel tasks: "+parallel.Value:"设置并行数："+parallel.Value);progress.Value=0;Busy(true);status.Text=L.T("准备转换…");
   var psi=new ProcessStartInfo(exe,Shared.Arguments(args.ToArray())){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};process=new Process{StartInfo=psi};var current=process;
   current.OutputDataReceived+=delegate(object s,DataReceivedEventArgs e){if(e.Data!=null)OnUi(delegate{HandleEvent(e.Data);});};current.ErrorDataReceived+=delegate(object s,DataReceivedEventArgs e){if(e.Data!=null)OnUi(delegate{Line(e.Data);});};
   try{current.Start();current.BeginOutputReadLine();current.BeginErrorReadLine();System.Threading.ThreadPool.QueueUserWorkItem(delegate{current.WaitForExit();int code=current.ExitCode;OnUi(delegate{current.Dispose();process=null;Busy(false);CleanupControls();if(code!=0&&code!=130)status.Text=L.T("转换结束，存在错误，请查看记录。");else if(code==130)status.Text=L.T("已取消");if(closeWhenDone)Close();});});}catch(Exception e){current.Dispose();process=null;Busy(false);CleanupControls();Line(L.Error(e.Message));status.Text=L.T("启动失败");}
  }
  void RenderParallel(){
   if(parallelState==null){parallelStatus.Text="";parallelStatus.Visible=false;return;}
   string reason=Convert.ToString(parallelState["reason"]);
   bool pressure=reason=="memory_budget";
   parallelStatus.Visible=pressure;
   parallelStatus.Text=pressure?(L.English?"Memory pressure: current parallel tasks = ":"因内存压力，当前并行任务数为 ")+parallelState["active"]:"";
  }
  void CleanupControls(){try{Shared.DeleteOwnedDirectory(controlFolder);}catch(Exception e){Line(L.T("临时任务清理：")+L.Error(e.Message));}}
  void UpdateProgress(Dictionary<string,object> row,int value){if(fileProgress==null)return;int i=Convert.ToInt32(row["index"])-1;fileProgress[i]=Math.Max(fileProgress[i],value);progress.Value=fileProgress.Sum()/fileProgress.Length;}
  void HandleEvent(string line){try{var r=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(line);string type=Convert.ToString(r["type"]);
   if(type=="parallel_status"){parallelState=r;RenderParallel();string reason=Convert.ToString(r["reason"]);if(reason!=parallelReason){parallelReason=reason;if(parallelStatus.Visible)Line(parallelStatus.Text);}}
   else if(type=="file_started"){status.Text=L.T("正在转换 · 已完成 ")+finishedFiles+" / "+r["total"];Line(L.T("转换：")+r["input"]);}
   else if(type=="progress"){UpdateProgress(r,Math.Min(99,Convert.ToInt32(r["percent"])));}
   else if(type=="completed"){UpdateProgress(r,100);finishedFiles++;status.Text=L.T("正在转换 · 已完成 ")+finishedFiles+" / "+r["total"];string source=Convert.ToString(r["input"]),target=Convert.ToString(r["output"]);var p=inputs.Items.Cast<ListViewItem>().Select(i=>(Photo)i.Tag).FirstOrDefault(x=>x.Input.Equals(source,StringComparison.OrdinalIgnoreCase));if(p!=null){p.Output=target;p.Item.ToolTipText=p.Input+L.T("\n已转换：")+target;}Line(L.T("完成：")+target);}
   else if(type=="skipped"){UpdateProgress(r,100);finishedFiles++;Line(L.T("跳过：")+r["output"]);}
   else if(type=="error"||type=="file_error"||type=="cleanup_warning"){if(type=="file_error"){UpdateProgress(r,100);finishedFiles++;}Line(L.T("错误：")+(r.ContainsKey("input")?r["input"]+" — ":"")+L.Error(Convert.ToString(r["message"])));}
   else if(type=="summary"){status.Text=L.F("完成 {0}，跳过 {1}，失败 {2}",r["completed"],r["skipped"],r["failed"])+(Convert.ToBoolean(r["cancelled"])?L.T("，已取消"):"");if(!Convert.ToBoolean(r["cancelled"]))progress.Value=100;Line(status.Text);}
  }catch{Line(line);}}
 }
 static class Gui {
  [STAThread] static void Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.ApplicationExit+=delegate{FullPreview.Cleanup();};if(args.Length>1&&args[0]=="--view"){string p=Path.GetFullPath(args[1]);if(!Directory.Exists(p)&&!PreviewReader.Supported(p)){string reason;if(Path.GetExtension(p).Equals(".raf",StringComparison.OrdinalIgnoreCase))RafHeader.Accept(p,out reason);else reason=L.T("不支持此文件类型");MessageBox.Show(Path.GetFileName(p)+"\n"+L.Error(reason),L.T("无法打开"));return;}Application.Run(new ViewerForm(ViewerForm.InFolder(Directory.Exists(p)?p:Path.GetDirectoryName(p),false),p));return;}var window=new MainForm();window.Shown+=delegate{window.AddInputs(args);};Application.Run(window);}
 }
}

