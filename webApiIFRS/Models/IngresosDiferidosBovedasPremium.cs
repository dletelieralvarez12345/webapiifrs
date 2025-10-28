using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webApiIFRS.Models
{
    [Table("INGRESOS_DIFERIDOS_BOVEDAS_PREMIUM")]
    public class IngresosDiferidosBovedasPremium
    {
        [Key]
        public int ing_bovp_id { get; set; }
        public string ing_bovp_num_con { get; set; }
        public decimal ing_bovp_precio_base { get; set; }
        public int ing_bovp_nro_cuota { get; set; }
        public decimal ing_bovp_interes_diferido { get; set; }
        public DateTime? ing_bovp_fecha_devengo { get; set; }
        public DateTime? ing_bovp_fecha_contab { get; set; }
        public int ing_bovp_estado_contab { get; set; }
        public DateTime ing_bovp_fecha { get; set; }

        public IngresosDiferidosBovedasPremium()
        {
            ing_bovp_id = 0;
            ing_bovp_num_con = string.Empty;
            ing_bovp_precio_base = 0;
            ing_bovp_nro_cuota = 0;
            ing_bovp_interes_diferido = 0;
            ing_bovp_fecha_devengo = DateTime.Now;
            ing_bovp_fecha_contab = DateTime.Now;
            ing_bovp_estado_contab = 0;
            ing_bovp_fecha = DateTime.Now;
        }
    }
}
