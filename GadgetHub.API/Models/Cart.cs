using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GadgetHub.API.Models
{
    [Table("Cart")]
    public class Cart
    {
        [Key]
        public int CartID { get; set; }
        
        [Required]
        public int CustomerID { get; set; }
        
        [Required]
        public int ProductID { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        public DateTime AddedDate { get; set; }
        
        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }
        
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}