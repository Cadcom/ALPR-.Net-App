namespace AlprApi.Models
{
    public class AlprRequest
    {
        public string ImageData { get; set; } = string.Empty;
    }

    public class AlprResponse
    {
        public string LicensePlate { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
