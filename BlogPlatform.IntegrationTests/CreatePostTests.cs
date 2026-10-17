using System.Net;
using System.Net.Http.Json;
using BlogPlatform.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlogPlatform.IntegrationTests;

public class CreatePostTests
{
    [Fact]
    public async Task CreatePost_WhenRequestIsValid_ShouldPersistPostAndReturnCreated()
    {
        using var factory = new BlogPlatformFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var title = new string('t', 100);
        var content = new string('c', 10_000);
        var beforeRequest = DateTimeOffset.UtcNow;

        var response = await client.PostAsJsonAsync("/api/posts", new { title, content });

        var afterResponse = DateTimeOffset.UtcNow;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var post = await response.Content.ReadFromJsonAsync<CreatedPost>();

        Assert.NotNull(post);
        Assert.True(post.Id > 0);
        Assert.Equal(title, post.Title);
        Assert.Equal(content, post.Content);
        Assert.Equal(TimeSpan.Zero, post.CreatedAt.Offset);
        Assert.InRange(post.CreatedAt, beforeRequest, afterResponse);

        Assert.NotNull(response.Headers.Location);
        Assert.Equal(new Uri(client.BaseAddress!, $"/api/posts/{post.Id}"), response.Headers.Location);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var persistedPost = await dbContext.Posts.AsNoTracking().SingleAsync();

        Assert.Equal(post.Id, persistedPost.Id);
        Assert.Equal(title, persistedPost.Title);
        Assert.Equal(content, persistedPost.Content);
        Assert.Equal(post.CreatedAt, persistedPost.CreatedAt);
        Assert.Equal(TimeSpan.Zero, persistedPost.CreatedAt.Offset);
    }

    [Theory]
    [InlineData(null, "Content", "Title")]
    [InlineData("", "Content", "Title")]
    [InlineData("   ", "Content", "Title")]
    [InlineData("Title", null, "Content")]
    [InlineData("Title", "", "Content")]
    [InlineData("Title", "   ", "Content")]
    public async Task CreatePost_WhenRequiredFieldsAreInvalid_ShouldReturnBadRequest(
        string? title,
        string? content,
        string invalidField)
    {
        using var factory = new BlogPlatformFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsJsonAsync("/api/posts", new { title, content });

        await AssertValidationFailureAsync(factory, response, invalidField);
    }

    [Theory]
    [InlineData(101, 10_000, "Title")]
    [InlineData(100, 10_001, "Content")]
    public async Task CreatePost_WhenFieldExceedsMaxLength_ShouldReturnBadRequest(
        int titleLength,
        int contentLength,
        string invalidField)
    {
        using var factory = new BlogPlatformFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsJsonAsync("/api/posts", new
        {
            title = new string('t', titleLength),
            content = new string('c', contentLength)
        });

        await AssertValidationFailureAsync(factory, response, invalidField);
    }

    private static async Task AssertValidationFailureAsync(
        BlogPlatformFactory factory,
        HttpResponseMessage response,
        string invalidField)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Contains(invalidField, problem.Errors.Keys);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        Assert.False(await dbContext.Posts.AnyAsync());
    }

    private sealed record CreatedPost(int Id, string Title, string Content, DateTimeOffset CreatedAt);
}
