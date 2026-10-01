namespace HackerNews.Api.Models
{
    public class StoryResponse
    {
        public string? Title { get; set; }
        public string? Uri { get; set; }
        public string? PostedBy { get; set; }
        public DateTimeOffset Time { get; set; }

        public int Score { get; set; }

        public int CommentCount { get; set; }
    }
}
