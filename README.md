# Hacker News Best Stories API

An ASP.NET Core Web API that returns the best `n` Hacker News stories,
ordered by descending score.

## Requirements

- .NET 10 SDK

## Running the application

Clone the repository and restore the dependencies:

```bash
dotnet restore
```

Build the solution:

```bash
dotnet build
```

Run the API:

```bash
dotnet run --project HackerNews.Api
```

Alternatively, open the solution in Visual Studio and run the
`HackerNews.Api` project.

## API

### Get best stories

```http
GET /api/stories/best?n=10
```

`n` must be greater than zero.

Example response:

```json
[
  {
    "title": "Example story",
    "uri": "https://example.com",
    "postedBy": "user",
    "time": "2026-09-30T20:00:00+00:00",
    "score": 500,
    "commentCount": 100
  }
]
```

If `n` is greater than the number of stories currently available from
Hacker News, all available stories are returned.

## Running the tests

```bash
dotnet test
```

## Design

The application is split into three main responsibilities:

- `StoriesController` handles HTTP requests and responses.
- `StoryService` contains the application logic for retrieving,
  ordering, caching and mapping stories.
- `HackerNewsClient` handles communication with the Hacker News API.

`HackerNewsClient` is registered as a typed `HttpClient` through
dependency injection, so outbound calls use `IHttpClientFactory` and
are asynchronous.

The Hacker News response model is kept separate from the public API
response model so the application is not directly coupled to the
external API contract.

### Scalability and Hacker News API protection

The API caches the fully processed and score-ordered story collection
for two minutes. Requests made while the cache is valid therefore do
not make additional calls to Hacker News.

A shared refresh lock prevents multiple simultaneous callers from
refreshing an expired cache at the same time. The in-memory cache and
this in-process lock coordinate callers only within a single running
application instance.

When a refresh is required, story details are fetched concurrently,
but a `SemaphoreSlim` limits the number of simultaneous Hacker News
requests to 10. This provides better performance than sequential
requests without sending an unbounded number of requests to the
upstream service.

Cancellation tokens are propagated to outbound HTTP operations so work
can be cancelled when the originating request is cancelled.

## Assumptions

- The `beststories` endpoint identifies the candidate stories, but the
  application explicitly orders retrieved stories by their `score`
  because the required output is defined as descending score order.
- Missing optional Hacker News fields such as a URL are handled without
  failing the complete request.
- A two-minute in-memory cache provides an acceptable balance between
  freshness and limiting repeated calls to the Hacker News API.
- If fewer than `n` valid stories are available, all available stories
  are returned.

## Improvements with more time

For a production deployment running multiple application instances,
I would replace the in-memory cache with a distributed cache such as
Redis and use a distributed locking strategy so refresh coordination
works across instances.

Other potential improvements include:

- configurable cache duration and concurrency limits;
- richer resilience handling for transient Hacker News failures;
- metrics and structured logging for cache hits, refresh duration and
  upstream failures;
- additional integration and concurrency tests;
- serving stale cached data temporarily if Hacker News is unavailable;
- configuration of external API settings through strongly typed options.
