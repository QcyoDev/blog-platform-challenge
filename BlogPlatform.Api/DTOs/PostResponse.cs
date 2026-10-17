namespace BlogPlatform.Api.DTOs;

public record PostResponse(int Id, string Title, string Content, DateTimeOffset CreatedAt)
{
    public IReadOnlyList<CommentResponse> Comments { get; init; } = [];
}
