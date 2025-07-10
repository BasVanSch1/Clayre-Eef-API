using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CEApi.Models
{
    [PrimaryKey(nameof(ProductCode))]
    public class Product
    {
        public string ProductCode { get; set; }
        [MaxLength(13)]
        public string EanCode { get; set; }
        public string Description { get; set; }
        [Precision(14,2)]
        public double Price { get; set; }
        public string ImageUrl { get; set; }
    }
}
