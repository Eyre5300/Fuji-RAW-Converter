# Fuji S3/S5 Pro RAW Converter

**用富士原厂转换内核处理老款 RAF 的 Windows 桌面工具与命令行程序。**

[English](README.en.md) · [下载便携版](https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases/latest) · [版本记录](CHANGELOG.md)

Fuji S3/S5 Pro RAW Converter 从 FinePix S5 Pro 的日常选片与批量转换需求出发，把 Hyper-Utility3 的 RAW 浏览、LE 转换和高级 16 位转换组件封装成一个小工具。可以直接使用 GUI，也可以像调用 FFmpeg 一样从其他程序启动命令行进程。

原项目名为 S5Raw；命令行程序仍为 `s5raw.exe`，设置与预览缓存沿用原位置。

## 下载与开始使用

在 [Releases](https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases) 下载 `Fuji-S3-S5-RAW-Converter-1.6.2-Windows.zip`，完整解压后打开 `Fuji-S3-S5-RAW-Converter.exe`。

```text
Fuji-S3-S5-RAW-Converter/
├─ Fuji-S3-S5-RAW-Converter.exe
├─ 使用说明.txt
└─ data/
   ├─ s5raw.exe
   ├─ Fuji-S3-S5-RAW-Converter.exe
   └─ engine/
```

1. 添加 RAF 照片或文件夹，勾选要转换的照片。
2. 选择 TIFF / JPEG、位深、胶片模拟和输出位置。
3. 点击「转换选中项」。双击照片进入浏览器，「对照」自动查找对应结果。

界面支持简体中文和 English，语言选项位于顶栏。语言、上次打开位置、输出位置和源文件夹输出选项会自动保存。

运行环境为 Windows 与 .NET Framework 4.8；目前在 Windows 11 上验证。发布包带有所需转换组件，使用 Fuji S3/S5 Pro RAW Converter 不需要启动 Hyper-Utility3。尚未在全新 Windows 环境验证所有依赖。底层富士组件为 32 位，由独立子进程调用。

## 功能

- RAF 缩略图、勾选、图片浏览与照片参数显示。
- 只转换勾选项；批量转换默认并行数为 CPU 逻辑核心数，可手动调整。
- 完整尺寸 JPEG，以及原厂 8 位 / 原生 16 位 TIFF。
- 原厂胶片模拟：标准、F1、F1a、F1b、F1c、F2，实际选项随机型和色彩空间变化。
- RAW 与转换结果按文件夹、文件名自动匹配；可切换 TIFF / JPEG 结果，联动缩放和平移。
- 竖拍 TIFF 像素方向归一化，减少文件管理器忽略方向标签造成的横向预览。
- 默认输出到源文件夹；GUI 同名文件默认加序号。
- 命令行与 UTF-8 JSON Lines 事件，便于接入其他软件。

## 输出格式与胶片模拟

| 输出 | 转换流程 | 色彩空间 |
| --- | --- | --- |
| 标准 8 位 TIFF | 原厂 LE 转换 | 沿用 LE 的相机色彩设置 |
| 胶片模拟 8 位 TIFF | 原厂高级转换 | sRGB / Adobe RGB |
| 16 位 TIFF | 原厂原生 16 位转换 | sRGB / Adobe RGB |
| JPEG | 完整 16 位 sRGB 渲染，再编码为 8 位 JPEG | sRGB，质量 1–100，默认 92 |

胶片模拟可用于 JPEG 和 TIFF，写入导出的实际像素；16 位 TIFF 保持每通道 16 位。它调用富士原厂配置，不是另加一层滤镜。界面只显示已勾选照片共同支持的模式。

导出设置不会改变左侧 RAW 浏览图。相机 EXIF / MakerNote 中的原始拍摄设置保留；本次导出模式另记在 `Software` 标签和 JSON 的 `completed.film` 中。转换结果不承诺与相机直出 JPEG 完全一致。

## RAW 预览是什么

GUI 调用富士 `PIR030RAF` 浏览插件，沿用原厂浏览方式，不先转换 TIFF 来充当 RAW 预览。S5 Pro 样片显示的是 RAF 内嵌的相机 JPEG（1440×960）；完整 RAW 导出样片为 4256×2848。因此左右对照用于观察相机预览与渲染结果，不能把左图视为同分辨率的完整 RAW 解码。

预览采用后台加载、相邻图片预取、共享加载任务、内存及磁盘缓存。第一次读取大文件或缓存未命中仍可能较慢。带 ICC 的结果在界面中映射到 sRGB；显示缓冲为 8 位，不提供显示器软打样。

## 支持哪些相机

入口按 RAF 头中的处理器代码匹配打包的原厂模块，**没有写死只支持 S5 Pro**。没有对应模块的 RAF 会被拒绝导入。胶片模拟还会检查对应 FCC 配置。

已核对 RAF 文件头与模块的机型包括：S2 Pro、S3 Pro、S5 Pro、F700、E550、E900、S5000、S5500、S5200/S5600、S6000fd/S6500fd、S9500、S9600。S3 Pro 曾完成单张原生 16 位 TIFF 转换检查，日常及本版验证以 S5 Pro 为主；「能匹配模块」不等于所有机型和拍摄模式均已逐一测试。遇到问题请附相机型号、处理器代码（`info --json`）、输出设置和错误信息。

## 命令行

在解压后的 `Fuji S3/S5 Pro RAW Converter` 目录打开 PowerShell：

