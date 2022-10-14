using System.ComponentModel.DataAnnotations.Schema;

namespace MainProject.Models
{
    public class ExtraField
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}