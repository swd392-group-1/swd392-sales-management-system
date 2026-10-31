
using BookSales.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookSales.Infrastructure.Persistence
{
    public class BookSalesDbContext : DbContext
    {
        public BookSalesDbContext(
            DbContextOptions<BookSalesDbContext> options)
            : base(options)
        {
        }

        public DbSet<Book> Books => Set<Book>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Book>(entity =>
            {
                entity.ToTable("BOOK");

                entity.HasKey(b => b.BookId);

                entity.Property(b => b.BookId)
                    .IsRequired();

                entity.Property(b => b.Title)
                    .IsRequired();

                entity.Property(b => b.CurrentPrice)
                    .HasPrecision(18, 2);
            });
        }
    }
}
