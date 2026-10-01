using HackerNews.Api.Clients;
using HackerNews.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HackerNews.Api.Services;

public class StoryService : IStoryService
{
    private const int MaxConcurrentRequests = 10;
    private const string BestStoriesCacheKey = "best-stories";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    // Shared across StoryService instances so only one request refreshes
    // the cache at a time within this application instance.
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    private readonly IHackerNewsClient _hackerNewsClient;
    private readonly IMemoryCache _cache;

    public StoryService(
        IHackerNewsClient hackerNewsClient,
        IMemoryCache cache)
    {
        _hackerNewsClient = hackerNewsClient;
        _cache = cache;
    }

    public async Task<IReadOnlyList<StoryResponse>> GetBestStoriesAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        if (_cache.TryGetValue(
                BestStoriesCacheKey,
                out IReadOnlyList<StoryResponse>? cachedStories)
            && cachedStories is not null)
        {
            return cachedStories
                .Take(count)
                .ToList();
        }

        await RefreshLock.WaitAsync(cancellationToken);

        try
        {
            // Another request may have populated the cache while this
            // request was waiting for the refresh lock.
            if (_cache.TryGetValue(
                    BestStoriesCacheKey,
                    out cachedStories)
                && cachedStories is not null)
            {
                return cachedStories
                    .Take(count)
                    .ToList();
            }

            var bestStoryIds =
                await _hackerNewsClient.GetBestStoryIdsAsync(
                    cancellationToken);

            using var semaphore =
                new SemaphoreSlim(MaxConcurrentRequests);

            var storyTasks = bestStoryIds.Select(async id =>
            {
                await semaphore.WaitAsync(cancellationToken);

                try
                {
                    return await _hackerNewsClient.GetStoryAsync(
                        id,
                        cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var fetchedStories =
                await Task.WhenAll(storyTasks);

            IReadOnlyList<StoryResponse> stories = fetchedStories
                .OfType<HackerNewsItem>()
                .OrderByDescending(story => story.Score)
                .Select(story => new StoryResponse
                {
                    Title = story.Title ?? string.Empty,
                    Uri = story.Url,
                    PostedBy = story.By ?? string.Empty,
                    Time = DateTimeOffset.FromUnixTimeSeconds(story.Time),
                    Score = story.Score,
                    CommentCount = story.Descendants
                })
                .ToList();

            _cache.Set(
                BestStoriesCacheKey,
                stories,
                CacheDuration);

            return stories
                .Take(count)
                .ToList();
        }
        finally
        {
            RefreshLock.Release();
        }
    }
}