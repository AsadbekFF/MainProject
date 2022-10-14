using System.ComponentModel.DataAnnotations.Schema;

namespace MainProject.Models
{
    public class LikedUser
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public bool IsLiked { get; set; }
        public string UserItemId { get; set; }
    }
}