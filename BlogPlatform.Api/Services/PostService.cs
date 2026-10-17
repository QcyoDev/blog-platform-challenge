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
