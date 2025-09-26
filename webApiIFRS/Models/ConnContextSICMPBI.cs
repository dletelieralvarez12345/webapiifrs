using Microsoft.EntityFrameworkCore;

namespace webApiIFRS.Models
{
    public class ConnContextSICMPBI : DbContext
    {
        public ConnContextSICMPBI(DbContextOptions<ConnContextSICMPBI> options)
        : base(options)
        {

        }
    }
}
