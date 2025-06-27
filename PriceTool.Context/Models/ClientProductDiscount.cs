using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PriceTool.Context.Models;

public class ClientProductDiscount : BaseEntity
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    [JsonIgnore]
    public Client? Client { get; set; }

    public Guid ProductId { get; set; }

    [JsonIgnore]
    public Product? Product { get; set; }

    [Column(TypeName = "decimal(30,6)")]
    public decimal? FixedPrice { get; set; }

    [Column(TypeName = "decimal(16,6)")]
    public decimal? Discount { get; set; }
}
