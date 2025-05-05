using System;
using System.Collections.Generic;
using System.Web;

namespace Ecommerce.Models.ViewModels
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }
        public string Category { get; set; }
        public string ProductTitle { get; set; }
        public string ProductImagePath { get; set; }
        public int Price { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public HttpPostedFileBase ProductImage { get; set; }
        public string OtherCategory { get; set; }
        public List<ReviewViewModel> Reviews { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalQuantitySold { get; set; }
        public int TotalSalesInRupees { get; set; }
        public String SellerName { get; set; }
        
        public bool IsBestSelling {  get; set; }

    }
    public class ReviewViewModel
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public string Rating { get; set; }
        public string ReviewText { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Username { get; set; } // Added username field
    }
}
