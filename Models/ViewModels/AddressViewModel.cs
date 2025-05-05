using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Ecommerce.Models.ViewModels
{
    public class AddressViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string HouseNo { get; set; }
        public string SocietyName { get; set; }
        public string Landmark { get; set; }
        public string PinCode { get; set; }
        public string City { get; set; }
        public bool IsDefault { get; set; }
    }
}