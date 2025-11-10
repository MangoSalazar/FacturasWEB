using FacturasWEB.Components;
using FacturasWEB.Components.Data;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


builder.Services.AddSingleton<ServicioFacturas>();

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
    ID_articulos INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre TEXT NOT NULL,
    Precio REAL NOT NULL -- 'REAL' es el tipo para números decimales en SQLite
);


CREATE TABLE if not exists
Facturas (
    ID_facturas INTEGER PRIMARY KEY AUTOINCREMENT,
    Fecha TEXT NOT NULL, 
    Nombre TEXT NOT NULL 
);


CREATE TABLE if not exists
Contiene (
    ID_facturas INTEGER NOT NULL,
    ID_articulos INTEGER NOT NULL,
    Cantidad INTEGER NOT NULL,
    

    PRIMARY KEY (ID_facturas, ID_articulos),
    

    FOREIGN KEY (ID_facturas) REFERENCES Facturas(ID_facturas)
        ON DELETE CASCADE,
    FOREIGN KEY (ID_articulos) REFERENCES Articulos(ID_articulos)
        ON DELETE RESTRICT
);
";
comando.ExecuteNonQuery();

app.Run();
