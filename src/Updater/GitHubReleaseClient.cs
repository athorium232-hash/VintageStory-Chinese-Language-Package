using System.Net.Http.Headers;
using System.Text.Json;

namespace VscnLanguagePackUpdater;

internal sealed class GitHubReleaseClient : IDisposable
{
    private readonly HttpClient httpClient;

    public GitHubReleaseClient(TimeSpan timeout)
    {
        httpClient = new HttpClient
        {
            Timeout = timeout
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UpdaterConstants.UserAgent);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task<GitHubRelease> GetLatestReleaseAsync(
        IReadOnlyList<string> apiUrls,
        IReadOnlyList<string> latestReleaseUrls,
        Func<string, IReadOnlyList<GitHubReleaseAsset>> createFallbackAssets,
        Action<string, string>? onAttemptFailed,
        CancellationToken cancellationToken)
    {
        var failureCount = 0;
        var apiCandidates = NormalizeCandidateUrls(apiUrls).ToArray();
        var latestReleaseCandidates = NormalizeCandidateUrls(latestReleaseUrls).ToArray();
        var candidateCount = Math.Max(apiCandidates.Length, latestReleaseCandidates.Length);

        for (var index = 0; index < candidateCount; index++)
        {
            if (index < apiCandidates.Length)
            {
                var apiUrl = apiCandidates[index];
                try
                {
                    return await GetLatestReleaseFromUrlAsync(apiUrl, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failureCount++;
                    onAttemptFailed?.Invoke(apiUrl, ex.Message);
                }
            }

            if (index < latestReleaseCandidates.Length)
            {
                var latestReleaseUrl = latestReleaseCandidates[index];
                try
                {
                    return await GetLatestReleaseFromRedirectAsync(
                        latestReleaseUrl,
                        createFallbackAssets,
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failureCount++;
                    onAttemptFailed?.Invoke(latestReleaseUrl, ex.Message);
                }
            }
        }

        throw CreateAllChannelsFailedException("获取发布信息", failureCount);
    }

    private async Task<GitHubRelease> GetLatestReleaseFromUrlAsync(string apiUrl, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(apiUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        var tagName = GetRequiredString(root, "tag_name");
        var assets = new List<GitHubReleaseAsset>();
        if (root.TryGetProperty("assets", out var assetsElement) && assetsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assetsElement.EnumerateArray())
            {
                if (asset.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (!asset.TryGetProperty("name", out var nameElement) ||
                    !asset.TryGetProperty("browser_download_url", out var urlElement))
                {
                    continue;
                }

                var name = nameElement.GetString();
                var url = urlElement.GetString();
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                assets.Add(new GitHubReleaseAsset(name.Trim(), url.Trim()));
            }
        }

        return new GitHubRelease(tagName, assets);
    }

    private async Task<GitHubRelease> GetLatestReleaseFromRedirectAsync(
        string latestReleaseUrl,
        Func<string, IReadOnlyList<GitHubReleaseAsset>> createFallbackAssets,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(latestReleaseUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var finalUrl = response.RequestMessage?.RequestUri?.ToString();
        var tagName = ExtractTagNameFromReleaseUrl(finalUrl)
            ?? throw new InvalidOperationException("Latest release redirect did not contain a release tag.");

        var assets = createFallbackAssets(tagName);
        if (assets.Count == 0)
        {
            throw new InvalidOperationException($"Could not infer release assets from tag '{tagName}'.");
        }

        return new GitHubRelease(tagName, assets);
    }

    public async Task<string> DownloadFileAsync(
        IReadOnlyList<string> urls,
        string destinationPath,
        Action<string, string>? onAttemptFailed,
        CancellationToken cancellationToken)
    {
        var failureCount = 0;
        foreach (var url in NormalizeCandidateUrls(urls))
        {
            try
            {
                await DownloadFileFromUrlAsync(url, destinationPath, cancellationToken);
                return url;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failureCount++;
                TryDeleteFile(destinationPath);
                onAttemptFailed?.Invoke(url, ex.Message);
            }
        }

        throw CreateAllChannelsFailedException("下载汉化包", failureCount);
    }

    private async Task DownloadFileFromUrlAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await source.CopyToAsync(destination, cancellationToken);
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }

    private static IEnumerable<string> NormalizeCandidateUrls(IEnumerable<string> urls)
    {
        return urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static InvalidOperationException CreateAllChannelsFailedException(string operation, int failureCount)
    {
        return new InvalidOperationException(
            $"{operation}所有 {failureCount} 个通道均失败，请检查网络或更换加速通道。");
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private static string? ExtractTagNameFromReleaseUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var decoded = Uri.UnescapeDataString(url);
        const string marker = "/releases/tag/";
        var markerIndex = decoded.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        var start = markerIndex + marker.Length;
        var end = decoded.IndexOfAny(['?', '#', '/'], start);
        var tagName = end < 0 ? decoded[start..] : decoded[start..end];
        return string.IsNullOrWhiteSpace(tagName) ? null : tagName.Trim();
    }

    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString()))
        {
            return property.GetString()!.Trim();
        }

        throw new InvalidOperationException($"GitHub release response did not contain '{propertyName}'.");
    }
}

internal sealed record GitHubRelease(string TagName, IReadOnlyList<GitHubReleaseAsset> Assets);

internal sealed record GitHubReleaseAsset(string Name, string DownloadUrl);
