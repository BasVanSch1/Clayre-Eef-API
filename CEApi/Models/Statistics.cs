using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace CEApi.Models
{
    [PrimaryKey(nameof(Id))]
    [Index(nameof(Name), IsUnique = true)]
    public class Statistics
    {
        public string? Id { get; set; }
        public string Name { get; set; }
        public int LookupsByEAN { get; set; } = 0;
        public int LookupsByCode { get; set; } = 0;


        // These properties are not retrieved from the database, but are calculated based on the data in the database.
        [NotMapped]
        public int TotalLookups => LookupsByEAN + LookupsByCode;
        [NotMapped]
        public int TotalProducts { get; set; }
        [NotMapped]
        public int TotalUsers { get; set; }
    }
}
