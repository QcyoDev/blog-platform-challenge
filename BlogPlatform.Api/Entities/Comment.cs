namespace BlogPlatform.Api.Entities;

public class Comment
{
    public int Id { get; set; }
    public int BlogPostId { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public BlogPost BlogPost { get; set; } = null!;
}
