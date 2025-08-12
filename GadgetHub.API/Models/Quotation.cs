using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GadgetHub.API.Models
{
    [Table("Quotations")]
    public class Quotation
    {
        [Key]
        public int QuotationID { get; set; }
        
        [Required]
        public int CustomerID { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }
    }
}