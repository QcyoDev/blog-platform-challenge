using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlogPlatform.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlogPlatform.IntegrationTests;

public class ExceptionHandlingTests
{
    [Fact]
    public async Task GetPosts_WhenDatabaseFails_ShouldReturnSafeProblemDetailsAndLogException()
    {
        using var logs = new TestLoggerProvider();
        using var factory = new BlogPlatformFactory();
        using var configuredFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logs)));
        using var client = configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        await using (var scope = configuredFactory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync("DROP TABLE Posts");
        }

        var response = await client.GetAsync("/api/posts");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.Equal("An unexpected error occurred.", problem.Title);
        Assert.Null(problem.Detail);

        var traceId = Assert.IsType<JsonElement>(problem.Extensions["traceId"]).GetString();

        Assert.False(string.IsNullOrWhiteSpace(traceId));

        var entry = Assert.Single(logs.Entries, log =>
            log.Category == "BlogPlatform.Api.ExceptionHandling.GlobalExceptionHandler");

        Assert.NotNull(entry.Exception);
        Assert.Contains("GET", entry.Message);
        Assert.Contains("/api/posts", entry.Message);
        Assert.Contains(traceId!, entry.Message);
        Assert.DoesNotContain(logs.Entries, log =>
            log.Category.StartsWith("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", StringComparison.Ordinal));

        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(entry.Exception.Message, responseBody);
        Assert.DoesNotContain("SqliteException", responseBody);
        Assert.DoesNotContain("stackTrace", responseBody);
    }

    private sealed class TestLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new TestLogger(this, categoryName);

        public void Dispose()
        {
        }

        public sealed record LogEntry(string Category, string Message, Exception? Exception);

        private sealed class TestLogger(TestLoggerProvider provider, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Error;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (IsEnabled(logLevel))
                {
                    provider.Entries.Enqueue(new LogEntry(category, formatter(state, exception), exception));
                }
            }
        }
    }
}
