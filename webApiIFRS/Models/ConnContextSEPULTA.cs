using Microsoft.EntityFrameworkCore;

namespace webApiIFRS.Models
{
    public class ConnContextSEPULTA : DbContext
    {
        public ConnContextSEPULTA(DbContextOptions<ConnContextSEPULTA> options)
        : base(options)
        {

        }
    }
}