```powershell
# 原生 16 位 TIFF
.\data\s5raw.exe convert -i "D:\Photos\photo.RAF" -o "D:\Output\photo.tif" --bits 16

# 完整尺寸 JPEG，F1b 胶片模拟
.\data\s5raw.exe convert -i "D:\Photos\photo.RAF" -o "D:\Output\photo.jpg" --film f1b

# Adobe RGB 16 位 TIFF，F2 胶片模拟
.\data\s5raw.exe convert -i "D:\Photos\photo.RAF" -o "D:\Output\photo.tif" --bits 16 --film f2 --color-space adobe-rgb

# 批量转换，保留相对目录，遇到同名文件加序号
.\data\s5raw.exe convert -i "D:\Photos" --output-dir "D:\Output" --recursive --format jpeg --film f1b --existing rename --json

# 文件信息、可选模式、内核诊断
.\data\s5raw.exe info -i "D:\Photos\photo.RAF" --json
.\data\s5raw.exe doctor --json
.\data\s5raw.exe --help
```

`--film` 可选 `standard|f1|f1a|f1b|f1c|f2`。`--jobs N` 调整并行数，默认 CPU 逻辑核心数。CLI 默认 TIFF、8 位、标准模式、同名报错；GUI 的初始输出为 16 位 TIFF、同名加序号。未指定输出目录时，写入各源文件夹。

### 其他软件调用

启动 `data/s5raw.exe` 子进程并传入 `convert ... --json`。stdout 每行是一条 UTF-8 JSON；事件包括 `batch_started`、`file_started`、`progress`、`completed`、`file_error`、`skipped`、`summary`。用 `completed` 判断单张文件保存完成，读取其中的实际 `output` 路径、`format`、`bits`、`colorSpace`、`film`。

`--job-list jobs.json` 接受对象数组，每项含 `input` 和可选 `relative`；可用 `--cancel-file` 或 Ctrl+C 取消。退出码：0 成功/跳过，2 参数错误，3 输入错误，4 内核错误，5 转换失败，130 取消。命令行参数和 JSON 字段名不随界面语言改变，CLI 帮助目前为中文。

## 与 SuperCCD 的区别

| | Fuji S3/S5 Pro RAW Converter | [SuperCCD](https://github.com/anibarro/superccd) |
| --- | --- | --- |
| 核心目的 | 原厂风格的日常浏览、选片和批量渲染 | 将 S3/S5 的 S/R 响应合并为可继续编辑的 DNG |
| 转换路线 | 调用富士闭源内核及配置 | 自有处理流程与 LibRaw 等组件 |
| 主要输出 | JPEG、8/16 位 RGB TIFF | 6MP CFA DNG；另有渲染预览导出 |
| 调整方式 | 输出格式、原厂胶片模拟和色彩空间 | S/R 合并及预览参数调整 |
| 平台 | Windows | 以对方仓库说明为准 |

Fuji S3/S5 Pro RAW Converter 不生成 CFA DNG，TIFF 是已经去马赛克、完成色彩处理的 RGB 图像。希望继续 RAW 编辑时，可了解 SuperCCD；希望使用富士原厂转换结果时，可使用 Fuji S3/S5 Pro RAW Converter。本文参考了对方 README 的组织方式，Fuji S3/S5 Pro RAW Converter 不是该项目的分支。

## 从源码构建

源码使用 C# / WinForms / .NET Framework，构建脚本调用系统 Framework 的 `csc.exe`，无需 NuGet 还原。

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -DestinationPath .\dist\Fuji-S3-S5-RAW-Converter
```

源码仓库只包含本工具源码及文档，构建不会自动下载或生成富士闭源内核。要运行构建产物，将发布包中的 `data/engine` 完整复制至构建目录的 `data/engine`，保留构建生成的 `s5raw-browser.exe`；也可先把发布包解压到构建目标目录，再覆盖构建。

主要代码：`Gui.cs` 为主界面，`Cli.cs` 为命令行，`NativeWorker.cs` / `Native16.cs` 为原厂转换桥接，`NativeBrowser.cs` 为浏览插件桥接，`FilmSimulation.cs` 读取原厂胶片配置，`Viewer.cs` / `Compare.cs` / `Canvas.cs` 实现浏览与对照。

## 1.6.2 与官方软件归档

1.6.2 将 `FG05FC09.dll`（S5 Pro）和 `FG05FC0B.dll` 从 6.2.0.26 更新为 HS-V3 1.1b 中的 **6.2.0.27**，LE 与高级转换目录同步更新。胶片配置、色彩资源和 RAW 浏览插件未变。顶栏语言框改为与按钮垂直居中。

只用一张复制的 S5 Pro 样片验证标准 8 位 TIFF、F1b 16 位 TIFF 和 JPEG；F1b 的解码像素与旧内核一致。此验证不能推断所有机型兼容或画质改善。

Release 同时提供 `V3UP11B.EXE`：目前查到的 HS-V3 Windows 历史更新包 1.1b，**不是完整安装盘**，不需要运行它来使用已打包的 Fuji S3/S5 Pro RAW Converter。来源、校验值与提取记录见 [官方更新包说明](OFFICIAL_UPDATE.md)。

## 第三方组件

Fuji S3/S5 Pro RAW Converter 是独立的社区工具，不是富士官方产品。富士转换 DLL、浏览插件和配置文件仍归各自权利人所有；源码公开不改变这些组件的许可。详见 [第三方组件说明](THIRD_PARTY_NOTICES.md)。
