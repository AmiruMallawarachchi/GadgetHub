using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GadgetHub.API.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int OrderID { get; set; }
        
        [Required]
        public int QuotationID { get; set; }
        
        [Required]
        public int CustomerID { get; set; }
        
        [Required]
        public int SelectedDistributorID { get; set; }
        
        public decimal? TotalAmount { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; }
        
        public DateTime? EstimatedDeliveryDate { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public DateTime? ConfirmedDate { get; set; }
        
        [ForeignKey("QuotationID")]
        public virtual Quotation Quotation { get; set; }
        
        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }
        
        [ForeignKey("SelectedDistributorID")]
        public virtual Distributor SelectedDistributor { get; set; }
        
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}