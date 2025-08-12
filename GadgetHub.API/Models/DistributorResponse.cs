using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GadgetHub.API.Models
{
    [Table("DistributorResponses")]
    public class DistributorResponse
    {
        [Key]
        public int ResponseID { get; set; }
        
        [Required]
        public int QuotationID { get; set; }
        
        [Required]
        public int DistributorID { get; set; }
        
        [Required]
        public int ProductID { get; set; }
        
        public decimal? PricePerUnit { get; set; }
        
        public int? AvailableQuantity { get; set; }
        
        public int? DeliveryDays { get; set; }
        
        public bool IsSubmitted { get; set; }
        
        public DateTime? SubmittedDate { get; set; }
        
        [ForeignKey("QuotationID")]
        public virtual Quotation Quotation { get; set; }
        
        [ForeignKey("DistributorID")]
        public virtual Distributor Distributor { get; set; }
        
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}