namespace webApiIFRS.Models
{
    public class PagosRealizadosTerreno
    {
        public int numero_comprobante { get; set; }
        public DateTime? fecha_Pago { get; set; }
        public DateTime? fecha_vto { get; set; }
        public string contrato { get; set; }
        public int tipo_cto { get; set; }
        public int valor_cuota { get; set; }    
        public int tipo_pago { get; set; }

        public PagosRealizadosTerreno()
        {
            numero_comprobante = 0;
            fecha_Pago = null;
            fecha_vto = null;
            contrato = string.Empty;
            tipo_cto = 0;
            valor_cuota = 0;
            tipo_pago = 0;
        }
    }
}
