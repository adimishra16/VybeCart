using System;
using System.Collections.Generic;

namespace Ecommerce.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public List<OrderItem> OrderSummary { get; set; }
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; }
    }
    public class OrderItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
