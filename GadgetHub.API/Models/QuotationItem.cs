using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GadgetHub.API.Models
{
    [Table("QuotationItems")]
    public class QuotationItem
    {
        [Key]
        public int QuotationItemID { get; set; }
        
        [Required]
        public int QuotationID { get; set; }
        
        [Required]
        public int ProductID { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        [ForeignKey("QuotationID")]
        public virtual Quotation Quotation { get; set; }
        
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}