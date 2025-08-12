using System.Data.Entity;
using System.Configuration;

namespace GadgetHub.API.Models
{
    public class GadgetHubDBContext : DbContext
    {
        public GadgetHubDBContext() : base("GadgetHubConnection")
        {
            Database.SetInitializer<GadgetHubDBContext>(null);
        }
        
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Distributor> Distributors { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Cart { get; set; }
        public DbSet<Quotation> Quotations { get; set; }
        public DbSet<QuotationItem> QuotationItems { get; set; }
        public DbSet<DistributorResponse> DistributorResponses { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configure decimal properties with proper precision and scale
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(10, 2);
                
            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.PricePerUnit)
                .HasPrecision(10, 2);
                
            modelBuilder.Entity<DistributorResponse>()
                .Property(dr => dr.PricePerUnit)
                .HasPrecision(10, 2);
            
            base.OnModelCreating(modelBuilder);
        }
    }
}