using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MainProject.Models
{
    public class UserItem
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonIgnore]
        public string Id { get; set; }
        [JsonIgnore]
        public string UserCollectionsId { get; set; }
        [JsonIgnore]
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        [JsonPropertyName("Date of creation")]
        public DateTime DateOfEntered { get; set; } = DateTime.Now;
        public List<Tag> Tags { get; set; } = new();
        public List<Message> Chat { get; set; } = new();
        [JsonPropertyName("Extra fields")]
        [JsonIgnore]
        public List<ExtraFieldValue> ExtraFieldValues { get; set; } = new();
        [JsonIgnore]
        public byte[] Image { get; set; }
        public int LikeCount { get; set; }
        [JsonIgnore]
        public List<LikedUser> LikedUsers { get; set; } = new();
    }
}