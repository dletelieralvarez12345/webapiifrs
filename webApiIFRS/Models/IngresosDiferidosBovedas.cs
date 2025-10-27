using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webApiIFRS.Models
{
    [Table("INGRESOS_DIFERIDOS_BOVEDAS")]
    public class IngresosDiferidosBovedas
    {
        [Key]
        public int ing_bov_id { get; set; }
        public string ing_bov_num_con { get; set; }
        public decimal ing_bov_precio_base { get; set; }        
        public int ing_bov_nro_cuota { get; set; }
        public decimal ing_bov_interes_diferido { get; set; }
        public DateTime? ing_bov_fecha_devengo { get; set; }
        public DateTime? ing_bov_fecha_contab { get; set; }
        public int ing_bov_estado_contab { get; set; }
        public DateTime ing_bov_fecha { get; set; }

        public IngresosDiferidosBovedas()
        {
            ing_bov_id = 0;
            ing_bov_num_con = string.Empty;
            ing_bov_precio_base = 0;            
            ing_bov_nro_cuota = 0;
            ing_bov_interes_diferido = 0;
            ing_bov_fecha_devengo = DateTime.Now;
            ing_bov_fecha_contab = DateTime.Now;
            ing_bov_estado_contab = 0;
            ing_bov_fecha = DateTime.Now;
        }
    }
}
