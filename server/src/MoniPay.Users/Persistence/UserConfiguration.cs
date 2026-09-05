using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoniPay.Users.Domain;

namespace MoniPay.Users.Persistence;

/// <summary>Maps <see cref="User"/> onto the <c>users</c> table.</summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(UsersSchema.UsersTable);

        builder.HasKey(user => user.Id).HasName(UsersSchema.UsersPrimaryKey);
        builder.Property(user => user.Id).HasColumnName("id");
        builder.Property(user => user.SignUpId).HasColumnName("sign_up_id").IsRequired();
        builder.Property(user => user.FirstName).HasColumnName("first_name_ciphertext").IsRequired();
        builder.Property(user => user.LastName).HasColumnName("last_name_ciphertext").IsRequired();
        builder.OwnsOne(user => user.Phone, contact => MapContact(contact, "phone", UsersSchema.PhoneLookupHashUnique));
        builder.OwnsOne(user => user.Email, contact => MapContact(contact, "email", UsersSchema.EmailLookupHashUnique));
        builder.Property(user => user.Locale).HasColumnName("locale").IsRequired();
        builder.Property(user => user.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(user => user.SignUpId)
            .IsUnique()
            .HasDatabaseName(UsersSchema.SignUpIdUnique);

        builder.HasMany(user => user.Consents)
            .WithOne()
            .HasForeignKey(consent => consent.UserId)
            .HasConstraintName(UsersSchema.UserConsentsUserForeignKey)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(user => user.Consents).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    /// <summary>
    /// A contact is one value in the domain and two columns on the owner's table: the
    /// ciphertext to read back, the keyed hash to look up and keep unique.
    /// </summary>
    private static void MapContact(
        OwnedNavigationBuilder<User, ProtectedContact> contact,
        string column,
        string uniqueIndex)
    {
        contact.Property(value => value.Ciphertext).HasColumnName(column + "_ciphertext").IsRequired();
        contact.Property(value => value.Hash).HasColumnName(column + "_lookup_hash").IsRequired();
        contact.HasIndex(value => value.Hash).IsUnique().HasDatabaseName(uniqueIndex);
    }
}
