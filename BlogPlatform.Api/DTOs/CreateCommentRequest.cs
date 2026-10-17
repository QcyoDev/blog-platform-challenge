using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.DTOs;

public class CreateCommentRequest
{
    [Required]
    [MaxLength(250)]
    public string Content { get; init; } = string.Empty;
}
