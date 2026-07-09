# VSCN 汉化包自动更新器

`vscnlangpackupdater` 是一个客户端代码模组，用于在游戏启动后自动检查并下载最新版 VSCN Vintage Story 汉化包。

启动后它会请求最新 Release 信息：

```text
https://api.github.com/repos/vscn-studio/VintageStory-Chinese-Language-Package/releases/latest
```

默认会先通过 GitHub 加速通道访问 Release API 和下载地址，所有加速通道失败后再回退到原始 GitHub 地址。如果最新 Release 版本高于当前已加载的 `vscnlangpack` 版本，它会把匹配的 `VintageStory-Chinese-Language-Package-<version>.zip` 下载到玩家的 `Mods` 目录，并提示玩家重启游戏生效。

## 构建

将 `VINTAGE_STORY` 设置为包含 `VintagestoryAPI.dll` 的 Vintage Story 安装目录，然后运行。面向 Vintage Story 1.22.x 的代码模组目前需要 .NET 10 SDK。

```powershell
dotnet build src/Updater/Updater.csproj -c Release
```

模组文件会复制到：

```text
src/Updater/bin/Release/mod/
```

发布自动更新器时，将该 `mod` 目录中的内容打包为 zip。

## 配置

模组首次启动时会写入 `ModConfig/vscnlangpackupdater.json`。

```json
{
  "enabled": true,
  "checkDelayMilliseconds": 3000,
  "httpTimeoutSeconds": 30,
  "notifyWhenUpToDate": false,
  "notifyOnFailure": true,
  "deleteOldPackages": true,
  "releasesApiUrl": "https://api.github.com/repos/vscn-studio/VintageStory-Chinese-Language-Package/releases/latest",
  "releasesLatestUrl": "https://github.com/vscn-studio/VintageStory-Chinese-Language-Package/releases/latest",
  "releaseAssetDownloadUrlTemplate": "https://github.com/vscn-studio/VintageStory-Chinese-Language-Package/releases/download/{tag}/{asset}",
  "githubUrlTemplates": [
    "https://ghproxy.net/{url}",
    "https://v6.gh-proxy.com/{url}",
    "https://hk.gh-proxy.com/{url}",
    "https://cdn.gh-proxy.com/{url}",
    "https://edgeone.gh-proxy.com/{url}",
    "{url}"
  ],
  "assetFilePrefix": "VintageStory-Chinese-Language-Package-",
  "assetFileSuffix": ".zip"
}
```

`githubUrlTemplates` 同时用于 Release API、最新版跳转页和语言包 zip 下载。`{url}` 会替换为原始 GitHub 地址，`{escapedUrl}` 会替换为 URL 编码后的原始地址；如果某个模板不包含占位符，会直接把原始地址拼到模板末尾。建议保留 `{url}` 作为最后兜底通道。如果 GitHub API 无法访问，更新器会退回到 `releasesLatestUrl` 的跳转地址解析最新标签，再用 `releaseAssetDownloadUrlTemplate` 拼出语言包下载地址。默认通道优先使用 `ghproxy.net`，并参考 MVL 增加了 `v6`、`hk`、`cdn`、`edgeone` 四条 `gh-proxy.com` 子线路作为下载备用。
