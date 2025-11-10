namespace FacturasWEB.Components.Data
{
    public class Factura
    {
        public int ID_facturas { get; set; }
        public string Nombre { set; get; }
        public string Fecha { get; set; }
        public List<Articulo> Articulos { get; set; } = new List<Articulo>();
    }
}
