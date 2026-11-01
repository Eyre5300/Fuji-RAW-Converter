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

Windows 上运行 `powershell -ExecutionPolicy Bypass -File .\package.ps1`。使用 .NET Framework 自带编译器，无需 NuGet。输出在 `dist/Fuji-S3-S5-RAW-Converter` 及同目录的版本 ZIP。`build.ps1 -DestinationPath <目录>` 仅构建；`package.ps1 -OutputDirectory <目录>` 可使用独立交付目录。

`src` 为本项目 C# 源码，`resources/engine` 为已提取的依赖，构建时自动复制。五个程序均从源码编译；不需要旧发布包或官方完整安装程序。内核资源合计约 558 MiB，ZIP 约 513 MiB。

`RfcExWorker.cs` 使用独立 x64 进程及固定私有接口；SILKYPIX64.dll 8.1.20.0 的 SHA-256 被锁定，不能直接替换 DLL 版本。`extract-rfcex.ps1` 从匹配的本地安装提取依赖并生成文件校验清单。旧机型使用独立 x86 工作进程。界面依赖 WinForms/WPF，不支持 Linux/macOS 原生运行，未验证 Wine。

GitHub Actions 可构建 Windows ZIP。源码及第三方组件的许可范围见 [LICENSE](LICENSE) 和 [NOTICE](resources/NOTICE.txt)。MIT 仅适用于本项目自有代码，不改变第三方文件许可。本项目不是富士官方产品。


## 内存调度

每批先运行一个任务测量内存；按约 90% 的物理内存/提交量预算决定并行数。实时占用低于 88% 持续 5 秒时可增加并行，高于 92% 持续 3 秒时下调，达到 95% 时下调等待缩短为 1 秒。正常调整至少间隔 5 秒，增幅不超过当前数量的 25%（至少一个）。这不是操作系统强制内存限制。
