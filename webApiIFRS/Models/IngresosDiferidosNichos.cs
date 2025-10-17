﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webApiIFRS.Models
{
    [Table("INGRESOS_DIFERIDOS_NICHOS")]
    public class IngresosDiferidosNichos
    {
        [Key]
        public int ing_id { get; set; }
        public string ing_num_con { get; set; }
        public decimal ing_precio_base { get; set; }
        public decimal ing_a_diferir { get; set; }
        public int ing_nro_cuota { get; set; }  
        public decimal ing_interes_diferido { get; set; }
        public DateTime? ing_fecha_contab { get; set; }
        public int ing_estado_contab { get; set; }
        public DateTime ing_fecha { get; set; }

        public IngresosDiferidosNichos() { 
            ing_id = 0;
            ing_num_con = string.Empty; 
            ing_precio_base = 0;
            ing_a_diferir = 0;
            ing_nro_cuota = 0;
            ing_interes_diferido = 0;
            ing_fecha_contab= DateTime.Now;
            ing_estado_contab = 0;
            ing_fecha = DateTime.Now;
        }
    }
}
