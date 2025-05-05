using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Ecommerce.Models.ViewModels
{
    public class OrderViewModel
    {
        public int OrderId { get; set; } 
        public int UserId { get; set; }                 
        public string UserName { get; set; }           
        public int ProductId { get; set; }              
        public string ProductTitle { get; set; }
        public string ProductImagePath { get; set; }
        public int Quantity { get; set; }               
        public int OrderPrice { get; set; }
        public int ProductPrice { get; set; }
        public string Status { get; set; }             
        public string StatusDescription { get; set; }   
        public string ShippingAddress { get; set; }    
        public DateTime?DeliveryDate { get; set; }    
        public DateTime OrderDate { get; set; }
        public string PaymentMethod { get; set; }
    }
}