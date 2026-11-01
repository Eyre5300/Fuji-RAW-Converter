using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace S5Raw {
 sealed class ImageView {
  public Bitmap Picture,Overview;public float Zoom=1,PanX,PanY;public bool Fits=true;public Size Size;
  public void SetImage(Bitmap full,Bitmap overview,bool preserve){Picture=full;Overview=overview;if(!preserve){Fits=true;PanX=PanY=0;}Refit();}
  public void Resize(Size size){Size=size;Refit();}
  public void Refit(){if(Fits&&Picture!=null)Zoom=Math.Min((float)Math.Max(1,Size.Width-20)/Picture.Width,(float)Math.Max(1,Size.Height-20)/Picture.Height);}
  public void Fit(){Fits=true;PanX=PanY=0;Refit();}
  public void Actual(){Fits=false;Zoom=1;PanX=PanY=0;}
  public void Move(int dx,int dy){Fits=false;PanX+=dx;PanY+=dy;}
  public void Scale(float factor,Point anchor){if(Picture==null)return;float next=Math.Max(.03f,Math.Min(8,Zoom*factor)),ratio=next/Zoom;PanX=(anchor.X-Size.Width/2f)*(1-ratio)+PanX*ratio;PanY=(anchor.Y-Size.Height/2f)*(1-ratio)+PanY*ratio;Zoom=next;Fits=false;}
  public void Match(ImageView other){if(Picture==null||other.Picture==null)return;Fits=other.Fits;Zoom=other.Zoom*other.Picture.Width/Picture.Width;PanX=other.PanX;PanY=other.PanY;if(Fits)Refit();}
  public void Draw(Graphics graphics,Rectangle pane,bool moving){
   if(Picture==null)return;float w=Picture.Width*Zoom,h=Picture.Height*Zoom;
   var dest=new RectangleF(pane.X+(pane.Width-w)/2+PanX,pane.Y+(pane.Height-h)/2+PanY,w,h);
   var visible=RectangleF.Intersect(dest,pane);if(visible.Width<=0||visible.Height<=0)return;
   Bitmap image=Overview!=null&&w<=Overview.Width?Overview:Picture;
   var source=new RectangleF((visible.X-dest.X)/w*image.Width,(visible.Y-dest.Y)/h*image.Height,visible.Width/w*image.Width,visible.Height/h*image.Height);
   graphics.InterpolationMode=Zoom>=1?InterpolationMode.NearestNeighbor:moving?InterpolationMode.Bilinear:InterpolationMode.HighQualityBicubic;
   graphics.PixelOffsetMode=PixelOffsetMode.Half;graphics.DrawImage(image,visible,source,GraphicsUnit.Pixel);
  }
 }
 sealed class ImageCanvas:Control {
  readonly ImageView view=new ImageView();bool dragging;Point last;
  public event Action ZoomChanged,ViewChanged;
  public ImageCanvas(){DoubleBuffered=true;BackColor=Color.FromArgb(25,27,29);TabStop=true;}
  public float Zoom {get{return view.Zoom;}}
  public void SetImage(Bitmap image){view.SetImage(image,null,false);Changed();}
  public void SetPreview(PreviewImage image){view.SetImage(image==null?null:image.Bitmap,image==null?null:image.Overview,false);Changed();}
  public void Fit(){view.Fit();Changed();}
  public void Actual(){view.Actual();Changed();}
  public void ScaleBy(float factor){view.Scale(factor,new Point(Width/2,Height/2));Changed();}
  public void MatchView(ImageCanvas other){view.Match(other.view);Invalidate();if(ZoomChanged!=null)ZoomChanged();}
  void Changed(){if(ZoomChanged!=null)ZoomChanged();if(ViewChanged!=null)ViewChanged();Invalidate();}
  protected override void OnResize(EventArgs e){base.OnResize(e);view.Resize(ClientSize);Changed();}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);view.Draw(e.Graphics,ClientRectangle,dragging);}
  protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);view.Scale(e.Delta>0?1.25f:.8f,e.Location);Changed();}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(e.Button==MouseButtons.Left){dragging=true;last=e.Location;Capture=true;Cursor=Cursors.SizeAll;}}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging){view.Move(e.X-last.X,e.Y-last.Y);last=e.Location;Changed();}}
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;Cursor=Cursors.Default;Invalidate();}
  protected override void OnDoubleClick(EventArgs e){base.OnDoubleClick(e);if(view.Fits)Actual();else Fit();}
 }
 // Both panes are composed into one double-buffered frame and presented together.
 sealed class CompareCanvas:Control {
  readonly ImageView left=new ImageView(),right=new ImageView();bool dragging,linked=true;int active=1;Point last;
  public event Action ZoomChanged;
  public CompareCanvas(){DoubleBuffered=true;BackColor=Color.FromArgb(25,27,29);TabStop=true;}
  Rectangle LeftPane {get{return new Rectangle(0,0,Width/2-1,Height);}}
  Rectangle RightPane {get{return new Rectangle(Width/2+1,0,Width-Width/2-1,Height);}}
  ImageView Active {get{return active==0?left:right;}}
  public bool Linked {get{return linked;}set{linked=value;if(linked)right.Match(left);Changed();}}
  public float Zoom {get{return right.Zoom;}}
  public void SetBefore(PreviewImage image){left.SetImage(image==null?null:image.Bitmap,image==null?null:image.Overview,false);if(linked&&right.Picture!=null)left.Match(right);Changed();}
  public void SetAfter(PreviewImage image,bool preserve){right.SetImage(image==null?null:image.Bitmap,image==null?null:image.Overview,preserve);if(linked&&left.Picture!=null)right.Match(left);Changed();}
  public void Clear(){left.SetImage(null,null,false);right.SetImage(null,null,false);Changed();}
  public void Fit(){left.Fit();right.Fit();Changed();}
  public void Actual(){left.Actual();right.Actual();Changed();}
  public void ScaleBy(float factor){right.Scale(factor,new Point(right.Size.Width/2,right.Size.Height/2));if(linked)left.Match(right);Changed();}
  void SyncActive(){if(linked){if(active==0)right.Match(left);else left.Match(right);}Changed();}
  void Changed(){if(ZoomChanged!=null)ZoomChanged();Invalidate();}
  protected override void OnResize(EventArgs e){base.OnResize(e);left.Resize(LeftPane.Size);right.Resize(RightPane.Size);Changed();}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);left.Draw(e.Graphics,LeftPane,dragging);right.Draw(e.Graphics,RightPane,dragging);using(var pen=new Pen(Color.FromArgb(210,214,216),2))e.Graphics.DrawLine(pen,Width/2,0,Width/2,Height);}
  protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);active=e.X<Width/2?0:1;var pane=active==0?LeftPane:RightPane;Active.Scale(e.Delta>0?1.25f:.8f,new Point(e.X-pane.X,e.Y));SyncActive();}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(e.Button==MouseButtons.Left){active=e.X<Width/2?0:1;dragging=true;last=e.Location;Capture=true;Cursor=Cursors.SizeAll;}}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging){Active.Move(e.X-last.X,e.Y-last.Y);last=e.Location;SyncActive();}}
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;Cursor=Cursors.Default;Invalidate();}
  protected override void OnDoubleClick(EventArgs e){base.OnDoubleClick(e);if(Active.Fits)Actual();else Fit();}
 }
}
