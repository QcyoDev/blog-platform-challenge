using System.Net;
using System.Net.Http.Json;
using BlogPlatform.Api.Entities;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BlogPlatform.IntegrationTests;

public class GetPostsTests
{
    [Fact]
    public async Task GetPosts_WhenPostsExist_ShouldReturnAllOrderedSummariesWithCommentCounts()
    {
        using var factory = new BlogPlatformFactory();

        var createdAt = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var posts = Enumerable.Range(1, 12)
            .Select(index => new BlogPost
            {
                Title = $"Post {index}",
                Content = $"Content {index}",
                CreatedAt = createdAt.AddMinutes(index),
                Comments = Enumerable.Range(1, index % 3)
                    .Select(commentIndex => new Comment
                    {
                        Content = $"Comment {commentIndex}",
                        CreatedAt = createdAt.AddMinutes(1)
                    })
                    .ToList()
            })
            .ToArray();

        await factory.SeedPostsAsync(posts);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/posts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summaries = await response.Content.ReadFromJsonAsync<List<PostSummary>>();

        Assert.NotNull(summaries);
        Assert.Equal(12, summaries.Count);
        Assert.Equal(
            posts.Reverse().Select(post => (post.Id, post.Title, post.Comments.Count, post.CreatedAt)),
            summaries.Select(post => (post.Id, post.Title, post.CommentCount, post.CreatedAt)));

        Assert.All(summaries, post => Assert.Equal(TimeSpan.Zero, post.CreatedAt.Offset));
    }

    [Fact]
    public async Task GetPosts_WhenNoPostsExist_ShouldReturnEmptyList()
    {
        using var factory = new BlogPlatformFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/posts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summaries = await response.Content.ReadFromJsonAsync<List<PostSummary>>();

        Assert.NotNull(summaries);
        Assert.Empty(summaries);
    }

    private sealed record PostSummary(int Id, string Title, int CommentCount, DateTimeOffset CreatedAt);
}
