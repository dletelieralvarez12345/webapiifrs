using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webApiIFRS.Models
{
    [Table("INTERESES_POR_DEVENGAR")]
    public class InteresesPorDevengar
    {
        [Key]
        public int int_id { get; set; }
        public string int_num_con { get; set; }
        public int int_correlativo { get; set; }
        public int int_nro_cuota { get; set; }
        public int int_saldo_inicial { get; set; }
        public int int_tasa_interes { get; set; }
        public int int_cuota_final { get; set; }
        public int int_abono_a_capital { get; set; }
        public int int_saldo_final { get; set; }
        public int int_estado_cuota { get; set; }
        public DateTime? int_fecha_pago { get; set; }
        public DateTime? int_fecha_vcto { get; set; }
        public DateTime? int_fecha_contab { get; set; }
        public int int_estado_contab { get; set; }
        public string int_tipo_movimiento { get; set; }
        public int? int_cuotas_pactadas_mod { get;set; }
        public DateTime int_fecha { get; set; }

        public InteresesPorDevengar()
        {
            int_id = 0;
            int_num_con = string.Empty;
            int_correlativo = 0; 
            int_nro_cuota = 0;
            int_saldo_inicial = 0; 
            int_tasa_interes = 0;
            int_cuota_final = 0;
            int_abono_a_capital = 0;
            int_fecha_pago = DateTime.Now;
            int_estado_cuota = 0; 
            int_saldo_final = 0;             
            int_fecha_vcto = DateTime.Now;
            int_fecha_contab = DateTime.Now;
            int_estado_contab = 0;
            int_tipo_movimiento = string.Empty;
            int_cuotas_pactadas_mod = 0;
            int_fecha = DateTime.Now;
        }
    }
}
