using FacturasWEB.Components;
using FacturasWEB.Components.Data;
using FacturasWEB.Components.Servicios;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Retrieve the connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register SqliteConnection as a singleton service
builder.Services.AddScoped(sp => new SqliteConnection(connectionString));


builder.Services.AddScoped<ServicioControlador>();
builder.Services.AddScoped<ServicioFacturas>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

String ruta = "mibase.db";

using var conexion = new SqliteConnection($"DataSource={ruta}");
conexion.Open();
var comando = conexion.CreateCommand();
comando.CommandText = @"

PRAGMA foreign_keys = ON;


CREATE TABLE if not exists
Articulos (
    ID_articulo INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre TEXT NOT NULL,
    Precio REAL NOT NULL
);

CREATE TABLE if not exists
Facturas (
    ID_factura INTEGER PRIMARY KEY AUTOINCREMENT,
    Fecha TEXT NOT NULL, 
    Nombre TEXT NOT NULL
);

CREATE TABLE if not exists
Contiene (
    ID_factura INTEGER NOT NULL,
    ID_articulo INTEGER NOT NULL,
    Cantidad INTEGER NOT NULL,
    PRIMARY KEY (ID_factura, ID_articulo),
    FOREIGN KEY (ID_factura) REFERENCES Facturas(ID_factura)
        ON DELETE CASCADE,
    FOREIGN KEY (ID_articulo) REFERENCES Articulos(ID_articulo)
        ON DELETE RESTRICT
);
";
comando.ExecuteNonQuery();

app.Run();
