using FacturasWEB.Components.Data;

namespace FacturasWEB.Components.Servicios
{
    public class ServicioControlador
    {
        private readonly ServicioFacturas _servicoFacturas;

        public ServicioControlador(ServicioFacturas servicioFacturas)
        {
            _servicoFacturas = servicioFacturas;
        }

        public async Task<List<Factura>> obtenerFacturas()
        {
            return await _servicoFacturas.obtenerFacturas();
        }
    }
}
