using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Ecommerce.Models.ViewModels
{
    public class UserLoginActivity
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public DateTime LoginTime { get; set; }
        public string ActivityDescription { get; set; }
    }
}