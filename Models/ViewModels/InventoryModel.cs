namespace Ecommerce.Models.ViewModels
{
    public class InventoryViewModel
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public string Category { get; set; }
        public int Price { get; set; }
        public int Stock { get; set; }
        public string ProductImagePath { get; set; }
    }
}
