namespace BlogPlatform.Api.Entities;

public class BlogPost
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<Comment> Comments { get; set; } = [];
}
