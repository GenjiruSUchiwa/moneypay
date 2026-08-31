using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoniPay.Kernel;
using MoniPay.Users.Domain;

namespace MoniPay.Users.Persistence;

/// <summary>Maps <see cref="User"/> onto the <c>users</c> table.</summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(UsersConstraints.UsersTable);

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(user => user.SignUpId).HasColumnName("sign_up_id").IsRequired();
        builder.Property(user => user.FirstNameCiphertext).HasColumnName("first_name_ciphertext").IsRequired();
        builder.Property(user => user.LastNameCiphertext).HasColumnName("last_name_ciphertext").IsRequired();
        builder.Property(user => user.PhoneCiphertext).HasColumnName("phone_ciphertext").IsRequired();
        builder.Property(user => user.PhoneLookupHash).HasColumnName("phone_lookup_hash").IsRequired();
        builder.Property(user => user.EmailCiphertext).HasColumnName("email_ciphertext").IsRequired();
        builder.Property(user => user.EmailLookupHash).HasColumnName("email_lookup_hash").IsRequired();
        builder.Property(user => user.Locale).HasColumnName("locale").HasMaxLength(16).IsRequired();
        builder.Property(user => user.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(user => user.SignUpId)
            .IsUnique()
            .HasDatabaseName(UsersConstraints.SignUpIdUnique);
        builder.HasIndex(user => user.PhoneLookupHash)
            .IsUnique()
            .HasDatabaseName(UsersConstraints.PhoneLookupHashUnique);
        builder.HasIndex(user => user.EmailLookupHash)
            .IsUnique()
            .HasDatabaseName(UsersConstraints.EmailLookupHashUnique);
    }
}
