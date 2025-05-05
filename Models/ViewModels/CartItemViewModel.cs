using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Ecommerce.Models.ViewModels
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public int Price { get; set; }
        public int Quantity { get; set; }

        private int totalPrice;
        public int TotalPrice
        {
            get => Price * Quantity;
            set
            {
                totalPrice = value;
                // Optionally, set Price or Quantity based on your requirements
                // This assumes that when setting the total price, the logic for updating Price and Quantity needs to be handled manually
                if (Quantity != 0)
                {
                    Price = value / Quantity;  // For example: if we set total price, adjust price per unit based on quantity.
                }
            }
        }
        public string ProductImagePath { get; set; }
    }
}