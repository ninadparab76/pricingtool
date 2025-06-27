using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceTool.Context.Models
{
    public class Product : BaseEntity
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string ProductName { get; set; }

        public string Category { get; set; }

        [Required]
        [MaxLength(63)]
        public string Tld { get; set; }

        public int? Period { get; set; }

        [Column(TypeName = "decimal(30,6)")]
        public decimal BasePrice { get; set; }
    }
}
