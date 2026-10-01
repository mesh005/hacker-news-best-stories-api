using HackerNews.Api.Clients;
using HackerNews.Api.Models;
using HackerNews.Api.Services;
using Microsoft.Extensions.Caching.Memory;

namespace HackerNews.Api.Tests;

public class StoryServiceTests
{
    [Fact]
    public async Task GetBestStoriesAsync_ReturnsStoriesOrderedByScore()
    {
        var client = new FakeHackerNewsClient
        {
            StoryIds = [1, 2, 3],
            Stories =
            {
                [1] = CreateStory(1, 100),
                [2] = CreateStory(2, 300),
                [3] = CreateStory(3, 200)
            }
        };

        using var cache =
            new MemoryCache(new MemoryCacheOptions());

        var service = new StoryService(client, cache);

        var result =
            await service.GetBestStoriesAsync(2);

        Assert.Equal(2, result.Count);
        Assert.Equal(300, result[0].Score);
        Assert.Equal(200, result[1].Score);
    }

    [Fact]
    public async Task GetBestStoriesAsync_MapsStoryFields()
    {
        var client = new FakeHackerNewsClient
        {
            StoryIds = [1],
            Stories =
            {
                [1] = new HackerNewsItem
                {
                    Id = 1,
                    Title = "Test story",
                    Url = "https://example.com",
                    By = "test-user",
                    Time = 1_700_000_000,
                    Score = 123,
                    Descendants = 45
                }
            }
        };

        using var cache =
            new MemoryCache(new MemoryCacheOptions());

        var service = new StoryService(client, cache);

        var result =
            await service.GetBestStoriesAsync(1);

        var story = Assert.Single(result);

        Assert.Equal("Test story", story.Title);
        Assert.Equal("https://example.com", story.Uri);
        Assert.Equal("test-user", story.PostedBy);
        Assert.Equal(123, story.Score);
        Assert.Equal(45, story.CommentCount);
        Assert.Equal(
            DateTimeOffset.FromUnixTimeSeconds(1_700_000_000),
            story.Time);
    }

    [Fact]
    public async Task GetBestStoriesAsync_UsesCachedResult()
    {
        var client = new FakeHackerNewsClient
        {
            StoryIds = [1, 2],
            Stories =
            {
                [1] = CreateStory(1, 100),
                [2] = CreateStory(2, 200)
            }
        };

        using var cache =
            new MemoryCache(new MemoryCacheOptions());

        var service = new StoryService(client, cache);

        await service.GetBestStoriesAsync(2);
        await service.GetBestStoriesAsync(1);

        Assert.Equal(
            1,
            client.GetBestStoryIdsCallCount);

        Assert.Equal(
            2,
            client.GetStoryCallCount);
    }

    [Fact]
    public async Task GetBestStoriesAsync_WithInvalidCount_Throws()
    {
        var client = new FakeHackerNewsClient();

        using var cache =
            new MemoryCache(new MemoryCacheOptions());

        var service = new StoryService(client, cache);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetBestStoriesAsync(0));
    }

    private static HackerNewsItem CreateStory(
        int id,
        int score)
    {
        return new HackerNewsItem
        {
            Id = id,
            Title = $"Story {id}",
            Url = $"https://example.com/{id}",
            By = "test-user",
            Time = 1_700_000_000,
            Score = score,
            Descendants = 10
        };
    }

    private sealed class FakeHackerNewsClient
        : IHackerNewsClient
    {
        public IReadOnlyList<int> StoryIds { get; init; } = [];

        public Dictionary<int, HackerNewsItem?> Stories { get; init; }
            = new();

        public int GetBestStoryIdsCallCount { get; private set; }

        public int GetStoryCallCount { get; private set; }

        public Task<IReadOnlyList<int>> GetBestStoryIdsAsync(
            CancellationToken cancellationToken = default)
        {
            GetBestStoryIdsCallCount++;

            return Task.FromResult(StoryIds);
        }

        public Task<HackerNewsItem?> GetStoryAsync(
            int storyId,
            CancellationToken cancellationToken = default)
        {
            GetStoryCallCount++;

            Stories.TryGetValue(
                storyId,
                out var story);

            return Task.FromResult(story);
        }
    }
}