using BlogPlatform.Api.Controllers;
using BlogPlatform.Api.Data;
using BlogPlatform.Api.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlogPlatform.IntegrationTests;

public sealed class BlogPlatformFactory : WebApplicationFactory<PostsController>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(), $"blog-platform-tests-{Guid.NewGuid():N}.db");

    public string ConnectionString => $"Data Source={_databasePath};Pooling=False";

    public async Task SeedPostsAsync(params BlogPost[] posts)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        dbContext.Posts.AddRange(posts);

        await dbContext.SaveChangesAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BlogDatabase"] = ConnectionString
            }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            File.Delete(_databasePath);
        }
    }
}
