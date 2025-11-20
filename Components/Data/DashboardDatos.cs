namespace FacturasWEB.Components.Data
{
    public class DashboardDatos
    {
        public string ArticuloMasVendido { get; set; } = "Sin datos";
        public string MesConMasVentas { get; set; } = "Sin datos";
        public string ClienteMasRepetido { get; set; } = "Sin datos";

        public double PromedioComprasAnuales { get; set; }
        public double FacturaMasCara { get; set; }
        public double FacturaMasBarata { get; set; }

        public int AnioMasVentas { get; set; }
        public int AnioMenosVentas { get; set; }
    }
}
