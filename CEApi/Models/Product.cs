using Microsoft.EntityFrameworkCore;

namespace CEApi.Models
{
    [PrimaryKey(nameof(ProductCode))]
    public class Product
    {
        public string ProductCode { get; set; }
        public string Description { get; set; }
        public string EAN { get; set; }
        public double Price { get; set; }
        public string ImageLink { get; set; }
    }
}
