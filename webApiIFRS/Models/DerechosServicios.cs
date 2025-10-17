namespace webApiIFRS.Models
{
    public class DerechosServicios
    {
        public string numero_contrato { get; set; }
        public string numero_comprobante { get; set; }
        public decimal total_serv_der_sin_iva { get; set; }

        public DerechosServicios()
        {
            numero_contrato = string.Empty;
            numero_comprobante = string.Empty;
            total_serv_der_sin_iva = 0;
        }
    }
}
