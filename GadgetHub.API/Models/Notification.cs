using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GadgetHub.API.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }
        
        [Required]
        public int CustomerID { get; set; }
        
        public int? OrderID { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Message { get; set; }
        
        public bool IsRead { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }
        
        [ForeignKey("OrderID")]
        public virtual Order Order { get; set; }
    }
}