using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Data;
using System.Threading.Tasks;

namespace webApiIFRS.Models
{
    public class ConnContext : DbContext
    {
        public ConnContext(DbContextOptions<ConnContext> options)
        :base(options)
        { 

        }

        //DbSet representa el conjunto de entidades, en entityFram.. un conj de 
        //entidades se alinea con una tabla de la BD y una entidad corresponde 
        //a una fila individual dentro de la tabla.
        public DbSet<Contrato> Contrato { get; set; } = null;
        public DbSet<Ingresos_Diferidos> IngresosDiferidos { get; set; } = null;
        public DbSet<Intereses_Por_Devengar> InteresesPorDevengar { get; set; } = null;


        //PROCEDIMIENTOS ALMACENADOS SIN PARAMETROS
        public async Task<DataTable> getContratos()
        {
            var contratos = await Contrato
                .FromSqlRaw("EXEC SP_IFRS_GETALLCONTRATOS")
                .ToListAsync();
            return new DataTable();
        }

        //PROCEDIMIENTOS ALMACENADOS CON PARAMETROS
        public async Task<DataTable> getContratoByNumCon(int numContrato)
        {
            var contrato = await Contrato
                .FromSqlInterpolated($"EXEC SP_IFRS_GETCONTRATO_BY_NUMCON {numContrato}")
                .ToListAsync();
            return new DataTable(); 
        }
    }
}
