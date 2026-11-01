using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace S5Raw {
 static class L {
  internal static bool English=Preferences.Get("Language","zh-CN")=="en";
  internal static event Action Changed;
  static readonly Dictionary<string,string> words=new Dictionary<string,string>{
   {"源文件夹","Source folder"},
   {"包含子文件夹","Include subfolders"},
   {"JPEG 质量","JPEG quality"},
   {"添加照片","Add photos"},
   {"富士 RAW (*.RAF)|*.RAF","Fujifilm RAW (*.RAF)|*.RAF"},
   {"添加文件夹","Add folder"},
   {"全选","Select all"},
   {"全不选","Deselect all"},
   {"移除","Remove"},
   {"清空","Clear"},
   {"0 张","0 photos"},
   {"输出位置","Output folder"},
   {"浏览…","Browse…"},
   {"输出格式","Format"},
   {"TIFF · 16 位","TIFF · 16-bit"},
   {"TIFF · 8 位","TIFF · 8-bit"},
   {"同名文件","If file exists"},
   {"自动加序号","Add a number"},
   {"跳过","Skip"},
   {"报错","Report error"},
   {"覆盖","Overwrite"},
   {"并行数","Workers"},
   {"胶片模拟","Film simulation"},
   {"标准","Standard"},
   {"转换选中项 (0)","Convert selected (0)"},
   {"取消","Cancel"},
   {"预览","Preview"},
   {"对照","Compare"},
   {"浏览转换结果","Browse results"},
   {"打开输出文件夹","Open output folder"},
   {"日志","Log"},
   {"就绪","Ready"},
   {"显示 DPI：","Display DPI: "},
   {"；界面：","; window: "},
   {"共 {0} 张，已勾选 {1} 张","{0} photos · {1} selected"},
   {"转换选中项 ({0})","Convert selected ({0})"},
   {"已添加 {0} 张，跳过 {1} 张不支持或无效的 RAW","Added {0}; skipped {1} unsupported or invalid RAW files"},
   {"转换 {0} 张，并行 {1}","Converting {0} photos with {1} workers"},
   {"完成 {0}，跳过 {1}，失败 {2}","Completed {0}, skipped {1}, failed {2}"},
   {"跳过 ","Skipped "},
   {"\n预览不可用：","\nPreview unavailable: "},
   {"无法预览","Cannot preview"},
   {"转换结果","Converted images"},
   {"已匹配 ","Matched "},
   {"未找到转换结果","No converted image found"},
   {"无法对照","Cannot compare"},
   {"无法打开文件夹","Cannot open folder"},
   {"正在取消…","Cancelling…"},
   {"请勾选要转换的照片。","Select photos to convert."},
   {"请选择输出文件夹。","Choose an output folder."},
   {"准备转换…","Preparing…"},
   {"转换结束，存在错误，请查看记录。","Conversion finished with errors. See the log."},
   {"已取消","Cancelled"},
   {"启动失败","Could not start conversion"},
   {"临时任务清理：","Temporary file cleanup: "},
   {"正在转换 · 已完成 ","Converting · Completed "},
   {"转换：","Converting: "},
   {"\n已转换：","\nConverted: "},
   {"完成：","Completed: "},
   {"跳过：","Skipped: "},
   {"错误：","Error: "},
   {"，已取消",", cancelled"},
   {"不支持此文件类型","Unsupported file type"},
   {"无法打开","Cannot open"},
   {"不是有效的 RAF 文件","Not a valid RAF file"},
   {"此 RAF 没有可用的内嵌预览，请先转换 TIFF","This RAF has no embedded preview. Convert it to TIFF first."},
   {"图片浏览器","Image browser"},
   {"Fuji S3/S5 Pro RAW Converter · 图片浏览器","Fuji S3/S5 Pro RAW Converter · Image browser"},
   {"上一张 ←","Previous ←"},
   {"下一张 →","Next →"},
   {"适应窗口","Fit to window"},
   {"原始大小","Actual size"},
   {"系统打开","Open externally"},
   {"加载中…","Loading…"},
   {"无法预览：","Cannot preview: "},
   {" · 未匹配"," · No match"},
   {"同步","Linked"},
   {"Fuji S3/S5 Pro RAW Converter · 对照","Fuji S3/S5 Pro RAW Converter · Compare"},
   {"上一组","Previous"},
   {"下一组","Next"},
   {"缩放 ","Zoom "},
   {"Fuji S3/S5 Pro RAW Converter · 对照 — ","Fuji S3/S5 Pro RAW Converter · Compare — "},
   {" · 加载中…"," · Loading…"},
   {"RAW 预览解码超时","RAW preview timed out"},
   {" 秒"," s"},
   {"语言 / Language","语言 / Language"},
   {"不是有效的富士 RAF 文件","Not a valid Fujifilm RAF file"},
   {"RAF 预览范围无效","Invalid RAF preview range"},
   {"RAF 预览数据无效","Invalid RAF preview data"},
   {"当前原厂内核不支持：","Unsupported by the installed Fujifilm engine: "},
   {"原厂胶片配置无效：","Invalid Fujifilm film profile: "},
   {"此照片的原厂内核不支持 ","This photo’s Fujifilm engine does not support "},
   {"缺少原厂内核：","Fujifilm engine not found: "},
   {"输出文件已存在或批量名称冲突：","Output already exists or conflicts with another file: "},
   {"输出路径是文件夹：","Output path is a folder: "},
   {"原厂转换内核 ","Fujifilm conversion engine "},
   {"无法启动 Fuji S3/S5 Pro RAW Converter：","Cannot start Fuji S3/S5 Pro RAW Converter: "}
  };
  internal static string T(string text){string found;return English&&words.TryGetValue(text,out found)?found:text;}
  internal static string F(string text,params object[] values){return string.Format(T(text),values);}
  static string Current(string text){if(English)return T(text);foreach(var pair in words)if(pair.Value==text)return pair.Key;return text;}
  internal static string Error(string text){
   if(!English||string.IsNullOrEmpty(text))return text;
   foreach(var pair in words.OrderByDescending(p=>p.Key.Length))if(text.StartsWith(pair.Key,StringComparison.Ordinal))return pair.Value+text.Substring(pair.Key.Length);
   return text;
  }
  internal static void SetEnglish(bool english){
   if(English==english)return;English=english;Preferences.Set("Language",english?"en":"zh-CN");if(Changed!=null)Changed();
  }
  internal static void Apply(Control control){
   if(control is Button||control is Label||control is CheckBox)control.Text=Current(control.Text);
   var combo=control as ComboBox;
   if(combo!=null)for(int i=0;i<combo.Items.Count;i++)if(combo.Items[i] is string){string value=(string)combo.Items[i],translated=Current(value);if(value!=translated)combo.Items[i]=translated;}
   var strip=control as ToolStrip;if(strip!=null)foreach(ToolStripItem item in strip.Items)item.Text=Current(item.Text);
   foreach(Control child in control.Controls)Apply(child);
   if(control is Button||control is CheckBox){
    float dpi;using(var g=control.CreateGraphics())dpi=g.DpiX/96f;
    int width=TextRenderer.MeasureText(control.Text,control.Font).Width+(int)(24*dpi);
    control.MinimumSize=new Size(width,control.MinimumSize.Height);
    if(control is CheckBox&&!control.AutoSize)control.Width=Math.Max(control.Width,width);
   }
  }
 }
}
