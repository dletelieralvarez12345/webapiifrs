namespace webApiIFRS.Models
{
    public class ServiciosNUP
    {
        public string numero_contrato { get; set; }
        public DateTime fecha_contrato { get; set; }
        public int? numero_comprobante { get; set; }
        public int total_servicios { get; set; }

        public ServiciosNUP()
        {
            numero_contrato = string.Empty;
            fecha_contrato = DateTime.MinValue;
            numero_comprobante = null;
            total_servicios = 0;
        }
    }
}
