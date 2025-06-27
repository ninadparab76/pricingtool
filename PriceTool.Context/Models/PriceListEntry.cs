using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PriceTool.Context.Models;

public class PriceListEntry : BaseEntity
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    [JsonIgnore]
    public Product? Product { get; set; }

    public Guid PriceListId { get; set; }

    [JsonIgnore]
    public PriceList? PriceList { get; set; }

    [Column(TypeName = "decimal(30,6)")]
    public decimal? OutPrice { get; set; }
}
