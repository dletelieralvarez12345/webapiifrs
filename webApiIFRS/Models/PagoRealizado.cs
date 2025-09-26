using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webApiIFRS.Models
{
    //[Keyless]
    public class PagoRealizado
    {
        public int numero_comprobante { get; set; }
        public DateTime? fecha_Pago { get; set; }
        public DateTime? fecha_vcto { get; set; }
        public string contrato { get; set; }
        public int tipo_cto { get; set; }
        public int valor_cuota { get; set; }
        public int tipo_pago { get; set; } 
        public int? numero_cuota { get; set; }

        public PagoRealizado()
        {
            numero_comprobante = 0;
            fecha_Pago = null;
            fecha_vcto = null;
            contrato = string.Empty;
            tipo_cto = 0;
            valor_cuota = 0;
            tipo_pago = 0;
            numero_cuota = 0;
        }
    }
}
