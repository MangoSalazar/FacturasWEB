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
        SqliteConnection conexion = new SqliteConnection($"DataSource={"mibase.db"}");
        private List<Factura> facturas = new List<Factura>();
        private List<Articulo> articulos = new List<Articulo>();

        public async Task agregarArticuloTemporal(Articulo articulo)
        {
            var articuloExistente = articulos.FirstOrDefault(a => a.Nombre.Trim().ToLower() == articulo.Nombre.Trim().ToLower());

            if (articuloExistente != null)
            {
                articuloExistente.Cantidad += articulo.Cantidad;
                return;
            }
            articulos.Add(articulo);
            
        }
        public async Task eliminarArticuloTemporal(Articulo articulo)
        {
            articulos.RemoveAll(j => j.Nombre == articulo.Nombre);
        }
        public void actualizarArticuloTemporal(Articulo articuloOriginal, Articulo articuloActualizado)
        {
            var articuloEnLista = articulos.FirstOrDefault(a => a == articuloOriginal);
            if (articuloEnLista != null)
            {
                articuloEnLista.Nombre = articuloActualizado.Nombre;
                articuloEnLista.Precio = articuloActualizado.Precio;
                articuloEnLista.Cantidad = articuloActualizado.Cantidad;
            }

        }
        public async Task<List<Articulo>> obtenerArticulos()
        {
            return articulos;
        }

        public async Task<List<Factura>> obtenerFacturas()
        {
            iniciarConexion();
            facturas.Clear();
            var sql = @"
            SELECT * FROM Facturas;
        
            SELECT 
                c.ID_facturas,  -- ID de la factura a la que pertenece
                c.Cantidad,
                a.ID_articulos,
                a.Nombre,
                a.Precio
            FROM Articulos a
            JOIN Contiene c ON a.ID_articulos = c.ID_articulos;";

            using (var multi = await conexion.QueryMultipleAsync(sql))
            {
                var facturas = (await multi.ReadAsync<Factura>()).ToList();
                Console.WriteLine($"Facturas obtenidas: {facturas.Count}");
                // (Nota: Tu clase Articulo ya tiene ID_facturas y Cantidad)
                var articulos = (await multi.ReadAsync<Articulo>()).ToList();
                foreach (var factura in facturas)
                {
                    // Asigna a CADA factura su lista de artículos correspondiente
                    factura.Articulos.AddRange(articulos.Where(a => a.ID_facturas == factura.ID_facturas));
                    Console.WriteLine($"Factura ID {factura.ID_facturas} tiene {factura.Articulos.Count} artículos.");
                }
                return facturas;
            }
        }
        public async Task guardarFactura(Factura factura)
        {
            iniciarConexion();

            factura.Articulos = articulos;
            using (var transaction = conexion.BeginTransaction())
            {
                try
                {
                    var sqlFactura = "INSERT INTO Facturas (Fecha, Nombre) VALUES (@Fecha, @Nombre);";
                    await conexion.ExecuteAsync(sqlFactura, factura, transaction);

                    var nuevaFacturaId = await conexion.QuerySingleAsync<int>("SELECT last_insert_rowid();", transaction: transaction);
                    factura.ID_facturas = nuevaFacturaId;

                    var sqlBuscarArticulo = "SELECT ID_articulos FROM Articulos WHERE Nombre = @Nombre;";
                    var sqlCrearArticulo = "INSERT INTO Articulos (Nombre, Precio) VALUES (@Nombre, @Precio);";
                    var sqlCrearContiene = "INSERT INTO Contiene (ID_facturas, ID_articulos, Cantidad) VALUES (@ID_facturas, @ID_articulos, @Cantidad);";

                    foreach (var articulo in factura.Articulos)
                    {
                        int articuloId; // Necesitamos el ID del artículo para la tabla 'Contiene'

                        var idExistente = await conexion.QueryFirstOrDefaultAsync<int?>(sqlBuscarArticulo, new { articulo.Nombre }, transaction);

                        if (idExistente.HasValue)
                        {
                            articuloId = idExistente.Value;
                            Console.WriteLine("articulo existe");
                        }
                        else
                        {
                            await conexion.ExecuteAsync(sqlCrearArticulo, articulo, transaction);
                            articuloId = await conexion.QuerySingleAsync<int>("SELECT last_insert_rowid();", transaction: transaction);
                            Console.WriteLine("articulo añadido");
                        }
                        articulo.ID_articulo = articuloId;
                        await conexion.ExecuteAsync(sqlCrearContiene, new
                        {
                            ID_facturas = nuevaFacturaId,
                            ID_articulos = articuloId,
                            articulo.Cantidad
                        }, transaction);
                    }

                    transaction.Commit();
                    this.facturas.Add(factura);
                    articulos.Clear();
                    Console.WriteLine("Factura guardada correctamente.");

                    cerrarConexion();

                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
        public async Task eliminarFactura(Factura factura)
        {
            iniciarConexion();
            var sqlEliminarContiene = "DELETE FROM Contiene WHERE ID_facturas = @ID_facturas;";
            var sqlEliminarFactura = "DELETE FROM Facturas WHERE ID_facturas = @ID_facturas;";
            using (var transaction = conexion.BeginTransaction())
            {
                try
                {
                    await conexion.ExecuteAsync(sqlEliminarContiene, new { factura.ID_facturas }, transaction);
                    await conexion.ExecuteAsync(sqlEliminarFactura, new { factura.ID_facturas }, transaction);
                    transaction.Commit();
                    this.facturas.RemoveAll(f => f.ID_facturas == factura.ID_facturas);
                    Console.WriteLine("Factura eliminada correctamente.");
                    cerrarConexion();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
        private async Task iniciarConexion() 
        {
            await conexion.OpenAsync();
            if (conexion.State != System.Data.ConnectionState.Open)
            {
                await conexion.OpenAsync();
            }
            await conexion.ExecuteAsync("PRAGMA foreign_keys = ON;");
        }
        
        public async Task cerrarConexion()
        {
            if (conexion.State != System.Data.ConnectionState.Closed)
            {
                await conexion.CloseAsync();
            }
        }
    }
}
