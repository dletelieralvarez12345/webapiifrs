using System.ComponentModel.DataAnnotations;

namespace webApiIFRS.Models
{
    public class Contrato
    {
        [Key]
        public int con_id { get; set; }
        public string con_num_con { get; set; }
        public int con_id_tipo_ingreso { get; set; }
        public DateTime con_fecha_ingreso { get; set; }
        public int con_total_venta { get; set; }
        public int con_precio_base { get; set; }
        public int con_pie { get; set; }
        public int con_total_credito { get; set; }
        public int con_cuotas_pactadas { get; set; }    
        public int con_valor_cuota_pactada { get; set; }
        public int con_tasa_interes { get; set; }
        public int con_capacidad_sepultura { get; set; }
        public string con_tipo_compra { get; set; }
        public string con_terminos_pago { get; set; }
        public string con_nombre_cajero { get; set; }
        public DateTime con_fecha_primer_vcto_ori { get; set; }
        public int con_tipo_movimiento { get; set; }
        public int con_cuotas_pactadas_mod { get; set; }
        public int con_estado_contrato { get; set; }
        public int con_num_repactaciones { get; set; }
        public int con_anos_arriendo { get; set; }

        public Contrato()
        {
            con_id = 0;
            con_num_con = string.Empty;
            con_id_tipo_ingreso = 0;
            con_fecha_ingreso = DateTime.Now;
            con_total_venta = 0; 
            con_precio_base = 0;
            con_pie = 0; 
            con_total_credito = 0;
            con_cuotas_pactadas = 0;
            con_valor_cuota_pactada = 0;
            con_tasa_interes = 0; 
            con_capacidad_sepultura = 0;
            con_tipo_compra = string.Empty;
            con_terminos_pago= string.Empty;
            con_nombre_cajero= string.Empty;
            con_fecha_primer_vcto_ori=DateTime.Now;
            con_tipo_movimiento = 0;
            con_cuotas_pactadas_mod = 0;
            con_estado_contrato = 0; 
            con_num_repactaciones = 0;
            con_anos_arriendo = 0;
        }
    }
}
