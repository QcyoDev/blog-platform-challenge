using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.DTOs;

public class CreatePostRequest
{
    [Required]
    [MaxLength(100)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MaxLength(10_000)]
    public string Content { get; init; } = string.Empty;
}
