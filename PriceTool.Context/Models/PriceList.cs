using System.ComponentModel.DataAnnotations;

namespace PriceTool.Context.Models
{
    public class PriceList : BaseEntity
    {
        public Guid Id { get; set; }

        [MaxLength(255)]
        public string Name { get; set; }
    }
}