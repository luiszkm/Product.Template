using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Product.Template.Kernel.Application.Ai;

namespace Kernel.Infrastructure.Ai;

internal sealed class CachedEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingService _inner;
    private readonly IDistributedCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public CachedEmbeddingService(IEmbeddingService inner, IDistributedCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var key = BuildCacheKey(text);
        var cached = await _cache.GetAsync(key, cancellationToken);

        if (cached is not null)
            return JsonSerializer.Deserialize<EmbeddingResult>(cached)!;

        var result = await _inner.EmbedAsync(text, cancellationToken);

        await _cache.SetAsync(
            key,
            JsonSerializer.SerializeToUtf8Bytes(result),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration },
            cancellationToken);

        return result;
    }

    public async Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        var results = new EmbeddingResult[textList.Count];
        var uncachedIndices = new List<int>();
        var uncachedTexts = new List<string>();

        for (var i = 0; i < textList.Count; i++)
        {
            var key = BuildCacheKey(textList[i]);
            var cached = await _cache.GetAsync(key, cancellationToken);

            if (cached is not null)
                results[i] = JsonSerializer.Deserialize<EmbeddingResult>(cached)!;
            else
            {
                uncachedIndices.Add(i);
                uncachedTexts.Add(textList[i]);
            }
        }

        if (uncachedTexts.Count > 0)
        {
            var fresh = await _inner.EmbedBatchAsync(uncachedTexts, cancellationToken);

            for (var j = 0; j < uncachedIndices.Count; j++)
            {
                var idx = uncachedIndices[j];
                results[idx] = fresh[j];

                var key = BuildCacheKey(textList[idx]);
                await _cache.SetAsync(
                    key,
                    JsonSerializer.SerializeToUtf8Bytes(fresh[j]),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration },
                    cancellationToken);
            }
        }

        return results;
    }

    private static string BuildCacheKey(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return $"emb:{Convert.ToHexString(hash)}";
    }
}
