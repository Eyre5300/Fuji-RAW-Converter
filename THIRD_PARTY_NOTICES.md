# Third-party components

Fuji S3/S5 Pro RAW Converter is an independent community tool and is not affiliated with or endorsed by Fujifilm.

## Fujifilm Hyper-Utility components

The portable release contains proprietary conversion DLLs, camera profiles, film/color resources and RAW browsing components originally distributed with Fujifilm Hyper-Utility software. These are located under `data/engine/RAFCNVX`, `data/engine/RAFCNV` and `data/engine/browser`.

The separate `V3UP11B.EXE` Release attachment is a historical HS-V3 1.1b updater. Original components and the updater remain the property of their respective rights holders. Publishing Fuji S3/S5 Pro RAW Converter source does not relicense those components or grant rights beyond their original terms. Any license later applied to Fuji S3/S5 Pro RAW Converter's own code does not cover them.

富士转换内核、浏览组件、色彩与胶片资源，以及官方历史更新包，版权和许可仍归原权利人。本项目源码的公开不改变第三方组件的许可，也不代表富士官方授权或背书。

## Runtime components

The application uses Microsoft .NET Framework, Windows imaging APIs and legacy runtime components required by the original engine. Microsoft runtime files retain their original rights and terms.

## Referenced projects

- [SuperCCD](https://github.com/anibarro/superccd): related RAF-to-DNG research and README organization reference. It is not bundled as part of Fuji S3/S5 Pro RAW Converter and Fuji S3/S5 Pro RAW Converter is not a fork.
- [Unshield](https://github.com/twogood/unshield) and 7-Zip: used locally to inspect the historical updater. They are not runtime dependencies and are not included in the Fuji S3/S5 Pro RAW Converter portable release.
