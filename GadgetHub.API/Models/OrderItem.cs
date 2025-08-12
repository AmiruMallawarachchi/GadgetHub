using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GadgetHub.API.Models
{
    [Table("OrderItems")]
    public class OrderItem
    {
        [Key]
        public int OrderItemID { get; set; }
        
        [Required]
        public int OrderID { get; set; }
        
        [Required]
        public int ProductID { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        public decimal? PricePerUnit { get; set; }
        
        [ForeignKey("OrderID")]
        public virtual Order Order { get; set; }
        
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}