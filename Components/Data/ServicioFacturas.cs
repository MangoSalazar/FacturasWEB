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
            String ruta = "mibase.db";
            using var conexion = new SqliteConnection($"DataSource={ruta}");
            await conexion.OpenAsync();

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

            using (var multi = await conexion.QueryMultipleAsync(sql))
            {
                facturas = (await multi.ReadAsync<Factura>()).ToList();
                var articulos = (await multi.ReadAsync<Articulo>()).ToList();
                foreach (var f in facturas)
                {

                    f.Articulos.AddRange(articulos.Where(a => a.ID_facturas == f.ID_facturas));
                }
                return facturas;
            }
        }
        public async Task<List<Articulo>> ObtenerArticulosSimplesAsync()
        {
            String ruta = "mibase.db";
            using var conexion = new SqliteConnection($"DataSource={ruta}");
            await conexion.OpenAsync();

            var sql = "SELECT ID_articulos, Nombre, Precio FROM Articulos;";
            var articulos = await conexion.QueryAsync<Articulo>(sql);
            return articulos.ToList();
        }


        public async Task GuardarFacturaAsync(Factura factura)
        {
            String ruta = "mibase.db";
            using var conexion = new SqliteConnection($"DataSource={ruta}");
            await conexion.OpenAsync();

            await conexion.ExecuteAsync("PRAGMA foreign_keys = ON;");
            using (var transaction = conexion.BeginTransaction())
            {

                var sqlFactura = "INSERT INTO Facturas (Fecha, Nombre) VALUES (@Fecha, @Nombre);";
                await conexion.ExecuteAsync(sqlFactura, factura, transaction);


                var nuevaFacturaId = await conexion.QuerySingleAsync<int>("SELECT last_insert_rowid();", transaction: transaction);


                var sqlContiene = "INSERT INTO Contiene (ID_facturas, ID_articulos, Cantidad) VALUES (@ID_facturas, @ID_articulos, @Cantidad);";
                foreach (var articulo in factura.Articulos)
                {
                    await conexion.ExecuteAsync(sqlContiene, new
                    {
                        ID_facturas = nuevaFacturaId,
                        ID_articulos = articulo.ID_articulos,
                        articulo.Cantidad
                    }, transaction);
                }
                transaction.Commit();
            }

        }
        public async Task<int> CrearArticuloAsync(Articulo articulo)
        {
            String ruta = "mibase.db";
            using var conexion = new SqliteConnection($"DataSource={ruta}");
            await conexion.OpenAsync();
            // Habilitamos las FK
            await conexion.ExecuteAsync("PRAGMA foreign_keys = ON;");

            // 1. Insertar el nuevo artículo
            var sql = "INSERT INTO Articulos (Nombre, Precio) VALUES (@Nombre, @Precio);";
            await conexion.ExecuteAsync(sql, articulo);

            // 2. Obtener y devolver el ID del artículo que acabamos de crear
            var nuevoArticuloId = await conexion.QuerySingleAsync<int>("SELECT last_insert_rowid();");
            return nuevoArticuloId;
        }

    }
}
