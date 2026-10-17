using BlogPlatform.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlogPlatform.Api.Data.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Id)
            .ValueGeneratedOnAdd();

        builder.Property(comment => comment.Content)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(comment => comment.CreatedAt)
            .HasConversion(
                createdAt => createdAt.UtcDateTime,
                storedValue => new DateTimeOffset(DateTime.SpecifyKind(storedValue, DateTimeKind.Utc)))
            .IsRequired();

        builder.HasOne(comment => comment.BlogPost)
            .WithMany(post => post.Comments)
            .HasForeignKey(comment => comment.BlogPostId)
            .IsRequired();
    }
}
