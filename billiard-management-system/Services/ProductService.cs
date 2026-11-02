using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using billiard_management_system.Models;
using MySqlConnector;

namespace billiard_management_system.Services
{
    public interface IProductService
    {
        Task<List<ProductCategory>> GetCategoriesAsync();
        Task<List<Product>> GetProductsAsync(int? categoryId = null);
        Task<List<SessionProduct>> GetSessionProductsAsync(long sessionId);
        Task<(bool Success, string Message)> AddProductToSessionAsync(long sessionId, int productId, int quantity, decimal unitPrice);
        Task<(bool Success, string Message)> RemoveProductFromSessionAsync(long sessionProductId);
    }

    public class ProductService : IProductService
    {
        public async Task<List<ProductCategory>> GetCategoriesAsync()
        {
            var list = new List<ProductCategory>();
            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                const string sql = "SELECT CategoryId, CategoryName, Description, IsActive FROM ProductCategories WHERE IsActive = TRUE ORDER BY CategoryId;";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new ProductCategory
                        {
                            CategoryId = reader.GetInt32("CategoryId"),
                            CategoryName = reader.GetString("CategoryName"),
                            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                            IsActive = reader.GetBoolean("IsActive")
                        });
                    }
                }
            }
            return list;
        }

        public async Task<List<Product>> GetProductsAsync(int? categoryId = null)
        {
            var list = new List<Product>();
            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                string sql = @"
                    SELECT p.ProductId, p.CategoryId, p.ProductName, p.Unit, p.Price, p.ImageUrl, p.IsActive,
                           c.CategoryName
                    FROM Products p
                    INNER JOIN ProductCategories c ON p.CategoryId = c.CategoryId
                    WHERE p.IsActive = TRUE";

                if (categoryId.HasValue)
                {
                    sql += " AND p.CategoryId = @CategoryId";
                }
                sql += " ORDER BY c.CategoryId, p.ProductName;";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (categoryId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@CategoryId", categoryId.Value);
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new Product
                            {
                                ProductId = reader.GetInt32("ProductId"),
                                CategoryId = reader.GetInt32("CategoryId"),
                                ProductName = reader.GetString("ProductName"),
                                Unit = reader.GetString("Unit"),
                                Price = reader.GetDecimal("Price"),
                                ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString("ImageUrl"),
                                IsActive = reader.GetBoolean("IsActive"),
                                CategoryName = reader.GetString("CategoryName")
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<List<SessionProduct>> GetSessionProductsAsync(long sessionId)
        {
            var list = new List<SessionProduct>();
            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                const string sql = @"
                    SELECT sp.SessionProductId, sp.SessionId, sp.ProductId, sp.Quantity, sp.UnitPrice, sp.Amount, sp.CreatedAt,
                           p.ProductName, p.Unit
                    FROM SessionProducts sp
                    INNER JOIN Products p ON sp.ProductId = p.ProductId
                    WHERE sp.SessionId = @SessionId
                    ORDER BY sp.CreatedAt ASC;";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SessionId", sessionId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new SessionProduct
                            {
                                SessionProductId = reader.GetInt64("SessionProductId"),
                                SessionId = reader.GetInt64("SessionId"),
                                ProductId = reader.GetInt32("ProductId"),
                                ProductName = reader.GetString("ProductName"),
                                Unit = reader.GetString("Unit"),
                                Quantity = reader.GetInt32("Quantity"),
                                UnitPrice = reader.GetDecimal("UnitPrice"),
                                Amount = reader.GetDecimal("Amount"),
                                CreatedAt = reader.GetDateTime("CreatedAt")
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<(bool Success, string Message)> AddProductToSessionAsync(long sessionId, int productId, int quantity, decimal unitPrice)
        {
            if (quantity <= 0) return (false, "Số lượng phải lớn hơn 0!");

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    // Kiểm tra sản phẩm đã có trong session chưa
                    const string checkSql = @"
                        SELECT SessionProductId, Quantity 
                        FROM SessionProducts 
                        WHERE SessionId = @SessionId AND ProductId = @ProductId 
                        LIMIT 1;";

                    long? existingId = null;
                    int currentQty = 0;

                    using (var cmdCheck = new MySqlCommand(checkSql, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@SessionId", sessionId);
                        cmdCheck.Parameters.AddWithValue("@ProductId", productId);
                        using (var reader = await cmdCheck.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                existingId = reader.GetInt64("SessionProductId");
                                currentQty = reader.GetInt32("Quantity");
                            }
                        }
                    }

                    if (existingId.HasValue)
                    {
                        // Cập nhật tăng số lượng
                        int newQty = currentQty + quantity;
                        decimal newAmount = newQty * unitPrice;
                        const string updateSql = @"
                            UPDATE SessionProducts 
                            SET Quantity = @Quantity, UnitPrice = @UnitPrice, Amount = @Amount 
                            WHERE SessionProductId = @Id;";

                        using (var cmdUpdate = new MySqlCommand(updateSql, conn))
                        {
                            cmdUpdate.Parameters.AddWithValue("@Quantity", newQty);
                            cmdUpdate.Parameters.AddWithValue("@UnitPrice", unitPrice);
                            cmdUpdate.Parameters.AddWithValue("@Amount", newAmount);
                            cmdUpdate.Parameters.AddWithValue("@Id", existingId.Value);
                            await cmdUpdate.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        // Thêm dòng mới
                        decimal amount = quantity * unitPrice;
                        const string insertSql = @"
                            INSERT INTO SessionProducts (SessionId, ProductId, Quantity, UnitPrice, Amount, CreatedAt)
                            VALUES (@SessionId, @ProductId, @Quantity, @UnitPrice, @Amount, NOW());";

                        using (var cmdInsert = new MySqlCommand(insertSql, conn))
                        {
                            cmdInsert.Parameters.AddWithValue("@SessionId", sessionId);
                            cmdInsert.Parameters.AddWithValue("@ProductId", productId);
                            cmdInsert.Parameters.AddWithValue("@Quantity", quantity);
                            cmdInsert.Parameters.AddWithValue("@UnitPrice", unitPrice);
                            cmdInsert.Parameters.AddWithValue("@Amount", amount);
                            await cmdInsert.ExecuteNonQueryAsync();
                        }
                    }
                }
                return (true, "Thêm món thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi thêm món: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RemoveProductFromSessionAsync(long sessionProductId)
        {
            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string sql = "DELETE FROM SessionProducts WHERE SessionProductId = @Id;";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", sessionProductId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return (true, "Đã xóa món!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xóa món: {ex.Message}");
            }
        }
    }
}

