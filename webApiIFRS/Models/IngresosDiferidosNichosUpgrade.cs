using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webApiIFRS.Models
{
    [Table("INGRESOS_DIFERIDOS_NICHOS_UPGRADE")]
    public class IngresosDiferidosNichosUpgrade
    {
        [Key]
        public int ing_nup_id { get; set; }
        public string ing_nup_num_con { get; set; }
        public decimal ing_nup_precio_base { get; set; }
        public int ing_nup_nro_cuota { get; set; }
        public decimal ing_nup_interes_diferido { get; set; }
        public DateTime? ing_nup_fecha_vcto { get; set; }
        public DateTime? ing_nup_fecha_contab { get; set; }
        public int ing_nup_estado_contab { get; set; }
        public DateTime ing_nup_fecha { get; set; }
        public int ing_nup_estado_cuota { get; set; }
        public IngresosDiferidosNichosUpgrade()
        {
            ing_nup_id = 0;
            ing_nup_num_con = string.Empty;
            ing_nup_precio_base = 0;
            ing_nup_nro_cuota = 0;
            ing_nup_interes_diferido = 0;
            ing_nup_fecha_vcto = DateTime.Now;
            ing_nup_fecha_contab = DateTime.Now;
            ing_nup_estado_contab = 0;
            ing_nup_fecha = DateTime.Now;
            ing_nup_estado_cuota = 0;
        }
    }
}
