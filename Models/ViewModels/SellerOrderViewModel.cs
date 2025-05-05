using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Ecommerce.Models.ViewModels
{
    public class SellerOrderViewModel
    {
        public int OrderId { get; set; }
        public int UserId { get; set; } // Customer ID
        public string CustomerName { get; set; } // Full Name of the Customer
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; } // Ensure decimal for monetary values
        public String OrderDate { get; set; }
        public String DeliveryDate { get; set; } // Nullable, in case the order is not delivered yet
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; }
        public string OrderStatus { get; set; } // Pending, Shipped, Delivered, Canceled
        public string StatusDescription { get; set; } // Additional comments or status description
    }

}