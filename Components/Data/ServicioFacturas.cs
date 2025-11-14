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
        private async Task iniciarConexion() 
        {
            await conexion.OpenAsync();
            if (conexion.State != System.Data.ConnectionState.Open)
            {
                await conexion.OpenAsync();
            }
            await conexion.ExecuteAsync("PRAGMA foreign_keys = ON;");
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

                    var sqlBuscarArticulo = "SELECT ID_articulos FROM Articulos WHERE Nombre = @Nombre;";
                    var sqlCrearArticulo = "INSERT INTO Articulos (Nombre, Precio) VALUES (@Nombre, @Precio);";
                    var sqlCrearContiene = "INSERT INTO Contiene (ID_facturas, ID_articulos, Cantidad) VALUES (@ID_facturas, @ID_articulos, @Cantidad);";

                    foreach (var articulo in factura.Articulos)
                    {
                        int articuloId; // Necesitamos el ID del artículo para la tabla 'Contiene'

                        var idExistente = await conexion.QueryFirstOrDefaultAsync<int?>(sqlBuscarArticulo, new { articulo.Nombre }, transaction);

                        if (idExistente.HasValue)
                        {
                            // SÍ EXISTE: Usamos el ID que encontramos
                            articuloId = idExistente.Value;
                            Console.WriteLine("articulo existe");
                        }
                        else
                        {
                            // NO EXISTE: Lo creamos en la tabla 'Articulos'
                            await conexion.ExecuteAsync(sqlCrearArticulo, articulo, transaction);
                            articuloId = await conexion.QuerySingleAsync<int>("SELECT last_insert_rowid();", transaction: transaction);
                            Console.WriteLine("articulo añadido");
                        }

                        await conexion.ExecuteAsync(sqlCrearContiene, new
                        {
                            ID_facturas = nuevaFacturaId,
                            ID_articulos = articuloId,
                            articulo.Cantidad
                        }, transaction);
                    }

                    transaction.Commit();
                    articulos.Clear();
                    Console.WriteLine("Factura guardada correctamente.");

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
