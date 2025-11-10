using Dapper;
using FacturasWEB.Components.Data;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FacturasWEB.Components.Data
{
    public class ServicioFacturas
    {
        private readonly SqliteConnection _connection;
        private List<Factura> facturas = new List<Factura>();

        public ServicioFacturas(SqliteConnection connection)
        {
            _connection = connection;
        }

        public async Task<List<Factura>> obtenerFacturas()
        {
            facturas.Clear();
            var sql = @"
                SELECT * FROM Facturas;
                
                SELECT 
                    a.ID_articulos,
                    a.Nombre,
                    a.Precio,
                    c.Cantidad,
                    c.ID_facturas
                FROM Articulos a
                JOIN Contiene c ON a.ID_articulos = c.ID_articulos;
            ";

            using (var multi = await _connection.QueryMultipleAsync(sql))
            {
                facturas = (await multi.ReadAsync<Factura>()).ToList();
                var articulos = (await multi.ReadAsync<Articulo>()).ToList();
                foreach (var f in facturas)
                {

                    f.articulos.AddRange(articulos.Where(a => a.ID_facturas == f.ID_facturas));
                }

                return facturas;
            }
        }

    }
}
