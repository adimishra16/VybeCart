using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Ecommerce.Models.ViewModels
{
    public class Orderitem
    {
        public int ProductId { get; set; }  // Product ID
        public string ProductName { get; set; }  // Product Name
        public int Quantity { get; set; }  // Ordered quantity
        public int Price { get; set; }  // Price per item

        // Property to calculate total price per item
        public int Total => Quantity * Price;
    }
}