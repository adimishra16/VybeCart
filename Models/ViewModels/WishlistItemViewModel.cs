using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Ecommerce.Models.ViewModels
{
    public class WishlistItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public int Price { get; set; }
        public string ProductImagePath { get; set; }
    }
}