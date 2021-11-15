using Microsoft.EntityFrameworkCore;

namespace Learn.OpenIddict
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext>options) :base(options)
        {

        }

        public DbSet<User> Users { get; set; }
    }
}
