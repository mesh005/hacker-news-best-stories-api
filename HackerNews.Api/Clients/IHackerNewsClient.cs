using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HackerNews.Api.Models;

namespace HackerNews.Api.Clients
{
    public interface IHackerNewsClient
    {
        Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken = default);

        Task<HackerNewsItem?> GetStoryAsync(int storyId, CancellationToken cancellationToken = default);
    }
}
