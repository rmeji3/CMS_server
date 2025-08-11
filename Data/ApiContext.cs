using Microsoft.EntityFrameworkCore;
using CMS.Models;

namespace CMS.Data
{
    public class ApiContext : DbContext
    {  
        public DbSet<Credentials> Credentials { get; set; }
        public ApiContext(DbContextOptions<ApiContext> options) 
            :base(options)
        {
        }
    }
}
