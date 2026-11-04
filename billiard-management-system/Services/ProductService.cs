using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using billiard_management_system.Helpers;
using billiard_management_system.Models;
using MySqlConnector;

namespace billiard_management_system.Services
{
    public interface IProductService
    {
        Task<List<ProductCategory>> GetCategoriesAsync(bool includeInactive = false);
        Task<List<Product>> GetProductsAsync(int? categoryId = null, bool includeInactive = false);
        Task<bool> IsCategoryNameExistsAsync(string categoryName, int? excludeCategoryId = null);
        Task<bool> IsProductNameExistsAsync(string productName, int? excludeProductId = null);
        Task<(bool Success, string Message, int CategoryId)> CreateCategoryAsync(ProductCategory category);
        Task<(bool Success, string Message)> UpdateCategoryAsync(ProductCategory category);
        Task<(bool Success, string Message, int ProductId)> CreateProductAsync(Product product);
        Task<(bool Success, string Message)> UpdateProductAsync(Product product);
        Task<(bool Success, string Message)> ToggleProductActiveAsync(int productId, bool isActive);
        Task<List<SessionProduct>> GetSessionProductsAsync(long sessionId);
        Task<(bool Success, string Message)> AddProductToSessionAsync(long sessionId, int productId, int quantity, decimal unitPrice);
        Task<(bool Success, string Message)> UpdateSessionProductQuantityAsync(long sessionProductId, int newQuantity);
        Task<(bool Success, string Message)> RemoveProductFromSessionAsync(long sessionProductId);
    }

    public class ProductService : IProductService
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ICloudinaryService _cloudinaryService;

        public ProductService(IAuditLogService auditLogService = null, ICloudinaryService cloudinaryService = null)
        {
            _auditLogService = auditLogService ?? new AuditLogService();
            _cloudinaryService = cloudinaryService ?? new CloudinaryService();
        }

