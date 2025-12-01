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
        public async Task<List<Articulo>> cargarArticulos(List<Articulo> articulosCargados)
        {
            articulos.Clear();
            articulos = articulosCargados;
            return articulos;
        }

        public async Task<List<Factura>> obtenerFacturas()
        {
            iniciarConexion();
            var sql = @"
            SELECT ID_factura AS ID_factura, Nombre, Fecha, EsArchivada 
            FROM Facturas 
            WHERE EsArchivada = 0;
        
            SELECT 
                c.ID_factura,
                c.Cantidad,
                a.ID_articulo,
                a.Nombre,
                a.Precio
            FROM Articulos a
            JOIN Contiene c ON a.ID_articulo = c.ID_articulo;";

            using (var multi = await conexion.QueryMultipleAsync(sql))
            {
                var facturas = (await multi.ReadAsync<Factura>()).ToList();
                Console.WriteLine($"Facturas obtenidas: {facturas.Count}");
                var articulos = (await multi.ReadAsync<Articulo>()).ToList();
                foreach (var factura in facturas)
                {
                    factura.Articulos.AddRange(articulos.Where(a => a.ID_factura == factura.ID_factura));
                    Console.WriteLine($"Factura ID {factura.ID_factura} tiene {factura.Articulos.Count} artículos.");
                }
                return facturas;
            }
        }
        public async Task<Factura> obtenerFacturaPorId(int idFactura)
        {
            iniciarConexion();
            var sqlFactura = "SELECT * FROM Facturas WHERE ID_factura = @ID_factura;";
            var sqlArticulos = @"
            SELECT 
                c.ID_factura,
                c.Cantidad,
                a.ID_articulo,
                a.Nombre,
                a.Precio
            FROM Articulos a
            JOIN Contiene c ON a.ID_articulo = c.ID_articulo
            WHERE c.ID_factura = @ID_factura;";
            var factura = await conexion.QuerySingleOrDefaultAsync<Factura>(sqlFactura, new { ID_factura = idFactura });
            if (factura != null)
            {
                var articulos = (await conexion.QueryAsync<Articulo>(sqlArticulos, new { ID_factura = idFactura })).ToList();
                factura.Articulos.AddRange(articulos);
                Console.WriteLine($"Factura ID {factura.ID_factura} tiene {factura.Articulos.Count} artículos.");
            }
            else
            {
                Console.WriteLine($"No se encontró ninguna factura con ID {idFactura}.");
            }
            return factura;
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
                    factura.ID_factura = nuevaFacturaId;

                    var sqlBuscarArticulo = "SELECT ID_articulo FROM Articulos WHERE Nombre = @Nombre;";
                    var sqlCrearArticulo = "INSERT INTO Articulos (Nombre, Precio) VALUES (@Nombre, @Precio);";
                    var sqlCrearContiene = "INSERT INTO Contiene (ID_factura, ID_articulo, Cantidad) VALUES (@ID_factura, @ID_articulo, @Cantidad);";

                    foreach (var articulo in factura.Articulos)
                    {
                        int articuloId;

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
                            ID_factura = nuevaFacturaId,
                            ID_articulo = articuloId,
                            articulo.Cantidad
                        }, transaction);
                    }

                    transaction.Commit();
                    articulos.Clear();
                    Console.WriteLine("Factura guardada correctamente.");
                    cerrarConexion();

                }
                catch (Exception)
                {
                    Console.WriteLine("Error al guardar la factura. Realizando rollback.");

                    transaction.Rollback();
                    throw;
                }
            }
        }
        public async Task ActualizarFactura(Factura factura)
        {
            iniciarConexion();

            using (var transaction = conexion.BeginTransaction())
            {
                try
                {
                    var sqlUpdateFactura = "UPDATE Facturas SET Nombre = @Nombre, Fecha = @Fecha WHERE ID_factura = @ID_factura;";
                    await conexion.ExecuteAsync(sqlUpdateFactura, factura, transaction);

                    var sqlDeleteContiene = "DELETE FROM Contiene WHERE ID_factura = @ID_factura;";
                    await conexion.ExecuteAsync(sqlDeleteContiene, new { factura.ID_factura }, transaction);

                    var sqlBuscarArticulo = "SELECT ID_articulo FROM Articulos WHERE Nombre = @Nombre;";
                    var sqlCrearArticulo = "INSERT INTO Articulos (Nombre, Precio) VALUES (@Nombre, @Precio);";
                    var sqlCrearContiene = "INSERT INTO Contiene (ID_factura, ID_articulo, Cantidad) VALUES (@ID_factura, @ID_articulo, @Cantidad);";

                    foreach (var articulo in factura.Articulos)
                    {
                        int articuloId;
                        var idExistente = await conexion.QueryFirstOrDefaultAsync<int?>(sqlBuscarArticulo, new { articulo.Nombre }, transaction);

                        if (idExistente.HasValue)
                        {
                            articuloId = idExistente.Value;
                        }
                        else
                        {
                            await conexion.ExecuteAsync(sqlCrearArticulo, articulo, transaction);
                            articuloId = await conexion.QuerySingleAsync<int>("SELECT last_insert_rowid();", transaction: transaction);
                        }

                        await conexion.ExecuteAsync(sqlCrearContiene, new
                        {
                            ID_factura = factura.ID_factura,
                            ID_articulo = articuloId,
                            articulo.Cantidad
                        }, transaction);
                    }
                    transaction.Commit();
                    Console.WriteLine("Factura actualizada correctamente.");
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
            var sqlEliminarContiene = "DELETE FROM Contiene WHERE ID_factura = @ID_factura;";
            var sqlEliminarFactura = "DELETE FROM Facturas WHERE ID_factura = @ID_factura;";
            using (var transaction = conexion.BeginTransaction())
            {
                try
                {
                    await conexion.ExecuteAsync(sqlEliminarContiene, new { factura.ID_factura }, transaction);
                    await conexion.ExecuteAsync(sqlEliminarFactura, new { factura.ID_factura }, transaction);
                    transaction.Commit();
                    this.facturas.RemoveAll(f => f.ID_factura == factura.ID_factura);
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

        public async Task<List<Factura>> ObtenerFacturasPorAno(int ano)
        {
            iniciarConexion();

            var anoString = ano.ToString();

            var sql = @"
                SELECT ID_factura AS ID_factura, Nombre, Fecha 
                FROM Facturas 
                WHERE strftime('%Y', Fecha) = @Ano 
                AND EsArchivada = 0; -- ¡IMPORTANTE! QUE NO SALGAN EN EL REPORTE
        
                SELECT 
                    c.ID_factura AS ID_factura,
                    c.Cantidad,
                    a.ID_articulo,
                    a.Nombre,
                    a.Precio
                FROM Articulos a
                JOIN Contiene c ON a.ID_articulo = c.ID_articulo
                WHERE c.ID_factura IN (SELECT ID_factura FROM Facturas WHERE strftime('%Y', Fecha) = @Ano);
                ";

            using (var multi = await conexion.QueryMultipleAsync(sql, new { Ano = anoString }))
            {
                var facturas = (await multi.ReadAsync<Factura>()).ToList();
                var articulos = (await multi.ReadAsync<Articulo>()).ToList();

                foreach (var factura in facturas)
                {
                    factura.Articulos.AddRange(articulos.Where(a => a.ID_factura == factura.ID_factura));
                }
                cerrarConexion();
                return facturas;
               
            }
            
        }
        public async Task<DashboardDatos> ObtenerEstadisticas()
        {
            iniciarConexion();
            var datos = new DashboardDatos();

            var sql = @"
            SELECT A.Nombre FROM Contiene C JOIN Articulos A ON C.ID_articulo = A.ID_articulo GROUP BY A.ID_articulo ORDER BY SUM(C.Cantidad) DESC LIMIT 1;
        
            SELECT strftime('%m', Fecha) FROM Facturas GROUP BY strftime('%m', Fecha) ORDER BY COUNT(*) DESC LIMIT 1;
        
            SELECT Nombre FROM Facturas GROUP BY Nombre ORDER BY COUNT(*) DESC LIMIT 1;

            WITH TotalesPorFactura AS (
                SELECT F.ID_factura, F.Fecha, SUM(A.Precio * C.Cantidad) as Total
                FROM Facturas F
                JOIN Contiene C ON F.ID_factura = C.ID_factura
                JOIN Articulos A ON C.ID_articulo = A.ID_articulo
                GROUP BY F.ID_factura
            )
            SELECT MAX(Total), MIN(Total) FROM TotalesPorFactura;
        
            SELECT strftime('%Y', F.Fecha) as Anio, SUM(A.Precio * C.Cantidad) as TotalAnual
            FROM Facturas F
            JOIN Contiene C ON F.ID_factura = C.ID_factura
            JOIN Articulos A ON C.ID_articulo = A.ID_articulo
            GROUP BY Anio ORDER BY TotalAnual DESC;
            ";
            using (var multi = await conexion.QueryMultipleAsync(sql))
            {
                datos.ArticuloMasVendido = await multi.ReadFirstOrDefaultAsync<string>() ?? "N/A";

                var mesNumero = await multi.ReadFirstOrDefaultAsync<string>();
                datos.MesConMasVentas = mesNumero != null ? System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(int.Parse(mesNumero)) : "N/A";

                datos.ClienteMasRepetido = await multi.ReadFirstOrDefaultAsync<string>() ?? "N/A";

                var extremos = await multi.ReadFirstOrDefaultAsync<(double Max, double Min)>();
                datos.FacturaMasCara = extremos.Max;
                datos.FacturaMasBarata = extremos.Min;

                var ventasPorAno = (await multi.ReadAsync<(int Anio, double Total)>()).ToList();
                if (ventasPorAno.Any())
                {
                    datos.AnioMasVentas = ventasPorAno.First().Anio;
                    datos.AnioMenosVentas = ventasPorAno.Last().Anio;
                    datos.PromedioComprasAnuales = ventasPorAno.Average(x => x.Total);
                }
            }
            return datos;
        }
        public async Task<List<Factura>> ObtenerFacturasArchivadas()
        {
            iniciarConexion();
            var sql = @"
                SELECT ID_factura AS ID_factura, Nombre, Fecha, EsArchivada 
                FROM Facturas 
                WHERE EsArchivada = 1; -- AQUÍ BUSCAMOS LAS QUE SÍ ESTÁN ARCHIVADAS
        
                SELECT 
                    c.ID_factura AS ID_factura, c.Cantidad, a.ID_articulo, a.Nombre, a.Precio
                FROM Articulos a
                JOIN Contiene c ON a.ID_articulo = c.ID_articulo
                WHERE c.ID_factura IN (SELECT ID_factura FROM Facturas WHERE EsArchivada = 1);
            ";
            using (var multi = await conexion.QueryMultipleAsync(sql))
            {
                var facturas = (await multi.ReadAsync<Factura>()).ToList();
                Console.WriteLine($"Facturas obtenidas: {facturas.Count}");
                var articulos = (await multi.ReadAsync<Articulo>()).ToList();
                foreach (var factura in facturas)
                {
                    factura.Articulos.AddRange(articulos.Where(a => a.ID_factura == factura.ID_factura));
                    Console.WriteLine($"Factura ID {factura.ID_factura} tiene {factura.Articulos.Count} artículos.");
                }
                return facturas;
            }
        }

        public async Task ArchivarFacturaAsync(int idFactura)
        {
            iniciarConexion();
            // Simplemente cambiamos el 0 por un 1
            var sql = "UPDATE Facturas SET EsArchivada = 1 WHERE ID_factura = @Id";
            await conexion.ExecuteAsync(sql, new { Id = idFactura });
        }
        public async Task DesarchivarFacturaAsync(int idFactura)
        {
            iniciarConexion();
            var sql = "UPDATE Facturas SET EsArchivada = 0 WHERE ID_factura = @Id";
            await conexion.ExecuteAsync(sql, new { Id = idFactura });

        }
    }
}
