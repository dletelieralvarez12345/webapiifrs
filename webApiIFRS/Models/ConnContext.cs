using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Data;
using System.Security.Cryptography.Xml;
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

        #region DBSET REPRESENTA CONJUNTO DE ENTIDADES 
        public DbSet<Contrato> Contrato { get; set; } = null;
        public DbSet<ContratoDTO> ContratoDTO { get; set; } = null;
        public DbSet<IngresosDiferidosNichos> IngresosDiferidos { get; set; } = null;
        public DbSet<InteresesPorDevengar> InteresesPorDevengar { get; set; } = null;
        public DbSet<PagoRealizado> PagosRealizados { get; set; } = null;
        public DbSet<PagosRealizadosTerreno> pagosRealizadosTerreno { get; set; } = null;
        public DbSet<Modificaciones> Modificaciones { get; set; } = null;
        public DbSet<FechaPrimerVtoBOV> fechaPrimerVtoBOV { get; set; } = null;
        public DbSet<DerechosServicios> DerechosServicios { get; set; } = null;
        public DbSet<TerminoProducto> TerminoProducto { get; set; } = null;
        public DbSet<IngresosDiferidosBovedas> IngresosDiferidosBovedas { get; set; } = null;
        public DbSet<IngresosDiferidosSFT> IngresosDiferidosSFT { get; set; } = null;
        public DbSet<IngresosDiferidosBovedasPremium> IngresosDiferidosBovedasPremium { get; set; } = null;
        public DbSet<IngresosDiferidosNichosUpgrade> IngresosDiferidosNichosUpgrade { get; set; } = null;
        public DbSet<ServiciosNUP> serviciosNUP { get; set; } = null; 
        #endregion

        #region CONFIGURA EL MODELO DE DATOS QUE EF USARÁ PARA MAPEAR LAS CLASES C# A LAS TABLAS DE LA BASE DE DATOS.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //tablas que no existen en la bd y/o no tiene pk
            modelBuilder.Entity<PagoRealizado>().HasNoKey();
            modelBuilder.Entity<PagosRealizadosTerreno>().HasNoKey();
            modelBuilder.Entity<Modificaciones>().HasNoKey();
            modelBuilder.Entity<FechaPrimerVtoBOV>().HasNoKey();
            modelBuilder.Entity<ContratoDTO>().HasNoKey();
            modelBuilder.Entity<DerechosServicios>().HasNoKey();
            modelBuilder.Entity<TerminoProducto>().HasNoKey();
            modelBuilder.Entity<ServiciosNUP>().HasNoKey(); 

            modelBuilder.Entity<Contrato>(entity =>
            {
                entity.ToTable("CONTRATO");
                entity.Property(e => e.con_derechos_servicios_con_iva)
                      .HasColumnName("CON_DERECHOS_SERVICIOS_CON_IVA")
                      .HasPrecision(18,0)
                      .HasColumnType("decimal")
                      .ValueGeneratedNever();
            });

            modelBuilder.Entity<IngresosDiferidosNichos>(entity =>
            {
                entity.ToTable("INGRESOS_DIFERIDOS_NICHOS");
                entity.Property(e => e.ing_a_diferir)
                      .HasColumnName("ING_A_DIFERIR")
                      .HasPrecision(18, 0)
                      .HasColumnType("decimal")
                      .ValueGeneratedNever();
            });
        }
        #endregion

        #region PROC.ALMACENADO QUE RETORNA TODOS LOS CONTRATOS DE UNA FECHA, ESTOS DATOS PROVIENEN DE TABLA CONTRATOS
        public async Task<DataTable> ListaContratosPorAnio(string fechaDesde, string fechaHasta)
        {
            DataTable dt = new DataTable();
            var contratos = await Contrato
                .FromSqlInterpolated($"EXEC SP_IFRS_GETCONTRATOS {fechaDesde}, {fechaHasta}")
                .ToListAsync();

            //dt.Columns.Add("con_id", typeof(int));
            dt.Columns.Add("con_num_con", typeof(string));
            dt.Columns.Add("con_num_comprobante", typeof(int));
            dt.Columns.Add("con_id_tipo_ingreso", typeof(int));
            dt.Columns.Add("con_fecha_ingreso", typeof(DateTime));
            dt.Columns.Add("con_total_venta", typeof(decimal));
            dt.Columns.Add("con_precio_base", typeof(decimal));
            dt.Columns.Add("con_pie", typeof(decimal));
            dt.Columns.Add("con_total_credito", typeof(decimal));
            dt.Columns.Add("con_intereses", typeof(decimal));
            dt.Columns.Add("con_cuotas_pactadas", typeof(int));
            dt.Columns.Add("con_valor_cuota_pactada", typeof(decimal));
            dt.Columns.Add("con_tasa_interes", typeof(int));
            dt.Columns.Add("con_capacidad_sepultura", typeof(int));
            dt.Columns.Add("con_tipo_compra", typeof(string));
            dt.Columns.Add("con_terminos_pago", typeof(string));
            dt.Columns.Add("con_nombre_cajero", typeof(string));
            dt.Columns.Add("con_fecha_primer_vcto_ori", typeof(DateTime));
            dt.Columns.Add("con_tipo_movimiento", typeof(int));
            dt.Columns.Add("con_cuotas_pactadas_mod", typeof(int));
            dt.Columns.Add("con_estado_contrato", typeof(string));
            dt.Columns.Add("con_num_repactaciones", typeof(int));   
            dt.Columns.Add("con_anos_arriendo", typeof(int));
            dt.Columns.Add("con_derechos_servicios_con_iva", typeof(decimal));
            dt.Columns.Add("con_fecha_termino_producto", typeof(DateTime)); 

            foreach (var x in contratos)
            {
                dt.Rows.Add(
                    //x.con_id, 
                    x.con_num_con,
                    x.con_num_comprobante, 
                    x.con_id_tipo_ingreso,
                    (object?)x.con_fecha_ingreso ?? DBNull.Value,
                    x.con_total_venta,
                    x.con_precio_base,
                    x.con_pie,
                    x.con_total_credito,
                    x.con_intereses, 
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
                    x.con_anos_arriendo, 
                    x.con_derechos_servicios_con_iva, 
                    x.con_fecha_termino_producto
                );
            }

            return dt;
        }
        #endregion

        #region PROC.ALMACENADO QUE RETORNA TODOS LOS CONTRATOS DE UNA FECHA, ESTOS DATOS PROVIENEN DE VISTA INGRESOS CONTABLES
        public async Task<DataTable> ListaIngresosDeVentasAllContratos(string fechaDesde, string fechaHasta)
        {
            DataTable dt = new DataTable();
            var contratos = await Set<ContratoDTO>()
                .FromSqlInterpolated($"EXEC SP_IFRS_INGRESOS_DE_VENTAS_ALL_CONTRATOS {fechaDesde}, {fechaHasta}")
                .ToListAsync();

            //dt.Columns.Add("con_id", typeof(int));
            dt.Columns.Add("con_num_con", typeof(string));
            dt.Columns.Add("con_num_comprobante", typeof(int));
            dt.Columns.Add("con_id_tipo_ingreso", typeof(int));
            dt.Columns.Add("con_fecha_ingreso", typeof(DateTime));
            dt.Columns.Add("con_total_venta", typeof(decimal));
            dt.Columns.Add("con_precio_base", typeof(decimal));
            dt.Columns.Add("con_pie", typeof(decimal));
            dt.Columns.Add("con_total_credito", typeof(decimal));
            dt.Columns.Add("con_intereses",typeof(decimal));
            dt.Columns.Add("con_cuotas_pactadas", typeof(int));
            dt.Columns.Add("con_valor_cuota_pactada", typeof(decimal));
            dt.Columns.Add("con_tasa_interes", typeof(int));
            dt.Columns.Add("con_capacidad_sepultura", typeof(int));
            dt.Columns.Add("con_tipo_compra", typeof(string));
            dt.Columns.Add("con_terminos_pago", typeof(string));
            dt.Columns.Add("con_nombre_cajero", typeof(string));
            dt.Columns.Add("con_fecha_primer_vcto_ori", typeof(DateTime));
            dt.Columns.Add("con_tipo_movimiento", typeof(int));
            dt.Columns.Add("con_cuotas_pactadas_mod", typeof(int));
            dt.Columns.Add("con_estado_contrato", typeof(string));
            dt.Columns.Add("con_num_repactaciones", typeof(int));
            dt.Columns.Add("con_anos_arriendo", typeof(int));
            dt.Columns.Add("con_derechos_servicios_con_iva", typeof(decimal));
            dt.Columns.Add("con_fecha_termino_producto", typeof(DateTime));

            foreach (var x in contratos)
            {
                dt.Rows.Add(
                    //x.con_id,
                    x.con_num_con,
                    x.con_num_comprobante, 
                    x.con_id_tipo_ingreso,
                    (object?)x.con_fecha_ingreso ?? DBNull.Value,
                    x.con_total_venta,
                    x.con_precio_base,
                    x.con_pie,
                    x.con_total_credito,
                    x.con_intereses,
                    x.con_cuotas_pactadas,
                    x.con_valor_cuota_pactada,
                    x.con_tasa_interes,
                    x.con_capacidad_sepultura,
                    x.con_tipo_compra,
                    x.con_terminos_pago,
                    x.con_nombre_cajero,
                    (object?)x.con_fecha_primer_vcto_ori ?? "1990-01-01",
                    x.con_tipo_movimiento,
                    x.con_cuotas_pactadas_mod,
                    x.con_estado_contrato,
                    x.con_num_repactaciones,
                    x.con_anos_arriendo,
                    x.con_derechos_servicios_con_iva,
                    x.con_fecha_termino_producto
                );
            }
            return dt;
        }
        #endregion

        #region PROC.ALMACENADO QUE RETORNA PAGOS REALIZADOS DE UNA FECHA
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
        #endregion

        #region PROC.ALMACENADO QUE RETORNA PAGOS REALIZADOS TERRENO
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
        #endregion

        #region PROC.ALMACENADO QUE RETORNA MODIFICACIONES DE CONTRATOS DE UNA FECHA
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
        #endregion

        #region PROC.ALMACENADO QUE RETORNA FECHA PRIMER VENCIMIENTO BOVEDA DE UNA FECHA
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
        #endregion

        #region PROC.ALMACENADO QUE RETORNA INTERESES POR DEVENGAR POR FECHA
        public async Task<DataTable> ObtenerInteresPorDev_ListadoContratosYsusCuotas(int anio)
        {
            DataTable dt = new DataTable();

            var interesPorDevengar = await InteresesPorDevengar
                .FromSqlInterpolated($"EXEC SP_IFRS_LISTA_INTERES_POR_DEVENGAR {anio}")
                .ToListAsync();

            dt.Columns.Add("int_id", typeof(int));
            dt.Columns.Add("int_num_con", typeof(string));
            dt.Columns.Add("int_correlativo", typeof(string));
            dt.Columns.Add("int_nro_cuota", typeof(int));
            dt.Columns.Add("int_saldo_inicial", typeof(int));
            dt.Columns.Add("int_tasa_interes", typeof(int));
            dt.Columns.Add("int_cuota_final", typeof(int));
            dt.Columns.Add("int_abono_a_capital", typeof(int));
            dt.Columns.Add("int_saldo_final", typeof(int));
            dt.Columns.Add("int_estado_cuota", typeof(int));
            dt.Columns.Add("int_fecha_pago", typeof(DateTime));
            dt.Columns.Add("int_fecha_vcto", typeof(DateTime));
            dt.Columns.Add("int_fecha_contab", typeof(DateTime));
            dt.Columns.Add("int_estado_contab", typeof(int));
            dt.Columns.Add("int_tipo_movimiento", typeof(string));
            dt.Columns.Add("int_cuotas_pactadas_mod", typeof(int)); 

            foreach (var p in interesPorDevengar)
            {
                dt.Rows.Add(
                    p.int_id,
                    p.int_num_con,
                    p.int_correlativo,
                    p.int_nro_cuota,
                    p.int_saldo_inicial,
                    p.int_tasa_interes,
                    p.int_cuota_final,
                    p.int_abono_a_capital,
                    p.int_saldo_final,
                    p.int_estado_cuota,
                    (object?)p.int_fecha_pago ?? DBNull.Value,
                    (object?)p.int_fecha_vcto ?? DBNull.Value,
                    (object?)p.int_fecha_contab ?? DBNull.Value,
                    p.int_estado_contab, 
                    p.int_tipo_movimiento, 
                    p.int_cuotas_pactadas_mod
                );
            }

            return dt;
        }
        #endregion

        #region PROC.ALMACENADO QUE RETORNA INGRESOS DIFERIDOS NICHOS POR FECHA 
        public async Task<DataTable> ObtenerIngresosDiferidosNichos_ListaCuotas(int anio)
        {
            DataTable dt = new DataTable(); 

            var ingresosDiferidos = await IngresosDiferidos
                .FromSqlInterpolated($"EXEC SP_IFRS_LISTA_INGRESOS_DIFERIDOS_NICHOS {anio}")
                .ToListAsync();

            dt.Columns.Add("ing_id", typeof(int));  
            dt.Columns.Add("ing_num_con", typeof(string));
            dt.Columns.Add("ing_precio_base", typeof(decimal));
            dt.Columns.Add("ing_a_diferir", typeof(decimal));
            dt.Columns.Add("ing_nro_cuota", typeof(int));
            dt.Columns.Add("ing_interes_diferido", typeof(decimal));
            dt.Columns.Add("ing_fecha_devengo", typeof(DateTime));
            dt.Columns.Add("ing_fecha_contab", typeof(DateTime));
            dt.Columns.Add("ing_estado_contab", typeof(int));

            foreach(var p in ingresosDiferidos)
            {
                dt.Rows.Add(
                    p.ing_id,
                    p.ing_num_con,
                    p.ing_precio_base,
                    p.ing_a_diferir,
                    p.ing_nro_cuota,
                    p.ing_interes_diferido,
                    p.ing_fecha_devengo,
                    (object?)p.ing_fecha_contab ?? DBNull.Value,
                    p.ing_estado_contab
                );
            }

            return dt;
        }
        #endregion

        #region PROC.ALMACENADO QUE RETORNA DERECHOS Y SERVICIOS SIN IVA POR FECHA
        public async Task<DataTable> ObtenerDerechosServiciosSinIva(string fechaDesde, string fechaHasta)
        {
            DataTable dt = new DataTable();

            var derechosServicios = await DerechosServicios
                .FromSqlInterpolated($"EXEC SP_IFRS_LISTA_DERECHOS_SERVICIOS_ALL_CONTRATOS {fechaDesde}, {fechaHasta}")
                .ToListAsync();

            dt.Columns.Add("numero_contrato", typeof(string)); 
            dt.Columns.Add("numero_comprobante", typeof(int));
            dt.Columns.Add("total_serv_der_sin_iva", typeof(decimal));
            dt.Columns.Add("total_serv_der_con_iva", typeof(decimal));

            foreach (var p in derechosServicios)
            {
                dt.Rows.Add(
                    p.numero_contrato,
                    p.numero_comprobante,
                    p.total_serv_der_sin_iva, 
                    p.total_serv_der_con_iva
                    );
            }

            return dt;
        }
        #endregion

        #region PROC.ALMACENADO QUE RETORNA SERVICIOS NICHO UPGRADE POR FECHA
        public async Task<DataTable> ObtenerDerechosServiciosNUP(string fechaDesde, string fechaHasta)
        {
            DataTable dt = new DataTable();

            var servicios_NUP = await serviciosNUP
                .FromSqlInterpolated($"EXEC SP_IFRS_LISTA_SERVICIOS_CONTRATOS_NUP {fechaDesde}, {fechaHasta}")
                .ToListAsync();

            dt.Columns.Add("numero_contrato", typeof(string));
            dt.Columns.Add("fecha_contrato", typeof(DateTime));
            dt.Columns.Add("numero_comprobante", typeof(int));
            dt.Columns.Add("total_servicios", typeof(int));

            foreach (var p in servicios_NUP)
            {
                dt.Rows.Add(
                    p.numero_contrato,
                    p.fecha_contrato,
                    p.numero_comprobante,
                    p.total_servicios
                    );
            }

            return dt;
        }
        #endregion

        #region PROC.ALMACENADO QUE ACTUALIZA INTERESES POR DEVENGAR
        public async Task ActualizarInteresesPorDevengarSegunContrato()
        {
            await Database.ExecuteSqlRawAsync("SP_IFRS_ACTUALIZA_INTERESES_POR_CONTRATO"); 
        }
        #endregion

        #region PROC.ALMACENADO OBTIENE FECHA TERMINO DEL PRODUCTO 
        public async Task<DataTable> obtenerFechaTerminoProducto()
        {
            DataTable dt = new DataTable(); 
            var terminoProducto = await TerminoProducto
                .FromSqlInterpolated($"EXEC SP_IFRS_GETFECHATERMINO_PRODUCTO")
                .ToListAsync();
            
            dt.Columns.Add("codigo_producto", typeof(string));
            dt.Columns.Add("descripcion_producto", typeof(string)); 
            dt.Columns.Add("almacen", typeof(int));
            dt.Columns.Add("codigo_ubicacion_contrato", typeof(string)); 
            dt.Columns.Add("estado_serie", typeof(int));
            dt.Columns.Add("fecha_termino_produccion", typeof(DateTime)); 
            dt.Columns.Add("fecha_contrato", typeof(DateTime));
            dt.Columns.Add("ubi_nombre_completo", typeof(string));
            dt.Columns.Add("numero_contrato", typeof(string));

            foreach (var p in terminoProducto)
            {
                dt.Rows.Add(
                    p.codigo_producto,
                    p.descripcion_producto,
                    p.almacen,
                    p.codigo_ubicacion_contrato,
                    p.estado_serie,
                    p.fecha_termino_produccion,
                    p.fecha_contrato,
                    p.ubi_nombre_completo,
                    p.numero_contrato
                 );
            }

            return dt; 
        }
        #endregion 
    }
}
