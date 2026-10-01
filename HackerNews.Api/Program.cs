using HackerNews.Api.Clients;
using HackerNews.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<IHackerNewsClient, HackerNewsClient>(
    client =>
    {
        client.BaseAddress =
            new Uri("https://hacker-news.firebaseio.com/v0/");

        client.Timeout = TimeSpan.FromSeconds(10);
    });

builder.Services.AddScoped<IStoryService, StoryService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();