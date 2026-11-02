using Microsoft.EntityFrameworkCore;
using booking.core.DTOs;
using booking.core.Models;

namespace booking.infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserClaim> UserClaims => Set<UserClaim>();
    public DbSet<RoleClaim> RoleClaims => Set<RoleClaim>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<ServiceDetail> ServiceDetails => Set<ServiceDetail>();
    public DbSet<Employee> Employees => Set<Employee>();


    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().ToTable("Users");
        b.Entity<User>().HasKey(x => x.Id);
        b.Entity<User>().Property(x => x.EmailConfirmed).HasColumnType("INTEGER");
        b.Entity<User>().Property(x => x.TwoFactorEnabled).HasColumnType("INTEGER");

        b.Entity<Role>().ToTable("Roles").HasKey(x => x.Id);

        b.Entity<UserRole>().ToTable("UserRoles");
        b.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });
        b.Entity<UserRole>()
            .HasOne(ur => ur.User).WithMany(u => u.Roles)
            .HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<UserRole>()
            .HasOne(ur => ur.Role).WithMany(r => r.Users)
            .HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<UserClaim>().ToTable("UserClaims").HasKey(x => x.Id);
        b.Entity<RoleClaim>().ToTable("RoleClaims").HasKey(x => x.Id);

        b.Entity<Employee>(e =>
        {
            e.ToTable("Employees");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.FirstName).HasColumnName("first_name");
            e.Property(x => x.LastName).HasColumnName("last_name");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId);
        });

        b.Entity<Service>(e =>
        {
            e.ToTable("Service");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("ID");
            e.Property(x => x.OfficeId).HasColumnName("office_id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.MaxBookingsPerWeek).HasColumnName("max_bookings_week");
            e.Property(x => x.BookingPerTime).HasColumnName("booking_per_time");
            e.Property(x => x.MinMax).HasColumnName("min_max");
            e.Property(x => x.ImageUrl).HasColumnName("image");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasMany(x => x.ServiceDetails)
             .WithOne(d => d.Service!)
             .HasForeignKey(d => d.ServiceId);
        });

        b.Entity<ServiceDetail>(e =>
        {
            e.ToTable("Service_Details");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("ID");
            e.Property(x => x.ServiceId).HasColumnName("service_id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Capacity).HasColumnName("capacity");
            e.Property(x => x.IconUrl).HasColumnName("icon");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        b.Entity<Office>(e =>
        {
            e.ToTable("Office");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("ID");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Image).HasColumnName("image");
        });

        b.Entity<Booking>(e =>
        {
            e.ToTable("Booking");
            e.HasKey(x => x.Id);
            e.Property(x => x.ServiceDetailId).HasColumnName("service_detail_id");
            e.Property(x => x.EmployeeId).HasColumnName("employee_id");
            e.Property(x => x.StartDatetime).HasColumnName("start_datetime");
            e.Property(x => x.EndDatetime).HasColumnName("end_datetime");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
    }
}