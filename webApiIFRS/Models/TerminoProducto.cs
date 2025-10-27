namespace webApiIFRS.Models
{
    public class TerminoProducto
    {
        public string? codigo_producto { get; set; }
        public string? descripcion_producto { get; set; }
        public int almacen { get; set; } 
        public string? codigo_ubicacion_contrato { get; set; }
        public int estado_serie { get; set; }   
        public DateTime? fecha_termino_produccion { get; set; }
        public DateTime? fecha_contrato { get; set; }
        public string? ubi_nombre_completo { get; set; }
        public string? numero_contrato { get; set; }

        public TerminoProducto()
        {
            codigo_producto = string.Empty;
            descripcion_producto = string.Empty;
            almacen = 0;
            codigo_ubicacion_contrato = string.Empty;
            estado_serie = 0;
            fecha_termino_produccion = null;
            fecha_contrato = null;
            ubi_nombre_completo = string.Empty;
            numero_contrato= string.Empty;
        }
    }
}
