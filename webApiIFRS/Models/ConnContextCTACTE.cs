using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Data;
using System.Threading.Tasks;

namespace webApiIFRS.Models
{
    public class ConnContextCTACTE : DbContext
    {
        public ConnContextCTACTE(DbContextOptions<ConnContextCTACTE> options)
        : base(options)
        {

        }
    }
}
