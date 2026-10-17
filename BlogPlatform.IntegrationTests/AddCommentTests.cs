using System.Net;
using System.Net.Http.Json;
using BlogPlatform.Api.Data;
using BlogPlatform.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlogPlatform.IntegrationTests;

public class AddCommentTests
{
    public static TheoryData<string?> InvalidContents => new()
    {
        null,
        string.Empty,
        "   ",
        new string('c', 251)
    };

    [Fact]
    public async Task AddComment_WhenRequestIsValid_ShouldPersistCommentAndReturnCreated()
    {
        using var factory = new BlogPlatformFactory();

        var post = new BlogPost
        {
            Title = "My first post",
            Content = "Hello, world!",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await factory.SeedPostsAsync(post);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var content = new string('c', 250);
        var beforeRequest = DateTimeOffset.UtcNow;

        var response = await client.PostAsJsonAsync($"/api/posts/{post.Id}/comments", new { content });

        var afterResponse = DateTimeOffset.UtcNow;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var comment = await response.Content.ReadFromJsonAsync<CreatedComment>();

        Assert.NotNull(comment);
        Assert.True(comment.Id > 0);
        Assert.Equal(content, comment.Content);
        Assert.Equal(TimeSpan.Zero, comment.CreatedAt.Offset);
        Assert.InRange(comment.CreatedAt, beforeRequest, afterResponse);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var persistedComment = await dbContext.Comments.AsNoTracking().SingleAsync();

        Assert.Equal(comment.Id, persistedComment.Id);
        Assert.Equal(post.Id, persistedComment.BlogPostId);
        Assert.Equal(content, persistedComment.Content);
        Assert.Equal(comment.CreatedAt, persistedComment.CreatedAt);
        Assert.Equal(TimeSpan.Zero, persistedComment.CreatedAt.Offset);
    }

    [Fact]
    public async Task AddComment_WhenPostDoesNotExist_ShouldReturnNotFoundWithoutPersisting()
    {
        using var factory = new BlogPlatformFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsJsonAsync("/api/posts/1/comments", new { content = "First comment" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        Assert.False(await dbContext.Comments.AnyAsync());
    }

    [Theory]
    [MemberData(nameof(InvalidContents))]
    public async Task AddComment_WhenContentIsInvalid_ShouldReturnBadRequestWithoutPersisting(string? content)
    {
        using var factory = new BlogPlatformFactory();

        var post = new BlogPost
        {
            Title = "My first post",
            Content = "Hello, world!",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await factory.SeedPostsAsync(post);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsJsonAsync($"/api/posts/{post.Id}/comments", new { content });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Contains("Content", problem.Errors.Keys);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        Assert.False(await dbContext.Comments.AnyAsync());
    }

    private sealed record CreatedComment(int Id, string Content, DateTimeOffset CreatedAt);
}
