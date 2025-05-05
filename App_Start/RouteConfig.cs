using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Ecommerce
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Account", action = "Login", id = UrlParameter.Optional }
            );
            routes.MapRoute(
            name: "AdminUsers",
            url: "Admin/Users",
            defaults: new { controller = "Admin", action = "Users" }
            );
            routes.MapRoute(
           name: "ProductDetails",
           url: "Product/ProductDetails/{id}",
           defaults: new { controller = "Product", action = "ProductDetails" },
           constraints: new { id = @"\d+" } 
            );
           // routes.MapRoute(
           //name: "Category",
           //url: "Products/Category/{id}",
           //defaults: new { controller = "Products", action = "Category" }
           // );
        }
    }
}
