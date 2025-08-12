using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GadgetHub.API.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int ProductID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [StringLength(255)]
        public string ImageURL { get; set; }
        
        [StringLength(50)]
        public string Category { get; set; }
        
        public DateTime CreatedDate { get; set; }
    }
}