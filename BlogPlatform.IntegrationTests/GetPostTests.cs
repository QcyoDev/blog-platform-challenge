using System.Net;
using System.Net.Http.Json;
using BlogPlatform.Api.Entities;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BlogPlatform.IntegrationTests;

public class GetPostTests
{
    [Fact]
    public async Task GetPost_WhenPostExists_ShouldReturnPostWithComments()
    {
        using var factory = new BlogPlatformFactory();

        var createdAt = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var comment = new Comment
        {
            Content = "First comment",
            CreatedAt = createdAt.AddMinutes(1)
        };
        var post = new BlogPost
        {
            Title = "My first post",
            Content = "Hello, world!",
            CreatedAt = createdAt,
            Comments = [comment]
        };

        await factory.SeedPostsAsync(post);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync($"/api/posts/{post.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var retrievedPost = await response.Content.ReadFromJsonAsync<RetrievedPost>();

        Assert.NotNull(retrievedPost);
        Assert.Equal(post.Id, retrievedPost.Id);
        Assert.Equal(post.Title, retrievedPost.Title);
        Assert.Equal(post.Content, retrievedPost.Content);
        Assert.Equal(post.CreatedAt, retrievedPost.CreatedAt);
        Assert.Equal(TimeSpan.Zero, retrievedPost.CreatedAt.Offset);
        Assert.NotNull(retrievedPost.Comments);

        var retrievedComment = Assert.Single(retrievedPost.Comments);

        Assert.Equal(comment.Id, retrievedComment.Id);
        Assert.Equal(comment.Content, retrievedComment.Content);
        Assert.Equal(comment.CreatedAt, retrievedComment.CreatedAt);
        Assert.Equal(TimeSpan.Zero, retrievedComment.CreatedAt.Offset);
    }

    [Fact]
    public async Task GetPost_WhenPostDoesNotExist_ShouldReturnNotFound()
    {
        using var factory = new BlogPlatformFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/posts/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record RetrievedPost(
        int Id,
        string Title,
        string Content,
        DateTimeOffset CreatedAt,
        IReadOnlyList<RetrievedComment> Comments);

    private sealed record RetrievedComment(int Id, string Content, DateTimeOffset CreatedAt);
}
