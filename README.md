# Fuji S3/S5 Pro RAW Converter

富士 RAF 转换工具：集成 S3/S5 Pro 原厂内核与 RAW FILE CONVERTER EX（SILKYPIX）内核，提供 Windows 界面和命令行。

[English](README.en.md) · [下载 1.7.2 Windows 便携版](https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases/tag/v1.7.2)

## 使用

完整解压 `Fuji-S3-S5-RAW-Converter-1.7.2-Windows.zip`，双击根目录 `Fuji-S3-S5-RAW-Converter.exe`，保留旁边的 `data` 文件夹。需要 64 位 Windows 和 .NET Framework 4.8；无需启动官方转换软件或 USB 连接相机。

1. 添加 RAF 照片或文件夹，勾选需要导出的照片。
2. 选择 JPEG、8 位或原生 16 位 TIFF，以及输出位置。
3. 每种机型各有一个胶片选框，可以分别设置。例如 X-T50 选 PROVIA、S5 Pro 选 F1b，在同一批次各自生效。
4. 点击选框后的「帮助」，查看该机型全部可选模式的说明；会定位到当前选择，查看说明不会改变导出设置。
5. 点击「转换选中项」。默认同名文件自动加序号，原始 RAF 只读。

机型文字、选框和帮助按钮保持垂直居中。取消勾选某机型会禁用其选框，但保留选择；多于三行时可滚动。顶栏支持中英文切换。「对照」可匹配并浏览导出的 TIFF/JPEG。

## 输出和胶片

- X-T50：完整尺寸导出，拍摄设置及 20 个胶片选项，含 PROVIA、Velvia、ASTIA、Classic Chrome、REALA ACE、ACROS 等。
- S3/S5 Pro：保留原有内核、标准及实际配置支持的 F1/F2 系列。
- **ACROS 和普通黑白模式会导出黑白照片**，Sepia 为棕褐单色；需要彩色可明确选 PROVIA 等。默认模式可能跟随拍摄时的黑白设置。
- TIFF 可选 sRGB / Adobe RGB（旧内核标准 8 位 TIFF 使用相机色彩）。RFC EX 输出前检查实际位深与 ICC。JPEG 从完整 16 位 sRGB 图像编码，质量默认 92。
- RAW 浏览使用内嵌相机预览，胶片设置不会即时重绘预览；请查看导出结果。不导入官方软件另存的编辑旁车文件。
- 40MP 未压缩 16 位 RGB TIFF 约 239 MB，适合后期处理；日常分享可用 JPEG。
- 含 RFC EX 的批次最多并行处理两张以限制内存，旧机型批次可使用设置的并行数。

其他富士机型按附带固定版本内核的机型表尝试默认转换，尚未逐台实拍验证，现代机型的胶片覆盖参数目前仅开放给 X-T50；其他品牌 RAW 不支持。本机 Windows 11 已验证，未进行全新系统依赖验收。

## 命令行

```powershell
.\data\s5raw.exe convert -i photo.RAF -o photo.tif --bits 16 --film provia
.\data\s5raw.exe convert -i s5pro.RAF -o photo.jpg --film f1b
.\data\s5raw.exe convert --job-list jobs.json --output-dir output --format jpeg --json
.\data\s5raw.exe info -i photo.RAF --json
.\data\s5raw.exe doctor --json
```

`--job-list` 是 JSON 数组，每项含 `input`、`relative`，可单独指定 `film`、`colorSpace`。省略时使用全局选项。`--json` 输出 UTF-8 JSON Lines，以 `completed` 事件确认单张成功。完整参数见 `--help`。

## 构建与内核

Windows 上运行 `powershell -ExecutionPolicy Bypass -File .\package.ps1`。使用 .NET Framework 自带编译器，无需 NuGet。输出在 `dist/Fuji-S3-S5-RAW-Converter` 及同目录的版本 ZIP。`build.ps1 -DestinationPath <目录>` 仅构建；`package.ps1 -OutputDirectory <目录>` 可使用独立交付目录。

`src` 为本项目 C# 源码，`resources/engine` 为已提取的依赖，构建时自动复制。五个程序均从源码编译；不需要旧发布包或官方完整安装程序。内核资源合计约 558 MiB，ZIP 约 513 MiB。

`RfcExWorker.cs` 使用独立 x64 进程及固定私有接口；SILKYPIX64.dll 8.1.20.0 的 SHA-256 被锁定，不能直接替换 DLL 版本。`extract-rfcex.ps1` 从匹配的本地安装提取依赖并生成文件校验清单。旧机型使用独立 x86 工作进程。界面依赖 WinForms/WPF，不支持 Linux/macOS 原生运行，未验证 Wine。

GitHub Actions 可构建 Windows ZIP。源码及第三方组件的许可范围见 [LICENSE](LICENSE) 和 [NOTICE](resources/NOTICE.txt)。MIT 仅适用于本项目自有代码，不改变第三方文件许可。本项目不是富士官方产品。

## 说明来源

帮助内容概括自 [X-T50 官方手册](https://fujifilm-dsc.com/en/manual/x-t50/menu_shooting/image_quality_setting/#film_simulation) 与 [S5 Pro 官方手册第 111 页](https://dl.fujifilm-x.com/support/manual/s/finepix_s5pro_manual_01.pdf)。软件导出效果不承诺与相机直出 JPEG 完全一致。
