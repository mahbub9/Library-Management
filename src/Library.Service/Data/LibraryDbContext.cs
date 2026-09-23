using Library.Service.Domain;
using Microsoft.EntityFrameworkCore;

namespace Library.Service.Data;

public class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    public DbSet<Patron> Patrons => Set<Patron>();

    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Book>(book =>
        {
            book.Property(b => b.Title).HasMaxLength(200);
            book.Property(b => b.Author).HasMaxLength(120);
        });

        model.Entity<Patron>(patron => patron.Property(p => p.Name).HasMaxLength(120));

        model.Entity<Loan>(loan =>
        {
            loan.Ignore(l => l.IsOut);

            // A return lands only while the loan is still out: of two at once, the second fails.
            loan.Property(l => l.ReturnedOn).IsConcurrencyToken();

            loan.HasOne(l => l.Book).WithMany().HasForeignKey(l => l.BookId);
            loan.HasOne(l => l.Patron).WithMany().HasForeignKey(l => l.PatronId);

            loan.HasIndex(l => new { l.BookId, l.CheckedOutOn });
            loan.HasIndex(l => new { l.PatronId, l.CheckedOutOn });

            // One open loan per patron per title, enforced here rather than trusted to the service.
            // The filter keeps the index small however long the circulation history gets.
            loan.HasIndex(l => new { l.PatronId, l.BookId })
                .HasFilter("[ReturnedOn] IS NULL")
                .IsUnique();
        });
    }
}
