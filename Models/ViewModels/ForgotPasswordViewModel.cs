using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Ecommerce.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        public string Username { get; set; }
        public string SecurityAnswer { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}