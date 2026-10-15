# 正式游戏发布

桌面 `The Rain We Knew` 统一打开 `D:\游戏设计\正式游戏\ProjectMystery\The Rain We Knew.exe`。工程变动不会自动改变已导出的独立程序。发布新版本必须更新完整构建，不能只替换 exe。旧日期工作区的构建已按用户要求清理，今后完成发布核对后也应清除重复导出，不将它们设为桌面入口。

发布工具同时接受以 `The Rain We Knew` 或旧名 `ProjectMystery` 导出的完整构建，暂存时统一将 exe 和配套 `_Data` 文件夹改为新英文名。内部产品与存档命名空间保持兼容，不随显示名称改变。GitHub Release 的 ZIP 必须包含完整构建；提交代码或移动本地工程不会自动更新已发布下载包。

完整性检查支持散装 `globalgamemanagers`，以及当前 LZ4 构建使用的 `data.unity3d`；后者须有有效 UnityFS 文件头。两种布局均须包含程序、UnityPlayer 和主程序集，并对全部文件逐个核对大小和 SHA256。

完成构建和对应测试后，执行：

```powershell
& 'D:\GitHub Desktop\ProjectMystery\ProjectMystery\Tools\Release\Publish-DesktopRelease.ps1' `
  -BuildPath 'D:\实际的新版本构建目录' `
  -ReleaseLabel '本次版本名称' `
  -ValidationNote '实际通过的构建和运行检查；未验证项另行说明'
```

脚本先复制到 D 盘暂存目录，逐个核对所有文件的大小和 SHA256，通过后再切换正式目录并检查桌面快捷方式。新版不会与旧文件混装；原正式目录保留为 `ProjectMystery.previous`，后续发布仅保留上一版。失败时恢复原正式目录和入口；未由此工具管理、文件被额外改动或正在运行的正式游戏，会阻止更新。它不会强制关闭游戏，不会修改 Unity 工程、剧情、设置或存档。

交付前再独立验证一次：

```powershell
& 'D:\GitHub Desktop\ProjectMystery\ProjectMystery\Tools\Release\Publish-DesktopRelease.ps1' -VerifyOnly
```

核对 `Result`、版本名称、正式程序路径以及桌面快捷方式。再从桌面正常启动，完成本次改动对应的实际运行检查。只有代码或编辑器已更新、构建成功、暂存文件存在，都不能代替正式入口交付。不要自动发布未测试、未获准交付的实验版本。

`ProjectMystery_Release.json` 记录发布来源、检查说明和完整文件清单，仅用于版本核对，不参与游戏逻辑。`-ReleaseRoot` 和 `-DesktopLinkPath` 参数只在隔离的发布工具验证或明确要求迁移入口时使用；日常交付使用默认固定路径。

保留正式当前版及工具管理的上一版即可。验证视频在阶段验收后删除；需要保留的问题日志和最终静帧放在 D 盘。发布记录中的 `SourceBuild` 是历史来源，源导出清理后不应改写为其他版本，也不影响固定正式目录的全文件核验。
