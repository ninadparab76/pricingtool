namespace PriceTool.Context.Models;

using System.Text.Json.Serialization;

public class Domain : BaseEntity
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    [JsonIgnore]
    public Client? Client { get; set; }    
    
    public string? DomainName { get; set; }

    public string? Tld { get; set; }

    public DateTime? ExpirationDate { get; internal set; }

    public string? Comment { get; internal set; }

    public bool HasRegistryLock { get; set; }

    public int? RenewPeriod { get; set; }

    public bool HasLocalPresence { get; set; }

    public bool HasPrivacy { get; set; }

    public bool HasProxy { get; set; }
}
