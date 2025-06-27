using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PriceTool.Context.Models
{
    public abstract class BaseEntity
    {
        [JsonIgnore]
        [ScaffoldColumn(false)]
        public DateTime DateCreated { get; set; }

        [JsonIgnore]
        [ScaffoldColumn(false)]
        public DateTime DateModified { get; set; }
    }
}
