using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using Ecommerce.Models.ViewModels;

namespace Ecommerce.Controllers
{
    public class AdminController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["UserId"] == null || Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                filterContext.Result = RedirectToAction("Login", "Account");
            }

            base.OnActionExecuting(filterContext);
        }
        // Fetch data from the database
        public ActionResult Users()
        {
            List<UserViewModel> users = new List<UserViewModel>();

            // Connection string from Web.config
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            // SQL query to fetch data
            string query = "SELECT nUserId, sFirst_Name, sLast_Name, sUsername, sEmail, sPhone, sRole, dCreated_at, dUpdated_at,bActivity_status FROM User_1460";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new UserViewModel
                        {
                            UserId = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            Username = reader.GetString(3),
                            Email = reader.GetString(4),
                            Phone = reader.GetString(5),
                            Role = reader.GetString(6),
                            CreatedAt = reader.GetDateTime(7),
                            UpdatedAt = reader.GetDateTime(8),
                            Activitystatus = reader.GetBoolean(9)
                        });
                    }
                }
            }

            return View(users);
        }
        [HttpGet]
        public JsonResult GetUsers(int page = 1, string search = "")
        {
            List<UserViewModel> users = new List<UserViewModel>();
            int pageSize = 10;
            int skip = (page - 1) * pageSize;

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            string query = @"SELECT nUserId, sFirst_Name, sLast_Name, sUsername, sEmail, sPhone, sRole, dCreated_at, dUpdated_at, bActivity_status
                     FROM User_1460
                     WHERE sFirst_Name LIKE @Search OR sLast_Name LIKE @Search OR sUsername LIKE @Search OR sEmail LIKE @Search
                     ORDER BY nUserId
                     OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Search", "%" + search + "%");
                command.Parameters.AddWithValue("@Skip", skip);
                command.Parameters.AddWithValue("@PageSize", pageSize);

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new UserViewModel
                        {
                            UserId = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            Username = reader.GetString(3),
                            Email = reader.GetString(4),
                            Phone = reader.GetString(5),
                            Role = reader.GetString(6),
                            CreatedAt = reader.GetDateTime(7),
                            UpdatedAt = reader.GetDateTime(8),
                            Activitystatus = reader.GetBoolean(9)
                        });
                    }
                }
            }

            // Fetch total user count for pagination
            int totalCount;
            string countQuery = @"SELECT COUNT(*) FROM User_1460 
                          WHERE sFirst_Name LIKE @Search OR sLast_Name LIKE @Search OR sUsername LIKE @Search OR sEmail LIKE @Search";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(countQuery, connection);
                command.Parameters.AddWithValue("@Search", "%" + search + "%");
                connection.Open();
                totalCount = (int)command.ExecuteScalar();
            }

            bool hasPrevious = page > 1;
            bool hasNext = (skip + pageSize) < totalCount;

            return Json(new { users, hasPrevious, hasNext, currentPage = page, totalPages = (int)Math.Ceiling((double)totalCount / pageSize) }, JsonRequestBehavior.AllowGet);
        }
        //public ActionResult Product()
        //{
        //    return View();
        //}

        // Save (Update) User Details

        [HttpPost]
        public ActionResult SaveUser(UserViewModel model)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            string updateQuery = "UPDATE User_1460 SET sFirst_Name = @FirstName, sLast_Name = @LastName, sUsername = @Username, sEmail = @Email, sPhone = @Phone, sRole = @Role, bActivity_status = @ActivityStatus WHERE nUserId = @UserId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(updateQuery, connection);

                command.Parameters.AddWithValue("@UserId", model.UserId);
                command.Parameters.AddWithValue("@FirstName", model.FirstName);
                command.Parameters.AddWithValue("@LastName", model.LastName);
                command.Parameters.AddWithValue("@Username", model.Username);
                command.Parameters.AddWithValue("@Email", model.Email);
                command.Parameters.AddWithValue("@Phone", model.Phone);
                command.Parameters.AddWithValue("@Role", model.Role);
                command.Parameters.AddWithValue("@ActivityStatus", model.Activitystatus);

                connection.Open();
                command.ExecuteNonQuery();
            }

            return Json(new { success = true }); // Return success status
        }
        [HttpPost]
        public ActionResult Delete(int userId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string query = "DELETE FROM User_1460 WHERE nUserId = @UserId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                connection.Open();
                command.ExecuteNonQuery();
            }

            return Json(new { success = true });
        }
        //Fetch data from the database
        public ActionResult Product()
        {
            List<ProductViewModel> products = new List<ProductViewModel>();
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
       SELECT 
    p.nProductId AS ProductId, 
    p.sProduct_title AS ProductTitle, 
    p.sCategory AS Category, 
    p.nPrice AS Price, 
    COALESCE(i.nStock, 0) AS Stock, 
    COALESCE(avg_reviews.AverageRating, 0) AS AverageRating, 
    COALESCE(total_orders.TotalQuantitySold, 0) AS TotalQuantitySold,
    COALESCE(total_orders.TotalSalesInRupees, 0) AS TotalSalesInRupees,
    u.sFirst_name + ' ' + u.sLast_name AS SellerName
    ROM 
    Products_1460 p
    LEFT JOIN 
    Inventory_1460 i ON p.nProductId = i.nProductId
    LEFT JOIN 
    (SELECT 
         r.nProduct_id, 
         AVG(r.sRating) AS AverageRating 
     FROM reviews_1460 r 
     GROUP BY r.nProduct_id
    ) avg_reviews ON p.nProductId = avg_reviews.nProduct_id
    LEFT JOIN 
    (SELECT 
         o.nProductId, 
         SUM(o.nQuantity) AS TotalQuantitySold, 
         SUM(o.nQuantity * o.nOrderprice) AS TotalSalesInRupees 
     FROM Orders_1460 o 
     GROUP BY o.nProductId
    ) total_orders ON p.nProductId = total_orders.nProductId
    JOIN 
    User_1460 u ON p.nSellerId = u.nUserId  
    ORDER BY 
    TotalSalesInRupees DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new ProductViewModel
                            {
                                ProductId = Convert.ToInt32(reader["ProductId"]),
                                ProductTitle = reader["ProductTitle"].ToString(),
                                Category = reader["Category"].ToString(),
                                Price = Convert.ToInt32(reader["Price"]),
                                Stock = Convert.ToInt32(reader["Stock"]),
                                AverageRating = Convert.ToDecimal(reader["AverageRating"]),
                                TotalQuantitySold = Convert.ToInt32(reader["TotalQuantitySold"]),
                                TotalSalesInRupees = Convert.ToInt32(reader["TotalSalesInRupees"]),
                                SellerName = reader["SellerName"].ToString()
                            });
                        }
                    }
                }
            }

            // Fetch Top 5 Most Sold Products
            ViewBag.TopSoldProducts = GetTopSellingProducts();

            return View(products);
        }

        public List<ProductViewModel> GetTopSellingProducts()
        {
            List<ProductViewModel> topSoldProducts = new List<ProductViewModel>();
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = @"
            SELECT TOP 5 
                p.nProductId AS ProductId, 
                p.sProduct_title AS ProductTitle, 
                COALESCE(SUM(o.nQuantity), 0) AS TotalSales,
                u.sFirst_name + ' ' + u.sLast_name AS SellerName
            FROM Products_1460 p
            LEFT JOIN Orders_1460 o ON p.nProductId = o.nProductId
            JOIN User_1460 u ON p.nSellerId = u.nUserId
            GROUP BY p.nProductId, p.sProduct_title, u.sFirst_name, u.sLast_name
            ORDER BY TotalSales DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            topSoldProducts.Add(new ProductViewModel
                            {
                                ProductId = reader.GetInt32(0),
                                ProductTitle = reader.GetString(1),
                                TotalQuantitySold = reader.GetInt32(2),
                                SellerName = reader.GetString(3)

                            });
                        }
                    }
                }
            }

            return topSoldProducts;
        }


        [HttpPost]
        public ActionResult SaveProduct(ProductViewModel product)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            string updateProductQuery = @"
    UPDATE Products_1460 
    SET sProductTitle = @ProductTitle, 
        sCategory = @Category, 
        fPrice = @Price 
    WHERE nProductId = @ProductId";

            string updateInventoryQuery = @"
    UPDATE Inventory_1460 
    SET nStock = @Stock 
    WHERE nProductId = @ProductId";

            // ✅ Get current stock before updating
            string getCurrentStockQuery = @"
    SELECT nStock FROM Inventory_1460 WHERE nProductId = @ProductId";

            // ✅ Get delivered quantity
            string getDeliveredQuantityQuery = @"
    SELECT COALESCE(SUM(nQuantity), 0) 
    FROM Orders_1460 
    WHERE nProductId = @ProductId AND sStatus = 'Completed'";

            // ✅ Adjust Reserved Stock & Stock when Order is Delivered
            string updateReservedAndStockQuery = @"
    UPDATE Inventory_1460 
    SET 
        nReservedStock = CASE 
                            WHEN nReservedStock >= @DeliveredQuantity THEN nReservedStock - @DeliveredQuantity 
                            ELSE 0 
                         END,
        nStock = CASE 
                    WHEN nStock >= @DeliveredQuantity THEN nStock - @DeliveredQuantity 
                    ELSE 0 
                 END
    WHERE nProductId = @ProductId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    // ✅ Update Product Details
                    SqlCommand productCommand = new SqlCommand(updateProductQuery, connection, transaction);
                    productCommand.Parameters.AddWithValue("@ProductTitle", product.ProductTitle);
                    productCommand.Parameters.AddWithValue("@Category", product.Category);
                    productCommand.Parameters.AddWithValue("@Price", product.Price);
                    productCommand.Parameters.AddWithValue("@ProductId", product.ProductId);
                    productCommand.ExecuteNonQuery();

                    // ✅ Update Stock
                    SqlCommand inventoryCommand = new SqlCommand(updateInventoryQuery, connection, transaction);
                    inventoryCommand.Parameters.AddWithValue("@Stock", product.Stock);
                    inventoryCommand.Parameters.AddWithValue("@ProductId", product.ProductId);
                    inventoryCommand.ExecuteNonQuery();

                    // ✅ Fetch the current stock
                    SqlCommand getCurrentStockCommand = new SqlCommand(getCurrentStockQuery, connection, transaction);
                    getCurrentStockCommand.Parameters.AddWithValue("@ProductId", product.ProductId);
                    int currentStock = Convert.ToInt32(getCurrentStockCommand.ExecuteScalar());

                    // ✅ Get Delivered Quantity
                    SqlCommand getDeliveredCommand = new SqlCommand(getDeliveredQuantityQuery, connection, transaction);
                    getDeliveredCommand.Parameters.AddWithValue("@ProductId", product.ProductId);
                    int deliveredQuantity = Convert.ToInt32(getDeliveredCommand.ExecuteScalar());

                    // Debugging Logs
                    Console.WriteLine($"Current Stock: {currentStock}");
                    Console.WriteLine($"Delivered Quantity: {deliveredQuantity}");

                    if (deliveredQuantity > 0)
                    {
                        // ✅ Adjust Reserved Stock and Stock when Order is Delivered
                        SqlCommand updateReservedStockCommand = new SqlCommand(updateReservedAndStockQuery, connection, transaction);
                        updateReservedStockCommand.Parameters.AddWithValue("@DeliveredQuantity", deliveredQuantity);
                        updateReservedStockCommand.Parameters.AddWithValue("@ProductId", product.ProductId);
                        updateReservedStockCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine("Error: " + ex.Message);
                    return Json(new { success = false });
                }
            }
        }




        [HttpPost]
        public ActionResult DeleteProduct(int productId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string query = "DELETE FROM Products_1460 WHERE nProductId = @ProductId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ProductId", productId);

                connection.Open();
                command.ExecuteNonQuery();
            }

            return Json(new { success = true });
        }
        // GET: UserActivity
        public ActionResult UserActivity()
        {
            List<UserLoginActivity> usersLoggedInToday = new List<UserLoginActivity>();
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            // SQL Query to get details of users who logged in today
            string query = @"
                SELECT al.nuser_id, u.sUserName, 
           MAX(CONVERT(DATETIME, al.dcreated_at, 120)) AS dcreated_at,
           (SELECT TOP 1 al2.sactivity_description 
            FROM ActivityLog al2 
            WHERE al2.nuser_id = al.nuser_id 
            AND CONVERT(DATE, al2.dcreated_at, 120) = CONVERT(DATE, GETDATE())
            ORDER BY CONVERT(DATETIME, al2.dcreated_at, 120) DESC) AS sactivity_description
            FROM ActivityLog al
            JOIN User_1460 u ON al.nuser_id = u.nUserId
            WHERE CONVERT(DATE, al.dcreated_at, 120) = CONVERT(DATE, GETDATE())
            GROUP BY al.nuser_id, u.sUserName;";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);

                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        // Populate the list with user details
                        usersLoggedInToday.Add(new UserLoginActivity
                        {
                            UserId = reader.GetInt32(reader.GetOrdinal("nuser_id")),
                            UserName = reader.GetString(reader.GetOrdinal("sUserName")),
                            LoginTime = reader.GetDateTime(reader.GetOrdinal("dcreated_at")),
                            ActivityDescription = reader.GetString(reader.GetOrdinal("sactivity_description"))
                        });
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    // Handle exception (logging or rethrowing)
                    Console.WriteLine(ex.Message);
                }
            }

            // Return the "UserActivity" view instead of the default "Index" view
            return View("UserActivity", usersLoggedInToday);
        }
    }

}

