using FacturasWEB.Components.Data;

namespace FacturasWEB.Components.Servicios
{
    public class ServicioControlador
    {
        private readonly ServicioFacturas _servicioFacturas;

        public ServicioControlador(ServicioFacturas servicioFacturas)
        {
            _servicioFacturas = servicioFacturas;
        }

        public async Task agregarArticulo(Articulo articulo) 
        {
            _servicioFacturas.agregarArticulo(articulo);
        }
        public async Task eliminarArticulo(Articulo articulo)
        {
            _servicioFacturas.eliminarArticulo(articulo);
        }
        public async Task<List<Articulo>> obtenerArticulos()
        {
            return await _servicioFacturas.obtenerArticulos();
        }


        public async Task<List<Factura>> obtenerFacturas()
        {
            return await _servicioFacturas.obtenerFacturas();
        }
        public async Task guardarFactura(Factura factura)
        {
            _servicioFacturas.guardarFactura(factura);
        }
    }
}
