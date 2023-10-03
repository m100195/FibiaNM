using System.Data.Entity;

namespace FibiaNM.Logging
{

    /// <summary>
    /// Logging dbset
    /// </summary>
    public class FibiaNMDbContext : DbContext
    {
        public DbSet<Log>? Logs { get; set; }
    }
}
