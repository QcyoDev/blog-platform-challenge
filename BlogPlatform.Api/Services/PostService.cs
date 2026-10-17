using BlogPlatform.Api.Data;
using BlogPlatform.Api.DTOs;
using BlogPlatform.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Api.Services;

public class PostService(BlogDbContext dbContext)
{
    public async Task<PostResponse> CreateAsync(
        string title,
        string content,
        CancellationToken cancellationToken)
    {
        var post = new BlogPost
        {
            Title = title,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Posts.Add(post);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PostResponse(post.Id, post.Title, post.Content, post.CreatedAt);
    }

    public async Task<IReadOnlyList<PostSummaryResponse>> GetAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Posts
            .AsNoTracking()
            .OrderByDescending(post => post.Id)
            .Select(post => new PostSummaryResponse(post.Id, post.Title, post.Comments.Count, post.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public Task<PostResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return dbContext.Posts
            .AsNoTracking()
            .Where(post => post.Id == id)
            .Select(post => new PostResponse(post.Id, post.Title, post.Content, post.CreatedAt)
            {
                Comments = post.Comments
                    .Select(comment => new CommentResponse(comment.Id, comment.Content, comment.CreatedAt))
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }
}
