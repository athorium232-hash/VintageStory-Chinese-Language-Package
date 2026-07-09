namespace VscnLanguagePackUpdater;

public sealed class UpdaterConfig
{
    public bool Enabled { get; set; } = true;

    public int CheckDelayMilliseconds { get; set; } = 3000;

    public int HttpTimeoutSeconds { get; set; } = 30;

    public bool NotifyWhenUpToDate { get; set; }

    public bool NotifyOnFailure { get; set; } = true;

    public bool DeleteOldPackages { get; set; } = true;

    public string ReleasesApiUrl { get; set; } = UpdaterConstants.ReleasesApiUrl;

    public string[] GithubUrlTemplates { get; set; } = UpdaterConstants.GithubUrlTemplates.ToArray();

    public string AssetFilePrefix { get; set; } = UpdaterConstants.LanguagePackAssetPrefix;

    public string AssetFileSuffix { get; set; } = UpdaterConstants.LanguagePackAssetSuffix;

    public void Normalize()
    {
        if (CheckDelayMilliseconds < 0)
        {
            CheckDelayMilliseconds = 0;
        }

        if (HttpTimeoutSeconds < 5)
        {
            HttpTimeoutSeconds = 5;
        }

        if (string.IsNullOrWhiteSpace(ReleasesApiUrl))
        {
            ReleasesApiUrl = UpdaterConstants.ReleasesApiUrl;
        }

        GithubUrlTemplates = NormalizeList(GithubUrlTemplates, UpdaterConstants.GithubUrlTemplates);
        if (!GithubUrlTemplates.Any(template => string.Equals(template, "{url}", StringComparison.OrdinalIgnoreCase)))
        {
            GithubUrlTemplates = [.. GithubUrlTemplates, "{url}"];
        }

        if (string.IsNullOrWhiteSpace(AssetFilePrefix))
        {
            AssetFilePrefix = UpdaterConstants.LanguagePackAssetPrefix;
        }

        if (string.IsNullOrWhiteSpace(AssetFileSuffix))
        {
            AssetFileSuffix = UpdaterConstants.LanguagePackAssetSuffix;
        }
    }

    public IReadOnlyList<string> CreateGithubUrls(string originalUrl)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return [];
        }

        var normalizedOriginalUrl = originalUrl.Trim();
        var urls = GithubUrlTemplates
            .Select(template => CreateGitHubUrl(template, normalizedOriginalUrl))
            .Where(IsSupportedAbsoluteUrl)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return urls.Length == 0 ? [normalizedOriginalUrl] : urls;
    }

    private static string CreateGitHubUrl(string template, string originalUrl)
    {
        var normalizedTemplate = string.IsNullOrWhiteSpace(template) ? "{url}" : template.Trim();
        if (normalizedTemplate.Contains("{url}", StringComparison.OrdinalIgnoreCase) ||
            normalizedTemplate.Contains("{escapedUrl}", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedTemplate
                .Replace("{escapedUrl}", Uri.EscapeDataString(originalUrl), StringComparison.OrdinalIgnoreCase)
                .Replace("{url}", originalUrl, StringComparison.OrdinalIgnoreCase);
        }

        return normalizedTemplate + originalUrl;
    }

    private static bool IsSupportedAbsoluteUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
    }

    private static string[] NormalizeList(IEnumerable<string>? values, params string[] fallback)
    {
        var normalized = values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized is { Length: > 0 })
        {
            return normalized;
        }

        return fallback.ToArray();
    }
}
