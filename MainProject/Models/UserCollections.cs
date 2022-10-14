using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MainProject.Models
{
    public class UserCollections
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonIgnore]
        public string Id { get; set; }
        public string Name { get; set; }
        public List<UserItem> Items { get; set; } = new();
        [JsonIgnore]
        public List<ExtraField> ExtraFields { get; set; } = new();
        public string Topic { get; set; }
        public DateTime When { get; set; } = DateTime.Now;
        public string Description { get; set; }
        [JsonIgnore]
        public byte[] Image { get; set; }
        [JsonIgnore]
        public string UserId { get; set; }
    }
}