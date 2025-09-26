namespace webApiIFRS.Models
{
    public class Modificaciones
    {
        public string numero_contrato { get; set; }
        public DateTime? fecha_modificacion { get; set; }
        public int cuotas_pactadas_nuevo { get; set; }
        public int cuotas_pactadas_antiguo { get; set; }
        public int valor_cuota_nuevo { get; set; }
        public int valor_cuota_antiguo { get; set; }
        public int pie_nuevo { get; set; }
        public int valor_abonado { get; set; }
        public int total_venta_nuevo { get; set; }
        public DateTime? fecha_primer_vto { get; set; }
        public int di18 { get; set; }
        public int tipo_sistema { get; set; }
    }
}
