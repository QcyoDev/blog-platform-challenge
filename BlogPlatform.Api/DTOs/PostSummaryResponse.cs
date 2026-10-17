namespace BlogPlatform.Api.DTOs;

public record PostSummaryResponse(int Id, string Title, int CommentCount, DateTimeOffset CreatedAt);
