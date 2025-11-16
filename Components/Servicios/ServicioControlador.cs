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
            _servicioFacturas.agregarArticuloTemporal(articulo);
        }
        public async Task eliminarArticulo(Articulo articulo)
        {
            _servicioFacturas.eliminarArticuloTemporal(articulo);
        }
        public async Task<List<Articulo>> obtenerArticulos()
        {
            return await _servicioFacturas.obtenerArticulos();
        }

        public async Task actualizarArticulo(Articulo articuloOriginal, Articulo articulo)
        {
            _servicioFacturas.actualizarArticuloTemporal(articuloOriginal,articulo);
        }
        public async Task<List<Articulo>> cargarArticulos(List<Articulo> articulos)
        {
            return await _servicioFacturas.cargarArticulos(articulos);
        }
        public async Task<List<Factura>> obtenerFacturas()
        {
            return await _servicioFacturas.obtenerFacturas();
        }
        public async Task<Factura> obtenerFacturaPorId(int idFactura)
        {
            return await _servicioFacturas.obtenerFacturaPorId(idFactura);
        }
        public async Task guardarFactura(Factura factura)
        {
            _servicioFacturas.guardarFactura(factura);
        }
        public async Task ActualizarFactura(Factura factura)
        {
            await _servicioFacturas.ActualizarFactura(factura);
        }
        public async Task eliminarFactura(Factura factura)
        {
            _servicioFacturas.eliminarFactura(factura);
        }

        public async Task<List<Factura>> ObtenerFacturasPorAno(int ano)
        {
            return await _servicioFacturas.ObtenerFacturasPorAno(ano);
        }

    }
}