        public async Task<List<ProductCategory>> GetCategoriesAsync(bool includeInactive = false)
        {
            var list = new List<ProductCategory>();
            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                string sql = "SELECT CategoryId, CategoryName, Description, IsActive FROM ProductCategories";
                if (!includeInactive)
                {
                    sql += " WHERE IsActive = TRUE";
                }
                sql += " ORDER BY IsActive DESC, CategoryId ASC;";

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

        public async Task<List<Product>> GetProductsAsync(int? categoryId = null, bool includeInactive = false)
        {
            var list = new List<Product>();
            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                string sql = @"
                    SELECT p.ProductId, p.CategoryId, p.ProductName, p.Unit, p.Price, p.ImageUrl, p.IsActive,
                           c.CategoryName
                    FROM Products p
                    INNER JOIN ProductCategories c ON p.CategoryId = c.CategoryId
                    WHERE 1 = 1";

                if (!includeInactive)
                {
                    sql += " AND p.IsActive = TRUE AND c.IsActive = TRUE";
                }

                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    sql += " AND p.CategoryId = @CategoryId";
                }
                sql += " ORDER BY p.IsActive DESC, c.CategoryId ASC, p.ProductName ASC;";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (categoryId.HasValue && categoryId.Value > 0)
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

        public async Task<bool> IsCategoryNameExistsAsync(string categoryName, int? excludeCategoryId = null)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return false;

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                string sql = "SELECT COUNT(1) FROM ProductCategories WHERE LOWER(CategoryName) = LOWER(@Name)";
                if (excludeCategoryId.HasValue)
                {
                    sql += " AND CategoryId <> @ExcludeId";
                }

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", categoryName.Trim());
                    if (excludeCategoryId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@ExcludeId", excludeCategoryId.Value);
                    }
                    return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                }
            }
        }

        public async Task<bool> IsProductNameExistsAsync(string productName, int? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(productName)) return false;

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                string sql = "SELECT COUNT(1) FROM Products WHERE LOWER(ProductName) = LOWER(@Name)";
                if (excludeProductId.HasValue)
                {
                    sql += " AND ProductId <> @ExcludeId";
                }

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", productName.Trim());
                    if (excludeProductId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@ExcludeId", excludeProductId.Value);
                    }
                    return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                }
            }
        }

        public async Task<(bool Success, string Message, int CategoryId)> CreateCategoryAsync(ProductCategory category)
        {
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.ProductManage, "Thêm danh mục sản phẩm mới");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "ProductCategory", null, deniedMsg);
                return (false, deniedMsg, 0);
            }

            if (category == null || string.IsNullOrWhiteSpace(category.CategoryName))
            {
                return (false, "Tên danh mục không được để trống.", 0);
            }

            string cleanName = category.CategoryName.Trim();
            string cleanDesc = string.IsNullOrWhiteSpace(category.Description) ? null : category.Description.Trim();

            try
            {
                if (await IsCategoryNameExistsAsync(cleanName))
                {
                    return (false, $"Danh mục '{cleanName}' đã tồn tại trên hệ thống.", 0);
                }

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string sql = @"
                        INSERT INTO ProductCategories (CategoryName, Description, IsActive)
                        VALUES (@Name, @Description, @IsActive);
                        SELECT LAST_INSERT_ID();";

                    int newId;
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", cleanName);
                        cmd.Parameters.AddWithValue("@Description", (object)cleanDesc ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", category.IsActive);
                        newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId, "CREATE_CATEGORY", "ProductCategory", newId,
                        $"Tạo danh mục dịch vụ mới: '{cleanName}'.");

                    return (true, $"Thêm danh mục [{cleanName}] thành công!", newId);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi thêm danh mục: {ex.Message}", 0);
            }
        }

        public async Task<(bool Success, string Message)> UpdateCategoryAsync(ProductCategory category)
        {
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.ProductManage, "Cập nhật danh mục sản phẩm");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "ProductCategory", category?.CategoryId, deniedMsg);
                return (false, deniedMsg);
            }

            if (category == null || category.CategoryId <= 0 || string.IsNullOrWhiteSpace(category.CategoryName))
            {
                return (false, "Thông tin danh mục không hợp lệ.");
            }

            string cleanName = category.CategoryName.Trim();
            string cleanDesc = string.IsNullOrWhiteSpace(category.Description) ? null : category.Description.Trim();

            try
            {
                if (await IsCategoryNameExistsAsync(cleanName, category.CategoryId))
                {
                    return (false, $"Tên danh mục '{cleanName}' đã bị trùng với danh mục khác.");
                }

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string sql = @"
                        UPDATE ProductCategories
                        SET CategoryName = @Name, Description = @Description, IsActive = @IsActive
                        WHERE CategoryId = @Id;";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", cleanName);
                        cmd.Parameters.AddWithValue("@Description", (object)cleanDesc ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", category.IsActive);
                        cmd.Parameters.AddWithValue("@Id", category.CategoryId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId, "UPDATE_CATEGORY", "ProductCategory", category.CategoryId,
                        $"Cập nhật danh mục '{cleanName}' (IsActive={category.IsActive}).");

                    return (true, $"Cập nhật danh mục [{cleanName}] thành công!");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cập nhật danh mục: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, int ProductId)> CreateProductAsync(Product product)
        {
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.ProductManage, "Thêm sản phẩm/dịch vụ mới");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "Product", null, deniedMsg);
                return (false, deniedMsg, 0);
            }

            var (valid, validationMsg) = ValidateProductInput(product);
            if (!valid)
            {
                return (false, validationMsg, 0);
            }

            string cleanName = product.ProductName.Trim();
            string cleanUnit = product.Unit.Trim();
            string cleanImgUrl = string.IsNullOrWhiteSpace(product.ImageUrl) ? null : product.ImageUrl.Trim();

            try
            {
                if (await IsProductNameExistsAsync(cleanName))
                {
                    return (false, $"Sản phẩm '{cleanName}' đã tồn tại trên hệ thống.", 0);
                }

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string sql = @"
                        INSERT INTO Products (CategoryId, ProductName, Unit, Price, ImageUrl, IsActive)
                        VALUES (@CategoryId, @ProductName, @Unit, @Price, @ImageUrl, @IsActive);
                        SELECT LAST_INSERT_ID();";

                    int newId;
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CategoryId", product.CategoryId);
                        cmd.Parameters.AddWithValue("@ProductName", cleanName);
                        cmd.Parameters.AddWithValue("@Unit", cleanUnit);
                        cmd.Parameters.AddWithValue("@Price", product.Price);
                        cmd.Parameters.AddWithValue("@ImageUrl", (object)cleanImgUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", product.IsActive);
                        newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId, "CREATE_PRODUCT", "Product", newId,
                        $"Thêm sản phẩm '{cleanName}' ({cleanUnit}), Giá={product.Price:N0}đ.");

                    return (true, $"Thêm sản phẩm [{cleanName}] thành công!", newId);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi thêm sản phẩm: {ex.Message}", 0);
            }
        }

        public async Task<(bool Success, string Message)> UpdateProductAsync(Product product)
        {
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.ProductManage, "Cập nhật thông tin sản phẩm/dịch vụ");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "Product", product?.ProductId, deniedMsg);
                return (false, deniedMsg);
            }

            if (product == null || product.ProductId <= 0)
            {
                return (false, "Không xác định được sản phẩm cần cập nhật.");
            }

            var (valid, validationMsg) = ValidateProductInput(product);
            if (!valid)
            {
                return (false, validationMsg);
            }

            string cleanName = product.ProductName.Trim();
            string cleanUnit = product.Unit.Trim();
            string cleanImgUrl = string.IsNullOrWhiteSpace(product.ImageUrl) ? null : product.ImageUrl.Trim();

            try
            {
                if (await IsProductNameExistsAsync(cleanName, product.ProductId))
                {
                    return (false, $"Tên sản phẩm '{cleanName}' đã bị trùng với sản phẩm khác.");
                }

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    string oldValuesJson = null;
                    const string selectOldSql = "SELECT ProductName, Unit, Price, IsActive FROM Products WHERE ProductId = @ProductId LIMIT 1;";
                    using (var oldCmd = new MySqlCommand(selectOldSql, conn))
                    {
                        oldCmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                        using (var r = await oldCmd.ExecuteReaderAsync())
                        {
                            if (await r.ReadAsync())
                            {
                                string oldName = (r.GetString("ProductName") ?? string.Empty).Replace("\"", "\\\"");
                                string oldUnit = (r.GetString("Unit") ?? string.Empty).Replace("\"", "\\\"");
                                decimal oldPrice = r.GetDecimal("Price");
                                bool oldActive = r.GetBoolean("IsActive");
                                oldValuesJson = $"{{\"ProductName\":\"{oldName}\",\"Unit\":\"{oldUnit}\",\"Price\":{oldPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"IsActive\":{(oldActive ? "true" : "false")}}}";
                            }
                        }
                    }

                    // Chỉ cập nhật bảng Products. Các dòng SessionProducts và InvoiceDetails cũ giữ nguyên UnitPrice tại thời điểm bán!
                    const string sql = @"
                        UPDATE Products
                        SET CategoryId = @CategoryId,
                            ProductName = @ProductName,
                            Unit = @Unit,
                            Price = @Price,
                            ImageUrl = @ImageUrl,
                            IsActive = @IsActive
                        WHERE ProductId = @ProductId;";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CategoryId", product.CategoryId);
                        cmd.Parameters.AddWithValue("@ProductName", cleanName);
                        cmd.Parameters.AddWithValue("@Unit", cleanUnit);
                        cmd.Parameters.AddWithValue("@Price", product.Price);
                        cmd.Parameters.AddWithValue("@ImageUrl", (object)cleanImgUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", product.IsActive);
                        cmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    string newValuesJson = $"{{\"ProductName\":\"{cleanName.Replace("\"", "\\\"")}\",\"Unit\":\"{cleanUnit.Replace("\"", "\\\"")}\",\"Price\":{product.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"IsActive\":{(product.IsActive ? "true" : "false")}}}";

                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId, "UPDATE_PRODUCT", "Product", product.ProductId,
                        $"Cập nhật sản phẩm '{cleanName}' ({cleanUnit}), Giá mới={product.Price:N0}đ, IsActive={product.IsActive}.",
                        oldValuesJson,
                        newValuesJson);

                    return (true, $"Cập nhật sản phẩm [{cleanName}] thành công!");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cập nhật sản phẩm: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ToggleProductActiveAsync(int productId, bool isActive)
        {
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.ProductManage, "Thay đổi trạng thái kinh doanh sản phẩm");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "Product", productId, deniedMsg);
                return (false, deniedMsg);
            }

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string sql = "UPDATE Products SET IsActive = @IsActive WHERE ProductId = @ProductId;";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                string statusText = isActive ? "mở bán lại" : "ngừng kinh doanh";
                await _auditLogService.LogAsync(
                    UserSession.CurrentUser?.UserId, isActive ? "ACTIVATE_PRODUCT" : "DEACTIVATE_PRODUCT", "Product", productId,
                    $"Đã {statusText} sản phẩm ProductId={productId}.");

                return (true, $"Đã {statusText} sản phẩm thành công! Dữ liệu hóa đơn cũ vẫn được bảo toàn nguyên vẹn.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi thay đổi trạng thái kinh doanh: {ex.Message}");
            }
        }

        private (bool Valid, string Message) ValidateProductInput(Product product)
        {
            if (product == null)
            {
                return (false, "Dữ liệu sản phẩm không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                return (false, "Vui lòng nhập tên sản phẩm / dịch vụ.");
            }

            if (product.CategoryId <= 0)
            {
                return (false, "Vui lòng chọn danh mục sản phẩm hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(product.Unit))
            {
                return (false, "Vui lòng nhập đơn vị tính (vd: Lon, Chai, Tô, Phần).");
            }

            if (product.Price < 0)
            {
                return (false, "Giá bán sản phẩm không được phép là số âm!");
            }

            if (!string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                var urlCheck = _cloudinaryService.ValidateImageUrl(product.ImageUrl);
                if (!urlCheck.Valid)
                {
                    return urlCheck;
                }
            }

            return (true, string.Empty);
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
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.SessionOpen, "Thêm dịch vụ/sản phẩm vào phiên chơi");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "SessionProduct", sessionId, deniedMsg);
                return (false, deniedMsg);
            }

            if (sessionId <= 0) return (false, "Phiên chơi không hợp lệ!");
            if (productId <= 0) return (false, "Vui lòng chọn sản phẩm cần thêm!");
            if (quantity <= 0) return (false, "Số lượng sản phẩm phải lớn hơn 0!");
            if (unitPrice < 0) return (false, "Đơn giá sản phẩm không hợp lệ!");

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                using (var trans = await conn.BeginTransactionAsync())
                {
                    // 1. Chỉ thêm sản phẩm vào phiên đang hoạt động (Active)
                    const string checkSessionSql = "SELECT Status FROM Sessions WHERE SessionId = @SessionId FOR UPDATE;";
                    using (var cmdSess = new MySqlCommand(checkSessionSql, conn, trans))
                    {
                        cmdSess.Parameters.AddWithValue("@SessionId", sessionId);
                        var sessStatus = await cmdSess.ExecuteScalarAsync() as string;
                        if (!string.Equals(sessStatus, "Active", StringComparison.OrdinalIgnoreCase))
                        {
                            await trans.RollbackAsync();
                            return (false, "Chỉ được phép thêm sản phẩm vào phiên chơi đang hoạt động (Active)!");
                        }
                    }

                    // 2. Kiểm tra sản phẩm còn đang kinh doanh và lấy đơn giá hiện tại nếu cần
                    string productName = null;
                    decimal effectiveUnitPrice = unitPrice;
                    const string checkProdSql = "SELECT ProductName, Price, IsActive FROM Products WHERE ProductId = @ProductId LIMIT 1;";
                    using (var cmdProd = new MySqlCommand(checkProdSql, conn, trans))
                    {
                        cmdProd.Parameters.AddWithValue("@ProductId", productId);
                        using (var reader = await cmdProd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                await trans.RollbackAsync();
                                return (false, "Không tìm thấy sản phẩm trong danh mục!");
                            }

                            bool isProdActive = reader.GetBoolean("IsActive");
                            productName = reader.GetString("ProductName");
                            decimal dbPrice = reader.GetDecimal("Price");
                            reader.Close();

                            if (!isProdActive)
                            {
                                await trans.RollbackAsync();
                                return (false, $"Sản phẩm [{productName}] hiện đã ngừng kinh doanh, không thể thêm vào phiên chơi!");
                            }

                            if (effectiveUnitPrice <= 0 && dbPrice > 0)
                            {
                                effectiveUnitPrice = dbPrice;
                            }
                        }
                    }

                    // 3. Kiểm tra sản phẩm cùng mức UnitPrice đã có trong session chưa
                    // (Nếu sản phẩm được sửa giá sau đó, các dòng đã ghi nhận giữ nguyên UnitPrice cũ, dòng mới tạo với UnitPrice mới)
                    const string checkSql = @"
                        SELECT SessionProductId, Quantity 
                        FROM SessionProducts 
                        WHERE SessionId = @SessionId AND ProductId = @ProductId AND UnitPrice = @UnitPrice
                        LIMIT 1;";

                    long? existingId = null;
                    int currentQty = 0;

                    using (var cmdCheck = new MySqlCommand(checkSql, conn, trans))
                    {
                        cmdCheck.Parameters.AddWithValue("@SessionId", sessionId);
                        cmdCheck.Parameters.AddWithValue("@ProductId", productId);
                        cmdCheck.Parameters.AddWithValue("@UnitPrice", effectiveUnitPrice);
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
                        int newQty = currentQty + quantity;
                        decimal newAmount = newQty * effectiveUnitPrice;
                        const string updateSql = @"
                            UPDATE SessionProducts 
                            SET Quantity = @Quantity, Amount = @Amount 
                            WHERE SessionProductId = @Id;";

                        using (var cmdUpdate = new MySqlCommand(updateSql, conn, trans))
                        {
                            cmdUpdate.Parameters.AddWithValue("@Quantity", newQty);
                            cmdUpdate.Parameters.AddWithValue("@Amount", newAmount);
                            cmdUpdate.Parameters.AddWithValue("@Id", existingId.Value);
                            await cmdUpdate.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        decimal amount = quantity * effectiveUnitPrice;
                        const string insertSql = @"
                            INSERT INTO SessionProducts (SessionId, ProductId, Quantity, UnitPrice, Amount, CreatedAt)
                            VALUES (@SessionId, @ProductId, @Quantity, @UnitPrice, @Amount, @CreatedAt);";

                        using (var cmdInsert = new MySqlCommand(insertSql, conn, trans))
                        {
                            cmdInsert.Parameters.AddWithValue("@SessionId", sessionId);
                            cmdInsert.Parameters.AddWithValue("@ProductId", productId);
                            cmdInsert.Parameters.AddWithValue("@Quantity", quantity);
                            cmdInsert.Parameters.AddWithValue("@UnitPrice", effectiveUnitPrice);
                            cmdInsert.Parameters.AddWithValue("@Amount", amount);
                            cmdInsert.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                            await cmdInsert.ExecuteNonQueryAsync();
                        }
                    }

                    await trans.CommitAsync();

                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId, "ADD_SESSION_PRODUCT", "SessionProduct", sessionId,
                        $"Thêm {quantity} x '{productName}' (Đơn giá={effectiveUnitPrice:N0}đ) vào phiên #{sessionId}.");

                    return (true, $"Đã thêm {quantity} {productName} vào bàn!");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi thêm món: {ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật số lượng sản phẩm đã gọi trong phiên chơi.
        /// Giữ nguyên UnitPrice lịch sử lúc gọi món và tính lại Amount = newQuantity * UnitPrice.
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateSessionProductQuantityAsync(long sessionProductId, int newQuantity)
        {
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.SessionOpen, "Cập nhật số lượng món trong phiên chơi");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "SessionProduct", sessionProductId, deniedMsg);
                return (false, deniedMsg);
            }

            if (newQuantity <= 0)
            {
                return (false, "Số lượng sản phẩm phải lớn hơn 0!");
            }

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                using (var trans = await conn.BeginTransactionAsync())
                {
                    const string getSql = @"
                        SELECT sp.SessionId, sp.UnitPrice, s.Status
                        FROM SessionProducts sp
                        INNER JOIN Sessions s ON sp.SessionId = s.SessionId
                        WHERE sp.SessionProductId = @Id
                        FOR UPDATE;";

                    decimal historicalUnitPrice;
                    string sessionStatus;

                    using (var cmdGet = new MySqlCommand(getSql, conn, trans))
                    {
                        cmdGet.Parameters.AddWithValue("@Id", sessionProductId);
                        using (var reader = await cmdGet.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                await trans.RollbackAsync();
                                return (false, "Không tìm thấy dòng sản phẩm trong phiên chơi.");
                            }

                            historicalUnitPrice = reader.GetDecimal("UnitPrice");
                            sessionStatus = reader.GetString("Status");
                        }
                    }

                    if (!string.Equals(sessionStatus, "Active", StringComparison.OrdinalIgnoreCase))
                    {
                        await trans.RollbackAsync();
                        return (false, "Chỉ được phép chỉnh sửa sản phẩm khi phiên chơi đang hoạt động (Active)!");
                    }

                    decimal newAmount = newQuantity * historicalUnitPrice;
                    const string updateSql = @"
                        UPDATE SessionProducts
                        SET Quantity = @Quantity,
                            Amount = @Amount
                        WHERE SessionProductId = @Id;";

                    using (var cmdUpd = new MySqlCommand(updateSql, conn, trans))
                    {
                        cmdUpd.Parameters.AddWithValue("@Quantity", newQuantity);
                        cmdUpd.Parameters.AddWithValue("@Amount", newAmount);
                        cmdUpd.Parameters.AddWithValue("@Id", sessionProductId);
                        await cmdUpd.ExecuteNonQueryAsync();
                    }

                    await trans.CommitAsync();
                    return (true, "Đã cập nhật số lượng món!");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cập nhật số lượng: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RemoveProductFromSessionAsync(long sessionProductId)
        {
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.SessionOpen, "Xóa dịch vụ/sản phẩm khỏi phiên chơi");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "SessionProduct", sessionProductId, deniedMsg);
                return (false, deniedMsg);
            }

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string checkSql = @"
                        SELECT s.Status
                        FROM SessionProducts sp
                        INNER JOIN Sessions s ON sp.SessionId = s.SessionId
                        WHERE sp.SessionProductId = @Id;";

                    using (var cmdCheck = new MySqlCommand(checkSql, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@Id", sessionProductId);
                        var status = await cmdCheck.ExecuteScalarAsync() as string;
                        if (status == null)
                        {
                            return (false, "Món cần xóa không tồn tại.");
                        }
                        if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                        {
                            return (false, "Không thể xóa sản phẩm khỏi phiên chơi đã kết thúc hoặc đã hủy!");
                        }
                    }

                    const string sql = "DELETE FROM SessionProducts WHERE SessionProductId = @Id;";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", sessionProductId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return (true, "Đã xóa món khỏi phiên chơi!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xóa món: {ex.Message}");
            }
        }
    }
}
