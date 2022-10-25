using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MainProject.Models
{
    public class ExtraFieldValue
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonIgnore]
        public string Id { get; set; }
        public string Value { get; set; }
        [JsonIgnore]
        public string UserItemId { get; set; }
        public string ExtraFieldName { get; set; }
    }
}