# 开发者与命令行文档

用户使用说明与机型清单见 [README](../README.md)。

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

Windows 上运行 `powershell -ExecutionPolicy Bypass -File .\package.ps1`。使用 .NET Framework 自带编译器，无需 NuGet。输出在 `dist/Fuji-RAW-Converter` 及同目录的版本 ZIP。`build.ps1 -DestinationPath <目录>` 仅构建；`package.ps1 -OutputDirectory <目录>` 可使用独立交付目录。

`src` 为本项目 C# 源码，`resources/engine` 为已提取的依赖，构建时自动复制。五个程序均从源码编译；不需要旧发布包或官方完整安装程序。内核资源合计约 558 MiB，ZIP 约 513 MiB。

`RfcExWorker.cs` 使用独立 x64 进程及固定私有接口；SILKYPIX64.dll 8.1.20.0 的 SHA-256 被锁定，不能直接替换 DLL 版本。`extract-rfcex.ps1` 从匹配的本地安装提取依赖并生成文件校验清单。旧机型使用独立 x86 工作进程。界面依赖 WinForms/WPF，不支持 Linux/macOS 原生运行，未验证 Wine。

GitHub Actions 可构建 Windows ZIP。源码及第三方组件的许可范围见 [LICENSE](../LICENSE) 和 [NOTICE](../resources/NOTICE.txt)。MIT 仅适用于本项目自有代码，不改变第三方文件许可。本项目不是富士官方产品。

## 胶片适配

`RfcEx.cs` 保存机型菜单与官方模式编号；界面、CLI `info`、转换参数检查和独立工作进程共用同一份定义。完整菜单见 [CAMERAS](CAMERAS.md)。添加机型时，应核对官方版本记录及随附色彩资源，不应将本项目尚未接入的选项描述为官方转换器不具备的能力。

`RfcExWorker.cs` 先读取相机参数，再通过官方内核的参数方法设置胶片并显影。默认模式保留内核默认处理。项目没有自制胶片 LUT，也不对显影后的像素添加胶片滤镜。

`PreviewJpeg.cs` 为 Windows 图像组件提供内嵌 JPEG 兼容读取：先尝试 WIC，必要时由 GDI+ 在内存中规范化预览副本并保留 EXIF。它用于预览、拍摄信息和 JPEG 元数据；导出图像的像素始终来自官方内核渲染的完整 TIFF。


## 内存调度

每批先运行一个任务测量内存；按约 90% 的物理内存/提交量预算决定并行数。实时占用低于 88% 持续 5 秒时可增加并行，高于 92% 持续 3 秒时下调，达到 95% 时下调等待缩短为 1 秒。正常调整至少间隔 5 秒，增幅不超过当前数量的 25%（至少一个）。这不是操作系统强制内存限制。
