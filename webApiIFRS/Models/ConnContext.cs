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
        public DbSet<InteresesPorDevengar> InteresesPorDevengar { get; set; } = null;
        public DbSet<PagoRealizado> PagosRealizados { get; set; } = null;
        public DbSet<PagosRealizadosTerreno> pagosRealizadosTerreno { get; set; } = null;
        public DbSet<Modificaciones> Modificaciones { get; set; } = null;
        public DbSet<FechaPrimerVtoBOV> fechaPrimerVtoBOV { get; set; } = null;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //tablas que no existen en la bd y/o no tiene pk
            modelBuilder.Entity<PagoRealizado>().HasNoKey();
            modelBuilder.Entity<PagosRealizadosTerreno>().HasNoKey();
            modelBuilder.Entity<Modificaciones>().HasNoKey();
            modelBuilder.Entity<FechaPrimerVtoBOV>().HasNoKey();
        }


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

        public async Task<DataTable> ListaContratosPorAnio(int anio)
        {
            DataTable dt = new DataTable();
            var contratos = await Contrato
                .FromSqlInterpolated($"EXEC SP_IFRS_GETCONTRATOS {anio}")
                .ToListAsync();

            dt.Columns.Add("con_id", typeof(int));
            dt.Columns.Add("con_num_con", typeof(string));
            dt.Columns.Add("con_id_tipo_ingreso", typeof(int));
            dt.Columns.Add("con_fecha_ingreso", typeof(DateTime));
            dt.Columns.Add("con_total_venta", typeof(int));
            dt.Columns.Add("con_precio_base", typeof(int));
            dt.Columns.Add("con_pie", typeof(int));
            dt.Columns.Add("con_total_credito", typeof(int));
            dt.Columns.Add("con_cuotas_pactadas", typeof(int));
            dt.Columns.Add("con_valor_cuota_pactada", typeof(int));
            dt.Columns.Add("con_tasa_interes", typeof(int));
            dt.Columns.Add("con_capacidad_sepultura", typeof(int));
            dt.Columns.Add("con_tipo_compra", typeof(string));
            dt.Columns.Add("con_terminos_pago", typeof(string));
            dt.Columns.Add("con_nombre_cajero", typeof(string));
            dt.Columns.Add("con_fecha_primer_vcto_ori", typeof(DateTime));
            dt.Columns.Add("con_tipo_movimiento", typeof(int));
            dt.Columns.Add("con_cuotas_pactadas_mod", typeof(int));
            dt.Columns.Add("con_estado_contrato", typeof(int));
            dt.Columns.Add("con_num_repactaciones", typeof(int));   
            dt.Columns.Add("con_anos_arriendo", typeof(int));

            foreach (var x in contratos)
            {
                dt.Rows.Add(
                    x.con_id, 
                    x.con_num_con,
                    x.con_id_tipo_ingreso,
                    (object?)x.con_fecha_ingreso ?? DBNull.Value,
                    x.con_total_venta,
                    x.con_precio_base,
                    x.con_pie,
                    x.con_total_credito,
                    x.con_cuotas_pactadas,
                    x.con_valor_cuota_pactada,
                    x.con_tasa_interes,
                    x.con_capacidad_sepultura,
                    x.con_tipo_compra,
                    x.con_terminos_pago,
                    x.con_nombre_cajero,
                    (object?)x.con_fecha_primer_vcto_ori ?? DBNull.Value,
                    x.con_tipo_movimiento,
                    x.con_cuotas_pactadas_mod,
                    x.con_estado_contrato,
                    x.con_num_repactaciones,
                    x.con_anos_arriendo
                );
            }

            return dt;
        }

        public async Task<DataTable> ObtenerPagosRealizados(int anio)
        {
            DataTable dt = new DataTable();

            var pagoRealizados = await PagosRealizados
                .FromSqlInterpolated($"EXEC SP_IFRS_GETPAGOSREALIZADOS_BY_NUMCON {anio}")
                .ToListAsync();

            dt.Columns.Add("numero_comprobante", typeof(int));
            dt.Columns.Add("fecha_Pago", typeof(DateTime));
            dt.Columns.Add("fecha_vcto", typeof(DateTime));
            dt.Columns.Add("contrato", typeof(string));
            dt.Columns.Add("tipo_cto", typeof(int));
            dt.Columns.Add("valor_cuota", typeof(int));
            dt.Columns.Add("tipo_pago", typeof(int));
            dt.Columns.Add("numero_cuota", typeof(int));

            foreach (var x in pagoRealizados)
            {
                dt.Rows.Add(
                    x.numero_comprobante,
                    (object?)x.fecha_Pago ?? DBNull.Value,
                    (object?)x.fecha_vcto ?? DBNull.Value,
                    x.contrato,
                    x.tipo_cto,
                    x.valor_cuota,
                    x.tipo_pago, 
                    x.numero_cuota
                    );
            }

            return dt;
        }

        public async Task<DataTable> ObtenerPagosRealizadosTerreno(int anio)
        {
            DataTable dt = new DataTable();
            var pagoRealizadosTerreno = await pagosRealizadosTerreno
                .FromSqlInterpolated($"EXEC SP_IFRS_GETPAGOS_REALIZADOS_TERRENO {anio}")
                .ToListAsync();

            dt.Columns.Add("numero_comprobante", typeof(int));
            dt.Columns.Add("fecha_Pago", typeof(DateTime));
            dt.Columns.Add("fecha_Vto", typeof(DateTime));
            dt.Columns.Add("contrato", typeof(string));
            dt.Columns.Add("tipo_cto", typeof(int));
            dt.Columns.Add("valor_cuota", typeof(int));
            dt.Columns.Add("tipo_pago", typeof(int));

            foreach (var x in pagoRealizadosTerreno)
            {
                dt.Rows.Add(
                    x.numero_comprobante,
                    (object?)x.fecha_Pago ?? DBNull.Value,
                    (object?)x.fecha_vto ?? DBNull.Value,
                    x.contrato,
                    x.tipo_cto,
                    x.valor_cuota,
                    x.tipo_pago
                );
            }
            return dt;
        }

        public async Task<DataTable> ObtenerModificaciones(int anio)
        {
            DataTable dt = new DataTable();

            var modificaciones = await Modificaciones
                .FromSqlInterpolated($"EXEC SP_IFRS_GETMODIFICACIONES_BY_NUMCON {anio}")
                .ToListAsync();

            dt.Columns.Add("numero_contrato", typeof(string));
            dt.Columns.Add("fecha_modificacion", typeof(DateTime));
            dt.Columns.Add("cuotas_pactadas_nuevo", typeof(int));
            dt.Columns.Add("cuotas_pactadas_antiguo", typeof(int));
            dt.Columns.Add("valor_cuota_nuevo", typeof(int));
            dt.Columns.Add("valor_cuota_antiguo", typeof(int));
            dt.Columns.Add("pie_nuevo", typeof(int));
            dt.Columns.Add("valor_abonado", typeof(int));
            dt.Columns.Add("total_venta_nuevo", typeof(int));
            dt.Columns.Add("fecha_primer_vto", typeof(DateTime));
            dt.Columns.Add("di18", typeof(int));
            dt.Columns.Add("tipo_sistema", typeof(int));

            foreach (var p in modificaciones)
            {
                dt.Rows.Add(
                    p.numero_contrato,
                    (object?)p.fecha_modificacion ?? DBNull.Value,
                    p.cuotas_pactadas_nuevo,
                    p.cuotas_pactadas_antiguo,
                    p.valor_cuota_nuevo,
                    p.valor_cuota_antiguo,
                    p.pie_nuevo,
                    p.valor_abonado,
                    p.total_venta_nuevo,
                    (object?)p.fecha_primer_vto ?? DBNull.Value,
                    p.di18,
                    p.tipo_sistema
                );
            }

            return dt;
        }

        public async Task<DataTable> ObtenerFechaPrimerVctoBov(int anio)
        {

            DataTable dt = new DataTable();

            var fecPrimerVtoBOV = await fechaPrimerVtoBOV
                .FromSqlInterpolated($"EXEC SP_IFRS_GETFECHA_PRIMER_VTO_BOV {anio}")
                .ToListAsync();

            dt.Columns.Add("num_contrato", typeof(string));
            dt.Columns.Add("fecha_primer_vcto", typeof(DateTime));
            dt.Columns.Add("fecha_vto_cuota", typeof(DateTime));

            foreach (var p in fecPrimerVtoBOV)
            {
                dt.Rows.Add(
                        p.num_contrato,
                        (object?)p.fecha_primer_vcto ?? DBNull.Value,
                        (object?)p.fecha_vto_cuota ?? DBNull.Value
                );
            }

            return dt;
        }

    }
}
