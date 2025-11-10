namespace FacturasWEB.Components.Data
{
    public class Factura
    {
        public int ID_facturas { get; set; }
        public string nombre { set; get; }
        public string fecha { get; set; }
        public List<Articulo> articulos { get; set; } = new List<Articulo>();
    }
}
