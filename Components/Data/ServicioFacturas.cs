using Dapper;
using FacturasWEB.Components.Data;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace FacturasWEB.Components.Data
{
    public class ServicioFacturas
    {
        private List<Factura> facturas = new List<Factura>();
        private List<Articulo> articulos = new List<Articulo>();

        public async Task agregarArticulo(Articulo articulo)
        {
            var articuloExistente = articulos.FirstOrDefault(a => a.Nombre.Trim().ToLower() == articulo.Nombre.Trim().ToLower());

            if (articuloExistente != null)
            {
                articuloExistente.Cantidad += articulo.Cantidad;
                return;
            }
            articulos.Add(articulo);
            
        }
        public async Task eliminarArticulo(Articulo articulo)
        {
            articulos.RemoveAll(j => j.Nombre == articulo.Nombre);
        }

        public async Task<List<Articulo>> obtenerArticulos()
        {
            return articulos;
        }

        public async Task<List<Factura>> obtenerFacturas()
        {
            return facturas;
        }


        public async Task guardarFactura(Factura factura)
        {

        }

    }
}
