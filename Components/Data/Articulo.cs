namespace FacturasWEB.Components.Data
{
    public class Articulo
    {
        public int ID_articulo { get; set; }
        public string Nombre { get; set; }
        public double Precio { get; set; }

        // Propiedades que se llenarán desde la tabla 'Contiene'
        public int Cantidad { get; set; }
        public int ID_facturas { get; set; }
    }
}
