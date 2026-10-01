using HackerNews.Api.Models;
using HackerNews.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HackerNews.Api.Controllers;

[ApiController]
[Route("api/stories")]
public class StoriesController : ControllerBase
{
    private readonly IStoryService _storyService;

    public StoriesController(IStoryService storyService)
    {
        _storyService = storyService;
    }

    [HttpGet("best")]
    [ProducesResponseType(
        typeof(IReadOnlyList<StoryResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<StoryResponse>>>
        GetBestStories(
            [FromQuery] int n,
            CancellationToken cancellationToken)
    {
        if (n <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid story count",
                Detail = "The query parameter 'n' must be greater than zero.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var stories = await _storyService.GetBestStoriesAsync(
                n,
                cancellationToken);

            return Ok(stories);
        }
        catch (HttpRequestException)
        {
            return Problem(
                title: "Hacker News API request failed",
                detail: "The upstream Hacker News API could not be reached.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}