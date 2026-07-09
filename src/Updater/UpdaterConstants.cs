namespace VscnLanguagePackUpdater;

internal static class UpdaterConstants
{
    public const string UpdaterModId = "vscnlangpackupdater";
    public const string LanguagePackModId = "vscnlangpack";
    public const string LanguagePackAssetPrefix = "VintageStory-Chinese-Language-Package-";
    public const string LanguagePackAssetSuffix = ".zip";
    public const string ReleasesApiUrl = "https://api.github.com/repos/vscn-studio/VintageStory-Chinese-Language-Package/releases/latest";
    public const string ReleasesLatestUrl = "https://github.com/vscn-studio/VintageStory-Chinese-Language-Package/releases/latest";
    public const string ReleaseAssetDownloadUrlTemplate = "https://github.com/vscn-studio/VintageStory-Chinese-Language-Package/releases/download/{tag}/{asset}";
    public const string ConfigFileName = "vscnlangpackupdater.json";
    public const string UserAgent = "vscnlangpackupdater/0.1.0";

    public static readonly string[] GithubUrlTemplates =
    [
        "https://ghproxy.net/{url}",
        "https://v6.gh-proxy.com/{url}",
        "https://hk.gh-proxy.com/{url}",
        "https://cdn.gh-proxy.com/{url}",
        "https://edgeone.gh-proxy.com/{url}",
        "{url}"
    ];

    public static readonly string[] LegacyGithubUrlTemplates =
    [
        "https://ghproxy.net/{url}",
        "{url}"
    ];

    public static readonly string[] DeprecatedGithubUrlTemplates =
    [
        "https://gh.llkk.cc/{url}",
        "https://ghfast.top/{url}",
        "https://gh-proxy.com/{url}"
    ];
}
