using Ecommerce.Models.ViewModels;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using System;

public class OrderController : Controller
{
    private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

    // Checkout action
    public ActionResult Checkout()
    {
        // Ensure user is logged in
        int userId = 0;
        if (Session["UserId"] == null || !int.TryParse(Session["UserId"].ToString(), out userId))
        {
            return RedirectToAction("Login");
        }

        // Fetch cart items
        List<OrderItem> cartItems = new List<OrderItem>();

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            string cartQuery = @"
        SELECT p.nProductId, p.sProduct_title, p.nPrice, c.nQuantity
        FROM Shopping_cart_1460 c
        JOIN Products_1460 p ON c.nProductId = p.nProductId
        WHERE c.nUserId = @UserId";

            using (SqlCommand command = new SqlCommand(cartQuery, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cartItems.Add(new OrderItem
                        {
                            ProductId = reader.GetInt32(0),
                            ProductName = reader.GetString(1),
                            Price = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                            Quantity = reader.IsDBNull(3) ? 0 : reader.GetInt32(3)
                        });
                    }
                }
            }
        }

        // If the cart is empty, redirect to home
        if (cartItems.Count == 0)
        {
            return RedirectToAction("Index", "Home");
        }

        // Store cart in session ✅
        Session["Cart"] = cartItems;

        // Get the user's shipping address
        string userAddress = GetUserAddress(userId);

        var model = new CheckoutViewModel
        {
            OrderSummary = cartItems,
            ShippingAddress = userAddress ?? "No address found"
        };

        return View(model);
    }



    // Retrieve user address
    private string GetUserAddress(int userId)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string query = "SELECT sAddress FROM User_1460 WHERE nUserId = @UserId";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader["sAddress"].ToString();
                    }
                }
            }
        }

        return "No address found";
    }

    [HttpPost]
    public ActionResult OrderSuccess(CheckoutViewModel model)
    {
        if (model == null || string.IsNullOrEmpty(model.ShippingAddress) || string.IsNullOrEmpty(model.PaymentMethod))
        {
            ModelState.AddModelError("", "Please provide all required fields.");
            return RedirectToAction("Checkout");
        }

        int userId = 0;
        if (Session["UserId"] == null || !int.TryParse(Session["UserId"].ToString(), out userId))
        {
            return RedirectToAction("Login");
        }

        // ✅ Fetch cart from session
        var cartItems = Session["Cart"] as List<OrderItem>;
        if (cartItems == null || cartItems.Count == 0)
        {
            TempData["ErrorMessage"] = "Your cart is empty!";
            return RedirectToAction("Checkout");
        }

        foreach (var item in cartItems)
        {
            SaveOrderToDatabase(userId, item, model.ShippingAddress, model.PaymentMethod);
        }

        // ✅ Clear cart after order is placed
        Session["Cart"] = null;
        ClearUserCart(userId);

        return RedirectToAction("OrderSuccess");
    }


    private void ClearUserCart(int userId)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            string query = "DELETE FROM Shopping_cart_1460 WHERE nUserId = @UserId";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }

    private void SaveOrderToDatabase(int userId, OrderItem item, string shippingAddress, string paymentMethod)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    // ✅ Get the seller's delivery days
                    var deliveryDaysCommand = new SqlCommand(@"
                SELECT nDelivery_days FROM user_1460 
                WHERE nuserid = (SELECT nSellerId FROM Products_1460 WHERE nProductId = @nProductId)",
                    connection, transaction);

                    deliveryDaysCommand.Parameters.AddWithValue("@nProductId", item.ProductId);
                    int deliveryDays = Convert.ToInt32(deliveryDaysCommand.ExecuteScalar() ?? 0);

                    // ✅ Calculate Order Date & Delivery Date
                    DateTime orderDate = DateTime.Now;
                    DateTime deliveryDate = orderDate.AddDays(deliveryDays);

                    // ✅ Insert new order into Orders_1460
                    var orderCommand = new SqlCommand(@"
                INSERT INTO Orders_1460 
                (nUserId, nProductId, dOrderDate, nQuantity, nOrderprice, sStatus, sShipping_address, sPayment_method, sStatus_description, dDeliveryDate) 
                VALUES 
                (@nUserId, @nProductId, @dOrderDate, @nQuantity, @nOrderprice, @sStatus, @sShipping_address, @sPayment_method, @sStatus_description, @dDeliveryDate)",
                        connection, transaction);

                    orderCommand.Parameters.AddWithValue("@nUserId", userId);
                    orderCommand.Parameters.AddWithValue("@nProductId", item.ProductId);
                    orderCommand.Parameters.AddWithValue("@dOrderDate", orderDate);
                    orderCommand.Parameters.AddWithValue("@nQuantity", item.Quantity);
                    orderCommand.Parameters.AddWithValue("@nOrderprice", item.Price);
                    orderCommand.Parameters.AddWithValue("@sStatus", "Pending");
                    orderCommand.Parameters.AddWithValue("@sShipping_address", shippingAddress);
                    orderCommand.Parameters.AddWithValue("@sPayment_method", paymentMethod);
                    orderCommand.Parameters.AddWithValue("@sStatus_description", "Order is being processed");
                    orderCommand.Parameters.AddWithValue("@dDeliveryDate", deliveryDate);

                    orderCommand.ExecuteNonQuery();

                    // ✅ Update Inventory: Increase Reserved Stock
                    var inventoryCommand = new SqlCommand(@"
                UPDATE Inventory_1460 
                SET nReservedStock = nReservedStock + @nQuantity 
                WHERE nProductId = @nProductId", connection, transaction);

                    inventoryCommand.Parameters.AddWithValue("@nProductId", item.ProductId);
                    inventoryCommand.Parameters.AddWithValue("@nQuantity", item.Quantity);

                    inventoryCommand.ExecuteNonQuery();

                    // ✅ Commit Transaction
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("Order placement failed: " + ex.Message);
                }
            }
        }
    }
    public ActionResult OrderSuccess()
    {
        ViewBag.Message = "Your order has been placed successfully!";
        return View();
    }
}
