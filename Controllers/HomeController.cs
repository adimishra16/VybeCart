using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Ecommerce.Models.ViewModels;

namespace Ecommerce.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            // Ensure the user is logged in
            if (Session["FirstName"] == null || Session["LastName"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch top 5 categories based on sales with their icons
            List<CategoryViewModel> categoryList = new List<CategoryViewModel>();

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            string categoryQuery = @"
        SELECT TOP 5 p.sCategory, SUM(o.nQuantity * p.nPrice) AS TotalRevenue, c.sIcon
        FROM Products_1460 p
        JOIN Orders_1460 o ON p.nProductid = o.nProductid
        JOIN categories c ON p.sCategory = c.scategory_name
        GROUP BY p.sCategory, c.sIcon
        ORDER BY TotalRevenue DESC";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand categoryCommand = new SqlCommand(categoryQuery, connection);
                connection.Open();

                using (SqlDataReader reader = categoryCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categoryList.Add(new CategoryViewModel
                        {
                            CategoryName = reader.GetString(0),
                            IconPath = reader["sIcon"] != DBNull.Value ? reader.GetString(2) : null  // Handle null icons
                        });
                    }
                }
            }

            // Store in ViewBag
            ViewBag.Categories = categoryList;

            // Pass user details to the view using ViewBag
            ViewBag.FullName = $"{Session["FirstName"]} {Session["LastName"]}";

            // Fetch product list
            List<ProductViewModel> products = new List<ProductViewModel>();

            // Query to get total revenue (price * quantity) and find the top 5 best-selling products
            string bestSellingQuery = @"
    WITH RankedProducts AS (
        SELECT 
            p.nProductid,
            p.sCategory,  
            SUM(o.nQuantity * p.nPrice) AS TotalSales,
            ROW_NUMBER() OVER (PARTITION BY p.sCategory ORDER BY SUM(o.nQuantity * p.nPrice) DESC) AS Rank
        FROM Products_1460 p
        JOIN Orders_1460 o ON p.nProductid = o.nProductid
        GROUP BY p.nProductid, p.sCategory
    )
    SELECT nProductid, sCategory, TotalSales
    FROM RankedProducts
    WHERE Rank = 1  
    ORDER BY sCategory";

            HashSet<int> bestSellingProductIds = new HashSet<int>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand bestSellingCommand = new SqlCommand(bestSellingQuery, connection);
                connection.Open();

                using (SqlDataReader reader = bestSellingCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bestSellingProductIds.Add(reader.GetInt32(0));
                    }
                }
            }

            // Query to fetch all products along with average ratings
            string query = @"
       SELECT 
            p.nProductid, 
            p.sProduct_title, 
            p.sCategory, 
            p.nPrice, 
            p.sProduct_image,
            COALESCE(AVG(r.sRating), 0) AS AvgRating 
        FROM Products_1460 p
        LEFT JOIN reviews_1460 r ON p.nProductid = r.nProduct_id
        WHERE p.bActive = 1
        GROUP BY p.nProductid, p.sProduct_title, p.sCategory, p.nPrice, p.sProduct_image";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new ProductViewModel
                        {
                            ProductId = reader.GetInt32(0),
                            ProductTitle = reader.GetString(1),
                            Category = reader.GetString(2),
                            Price = reader.GetInt32(3),
                            ProductImagePath = reader.GetString(4),
                            AverageRating = reader.GetDecimal(5),
                            IsBestSelling = bestSellingProductIds.Contains(reader.GetInt32(0))
                        });
                    }
                }
            }
            return View(products);
        }




        [HttpPost]
        public JsonResult AddToCart(int productId)
        {
            // Check if the user is logged in
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "User is not logged in." });
            }

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            int userId = Convert.ToInt32(Session["UserId"]);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Check if the product is already in the cart
                string checkQuery = "SELECT nQuantity FROM Shopping_cart_1460 WHERE nUserId = @UserId AND nProductId = @ProductId";
                SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@UserId", userId);
                checkCommand.Parameters.AddWithValue("@ProductId", productId);

                object result = checkCommand.ExecuteScalar();

                if (result != null) // Product already exists in cart
                {
                    int currentQuantity = Convert.ToInt32(result);
                    int newQuantity = currentQuantity + 1;

                    // Update quantity in the cart
                    string updateQuery = "UPDATE Shopping_cart_1460 SET nQuantity = @NewQuantity WHERE nUserId = @UserId AND nProductId = @ProductId";
                    SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@NewQuantity", newQuantity);
                    updateCommand.Parameters.AddWithValue("@UserId", userId);
                    updateCommand.Parameters.AddWithValue("@ProductId", productId);
                    updateCommand.ExecuteNonQuery();

                    return Json(new { success = true, message = "Product quantity updated in cart." });
                }
                else // Product is not in the cart, add it with quantity 1
                {
                    string insertQuery = "INSERT INTO Shopping_cart_1460 (nUserId, nProductId, nQuantity) VALUES (@UserId, @ProductId, 1)";
                    SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@UserId", userId);
                    insertCommand.Parameters.AddWithValue("@ProductId", productId);
                    insertCommand.ExecuteNonQuery();

                    return Json(new { success = true, message = "Product added to cart successfully." });
                }
            }
        }
        [HttpPost]
        public JsonResult UpdateCartQuantity(int productId, int quantity)
        {
            try
            {
                int userId = Convert.ToInt32(Session["UserId"]);
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Update the quantity in Shopping_cart_1460 table
                    string updateQuery = "UPDATE Shopping_cart_1460 SET nQuantity = @Quantity WHERE nProductId = @ProductId AND nUserId = @UserId";

                    using (var command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Quantity", quantity);
                        command.Parameters.AddWithValue("@ProductId", productId);
                        command.Parameters.AddWithValue("@UserId", userId);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return Json(new { success = true });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            return Json(new { success = false });
        }



        [HttpPost]
        public JsonResult AddToWishlist(int productId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "User is not logged in." });
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Check if the product already exists in the wishlist
                string checkQuery = "SELECT bActive FROM Wishlists_1460 WHERE nUserId = @UserId AND nProductId = @ProductId";
                SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@UserId", userId);
                checkCommand.Parameters.AddWithValue("@ProductId", productId);

                object result = checkCommand.ExecuteScalar();

                if (result != null)  // If product exists in wishlist
                {
                    int bActive = Convert.ToInt32(result);
                    if (bActive == 0) // If previously removed, activate it
                    {
                        string updateQuery = "UPDATE Wishlists_1460 SET bActive = 1 WHERE nUserId = @UserId AND nProductId = @ProductId";
                        SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                        updateCommand.Parameters.AddWithValue("@UserId", userId);
                        updateCommand.Parameters.AddWithValue("@ProductId", productId);
                        updateCommand.ExecuteNonQuery();

                        return Json(new { success = true, message = "Product added back to wishlist." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Product is already in wishlist." });
                    }
                }
                else  // Insert new wishlist entry
                {
                    string insertQuery = "INSERT INTO Wishlists_1460 (nUserId, nProductId, bActive) VALUES (@UserId, @ProductId, 1)";
                    SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@UserId", userId);
                    insertCommand.Parameters.AddWithValue("@ProductId", productId);
                    insertCommand.ExecuteNonQuery();

                    return Json(new { success = true, message = "Product added to wishlist." });
                }
            }
        }

        // Action to display the user's shopping cart
        public ActionResult Cart()
        {
            // Ensure the user is logged in
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            List<CartItemViewModel> cartItems = new List<CartItemViewModel>();
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            // Fetch items in the user's cart
            string query = @"
        SELECT c.nProductId, p.sProduct_title, p.nPrice, c.nQuantity
        FROM Shopping_cart_1460 c
        JOIN Products_1460 p ON c.nProductId = p.nProductid
        WHERE c.nUserId = @UserId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cartItems.Add(new CartItemViewModel
                        {
                            ProductId = reader.GetInt32(0),
                            ProductTitle = reader.GetString(1),
                            Price = reader.GetInt32(2),
                            Quantity = reader.GetInt32(3)
                        });
                    }
                }
            }

            return View(cartItems);  // Make sure to return List<CartItemViewModel> here
        }

        // Action to display the user's wishlist
        public ActionResult Wishlist()
        {
            // Ensure the user is logged in
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            List<WishlistItemViewModel> wishlistItems = new List<WishlistItemViewModel>();
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            // Fetch items in the user's wishlist
            string query = @"
            SELECT w.nProductId,p.sProduct_title, p.nPrice,p.sProduct_image
            FROM Wishlists_1460 w
            JOIN Products_1460 p ON w.nProductId = p.nProductid
            WHERE w.nUserId = @UserId AND w.bActive = 1";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        wishlistItems.Add(new WishlistItemViewModel
                        {
                            ProductId = reader.GetInt32(0),
                            ProductTitle = reader.GetString(1),
                            Price = reader.GetInt32(2),
                            ProductImagePath = reader.GetString(3),
                        });
                    }
                }
            }

            return View(wishlistItems);
        }

        // Remove Items from the cart
        [HttpPost]
        public JsonResult RemoveItem(int productId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            int userId = Convert.ToInt32(Session["UserId"]);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "DELETE FROM Shopping_cart_1460 WHERE nUserId = @UserId AND nProductId = @ProductId";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@ProductId", productId);
                command.ExecuteNonQuery();
            }

            return Json(new { success = true, message = "Product removed from cart." });
        }
        [HttpPost]
        public JsonResult ToggleWishlist(int productId)
        {
            // Ensure the user is logged in
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "User is not logged in." });
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Check if the product already exists in the wishlist
                string checkQuery = "SELECT bActive FROM Wishlists_1460 WHERE nUserId = @UserId AND nProductId = @ProductId";
                SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@UserId", userId);
                checkCommand.Parameters.AddWithValue("@ProductId", productId);

                object result = checkCommand.ExecuteScalar();

                if (result != null) // If product exists in the wishlist
                {
                    int bActive = Convert.ToInt32(result);
                    if (bActive == 1) // If it's active, deactivate it
                    {
                        string updateQuery = "UPDATE Wishlists_1460 SET bActive = 0 WHERE nUserId = @UserId AND nProductId = @ProductId";
                        SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                        updateCommand.Parameters.AddWithValue("@UserId", userId);
                        updateCommand.Parameters.AddWithValue("@ProductId", productId);
                        updateCommand.ExecuteNonQuery();

                        return Json(new { success = true, message = "Product removed from wishlist." });
                    }
                    else // If it's inactive, activate it
                    {
                        string updateQuery = "UPDATE Wishlists_1460 SET bActive = 1 WHERE nUserId = @UserId AND nProductId = @ProductId";
                        SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                        updateCommand.Parameters.AddWithValue("@UserId", userId);
                        updateCommand.Parameters.AddWithValue("@ProductId", productId);
                        updateCommand.ExecuteNonQuery();

                        return Json(new { success = true, message = "Product added back to wishlist." });
                    }
                }
                else // If the product does not exist in the wishlist, insert it as active
                {
                    string insertQuery = "INSERT INTO Wishlists_1460 (nUserId, nProductId, bActive) VALUES (@UserId, @ProductId, 1)";
                    SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@UserId", userId);
                    insertCommand.Parameters.AddWithValue("@ProductId", productId);
                    insertCommand.ExecuteNonQuery();

                    return Json(new { success = true, message = "Product added to wishlist." });
                }
            }

        }
        [HttpPost]
        public ActionResult RemoveFromWishlist(int productId)
        {
            int userId = Convert.ToInt32(Session["UserId"]); // Ensure the correct session variable for user ID
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            try
            {
                // Ensure correct table structure by using proper column names
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                UPDATE Wishlists_1460 
                SET bActive = 0 
                WHERE nProductId = @ProductId AND nUserId = @UserId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", productId);
                        command.Parameters.AddWithValue("@UserId", userId);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return Json(new { success = true });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Item not found in wishlist." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error (you can use a logger here)
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }
        public ActionResult Profile()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            UserViewModel model = new UserViewModel();
            List<AddressViewModel> addresses = new List<AddressViewModel>();
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // Fetch user details
                string userQuery = "SELECT sFirst_name, sLast_name, sUsername, sEmail, sPhone FROM User_1460 WHERE nUserId = @UserId";
                using (SqlCommand userCmd = new SqlCommand(userQuery, con))
                {
                    userCmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    using (SqlDataReader reader = userCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.FirstName = reader["sFirst_name"]?.ToString() ?? "";
                            model.LastName = reader["sLast_name"]?.ToString() ?? "";
                            model.Username = reader["sUsername"]?.ToString() ?? "";
                            model.Email = reader["sEmail"]?.ToString() ?? "";
                            model.Phone = reader["sPhone"]?.ToString() ?? "";
                        }
                    }
                }

                // Fetch addresses
                string addressQuery = "SELECT nId, vHouseNo, vSocietyName, vLandmark, nPinCode, vCity, bIsDefault FROM Addresses WHERE nUserId = @UserId";
                using (SqlCommand addressCmd = new SqlCommand(addressQuery, con))
                {
                    addressCmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    using (SqlDataReader reader = addressCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            addresses.Add(new AddressViewModel
                            {
                                Id = reader.GetInt32(0),
                                HouseNo = reader.GetString(1),
                                SocietyName = reader.GetString(2),
                                Landmark = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                PinCode = reader.GetString(4),
                                City = reader.GetString(5),
                                IsDefault = reader.GetBoolean(6)
                            });
                        }
                    }
                }
            }

            model.Addresses = addresses;
            return View(model);
        }

        [HttpPost]
        public JsonResult UpdateProfile(UserViewModel model)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"UPDATE User_1460 
                         SET sFirst_name = @FirstName, sLast_name = @LastName, sUsername = @Username, 
                             sEmail = @Email, sPhone = @Phone ,dupdated_at=GetDate()
                         WHERE nUserId = @UserId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FirstName", model.FirstName);
                    command.Parameters.AddWithValue("@LastName", model.LastName);
                    command.Parameters.AddWithValue("@Username", model.Username);
                    command.Parameters.AddWithValue("@Email", model.Email);
                    command.Parameters.AddWithValue("@Phone", model.Phone);
                    command.Parameters.AddWithValue("@UserId", userId);

                    command.ExecuteNonQuery();
                }
            }

            return Json(new { success = true, message = "Profile updated successfully!" });
        }

        [HttpPost]
        public JsonResult SaveAddress(AddressViewModel model)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                string insertQuery = @"INSERT INTO Addresses (nUserId, vHouseNo, vSocietyName, vLandmark, nPinCode, vCity, bIsDefault) 
                               VALUES (@UserId, @HouseNo, @SocietyName, @Landmark, @PinCode, @City, 0)";

                using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                {
                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    cmd.Parameters.Add("@HouseNo", SqlDbType.NVarChar, 50).Value = model.HouseNo;
                    cmd.Parameters.Add("@SocietyName", SqlDbType.NVarChar, 255).Value = model.SocietyName;
                    cmd.Parameters.Add("@Landmark", SqlDbType.NVarChar, 255).Value = string.IsNullOrEmpty(model.Landmark) ? (object)DBNull.Value : model.Landmark;
                    cmd.Parameters.Add("@PinCode", SqlDbType.NVarChar, 10).Value = model.PinCode;
                    cmd.Parameters.Add("@City", SqlDbType.NVarChar, 100).Value = model.City;

                    cmd.ExecuteNonQuery();
                }
            }

            return Json(new { success = true, message = "Address saved successfully!" });
        }

        [HttpPost]
        public JsonResult SetDefaultAddress(int addressId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        string resetQuery = "UPDATE Addresses SET bIsDefault = 0 WHERE nUserId = @UserId";
                        string setDefaultQuery = "UPDATE Addresses SET bIsDefault = 1 WHERE nId = @AddressId AND nUserId = @UserId";

                        using (SqlCommand resetCmd = new SqlCommand(resetQuery, con, transaction))
                        {
                            resetCmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                            resetCmd.ExecuteNonQuery();
                        }

                        using (SqlCommand setCmd = new SqlCommand(setDefaultQuery, con, transaction))
                        {
                            setCmd.Parameters.Add("@AddressId", SqlDbType.Int).Value = addressId;
                            setCmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                            setCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return Json(new { success = true, message = "Default address updated!" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }
        }
        [HttpPost]
        public JsonResult DeleteAddress(int addressId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Check if the address exists and if it's default
                    string checkQuery = "SELECT bIsDefault FROM Addresses WHERE nId = @AddressId";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@AddressId", addressId);
                        object result = checkCmd.ExecuteScalar();

                        if (result == null)
                        {
                            return Json(new { success = false, message = "Address not found." });
                        }

                        bool isDefault = Convert.ToBoolean(result);

                        if (isDefault)
                        {
                            return Json(new { success = false, message = "Default address cannot be deleted." });
                        }
                    }

                    // Delete the address
                    string deleteQuery = "DELETE FROM Addresses WHERE nId = @AddressId";
                    using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, con))
                    {
                        deleteCmd.Parameters.AddWithValue("@AddressId", addressId);
                        int rowsAffected = deleteCmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Json(new { success = true, message = "Address deleted successfully." });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Failed to delete the address." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }


        // ✅ Display My Orders Page
        public ActionResult MyOrders()
        {
            return View();
        }

        // ✅ Fetch Orders for the Logged-in User
        // ✅ Fetch Orders for the Logged-in User (Including Image Path)
        [HttpGet]
        public ActionResult GetMyOrders(string status = "")
        {
            int userId = Convert.ToInt32(Session["UserId"]); // Get logged-in user ID

            List<OrderViewModel> orders = new List<OrderViewModel>();
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // ✅ Fetching Product Image Path and Handling NULL DeliveryDate
                string query = @"
                    SELECT P.sProduct_title, P.nPrice, 
                           O.nQuantity, O.nOrderprice, O.sStatus, O.sStatus_description, 
                           P.sProduct_image, O.dDeliveryDate
                    FROM Orders_1460 O
                    INNER JOIN Products_1460 P ON O.nProductId = P.nProductId
                    WHERE O.nUserId = @userId";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND O.sStatus = @status";
                }

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    if (!string.IsNullOrEmpty(status))
                    {
                        command.Parameters.AddWithValue("@status", status);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orders.Add(new OrderViewModel
                            {
                                
                                ProductTitle = reader.GetString(0),        // sProduct_title
                                ProductPrice = reader.GetInt32(1),       // nPrice (decimal fix)
                                Quantity = reader.GetInt32(2),             // nQuantity
                                OrderPrice = reader.GetInt32(3),         // nOrderprice (decimal fix)
                                Status = reader.GetString(4),              // sStatus
                                StatusDescription = reader.GetString(5),   // sStatus_description
                                ProductImagePath = reader.IsDBNull(6) ? "/images/default.jpg" : reader.GetString(6), // sProduct_image
                                DeliveryDate = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7),
                            });
                        }
                    }
                }
            }

            return Json(orders, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult UpdateOrderStatus(int orderId, string status, string statusDescription, DateTime? deliveryDate)
        {
            if (orderId <= 0 || string.IsNullOrEmpty(status))
            {
                return Json(new { success = false, message = "Invalid input data." });
            }
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var transaction = connection.BeginTransaction(); // Start transaction

                try
                {
                    // Update order status and delivery date
                    string query = @"
                UPDATE Orders_1460 
                SET sStatus = @Status, 
                    sStatus_Description = @StatusDescription,
                    dDeliveryDate = @DeliveryDate
                WHERE nOrder_Id = @OrderId";

                    using (var command = new SqlCommand(query, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@OrderId", orderId);
                        command.Parameters.AddWithValue("@Status", status);
                        command.Parameters.AddWithValue("@StatusDescription", statusDescription ?? string.Empty);
                        command.Parameters.AddWithValue("@DeliveryDate",
                            (object)deliveryDate ?? DBNull.Value);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            return Json(new { success = false, message = "Order not found or no changes made." });
                        }
                    }

                    // If the order is delivered, update inventory
                    if (status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                    {
                        string getProductQuery = @"
                    SELECT o.nProductId, o.nQuantity
                    FROM Orders_1460 o
                    WHERE o.nOrder_Id = @OrderId";

                        int productId = 0;
                        int quantity = 0;

                        using (var command = new SqlCommand(getProductQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@OrderId", orderId);

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    productId = reader.GetInt32(0);
                                    quantity = reader.GetInt32(1);
                                }
                            }
                        }

                        if (productId > 0 && quantity > 0)
                        {
                            // Update reserved stock and main stock
                            string updateInventoryQuery = @"
                        UPDATE Inventory_1460 
                        SET 
                            nReservedStock = CASE 
                                WHEN nReservedStock >= @Quantity THEN nReservedStock - @Quantity 
                                ELSE 0 
                            END,
                            nStock = CASE 
                                WHEN nStock >= @Quantity THEN nStock - @Quantity 
                                ELSE 0 
                            END
                        WHERE nProductId = @ProductId 
                        AND nReservedStock IS NOT NULL
                        AND nStock IS NOT NULL;";

                            using (var command = new SqlCommand(updateInventoryQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Quantity", quantity);
                                command.Parameters.AddWithValue("@ProductId", productId);

                                int inventoryRowsAffected = command.ExecuteNonQuery();

                                if (inventoryRowsAffected == 0)
                                {
                                    throw new Exception("Failed to update inventory. Product may not exist or stock is insufficient.");
                                }
                            }
                        }
                    }
                    // Commit transaction
                    transaction.Commit();

                    return Json(new { success = true, message = "Order status and delivery date updated successfully." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); // Rollback in case of error
                    return Json(new { success = false, message = "An error occurred: " + ex.Message });
                }
            }
        }


        [HttpPost]
        public JsonResult UpdateQuantity(int productId, int quantity)
        {
            try
            {
                // ✅ 1️⃣ Update Session
                if (Session["Cart"] != null)
                {
                    List<CartItemViewModel> cart = (List<CartItemViewModel>)Session["Cart"];
                    var item = cart.FirstOrDefault(p => p.ProductId == productId);
                    if (item != null)
                    {
                        item.Quantity = quantity;
                        item.TotalPrice = item.Price * quantity;
                    }
                    Session["Cart"] = cart; // ✅ Save updated cart in session
                }

                // ✅ 2️⃣ Update Database using ADO.NET
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "UPDATE Cart SET Quantity = @Quantity WHERE ProductId = @ProductId";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Quantity", quantity);
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, newTotal = quantity });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        public ActionResult Logout()
        {
            // Clear the session
            Session.Clear();
            Session.Abandon();

            // Remove authentication cookie
            if (Request.Cookies["AuthCookie"] != null)
            {
                var cookie = new HttpCookie("AuthCookie");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }

            // Redirect to login page
            return RedirectToAction("Login", "Account");
        }
        public ActionResult Category(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return HttpNotFound(); // If no category is passed, return 404
            }

            List<ProductViewModel> products = new List<ProductViewModel>();

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            string query = @"
            select DISTINCT scategory from products_1460";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Category", id);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new ProductViewModel
                        {
                            //ProductId = reader.GetInt32(0),
                            //ProductTitle = reader.GetString(1),
                            Category = reader.GetString(0),
                            //Price = reader.GetInt32(3),
                            //ProductImagePath = reader.GetString(4),
                            //AverageRating = reader.GetDecimal(5)
                        });
                    }
                }
            }

            if (products.Count == 0)
            {
                return HttpNotFound(); // If no products found, return 404
            }

            ViewBag.SelectedCategory = id; // Store selected category for display
            return View("Category", products);
        }
        private List<ProductViewModel> GetAllProducts()
        {
            List<ProductViewModel> products = new List<ProductViewModel>();
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT nProductid, sProduct_title, sCategory, nPrice, sProduct_image FROM Products_1460";
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new ProductViewModel
                        {
                            ProductId = reader.GetInt32(0),
                            ProductTitle = reader.GetString(1),
                            Category = reader.GetString(2),
                            Price = reader.GetInt32(3),
                            ProductImagePath = reader.GetString(4)
                        });
                    }
                }
            }
            return products;
        }

        [HttpGet]
        public JsonResult SearchProducts(string query)
        {
            List<ProductViewModel> products = GetAllProducts();
            var filteredProducts = products
                .Where(p => p.ProductTitle.ToLower().Contains(query.ToLower()))
                .Select(p => new
                {
                    id = p.ProductId,
                    title = p.ProductTitle,
                    image = p.ProductImagePath
                }).Take(5).ToList();

            return Json(filteredProducts, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult ChangePassword(string OldPassword, string NewPassword)
        {
            int userId = Convert.ToInt32(Session["UserId"]);
            if (userId == 0)
            {
                return Json(new { success = false, message = "User not logged in!" });
            }
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // Check if old password is correct
                string checkPasswordQuery = "SELECT sPassword FROM User_1460 WHERE nUserId = @UserId";
                using (SqlCommand cmd = new SqlCommand(checkPasswordQuery, con))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    string storedPassword = cmd.ExecuteScalar()?.ToString();

                    if (storedPassword != OldPassword)
                    {
                        return Json(new { success = false, message = "Old password is incorrect!" });
                    }
                }

                // Update the password
                string updatePasswordQuery = "UPDATE User_1460 SET sPassword = @NewPassword , dupdated_at=GETDATE() WHERE nUserId = @UserId";
                using (SqlCommand cmd = new SqlCommand(updatePasswordQuery, con))
                {
                    cmd.Parameters.AddWithValue("@NewPassword", NewPassword);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }

            return Json(new { success = true, message = "Password updated successfully!" });
        }

    }
}

