using Ecommerce.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.Mvc;

public class SellerController : Controller
{
    private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

    // Seller Dashboard - Fetch Orders and Products
    public ActionResult Dashboard()
    {
        if (Session["UserId"] == null)
        {
            return RedirectToAction("Login", "Account"); // Redirect if user is not logged in
        }

        int sellerId = (int)Session["UserId"]; // Get logged-in seller's ID
        var productList = new List<ProductViewModel>();

        string firstName = "";
        string lastName = "";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            // Fetch first and last name of the seller
            string userQuery = "SELECT sFirst_Name, sLast_Name FROM User_1460 WHERE nUserId = @SellerId";
            SqlCommand userCommand = new SqlCommand(userQuery, connection);
            userCommand.Parameters.AddWithValue("@SellerId", sellerId);

            SqlDataReader userReader = userCommand.ExecuteReader();
            if (userReader.Read())
            {
                firstName = userReader.GetString(0);
                lastName = userReader.GetString(1);
            }
            userReader.Close();  // Close the DataReader after use

            // Fetch products for the seller
            string productQuery = @"
            SELECT p.nProductId, p.sCategory, p.sProduct_title, p.sProduct_image, p.nPrice, p.sDescription, i.nStock
            FROM Products_1460 p
            INNER JOIN Inventory_1460 i ON p.nProductId = i.nProductId
            WHERE p.nSellerId = @SellerId AND p.bActive = 1";

            SqlCommand productCommand = new SqlCommand(productQuery, connection);
            productCommand.Parameters.AddWithValue("@SellerId", sellerId);

            SqlDataReader productReader = productCommand.ExecuteReader();
            while (productReader.Read())
            {
                productList.Add(new ProductViewModel
                {
                    ProductId = productReader.GetInt32(0),
                    Category = productReader.GetString(1),
                    ProductTitle = productReader.GetString(2),
                    ProductImagePath = productReader.GetString(3),
                    Price = productReader.GetInt32(4),
                    Description = productReader.GetString(5),
                    Stock = productReader.GetInt32(6)
                });
            }
            productReader.Close();  // Close the DataReader after use
        }

        ViewBag.Products = productList;
        ViewBag.FirstName = firstName;
        ViewBag.LastName = lastName;

