using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public DbSet<PaymentRecord> PaymentRecords { get; set; }

    // Add other DbSet properties as needed

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
}

public class PaymentRecord
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string PaymentId { get; set; }
    public string PayerId { get; set; }
    public string Email { get; set; }
    public string PaymentStatus { get; set; }
}