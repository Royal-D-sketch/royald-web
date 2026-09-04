using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalD.Web.Models
{
    public class UserMenuPermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string MenuKey { get; set; } = string.Empty;

        public bool IsAllowed { get; set; }
    }
}
