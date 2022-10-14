using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MainProject.Models
{
    public class Message
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonIgnore]
        public string Id { get; set; }
        [JsonIgnore]
        public string RecieverId { get; set; }
        [JsonIgnore]
        public string Sender { get; set; }
        public string Text { get; set; }
        public DateTime When { get; set; } = DateTime.Now;
        [JsonIgnore]
        public string UserItemId { get; set; }
    }
}