using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MainProject.Models
{
    public class Tag
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonIgnore]
        public string Id { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public string UserItemId { get; set; }
    }
}