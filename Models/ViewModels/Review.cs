using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Ecommerce.Models.ViewModels
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string ReviewText { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}