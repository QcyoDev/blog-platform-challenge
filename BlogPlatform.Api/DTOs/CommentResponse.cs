namespace BlogPlatform.Api.DTOs;

public record CommentResponse(int Id, string Content, DateTimeOffset CreatedAt);