        return View(productList);
    }

    // Seller Inventory - Add Product Form
    public ActionResult Inventory()
    {
        if (Session["UserId"] == null)
        {
            return RedirectToAction("Login", "Account");
        }

        int sellerId = (int)Session["UserId"]; // Get logged-in seller's ID
        var productList = new List<ProductViewModel>();

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            string query = @"
            SELECT p.nProductId, p.sCategory, p.sProduct_title, p.sProduct_image, p.nPrice, p.sDescription, i.nStock
            FROM Products_1460 p
            INNER JOIN Inventory_1460 i ON p.nProductId = i.nProductId
            WHERE p.nSellerId = @SellerId";

            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SellerId", sellerId);

            connection.Open();
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                productList.Add(new ProductViewModel
                {
                    ProductId = reader.GetInt32(0),
                    Category = reader.GetString(1),
                    ProductTitle = reader.GetString(2),
                    ProductImagePath = reader.GetString(3),
                    Price = reader.GetInt32(4),
                    Description = reader.GetString(5),
                    Stock = reader.GetInt32(6)  // Fetching the stock from Inventory_1460 table
                });
            }
            reader.Close();  // Close the DataReader after use
        }

        ViewBag.Products = productList;
        return View(productList); // Ensure this is returning the correct list of products
    }
    public ActionResult ManageOrders()
    {
        if (Session["UserId"] == null)
        {
             
        }
        return View();
    }

    public ActionResult AddProduct()
    {
        return View();
    }

    [HttpPost]
    // Action for handling product addition
    [ValidateAntiForgeryToken]
    public ActionResult AddProduct(ProductViewModel product, HttpPostedFileBase ProductImage, HttpPostedFileBase CategoryIcon)
    {
        if (ModelState.IsValid)
        {
            int sellerId = (int)Session["UserId"]; // Get seller ID from session
            string sellerName = ""; // Variable to store seller name

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Fetch seller name using sellerId
                string getSellerNameQuery = "SELECT sUsername FROM user_1460 WHERE nUserId = @SellerId";
                SqlCommand getSellerNameCmd = new SqlCommand(getSellerNameQuery, conn);
                getSellerNameCmd.Parameters.AddWithValue("@SellerId", sellerId);

                object result = getSellerNameCmd.ExecuteScalar();
                if (result != null)
                {
                    sellerName = result.ToString();
                }
                else
                {
                    ModelState.AddModelError("", "Seller not found.");
                    return View(product);
                }
            }

            // Handle 'OtherCategory' if "Others" is selected
            string category = product.Category == "Others" && !string.IsNullOrEmpty(product.OtherCategory)
                ? product.OtherCategory
                : product.Category;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Check if category exists
                string checkCategoryQuery = "SELECT COUNT(*) FROM categories WHERE scategory_name = @CategoryName";
                SqlCommand checkCategoryCmd = new SqlCommand(checkCategoryQuery, conn);
                checkCategoryCmd.Parameters.AddWithValue("@CategoryName", category);

                int categoryExists = (int)checkCategoryCmd.ExecuteScalar();
                string categoryIconPath = null;

                if (categoryExists == 0) // If category does not exist, insert it
                {
                    categoryIconPath = SaveProductIcon(CategoryIcon, category); // Save category icon

                    string insertCategoryQuery = @"
                INSERT INTO categories (scategory_name, saddedby, dcreated_at, sIcon) 
                VALUES (@CategoryName, @AddedBy, GETDATE(), @IconPath)";

                    SqlCommand insertCategoryCmd = new SqlCommand(insertCategoryQuery, conn);
                    insertCategoryCmd.Parameters.AddWithValue("@CategoryName", category);
                    insertCategoryCmd.Parameters.AddWithValue("@AddedBy", sellerName); // Insert Seller Name instead of ID
                    insertCategoryCmd.Parameters.AddWithValue("@IconPath", (object)categoryIconPath ?? DBNull.Value);

                    insertCategoryCmd.ExecuteNonQuery();
                }
            }

            // Save the product image and get the file path
            string imagePath = SaveProductIcon(ProductImage, product.ProductTitle);

            // Insert product details into Products_1460 table
            string insertProductQuery = @"
        INSERT INTO Products_1460 (nSellerId, sCategory, sProduct_title, sProduct_image, nPrice, sDescription) 
        VALUES (@SellerId, @Category, @ProductTitle, @ProductImage, @Price, @Description)";

            int newProductId = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(insertProductQuery, conn);
                cmd.Parameters.AddWithValue("@SellerId", sellerId);
                cmd.Parameters.AddWithValue("@Category", category);
                cmd.Parameters.AddWithValue("@ProductTitle", product.ProductTitle);
                cmd.Parameters.AddWithValue("@ProductImage", imagePath);
                cmd.Parameters.AddWithValue("@Price", product.Price);
                cmd.Parameters.AddWithValue("@Description", product.Description);

                conn.Open();
                cmd.ExecuteNonQuery();

                // Get the newly inserted product ID
                string getProductIdQuery = "SELECT TOP 1 nProductId FROM Products_1460 WHERE nSellerId = @SellerId ORDER BY nProductId DESC";
                SqlCommand getProductIdCmd = new SqlCommand(getProductIdQuery, conn);
                getProductIdCmd.Parameters.AddWithValue("@SellerId", sellerId);

                newProductId = (int)getProductIdCmd.ExecuteScalar();
            }

            // Insert stock into Inventory_1460 table
            string insertInventoryQuery = "INSERT INTO Inventory_1460 (nProductId, nStock) VALUES (@ProductId, @Stock)";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(insertInventoryQuery, conn);
                cmd.Parameters.AddWithValue("@ProductId", newProductId);
                cmd.Parameters.AddWithValue("@Stock", product.Stock);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Dashboard");
        }

        return View(product);
    }


    private string SaveProductIcon(HttpPostedFileBase file, string categoryName)
    {
        if (file == null || file.ContentLength == 0)
        {
            return null; // No file uploaded
        }

        string extension = Path.GetExtension(file.FileName).ToLower();
        string validExtensions = ".png,.jpg,.jpeg,.gif";

        if (!validExtensions.Contains(extension))
        {
            return null; // Invalid file type
        }

        string folderPath = HttpContext.Server.MapPath("~/Images/Icons/Category_icon/");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName = categoryName.Replace(" ", "_") + extension; // Ensure a unique filename
        string filePath = Path.Combine(folderPath, fileName);

        file.SaveAs(filePath); // Save the uploaded file

        return "/Images/Icons/Category_icon/" + fileName; // Return the saved file path
    }


    private string SaveProductImage(HttpPostedFileBase productImage)
    {
        string filePath = "/Images/Products/" + Guid.NewGuid().ToString() + System.IO.Path.GetExtension(productImage.FileName);
        string serverPath = Server.MapPath("~" + filePath);
        productImage.SaveAs(serverPath);
        return filePath;
    }
    [HttpPost]
    public ActionResult Save(ProductViewModel product)
    {
        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        string query = @"UPDATE Products_1460 
                             SET sProduct_title = @ProductTitle, 
                                 sCategory = @Category, 
                                 nPrice = @Price 
                             WHERE nProductId = @ProductId;

                             UPDATE Inventory_1460 
                             SET nStock = @Stock 
                             WHERE nProductId = @ProductId";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProductTitle", product.ProductTitle);
            command.Parameters.AddWithValue("@Category", product.Category);
            command.Parameters.AddWithValue("@Price", product.Price);
            command.Parameters.AddWithValue("@Stock", product.Stock);
            command.Parameters.AddWithValue("@ProductId", product.ProductId);

            connection.Open();
            command.ExecuteNonQuery();
        }

        return Json(new { success = true });
    }
    [HttpPost]
    public ActionResult Delete(int productId)
    {
        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            // Delete from Inventory_1460
            string deleteInventoryQuery = "DELETE FROM Inventory_1460 WHERE nProductId = @ProductId";
            SqlCommand deleteInventoryCommand = new SqlCommand(deleteInventoryQuery, connection);
            deleteInventoryCommand.Parameters.AddWithValue("@ProductId", productId);
            connection.Open();
            deleteInventoryCommand.ExecuteNonQuery();

            // Delete from Products_1460
            string deleteProductQuery = "DELETE FROM Products_1460 WHERE nProductId = @ProductId";
            SqlCommand deleteProductCommand = new SqlCommand(deleteProductQuery, connection);
            deleteProductCommand.Parameters.AddWithValue("@ProductId", productId);
            deleteProductCommand.ExecuteNonQuery();
        }

        return Json(new { success = true });
    }
    // POST: Seller/AddStock
    [HttpPost]
    public JsonResult AddStock(int productId, int additionalStock)
    {
        try
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "UPDATE Inventory_1460 SET nStock = nStock + @Stock WHERE nProductId = @ProductId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Stock", additionalStock);
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true, message = "Stock updated successfully!" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Stock update failed." });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    [HttpPost]
    public JsonResult RemoveStock(int productId, int removeStock)
    {
        try
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // Ensure stock does not go below zero
                string checkStockQuery = "SELECT nStock FROM Inventory_1460 WHERE nProductId = @ProductId";
                int currentStock = 0;

                using (SqlCommand checkCmd = new SqlCommand(checkStockQuery, con))
                {
                    checkCmd.Parameters.AddWithValue("@ProductId", productId);
                    object result = checkCmd.ExecuteScalar();
                    currentStock = result != null ? Convert.ToInt32(result) : 0;
                }

                if (currentStock < removeStock)
                {
                    return Json(new { success = false, message = "Not enough stock available to remove!" });
                }

                string updateQuery = "UPDATE Inventory_1460 SET nStock = nStock - @Stock WHERE nProductId = @ProductId";

                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@Stock", removeStock);
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true, message = "Stock removed successfully!" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Stock removal failed." });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    // POST: Seller/SaveProduct
    [HttpPost]
    public JsonResult SaveProduct(ProductViewModel updatedProduct)
    {
        bool success = false;
        string message = "Failed to update product.";

        // Convert IsActive to 1 (active) or 0 (inactive)
        int isActive = updatedProduct.IsActive ? 1 : 0;

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();

            // Update product details
            string query = "UPDATE Products_1460 SET sProduct_Title = @ProductTitle, sDescription = @Description, nPrice = @Price WHERE nProductId = @ProductId";
            SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@ProductTitle", updatedProduct.ProductTitle);
            cmd.Parameters.AddWithValue("@Description", updatedProduct.Description);
            cmd.Parameters.AddWithValue("@Price", updatedProduct.Price);
            cmd.Parameters.AddWithValue("@ProductId", updatedProduct.ProductId);

            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                // Update IsActive status in Products_1460 table
                string updateStatusQuery = "UPDATE Products_1460 SET bActive = @IsActive WHERE nProductId = @ProductId";
                SqlCommand statusCmd = new SqlCommand(updateStatusQuery, conn);
                statusCmd.Parameters.AddWithValue("@IsActive", isActive); // Set the value as 1 or 0
                statusCmd.Parameters.AddWithValue("@ProductId", updatedProduct.ProductId);

                statusCmd.ExecuteNonQuery();

                success = true;
                message = "Product updated successfully.";
            }
        }

        return Json(new { success = success, message = message });
    }

    // POST: Seller/DeleteProduct
    [HttpPost]
    public JsonResult DeleteProduct(int productId)
    {
        bool success = false;
        string message = "Failed to deactivate product.";

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();

            // Update the product's IsActive status to false (inactive) instead of deleting it
            string query = "UPDATE Products_1460 SET bActive = 0 WHERE nProductId = @ProductId";
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ProductId", productId);

            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                // Also update IsActive in the Inventory_1460 table
                string updateInventoryQuery = "UPDATE Inventory_1460 SET bActive = 0 WHERE nProductId = @ProductId";
                SqlCommand inventoryCmd = new SqlCommand(updateInventoryQuery, conn);
                inventoryCmd.Parameters.AddWithValue("@ProductId", productId);

                inventoryCmd.ExecuteNonQuery();

                success = true;
                message = "Product deactivated successfully.";
            }
        }

        return Json(new { success = success, message = message });
    }
    public ActionResult ManageOrder()
    {
        if (Session["FirstName"] == null || Session["LastName"] == null) 
        {
            return RedirectToAction("Login", "Account");
        }
        return View();
    }

    public JsonResult GetSellerOrders()
    {
        int sellerId = Convert.ToInt32(Session["UserId"]); // Get seller ID from session
        List<SellerOrderViewModel> orders = new List<SellerOrderViewModel>();

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            string query = @"
        SELECT u.sFirst_name, u.sLast_name, 
               o.nOrder_id, o.nUserId, o.sStatus, o.sStatus_description, 
               o.sShipping_address, o.sPayment_method, o.ddeliveryDate, 
               o.dOrderDate, o.nQuantity, o.nOrderprice, 
               p.nProductId, p.sProduct_title
        FROM Orders_1460 o
        INNER JOIN Products_1460 p ON o.nProductId = p.nProductId
        INNER JOIN User_1460 u ON o.nUserId = u.nUserId
        WHERE p.nSellerId = @sellerId";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@sellerId", sellerId);

            conn.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                orders.Add(new SellerOrderViewModel
                {
                    OrderId = Convert.ToInt32(reader["nOrder_id"]),
                    UserId = Convert.ToInt32(reader["nUserId"]),
                    OrderStatus = reader["sStatus"].ToString(),
                    StatusDescription = reader["sStatus_description"].ToString(),
                    ProductId = Convert.ToInt32(reader["nProductId"]),
                    ProductName = reader["sProduct_title"].ToString(),
                    Quantity = Convert.ToInt32(reader["nQuantity"]),
                    Price = Convert.ToDecimal(reader["nOrderprice"]),
                    OrderDate = Convert.ToDateTime(reader["dOrderDate"]).ToString("dd-MM-yyyy"), // Proper formatting
                    DeliveryDate = reader["ddeliveryDate"] != DBNull.Value ? Convert.ToDateTime(reader["ddeliveryDate"]).ToString("dd-MM-yyyy"): null,
                    ShippingAddress = reader["sShipping_address"].ToString(),
                    PaymentMethod = reader["sPayment_method"].ToString(),
                    CustomerName = reader["sFirst_name"].ToString() + " " + reader["sLast_name"].ToString()
                });
            }
            conn.Close();
        }
        return Json(orders, JsonRequestBehavior.AllowGet);
    }



    [HttpPost]
    public JsonResult UpdateOrderStatus(int orderId, string status, string description)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            string query = "UPDATE Orders_1460 SET sStatus = @Status, sStatus_description = @Description WHERE nOrder_id = @OrderId";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Description", description);
            cmd.Parameters.AddWithValue("@OrderId", orderId);

            conn.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            conn.Close();

            return Json(new { success = rowsAffected > 0 });
        }
    }
}

