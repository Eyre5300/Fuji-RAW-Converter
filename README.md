# Fuji S3/S5 Pro RAW Converter

将受支持的富士 RAF 照片批量转换为 JPEG 或 TIFF，按机型选择胶片模拟，并浏览、对照转换结果。

[下载 1.7.7 Windows 完整便携包](https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases/tag/v1.7.7) · [English](README.en.md)

## 开始使用

需要 64 位 Windows 和 .NET Framework 4.8。完整解压便携包，双击根目录的 `Fuji-S3-S5-RAW-Converter.exe`，保留旁边的 `data` 文件夹。无需另行安装官方转换软件，也无需连接相机。

1. 添加 RAF 照片或文件夹，勾选要转换的照片。
2. 选择输出位置、JPEG 或 TIFF，以及适用的色彩空间。
3. 在各机型对应的选框中选择胶片。不同机型可以分别设置；“帮助”可查看当前机型每种模式的说明。
4. 点击“转换选中项”。完成后可浏览结果，或使用“对照”比较原片与输出。

默认输出到源文件夹，同名文件自动加序号；也可选择跳过、报错或覆盖。原始 RAF 不会被修改。界面支持简体中文和 English。

## 支持的机型与胶片

机型范围按 **1.7.7 内置官方转换内核的支持清单**列出；胶片栏说明本软件当前可选择的模式及色彩空间条件。

| 机型 | 可选胶片 | 使用条件 |
|---|---|---|
| FinePix S5 Pro | 标准、F1、F1a、F1b、F1c、F2 | 胶片模式支持 sRGB 和 Adobe RGB |
| FinePix S3 Pro | 标准、F1、F2 | F1、F2 仅用于 sRGB；Adobe RGB 下仅提供标准 |
| X-T50 | 默认 / 拍摄设置，以及下列 20 种胶片模拟 | 支持 sRGB 和 Adobe RGB；JPEG 使用 sRGB |

**X-T50 的 20 种胶片模拟：**

- 彩色：PROVIA / 标准、Velvia / 鲜艳、ASTIA / 柔和、Classic Chrome、PRO Neg. Hi、PRO Neg. Std、Classic Neg.、ETERNA、ETERNA Bleach Bypass、Nostalgic Neg.、REALA ACE。
- 黑白：ACROS、ACROS + Ye、ACROS + R、ACROS + G、黑白、黑白 + Ye、黑白 + R、黑白 + G。
- 棕褐单色：棕褐色（Sepia）。

**以下各机型均提供默认转换，目前没有额外胶片选项：**

| 系列 | 支持机型 | 可选胶片 |
|---|---|---|
| X-T | X-T1、X-T2、X-T3、X-T4、X-T5、X-T10、X-T20、X-T30、X-T30 II、X-T100、X-T200 | 默认转换；无额外胶片选项 |
| X-H / X-Pro | X-H1、X-H2、X-H2S、X-Pro1、X-Pro2、X-Pro3 | 默认转换；无额外胶片选项 |
| X-E / X-M / X-S | X-E1、X-E2、X-E2S、X-E3、X-E4、X-E5、X-M1、X-M5、X-S1、X-S10、X-S20 | 默认转换；无额外胶片选项 |
| X-A | X-A1、X-A2、X-A3、X-A5、X-A7、X-A10、X-A20 | 默认转换；无额外胶片选项 |
| X100 | FinePix X100、X100S、X100T、X100F、X100V、X100VI | 默认转换；无额外胶片选项 |
| X 紧凑型 | X10、X20、X30、X70、XF1、XF10 | 默认转换；无额外胶片选项 |
| GFX | GFX 50R、GFX 50S、GFX50S II、GFX 100、GFX100S、GFX100 II、GFX100S II、GFX100RF | 默认转换；无额外胶片选项 |
| FinePix E / F | E550、E900、F700、F710、F505EXR、F550EXR、F600EXR、F605EXR、F770EXR、F775EXR、F800EXR、F805EXR、F900EXR | 默认转换；无额外胶片选项 |
| FinePix HS | HS10、HS11、HS20EXR、HS22EXR、HS30EXR、HS33EXR、HS35EXR、HS50EXR、HS55EXR | 默认转换；无额外胶片选项 |
| FinePix S / SL | S1、S2 Pro、S20 Pro、S100FS、S200EXR、S5000、S5100、S5200、S5500、S5600、S6000fd、S6500fd、S7000、S9000、S9500、S9600、SL1000、SL1010 | 默认转换；无额外胶片选项 |

支持范围以本版本列表为准；其他品牌 RAW 不支持。导入后显示的胶片选项会随识别到的文件及色彩空间匹配，不提供跨机型套用其他相机胶片的功能。

## 输出格式与色彩

| 输出 | 位深与色彩 | 适用场景 |
|---|---|---|
| JPEG | 8 位，sRGB；质量可调，默认 92 | 分享、查看和一般成片 |
| TIFF · 8 位 | 8 位/通道 RGB；可用时选择 sRGB 或 Adobe RGB | 兼顾文件大小和后期编辑 |
| TIFF · 16 位 | 原生 16 位/通道 RGB；sRGB 或 Adobe RGB | 需要较大色调调整空间的后期处理 |

部分早期 FinePix 机型在“标准 + 8 位 TIFF”组合下使用照片的相机色彩空间，不能另行指定。不可用的色彩选项会在界面中禁用。输出采用相应机型支持的完整图像尺寸。

- ACROS 和“黑白”系列输出黑白照片；Sepia 输出棕褐单色。希望得到彩色照片时，请选择支持的彩色模式。
- “默认 / 拍摄设置”可能保留拍摄时的黑白风格。
- 16 位 RGB TIFF 通常明显大于 RAF：每像素需要 6 字节保存 RGB 数据，另有文件元数据；这属于正常现象。

## 浏览、对照与批量处理

- 双击照片可浏览；“对照”可匹配导出的 TIFF/JPEG，并切换结果。
- 主界面 RAW 缩略图使用照片内嵌预览，不会随着胶片选择即时更新；胶片效果请看导出文件。
- 批量转换以你设置的并行数为上限，根据实际内存占用自动增减任务。内存预算约为 90%，并非强制占满内存；不会改写设置值。
- 只有因内存预算下调并行数时，才显示内存压力与当前任务数。首张测量和估算在后台进行。
- 不读取官方转换软件另存的编辑旁车文件；软件输出不保证与相机直出 JPEG 完全一致。

## 下载与许可

[1.7.7 发布页与 SHA-256 校验文件](https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases/tag/v1.7.7)。下载完整 Windows ZIP 即可使用，无需下载源码或自行编译。

本软件不是富士官方产品。第三方组件保留其原有许可，详见包内 `data/NOTICE.txt`。开发、命令行和技术资料见 [开发者文档](docs/DEVELOPMENT.md)。
