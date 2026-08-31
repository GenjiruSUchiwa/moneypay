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

        builder.ToTable(UsersSchema.UsersTable);
        builder.Ignore(user => user.Phone);
        builder.Ignore(user => user.Email);

        builder.HasKey(user => user.Id).HasName(UsersSchema.UsersPrimaryKey);
        builder.Property(user => user.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(user => user.SignUpId)
            .HasColumnName("sign_up_id")
            .HasConversion(id => id.Value, value => new SignUpId(value))
            .IsRequired();
        builder.Property(user => user.FirstName)
            .HasColumnName("first_name_ciphertext")
            .HasConversion(ciphertext => ciphertext.Value, value => new Ciphertext(value))
            .IsRequired();
        builder.Property(user => user.LastName)
            .HasColumnName("last_name_ciphertext")
            .HasConversion(ciphertext => ciphertext.Value, value => new Ciphertext(value))
            .IsRequired();
        MapContact(builder, "Phone", "phone");
        MapContact(builder, "Email", "email");
        builder.Property(user => user.Locale)
            .HasColumnName("locale")
            .HasConversion(locale => locale.Value, value => new Locale(value))
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(user => user.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(user => user.SignUpId)
            .IsUnique()
            .HasDatabaseName(UsersSchema.SignUpIdUnique);
        builder.HasIndex(PhoneHash)
            .IsUnique()
            .HasDatabaseName(UsersSchema.PhoneLookupHashUnique);
        builder.HasIndex(EmailHash)
            .IsUnique()
            .HasDatabaseName(UsersSchema.EmailLookupHashUnique);

        builder.HasMany(user => user.Consents)
            .WithOne()
            .HasForeignKey(consent => consent.UserId)
            .HasConstraintName(UsersSchema.UserConsentsUserForeignKey)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(user => user.Consents).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private const string PhoneHash = "PhoneHash";
    private const string EmailHash = "EmailHash";

    /// <summary>
    /// A contact is one value in the domain and two columns in the table. The entity keeps the
    /// columns as private properties named <c>&lt;Contact&gt;Ciphertext</c> and
    /// <c>&lt;Contact&gt;Hash</c>; mapping them by name is what lets the hash carry an index.
    /// </summary>
    private static void MapContact(EntityTypeBuilder<User> builder, string contact, string column)
    {
        builder.Property<Ciphertext>(contact + "Ciphertext")
            .HasColumnName(column + "_ciphertext")
            .HasConversion(ciphertext => ciphertext.Value, value => new Ciphertext(value))
            .IsRequired();
        builder.Property<LookupHash>(contact + "Hash")
            .HasColumnName(column + "_lookup_hash")
            .HasConversion(hash => hash.Value, value => new LookupHash(value))
            .IsRequired();
    }
}
