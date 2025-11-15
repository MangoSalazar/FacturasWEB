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
            return facturas;
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


    }
}
