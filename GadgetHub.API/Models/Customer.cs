using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GadgetHub.API.Models
{
    [Table("Customers")]
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Email { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Password { get; set; }
        
        [StringLength(20)]
        public string Phone { get; set; }
        
        [StringLength(255)]
        public string Address { get; set; }
        
        public DateTime CreatedDate { get; set; }
    }
}