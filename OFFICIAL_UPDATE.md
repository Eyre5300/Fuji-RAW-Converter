# Hyper-Utility Software HS-V3 1.1b — Windows archive

Release 附件 `V3UP11B.EXE` 是本项目检索到的 HS-V3 Windows 1.1b 历史更新包，不是完整安装盘。它可能要求已有 HS-V3 安装或原版介质。已打包的 Fuji S3/S5 Pro RAW Converter 不需要运行这个更新程序。

This is a historical **updater**, not a full Hyper-Utility installer. It may require an existing installation or original media. It is supplied separately from the Fuji S3/S5 Pro RAW Converter portable ZIP.

## 来源 / Provenance

- [历史下载页 / Historical mirror listing](https://www.touslesdrivers.com/index.php?v_code=27749&v_page=23)
- [原镜像文件 / Mirror file](https://fichiers.touslesdrivers.com/27749/V3UP11B.EXE)
- [历史发布报道 / Historical announcement](https://www.dpnow.com/articles/4097.html)
- [旧富士下载地址 / Historical Fujifilm URL](https://www.fujifilm.com/support/digital_cameras/software/hyper_utility/hsv3_1/exe/index/V3UP11B.EXE)（本次获取时不可用 / unavailable when retrieved）

检索日期：2026-11-02。未找到比 1.1b 更新的同一 HS-V3 Windows 更新包；此描述不涉及其他代际 Hyper-Utility 产品。

| Item | Value |
| --- | --- |
| File | `V3UP11B.EXE` |
| Size | 283455488 bytes |
| SHA-256 | `03221b5da5ed0d4cacac97cf1db4ab35f7e077a796f64ea3f6a18c37e74bd237` |

文件来自第三方历史镜像，无 Authenticode 签名，未找到富士公开校验值。本地哈希用于标识归档内容，不能替代官方真实性认证。文件按原样提供，未修改、未运行安装器。

The file came from a historical third-party mirror. No Authenticode signature or official published checksum was available. The hash identifies the archived bytes; it is not vendor authentication. The installer was not executed or modified.

## 提取与转换内核变化 / Engine inspection

用 7-Zip 提取外层，再用 [Unshield](https://github.com/twogood/unshield) 静态解包 InstallShield CAB；没有更新本机 Hyper-Utility 安装。

| Module | Before | 1.1b | Size before → after |
| --- | --- | --- | --- |
| `FG05FC09.dll` (S5 Pro) | 6.2.0.26 | 6.2.0.27 | 1458176 → 1458176 |
| `FG05FC0B.dll` (processor FF392203; model not verified) | 6.2.0.26 | 6.2.0.27 | 1433600 → 1437696 |

两者导入依赖和导出函数名称/序号未变，代码段有变化。LE 目录另有 97 个文件完全相同，高级目录另有 121 个文件完全相同；两目录原有工具补充的 `msvcp60.dll` 保留。胶片配置、色彩资源和四个 RAW 浏览组件未变。Fuji S3/S5 Pro RAW Converter 1.6.2 在 LE 和高级目录都使用新版 DLL。

Both modules retain the same import dependencies and export names/ordinals. Code sections differ. Other conversion resources and the RAW browser components are unchanged. No algorithmic quality improvement is inferred from the version change.

New DLL SHA-256:

```text
650d0e21609dc1b9b828fe8b6888cf7d03fa09c6e31de66b617111951d3feaa3  FG05FC09.dll
9da2a57457df8670550eb68bd61d38cb4f28fbeb2e54ef60b6c7a8e76b871fb8  FG05FC0B.dll
```

其他宿主组件也有版本变化：HyperUtility.EXE 3.0.0.13 → 3.0.1.10，RAFCNV.dll / RAFBAT.dll 2.3.0.11 → 2.3.0.18，s5shoot.dll 1.0.1.9 → 1.0.1.18。Fuji S3/S5 Pro RAW Converter 不调用这些宿主组件，未将它们加入运行依赖。

仅用一张 S5 Pro 样片检查标准 8 位 TIFF、F1b 16 位 TIFF 与 JPEG。F1b 解码像素与旧内核相同。FF392203 没有实拍样片验证。
