using HackerNews.Api.Models;
using System.Net.Http.Json;

namespace HackerNews.Api.Clients;

public class HackerNewsClient : IHackerNewsClient
{
    private readonly HttpClient _httpClient;

    public HackerNewsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<int>> GetBestStoryIdsAsync(
        CancellationToken cancellationToken = default)
    {
        var ids = await _httpClient.GetFromJsonAsync<List<int>>(
            "beststories.json",
            cancellationToken);

        return ids ?? [];
    }

    public async Task<HackerNewsItem?> GetStoryAsync(
        int storyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storyId);

        return await _httpClient.GetFromJsonAsync<HackerNewsItem>(
            $"item/{storyId}.json",
            cancellationToken);
    }
}