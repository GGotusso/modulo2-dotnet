namespace Modulo2Web.Models
{
    public class VehicleMessage
    {
        public string patente { get; set; } = string.Empty;
        public string tipoVehiculo { get; set; } = string.Empty;
    }

    public class Vehicle
    {
        public string vehicle_id { get; set; } = string.Empty;
        public string plate { get; set; } = string.Empty;
        public string category_id { get; set; } = string.Empty;
        public string customer_id { get; set; } = string.Empty;
        public string? make { get; set; }
        public string? model { get; set; }
        public int? year { get; set; }
        public string? tag_id { get; set; }
        public string? associated_at { get; set; }
        public string? created_at { get; set; }
        public string? updated_at { get; set; }
    }

    public class Customer
    {
        public string customer_id { get; set; } = string.Empty;
        public string document_id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string? address { get; set; }
        public string state { get; set; } = string.Empty;
        public string? created_at { get; set; }
        public string? updated_at { get; set; }
    }

    public class PagoMessage
    {
        public string patente { get; set; } = string.Empty;
        public string tipoVehiculo { get; set; } = string.Empty;
    }

    public class MultaMessage
    {
        public string patente { get; set; } = string.Empty;
    }

    public class VehicleResponse
    {
        public List<Vehicle> data { get; set; } = new();
        public int limit { get; set; }
        public int offset { get; set; }
    }
}
