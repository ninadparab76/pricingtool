namespace PriceTool.Context.Models;

using System.Text.Json.Serialization;

public class Client : BaseEntity
{
    public Guid Id { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Title { get; set; }

    public Guid? PriceListId { get; set; }

    [JsonIgnore]
    public PriceList? PriceList { get; set; }
}