using BlogPlatform.Api.DTOs;
using BlogPlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogPlatform.Api.Controllers;

[ApiController]
[Route("api/posts")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public class PostsController(PostService postService) : ControllerBase
{
    /// <summary>
    /// Creates a post and returns its URL in the Location header.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<PostResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostResponse>> Create(CreatePostRequest request, CancellationToken cancellationToken)
    {
        var post = await postService.CreateAsync(
            request.Title,
            request.Content,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = post.Id },
            post);
    }

    /// <summary>
    /// Adds a comment to an existing post.
    /// </summary>
    [HttpPost("{id:int}/comments")]
    [ProducesResponseType<CommentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentResponse>> AddComment(int id, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var comment = await postService.AddCommentAsync(
            id,
            request.Content,
            cancellationToken);

        if (comment is null)
        {
            return NotFound();
        }

        return StatusCode(StatusCodes.Status201Created, comment);
    }

    /// <summary>
    /// Returns all posts with their comment counts, newest IDs first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PostSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PostSummaryResponse>>> Get(CancellationToken cancellationToken)
    {
        var posts = await postService.GetAsync(cancellationToken);

        return Ok(posts);
    }

    /// <summary>
    /// Returns a post with its content and comments.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PostResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var post = await postService.GetByIdAsync(id, cancellationToken);

        if (post is null)
        {
            return NotFound();
        }

        return Ok(post);
    }
}
