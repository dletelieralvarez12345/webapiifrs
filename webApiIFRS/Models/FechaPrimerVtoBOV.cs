namespace webApiIFRS.Models
{
    public class FechaPrimerVtoBOV
    {
        public string num_contrato { get; set; }   
        public DateTime? fecha_primer_vcto { get; set; }
        public DateTime? fecha_vto_cuota { get; set; }

        public FechaPrimerVtoBOV()
        {
            num_contrato = string.Empty;
            fecha_primer_vcto = null;
            fecha_vto_cuota = null;
        }
    }
}
