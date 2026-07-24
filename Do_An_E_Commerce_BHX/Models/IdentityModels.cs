using Do_An_E_Commerce_BHX.Models.Entities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections.Generic;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Do_An_E_Commerce_BHX.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public virtual ICollection<Entities.UserAddress> UserAddresses { get; set; }

        public int LoyaltyPoints { get; set; } = 0;

        public Cart Cart { get; set; } = new Cart();
        public List<Order> Orders { get; set; } = new List<Order>();




        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("name=DefaultConnection", throwIfV1Schema: false)
        {
        }
        public DbSet<Entities.UserAddress> UserAddresses { get; set; }
        public DbSet<Entities.Cart> Cart { get; set; }
        public DbSet<Entities.CartDetail> CartDetail { get; set; }
        public DbSet<Entities.Category>Category { get; set; }
        public DbSet<Entities.Order> Order { get; set; }
        public DbSet<Entities.OrderDetail> OrderDetail { get; set; }
        public DbSet<Entities.Preview> Preview { get; set; }
        public DbSet<Entities.Product> Product { get; set; }
        public DbSet<Entities.Promotion> Promotion { get; set; }
        public DbSet<Entities.Question> Question { get; set; }
        public DbSet<Entities.Review> Review{ get; set; }
        public DbSet<Entities.Waranty> Waranty { get; set; }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}