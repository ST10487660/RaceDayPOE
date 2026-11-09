namespace RaceDay.Web.Services;

/// <summary>
/// Turns an image URL returned by the API into something the browser can load.
/// Azure Blob URLs are absolute and used as-is; relative URLs (local dev fallback) are prefixed with the API's public address.
/// </summary>
public interface IAssetUrlResolver
{
    string? Resolve(string? url);
}

public class AssetUrlResolver : IAssetUrlResolver
{
    private readonly string _publicBase;

    public AssetUrlResolver(IConfiguration configuration) =>
        _publicBase = (configuration["Api:PublicBaseUrl"] ?? configuration["Api:BaseUrl"] ?? "http://localhost:5080").TrimEnd('/');

    public string? Resolve(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        return url.StartsWith('/') ? _publicBase + url : url;
    }
}
