using HackerNews.Api.Models;

namespace HackerNews.Api.Services
{
    public interface IStoryService
    {
        Task<IReadOnlyList<StoryResponse>> GetBestStoriesAsync(int count, CancellationToken cancellationToken = default);
    }
}
