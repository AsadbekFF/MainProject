using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MainProject.Models
{
    public class UserCollections
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonIgnore]
        public string Id { get; set; }
        [Required]
        public string Name { get; set; }
        public List<UserItem> Items { get; set; } = new();
        [JsonIgnore]
        public List<ExtraField> ExtraFields { get; set; } = new();
        [Required]
        public string Topic { get; set; }
        public DateTime When { get; set; } = DateTime.Now;
        public string Description { get; set; }
        [JsonIgnore]
        public string Image { get; set; }
        [JsonIgnore]
        public string UserId { get; set; }
    }
}