using Microsoft.EntityFrameworkCore;
using RevenueRecognitionAPI.Models;

namespace RevenueRecognitionAPI.Data;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }

    public DbSet<Client> Clients { get; set; }
    public DbSet<IndividualClient> IndividualClients { get; set; }
    public DbSet<CompanyClient> CompanyClients { get; set; }
    public DbSet<Software> Software { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<Employee> Employees { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Client>()
            .HasDiscriminator<string>("ClientType")
            .HasValue<IndividualClient>("Individual")
            .HasValue<CompanyClient>("Company");
        
        modelBuilder.Entity<Employee>().HasData(new List<Employee>
        {
            new Employee {
                Id = 1,
                Login = "admin",
                PasswordHash = "$2a$11$GjgLaRNVvUSJjZZDdK240uK/Sb6Ah3np4hOZjEc4ubwZcEHFhmHHy",
                Role = EmployeeRole.Admin,
                FirstName = "Admin",
                LastName = "User",
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Employee {
                Id = 2,
                Login = "user",
                PasswordHash = "$2a$11$GVGCtr1QpKhgZjMBZ.2ZN.0JQTArwK8Z0mIv.eUOhaKCyNAnEChRy",
                Role = EmployeeRole.StandardUser,
                FirstName = "Standard",
                LastName = "User",
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        });
        
        modelBuilder.Entity<Software>().HasData(new List<Software>
        {
            new Software {
                Id = 1,
                Name = "FinanceManager Pro",
                Description = "Comprehensive financial management software",
                CurrentVersion = "2.1.0",
                Category = "Finances",
                UpfrontPrice = 5000.00M
            },
            new Software {
                Id = 2,
                Name = "EduSoft Suite",
                Description = "Educational institution management system",
                CurrentVersion = "1.5.2",
                Category = "Education",
                UpfrontPrice = 3000.00M
            }
        });
        
        modelBuilder.Entity<Discount>().HasData(new List<Discount>
        {
            new Discount {
                Id = 1,
                Name = "Black Friday Discount",
                Percentage = 15.0M,
                StartDate = new DateTime(2025, 11, 25, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2025, 11, 30, 23, 59, 59, DateTimeKind.Utc),
                SoftwareId = null
            },
            new Discount {
                Id = 2,
                Name = "New Year Special",
                Percentage = 10.0M,
                StartDate = new DateTime(2025, 12, 20, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 1, 10, 23, 59, 59, DateTimeKind.Utc),
                SoftwareId = 1
            }
        });
        
        modelBuilder.Entity<IndividualClient>().HasData(new List<IndividualClient>
        {
            new IndividualClient {
                Id = 1,
                FirstName = "Jan",
                LastName = "Kowalski",
                PESEL = "85010112345",
                Address = "ul. Warszawska 10, 00-001 Warszawa",
                Email = "jan.kowalski@email.com",
                PhoneNumber = "+48123456789",
                CreatedAt = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc)
            }
        });
        
        modelBuilder.Entity<CompanyClient>().HasData(new List<CompanyClient>
        {
            new CompanyClient {
                Id = 2,
                CompanyName = "Tech Solutions Sp. z o.o.",
                KRS = "0000123456",
                Address = "ul. Biznesowa 5, 02-001 Warszawa",
                Email = "contact@techsolutions.pl",
                PhoneNumber = "+48987654321",
                CreatedAt = new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc)
            }
        });
    }
}