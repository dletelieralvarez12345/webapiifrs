using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webApiIFRS.Models
{
    [Table("INGRESOS_DIFERIDOS_PATIOS")]
    public class IngresosDiferidosPatios
    {
        [Key]
        public int ing_pat_id { get; set; }
        public string ing_pat_num_con { get; set; }
        public decimal ing_pat_precio_base { get; set; }
        public decimal ing_pat_a_diferir { get; set; }
        public int ing_pat_nro_cuota { get; set; }
        public decimal ing_pat_interes_diferido { get; set; }
        public DateTime? ing_pat_fecha_vcto { get; set; }
        public DateTime? ing_pat_fecha_contab { get; set; }
        public int ing_pat_estado_contab { get; set; }
        public DateTime ing_pat_fecha { get; set; }
        public int ing_pat_estado_cuota { get; set; }
        public IngresosDiferidosPatios()
        {
            ing_pat_id = 0;
            ing_pat_num_con = string.Empty;
            ing_pat_precio_base = 0;
            ing_pat_a_diferir = 0;
            ing_pat_nro_cuota = 0;
            ing_pat_interes_diferido = 0;
            ing_pat_fecha_vcto = DateTime.Now;
            ing_pat_fecha_contab = DateTime.Now;
            ing_pat_estado_contab = 0;
            ing_pat_fecha = DateTime.Now;
            ing_pat_estado_cuota = 0;
        }
    }
}
