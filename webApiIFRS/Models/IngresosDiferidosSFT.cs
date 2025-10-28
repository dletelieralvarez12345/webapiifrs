using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webApiIFRS.Models
{
    [Table("INGRESOS_DIFERIDOS_SFT")]
    public class IngresosDiferidosSFT
    {
        [Key]
        public int ing_sft_id { get; set; }
        public string ing_sft_num_con { get; set; }
        public decimal ing_sft_precio_base { get; set; }
        public int ing_sft_nro_cuota { get; set; }
        public decimal ing_sft_interes_diferido { get; set; }
        public DateTime? ing_sft_fecha_devengo { get; set; }
        public DateTime? ing_sft_fecha_contab { get; set; }
        public int ing_sft_estado_contab { get; set; }
        public DateTime ing_sft_fecha { get; set; }

        public IngresosDiferidosSFT()
        {
            ing_sft_id = 0;
            ing_sft_num_con = string.Empty;
            ing_sft_precio_base = 0;
            ing_sft_nro_cuota = 0;
            ing_sft_interes_diferido = 0;
            ing_sft_fecha_devengo = DateTime.Now;
            ing_sft_fecha_contab = DateTime.Now;
            ing_sft_estado_contab = 0;
            ing_sft_fecha = DateTime.Now;
        }
    }
    
}
