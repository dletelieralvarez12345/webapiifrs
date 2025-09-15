using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webApiIFRS.Models
{
    [Table("INTERESES_POR_DEVENGAR")]
    public class Intereses_Por_Devengar
    {
        [Key]
        public int int_id { get; set; }
        public string int_num_con { get; set; }
        public int int_nro_cuota { get; set; }
        public int int_saldo_inicial { get; set; }
        public int int_tasa_interes { get; set; }
        public int int_cuota_final { get; set; }
        public int int_abono_a_capital { get; set; }
        public int int_saldo_final { get; set; }
        public DateTime int_fecha_vcto { get; set; }
        public DateTime int_fecha_contab { get; set; }
        public int int_estado_contab { get; set; }

        public Intereses_Por_Devengar()
        {
            int_id = 0;
            int_num_con = string.Empty;
            int_nro_cuota = 0;
            int_saldo_inicial = 0; 
            int_tasa_interes = 0;
            int_cuota_final = 0;
            int_abono_a_capital = 0;
            int_saldo_final = 0; 
            int_fecha_vcto = DateTime.Now;
            int_fecha_contab = DateTime.Now;
            int_estado_contab = 0; 
        }
    }
}
