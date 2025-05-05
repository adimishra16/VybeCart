using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Ecommerce.Models.ViewModels;

namespace Ecommerce.Controllers
{
    public class ProductController : Controller
    {
        // Database connection string
        private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ActionResult Index()
        {
            return View();
        }

        // GET: Product/ProductDetails/{productId}
        public ActionResult ProductDetails(int? productId) // Make productId nullable to avoid error
        {
            // Check if the user is logged in (i.e., session is not empty)
            if (Session["UserId"] == null)
            {
                // Redirect to Login page if user is not logged in
                return RedirectToAction("Login", "Account");
            }

            // Ensure the productId is passed
            if (!productId.HasValue)
            {
                // Handle the case where the productId is not provided or is invalid
                return RedirectToAction("Index", "Home"); // Redirect to home page or other appropriate action
            }

            // Fetch product details using productId
            var product = GetProductById(productId.Value);
            if (product == null)
            {
                return HttpNotFound();
            }

            // Fetch reviews including the reviewer's username and calculate average rating
            List<ReviewViewModel> reviews = new List<ReviewViewModel>();
            decimal averageRating = 0; // To store average rating

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT r.nReviewsId, r.nProduct_id, r.sRating, r.sReview_text, r.dCreated_at, 
                   u.sUsername  -- Fetch username from User_1460 table
            FROM reviews_1460 r
            INNER JOIN User_1460 u ON r.nUserId = u.nUserId  -- Join with User table
            WHERE r.nProduct_id = @ProductId
            ORDER BY r.dCreated_at DESC";  // Order by latest reviews first

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", productId.Value);

                    using (var reader = command.ExecuteReader())
                    {
                        int reviewCount = 0;
                        decimal totalRating = 0;

                        while (reader.Read())
                        {
                            var review = new ReviewViewModel
                            {
                                ReviewId = Convert.ToInt32(reader["nReviewsId"]),
                                ProductId = Convert.ToInt32(reader["nProduct_id"]),
                                Rating = reader["sRating"].ToString(),
                                ReviewText = reader["sReview_text"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["dCreated_at"]),
                                Username = reader["sUsername"].ToString() // Get reviewer's username
                            };

                            reviews.Add(review);

                            // Accumulate rating for average calculation
                            totalRating += Convert.ToDecimal(review.Rating);
                            reviewCount++;
                        }

                        // Calculate the average rating if reviews exist
                        if (reviewCount > 0)
                        {
                            averageRating = totalRating / reviewCount;
                        }
                    }
                }
            }

            // Create the view model and pass it to the view
            var productViewModel = new ProductViewModel
            {
                ProductId = product.ProductId,
                ProductTitle = product.ProductTitle,
                ProductImagePath = product.ProductImagePath,
                Category = product.Category,
                Description = product.Description,
                Price = product.Price,
                Reviews = reviews,
                AverageRating = averageRating // Pass the average rating to the view
            };

            return View(productViewModel);
        }
        // Method to fetch product details by ID
        private ProductViewModel GetProductById(int id)
        {
            ProductViewModel product = null;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT nProductId, sCategory, sProduct_Title, sProduct_Image, nPrice, sDescription, bActive FROM Products_1460 WHERE nProductId = @ProductId";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProductId", id);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        product = new ProductViewModel
                        {
                            ProductId = reader.GetInt32(0),
                            Category = reader.GetString(1),
                            ProductTitle = reader.GetString(2),
                            ProductImagePath = reader.GetString(3),
                            Price = reader.GetInt32(4),
                            Description = reader.GetString(5),
                            IsActive = reader.GetBoolean(6),
                        };
                    }
                }
            }

            return product;
        }

        [HttpPost]
        public ActionResult AddReview(int productId, int rating, string reviewText)
        {
            try
            {
                // Ensure the user is logged in
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                int userId = Convert.ToInt32(Session["UserId"]);

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO reviews_1460 (nUserId, sRating, nProduct_id, sReview_text, dCreated_at) " +
                                   "VALUES (@UserId, @Rating, @ProductId, @ReviewText, @CreatedAt)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Rating", rating);
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    cmd.Parameters.AddWithValue("@ReviewText", reviewText);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                // Fetch updated product details (including new reviews)
                return RedirectToAction("ProductDetails", new { productId = productId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while submitting your review.";
                return RedirectToAction("ProductDetails", new { productId = productId });
            }
        }
        public ActionResult Category(string category)
        {
            // Ensure the user is logged in
            if (Session["FirstName"] == null || Session["LastName"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            List<string> allowedCategories = new List<string>();

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            // Fetch categories dynamically from the database
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string categoryQuery = "SELECT scategory_name FROM Categories";
                SqlCommand categoryCommand = new SqlCommand(categoryQuery, connection);

                connection.Open();

                using (SqlDataReader reader = categoryCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        allowedCategories.Add(reader.GetString(0));
                    }
                }
            }

            // If the category provided is not in the allowed list, redirect to a default category or show an error page
            if (!string.IsNullOrEmpty(category) && !allowedCategories.Contains(category))
            {
                return RedirectToAction("Index", "Home"); // Redirecting to home if category is invalid
            }

            List<ProductViewModel> products = new List<ProductViewModel>();

            string productQuery = @"
        SELECT p.nProductid, p.sProduct_title, p.sCategory, p.nPrice, p.sProduct_image, AVG(r.sRating) AS AverageRating
        FROM Products_1460 p
        LEFT JOIN reviews_1460 r ON p.nProductid = r.nProduct_id";

            if (!string.IsNullOrEmpty(category))
            {
                productQuery += " WHERE p.sCategory = @category";
            }

            productQuery += " GROUP BY p.nProductid, p.sProduct_title, p.sCategory, p.nPrice, p.sProduct_image";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(productQuery, connection);

                if (!string.IsNullOrEmpty(category))
                {
                    command.Parameters.AddWithValue("@category", category);
                }

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
                            AverageRating = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5)
                        });
                    }
                }
            }

            return View(products);
        }

        public ActionResult GetCartCount()
        {
            int cartCount = 0;
            if (Session["UserId"] == null)
            {
                return Json(cartCount, JsonRequestBehavior.AllowGet); // Return 0 if user is not logged in
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT SUM(nQuantity) FROM Shopping_cart_1460 WHERE nUserId = @UserId";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    con.Open();
                    cartCount = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            return Json(cartCount, JsonRequestBehavior.AllowGet);
        }
        public JsonResult FetchCategories()
        {
            List<Category> categories = new List<Category>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT DISTINCT scategoryid,scategory_name FROM Categories";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    categories.Add(new Category
                    {
                        Id = Convert.ToInt32(reader["scategoryid"]),
                        Name = reader["scategory_name"].ToString()
                    });
                }
            }

            return Json(categories, JsonRequestBehavior.AllowGet);
        }
    }

}
