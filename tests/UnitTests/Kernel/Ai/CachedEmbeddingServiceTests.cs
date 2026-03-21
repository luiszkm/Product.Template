using Kernel.Infrastructure.Ai;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Product.Template.Kernel.Application.Ai;

namespace UnitTests.Kernel.Ai;

public class CachedEmbeddingServiceTests
{
    [Fact]
    public async Task EmbedAsync_ShouldReturnCachedResult_WhenCalledTwiceWithSameText()
    {
        var inner = new CountingEmbeddingService();
        var cache = BuildCache();
        var sut = new CachedEmbeddingService(inner, cache);

        var first = await sut.EmbedAsync("hello world");
        var second = await sut.EmbedAsync("hello world");

        Assert.Equal(first.Vector, second.Vector);
        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public async Task EmbedAsync_ShouldCallInner_WhenCacheIsEmpty()
    {
        var inner = new CountingEmbeddingService();
        var cache = BuildCache();
        var sut = new CachedEmbeddingService(inner, cache);

        await sut.EmbedAsync("text one");
        await sut.EmbedAsync("text two");

        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task EmbedBatchAsync_ShouldOnlyCallInner_ForUncachedTexts()
    {
        var inner = new CountingEmbeddingService();
        var cache = BuildCache();
        var sut = new CachedEmbeddingService(inner, cache);

        await sut.EmbedAsync("already cached");

        var results = await sut.EmbedBatchAsync(["already cached", "new text"]);

        Assert.Equal(2, results.Count);
        Assert.Equal(2, inner.CallCount); // 1 from EmbedAsync + 1 batch call for new text
    }

    [Fact]
    public async Task EmbedBatchAsync_ShouldReturnAllResults_WhenAllCached()
    {
        var inner = new CountingEmbeddingService();
        var cache = BuildCache();
        var sut = new CachedEmbeddingService(inner, cache);

        await sut.EmbedBatchAsync(["a", "b"]);
        var callsAfterFirstBatch = inner.CallCount;

        var results = await sut.EmbedBatchAsync(["a", "b"]);

        Assert.Equal(2, results.Count);
        Assert.Equal(callsAfterFirstBatch, inner.CallCount); // no new calls
    }

    private static MemoryDistributedCache BuildCache()
    {
        var opts = Options.Create(new MemoryDistributedCacheOptions());
        return new MemoryDistributedCache(opts);
    }

    private sealed class CountingEmbeddingService : IEmbeddingService
    {
        public int CallCount { get; private set; }

        public Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new EmbeddingResult(
                Vector: [0.1f, 0.2f, 0.3f],
                Dimensions: 3,
                TokensUsed: text.Length,
                Model: "stub"));
        }

        public async Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(
            IEnumerable<string> texts,
            CancellationToken cancellationToken = default)
        {
            var results = new List<EmbeddingResult>();
            foreach (var text in texts)
            {
                CallCount++;
                results.Add(new EmbeddingResult(
                    Vector: [0.1f, 0.2f, 0.3f],
                    Dimensions: 3,
                    TokensUsed: text.Length,
                    Model: "stub"));
            }

            return await Task.FromResult(results);
        }
    }
}
