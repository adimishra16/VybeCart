using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web.Mvc;
using Ecommerce.Models.ViewModels;
//using ECommerceMVC.Models.ViewModels;

public class AccountController : Controller
{
    /private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

    // Login Action
    [HttpGet]
    public ActionResult Login()
    {
        return View();
    }

    public ActionResult Login(LoginViewModel model)
    {
        Debug.WriteLine("This message will appear in the Visual Studio Debug window.");

        if (ModelState.IsValid)
        {
            // Query to get role, username, password, and activity status from the database
            string query = @"SELECT nUserId, sFirst_Name, sLast_Name, sRole, sPassword, bActivity_status 
                       FROM User_1460 
                       WHERE sUsername = @Username";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", model.Username);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    string dbRole = reader["sRole"].ToString();
                    string dbPassword = reader["sPassword"].ToString();
                    string firstName = reader["sFirst_Name"].ToString();
                    string lastName = reader["sLast_Name"].ToString();
                    bool isActive = Convert.ToBoolean(reader["bActivity_status"]);

                    // Check if user is active
                    if (!isActive)
                    {
                        // User is inactive, display error
                        ModelState.AddModelError("", "User does not exist");
                        return View(model);
                    }

                    // Check if password matches
                    if (dbPassword == model.Password)
                    {
                        // Save user information to the session
                        int userId = Convert.ToInt32(reader["nUserId"]);
                        Session["UserId"] = userId;
                        Session["FirstName"] = firstName;
                        Session["LastName"] = lastName;
                        Session["Role"] = dbRole;

                        // Close the reader before executing the log query
                        reader.Close();

                        // Log the login activity into the ActivityLog table
                        string logQuery = @"INSERT INTO ActivityLog (sactivity_description, nuser_id, dcreated_at)
                                    VALUES (@ActivityDescription, @UserId, GETDATE())";

                        using (SqlCommand logCommand = new SqlCommand(logQuery, connection))
                        {
                            logCommand.Parameters.AddWithValue("@ActivityDescription", "Login");
                            logCommand.Parameters.AddWithValue("@UserId", userId);

                            logCommand.ExecuteNonQuery();
                        }

                        // Redirect based on role
                        if (dbRole == "Admin")
                        {
                            return RedirectToAction("Product", "Admin"); // Admin dashboard
                        }
                        else if (dbRole == "Seller")
                        {
                            return RedirectToAction("Dashboard", "Seller"); // Seller dashboard
                        }
                        else if (dbRole == "User")
                        {
                            return RedirectToAction("Index", "Home"); // User home page
                        }
                        else
                        {
                            ModelState.AddModelError("", "Role not recognized.");
                        }
                    }
                    else
                    {
                        // Password mismatch
                        ModelState.AddModelError("", "Invalid username or password.");
                    }
                }
                else
                {
                    // Invalid username
                    ModelState.AddModelError("", "Invalid username or password.");
                }
            }
        }

        return View(model); // Return the view with the model if validation fails
    }



    // Register Action
    [HttpGet]
    public ActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public ActionResult Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Default role
            string role = model.Role;

            // Hash the password (implement hashing later)
            string hashedPassword = model.Password;

            // SQL Query - Conditional Insert for Seller
            string query = role == "Seller"
                ? @"
                INSERT INTO User_1460 
                (sFirst_name, sLast_name, sUsername, sEmail, sPhone, sPassword, sRole, sSecurityAnswer, ndelivery_days, dCreated_at, dUpdated_at)
                VALUES 
                (@FirstName, @LastName, @Username, @Email, @Phone, @Password, @Role, @SecurityAnswer, @DeliveryDays, GETDATE(), GETDATE())"
                : @"
                INSERT INTO User_1460 
                (sFirst_name, sLast_name, sUsername, sEmail, sPhone, sPassword, sRole, sSecurityAnswer, dCreated_at, dUpdated_at)
                VALUES 
                (@FirstName, @LastName, @Username, @Email, @Phone, @Password, @Role, @SecurityAnswer, GETDATE(), GETDATE())";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FirstName", model.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", model.LastName);
                    cmd.Parameters.AddWithValue("@Username", model.Username);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Phone", model.Phone);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@Role", role);
                    cmd.Parameters.AddWithValue("@SecurityAnswer", model.SecurityAnswer);

                    // Only add ndelivery_days if the role is "Seller"
                    if (role == "Seller")
                    {
                        cmd.Parameters.AddWithValue("@DeliveryDays", model.DeliveryDays ?? (object)DBNull.Value);
                    }

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Login");
        }

        return View(model);
    }


    // Forgot Password Action
    [HttpGet]
    public ActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public ActionResult ForgotPassword(ForgotPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "UPDATE User_1460 SET sPassword = @NewPassword WHERE sUsername = @Username AND sSecurityAnswer = @SecurityAnswer";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", model.Username);
                cmd.Parameters.AddWithValue("@SecurityAnswer", model.SecurityAnswer);
                cmd.Parameters.AddWithValue("@NewPassword", model.NewPassword);

                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return RedirectToAction("Login");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid Username or security answer.");
                }
            }
        }
        return View(model);
    }
    // Show Wishlist
    public ActionResult Wishlist()
    {
        List<ProductViewModel> wishlistProducts = new List<ProductViewModel>();
        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        string query = "SELECT p.nProductid, p.sProduct_title, p.sCategory, p.nPrice, p.sDescription, p.sProduct_image FROM Wishlist_1460 w " +
                       "JOIN Products_1460 p ON w.nProductId = p.nProductid WHERE w.nUserId = @UserId AND w.Activity_status = 1";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", Session["UserId"]);

            connection.Open();
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    wishlistProducts.Add(new ProductViewModel
                    {
                        ProductId = reader.GetInt32(0),
                        ProductTitle = reader.GetString(1),
                        Category = reader.GetString(2),
                        Price = reader.GetInt32(3),
                        Description = reader.GetString(4),
                        ProductImagePath = reader.IsDBNull(5) ? "/images/default-product.jpg" : reader.GetString(5)
                    });
                }
            }
        }

        return View(wishlistProducts);
    }

    // Show Cart
    public ActionResult Cart()
    {
        List<ProductViewModel> cartProducts = new List<ProductViewModel>();
        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        string query = "SELECT p.nProductid, p.sProduct_title, p.sCategory, p.nPrice, p.sDescription, p.sProduct_image FROM Cart_1460 c " +
                       "JOIN Products_1460 p ON c.nProductId = p.nProductid WHERE c.nUserId = @UserId AND c.Activity_status = 1";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", Session["UserId"]);

            connection.Open();
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    cartProducts.Add(new ProductViewModel
                    {
                        ProductId = reader.GetInt32(0),
                        ProductTitle = reader.GetString(1),
                        Category = reader.GetString(2),
                        Price = reader.GetInt32(3),
                        Description = reader.GetString(4),
                        ProductImagePath = reader.IsDBNull(5) ? "/images/default-product.jpg" : reader.GetString(5)
                    });
                }
            }
        }

        return View(cartProducts);
    }

    // Add to Cart
    public ActionResult AddToCart(int id)
    {
        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        string query = "INSERT INTO Shopping_cart_1460 (nUserId, nProductId,nQuantity) VALUES (@UserId, @ProductId, 1)";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", Session["UserId"]);
            command.Parameters.AddWithValue("@ProductId", id);


            connection.Open();
            command.ExecuteNonQuery();
        }

        return RedirectToAction("Cart");
    }

    // Add to Wishlist
    public ActionResult AddToWishlist(int id)
    {
        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        string query = "INSERT INTO Wishlists_1460 (nUserId, nProductId, bActive) VALUES (@UserId, @ProductId, 1)";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", Session["UserId"]);
            command.Parameters.AddWithValue("@ProductId", id);

            connection.Open();
            command.ExecuteNonQuery();
        }

        return RedirectToAction("Wishlist");
    }

    // Remove from Wishlist
    public ActionResult RemoveFromWishlist(int id)
    {
        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        string query = "UPDATE Wishlists_1460 SET Activity_status = 0 WHERE nUserId = @UserId AND nProductId = @ProductId";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", Session["UserId"]);
            command.Parameters.AddWithValue("@ProductId", id);

            connection.Open();
            command.ExecuteNonQuery();
        }

        return RedirectToAction("Wishlist");
    }

    // Remove from Cart
    public ActionResult RemoveFromCart(int id)
    {
        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        string query = "UPDATE Shopping_cart_1460 SET Activity_status = 0 WHERE nUserId = @UserId AND nProductId = @ProductId";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", Session["UserId"]);
            command.Parameters.AddWithValue("@ProductId", id);

            connection.Open();
            command.ExecuteNonQuery();
        }

        return RedirectToAction("Cart");
    }
}
