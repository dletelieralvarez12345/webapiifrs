using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace webApiIFRS.Models
{
    public class ConnContextSICM : DbContext
    {
        public ConnContextSICM(DbContextOptions<ConnContextSICM> options)
        : base(options)
        {

        }

        public DbSet<PagoRealizado> PagosRealizados { get; set; } = null;
        public DbSet<Modificaciones> Modificaciones { get; set; } = null;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //tablas que no existen en la bd y/o no tiene pk
            modelBuilder.Entity<PagoRealizado>().HasNoKey();
            modelBuilder.Entity<Modificaciones>().HasNoKey();
            base.OnModelCreating(modelBuilder);
        }

        public async Task<List<PagoRealizado>> ObtenerPagosRealizadosAsync(int anioConsulta)
        {
            string consultaSQL = string.Format(@"
                select   
                    cast(mo1 as int) as numero_comprobante,  
                    isnull(cast(mo2 as date), '1900-01-01') as fecha_pago,  
                    isnull(cast(MO4 as date), '1900-01-01') as fecha_vcto,  
                    cast(mo3 as varchar(15)) as contrato,  
                    isnull(cast(mo8 as int),0) as tipo_cto,  
                    isnull(cast(mo6 as int),0) as valor_cuota,  
                    isnull(cast(mo17 as int),0) as tipo_pago  
                    from ctacte.dbo.CCMOVI1  
                    where year(mo2)>= {0} 
                        and mo8 in(1,3,4,9) 
                    union all 
                    select  
                    cast(cpr_numero_comprobante as int), 
                    isnull(cast(cpr_fecha_comprobante as date), '1900-01-01'), 
                    isnull(cast(cpr_cuota_fecha_vencimiento as date), '1900-01-01'), 
                    cast(case 
                        when LEN(cpr_numero_contrato)> 3
                            then LEFT(cpr_numero_contrato, LEN(cpr_numero_contrato) - 3) 
                        else 0 end as varchar(15)
                    ) as numero_contrato, 
                    1, 
                    isnull(cast(cpr_cuota_valor as int),0), 
                    1 
                    from SICM.DBO.cta_pagos_realizados 
                    where cpr_numero_contrato is not null", anioConsulta);


            return await PagosRealizados
                .FromSqlRaw(consultaSQL)
                .ToListAsync();
        }

        public async Task<List<Modificaciones>> ObtenerModificacionesAsync(int anioConsulta)
        {
            string consultaSQL = string.Format(@"
                select * from (  
                    select     
                        cast(CO1 as varchar(15)) as numero_contrato,   
                        isnull(cast(di15 as date), '1900-01-01') as fecha_modificacion,   
                        isnull(cast(max(di11) as int),0) as cuotas_pactadas_nuevo,    
                        isnull(cast(max(DI16) as int),0) as cuotas_pactadas_antiguo,   
                        isnull(cast(sum(DI12) as int),0) as valor_cuota_nuevo,   
                        isnull(cast(sum(DI17) as int),0) as valor_cuota_antiguo,   
                        isnull(cast(sum(di10) as int),0) as pie_nuevo,   
                        isnull(cast(sum(di9) as int),0) as valor_abonado,    
                        isnull(cast(sum(di10)+sum(di9)+(max(di11)*sum(DI12)) as int),0) as total_venta_nuevo,    
                        cast(isnull(max(di14),cast(di15 as date)) as date) as fecha_primer_vto,    
                        cast(di18 as int) as di18, 1 as tipo_sistema   
                     from (   
                            select cont.CO1,modi.* from ctacte.dbo.CCMODI1 modi   
                                left join ctacte.dbo.CCCONT1 cont on cont.CO1=modi.DI1 and modi.DI2=1    
                                UNION ALL    
                                select cont.CO1,modi.* from ctacte.dbo.CCMODI1 modi    
                                left join ctacte.dbo.CCCONT1 cont on cont.CO2=modi.DI1 and modi.DI2=2 ) as t    
                            where co1 is not null  and year(di15)>={0} and (isnull(DI8,0)-isnull(DI7,0) != 0)  
                            group by CO1,di15,di18   
   
                            UNION ALL 
                            select     
                                cast(DI1 as varchar(15)) as numero_contrato,    
                                isnull(cast(di15 as date), '1900-01-01') as fecha_modificacion,    
                                isnull(cast(max(di11) as int),0) as cuotas_pactadas_nuevo,    
                                isnull(cast(max(DI16) as int),0) as cuotas_pactadas_antiguo,   
                                isnull(cast(sum(DI12) as int),0) as valor_cuota_nuevo,    
                                isnull(cast(sum(DI17) as int),0) as valor_cuota_antiguo,   
                                isnull(cast(sum(di10) as int),0) as pie_nuevo,   
                                isnull(cast(sum(di9) as int),0) as valor_abonado,   
                                isnull(cast(sum(di10)+sum(di9)+(max(di11)*sum(DI12)) as int),0) as total_venta_nuevo,  
                                isnull(cast(max(di14) as date), '1900-01-01') as fecha_primer_vto,    
                                cast(di18 as int) as di18, 1 as tipo_sistema    
                             from CTACTE.DBO.CCMODI1 
                             where di2 in (3,4) and year(di15)>={0}  
                             group by DI1,di15,di18   

                            UNION ALL 
                            select     
                                cast(case when LEN(coo_numero_contrato)> 3 then LEFT(coo_numero_contrato, LEN(coo_numero_contrato) - 3) else 0 end as varchar(15)) as numero_contrato,    
                                isnull(cast(mcv_fecha_ingreso as date), '1900-01-01') as fecha_modificacion,    
                                isnull(cast(modi.mcv_nuevo_cuotas_por_pagar as int),0) as cuotas_pactadas_nuevo,    
                                isnull(cast(modi.mcv_actual_numero_cuotas_por_pagar as int),0) as cuotas_pactadas_antiguo,    
                                isnull(cast(mcv_nuevo_valor_cuota as int),0) as valor_cuota_nuevo,    
                                isnull(cast(mcv_actual_valor_cuota as int),0) as valor_cuota_antiguo,    
                                isnull(cast(mcv_nuevo_pie as int),0) as pie_nuevo,    
                                0 as valor_abonado,   
                                isnull(cast(mcv_nuevo_total_venta as int),0) as total_venta_nuevo,    
                                isnull(cast(mcv_nuevo_fecha_primer_vto as date), '1900-01-01') as fecha_primer_vto,    
                                cast(case when ecv_id = 2 then 2  
                                    when ecv_id = 3 then 1  
                                    when ecv_id = 4 then 1  
                                    when ecv_id = 5 then 3 
                                    when ecv_id = 6 then 1
                                else 0  end as int) as di18,
                                2 as tipo_sistema    
                            from sicm.dbo.cta_modificaciones_contrato_venta modi ) as t  
                    order by numero_contrato,fecha_modificacion,di18", anioConsulta);


            return await Modificaciones
                .FromSqlRaw(consultaSQL)
                .ToListAsync();
        }

       
        
    }
}
