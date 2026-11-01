# Fuji S3/S5 Pro RAW Converter

富士 S3 Pro / S5 Pro RAF 照片转换工具，使用富士原厂内核，提供 Windows 图形界面和命令行。

[English](README.en.md) · [下载 Windows 便携版](https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases/latest)

## 下载后怎么用

1. 打开[下载页面](https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases/latest)，下载 **`Fuji-S3-S5-RAW-Converter-1.6.2-Windows.zip`**。
2. **完整解压 ZIP**，双击主目录的 **`Fuji-S3-S5-RAW-Converter.exe`**。
3. 点击「添加照片」或「添加文件夹」，勾选照片，选择输出格式和胶片模拟，点击「转换选中项」。

无需下载 Source code、编译源码或运行官方更新包。请把 `data` 文件夹与启动程序放在一起。

```text
Fuji-S3-S5-RAW-Converter/
├─ Fuji-S3-S5-RAW-Converter.exe  ← 双击这个
├─ 使用说明.txt
└─ data/                       ← 程序运行所需文件
```

运行环境：Windows、.NET Framework 4.8。目前在 Windows 11 上验证，尚未在全新系统上验证所有依赖。

## 功能

- RAF 缩略图选片、图片浏览、照片参数显示；只转换勾选项。
- 原厂 **8 位 / 原生 16 位 TIFF**、**完整尺寸 JPEG**。
- 原厂胶片模拟：标准、F1、F1a、F1b、F1c、F2；按所选照片和色彩空间显示可用模式，JPEG 与 TIFF 均可选择。
- 双击照片浏览；「对照」按文件夹和文件名自动匹配，右侧可切换 TIFF / JPEG 结果。
- 批量并行数默认使用 CPU 逻辑核心数，可调整。
- 默认输出到源文件夹，同名文件自动加序号；记住打开与输出位置。
- 顶栏切换简体中文 / English；适配高 DPI 屏幕。
- 修正竖拍 TIFF 的像素与缩略图方向。

16 位 TIFF 支持 sRGB / Adobe RGB；JPEG 使用完整 16 位 sRGB 渲染后编码，质量默认 92。胶片模拟会写入输出像素，不改变 RAW 浏览图，也不承诺与相机直出 JPEG 完全一致。

RAW 浏览调用富士原厂插件。S5 Pro 样片显示的是内嵌相机 JPEG（1440×960），完整导出为 4256×2848；左侧预览不是同分辨率的完整 RAW 解码。界面显示为 8 位，带 ICC 的结果映射到 sRGB。

## 适用机型

主要面向 **S3 Pro / S5 Pro**。按 RAF 处理器代码匹配原厂模块，没有写死只支持这两款。

已核对文件头与模块的其他机型包括 S2 Pro、F700、E550、E900、S5000、S5500、S5200/S5600、S6000fd/S6500fd、S9500、S9600。模块匹配不代表全部机型均已完成实拍转换验收。S3 Pro 曾完成单张 16 位 TIFF 检查，当前主要用 S5 Pro 验证。

## 命令行与其他软件调用

命令行程序位于 `data/s5raw.exe`，保留原 S5Raw 接口。在解压目录打开 PowerShell：

```powershell
# 16 位 TIFF
.\data\s5raw.exe convert -i "D:\Photos\photo.RAF" -o "D:\Output\photo.tif" --bits 16

# JPEG 与 F1b 胶片模拟
.\data\s5raw.exe convert -i "D:\Photos\photo.RAF" -o "D:\Output\photo.jpg" --film f1b

# 批量转换
.\data\s5raw.exe convert -i "D:\Photos" --output-dir "D:\Output" --recursive --format jpeg --existing rename --json

.\data\s5raw.exe --help
```

`--film`：`standard|f1|f1a|f1b|f1c|f2`；`--jobs N`：并行数。16 位或胶片模拟 TIFF 可用 `--color-space adobe-rgb`。

其他程序启动上述进程并传 `--json`，读取 UTF-8 JSON Lines，以 `completed` 事件确认单张输出完成。CLI 默认标准 8 位 TIFF、同名报错；GUI 初始为 16 位 TIFF、同名加序号。

## 源码构建

普通使用者跳过本节。Windows 上运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -DestinationPath .\dist\Fuji-S3-S5-RAW-Converter
```

使用 .NET Framework 自带 C# 编译器，无需 NuGet。构建不生成富士闭源内核：可先将便携版解压到目标目录，再执行构建；或补齐发布包的 `data/engine`，保留刚编译的 `s5raw-browser.exe`。

## 官方软件更新包

下载页另外提供 **`V3UP11B.EXE`**，是目前查到的 HS-V3 Windows **1.1b 历史更新包**，不是完整安装盘，可能需要原版安装或介质。**使用本工具不需要运行它。**

来自[历史镜像](https://www.touslesdrivers.com/index.php?v_code=27749&v_page=23)。来源和内核检查记录在下载页的 `HS-V3-1.1b-source-and-checksum.txt`，附件校验值在 `SHA256SUMS.txt`。

1.6.2 已将两个转换模块更新至 **6.2.0.27**，并修正语言框对齐。一张复制的 S5 Pro 样片通过 8 位 TIFF、16 位 TIFF 和 JPEG 检查；F1b 像素与旧内核一致，不据此宣称画质提升。

本项目不是富士官方产品。富士内核、浏览组件及配置仍归原权利人，源码公开不改变它们的许可。
