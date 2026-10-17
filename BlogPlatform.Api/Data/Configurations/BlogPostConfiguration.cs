using BlogPlatform.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlogPlatform.Api.Data.Configurations;

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.HasKey(post => post.Id);

        builder.Property(post => post.Id)
            .ValueGeneratedOnAdd();

        builder.Property(post => post.Title)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(post => post.Content)
            .HasMaxLength(10_000)
            .IsRequired();

        builder.Property(post => post.CreatedAt)
            .HasConversion(
                createdAt => createdAt.UtcDateTime,
                storedValue => new DateTimeOffset(DateTime.SpecifyKind(storedValue, DateTimeKind.Utc)))
            .IsRequired();
    }
}
