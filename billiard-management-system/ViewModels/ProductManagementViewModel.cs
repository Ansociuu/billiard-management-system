using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using billiard_management_system.Core;
using billiard_management_system.Models;
using billiard_management_system.Services;
using Microsoft.Win32;

namespace billiard_management_system.ViewModels
{
    public class ProductManagementViewModel : BaseViewModel
    {
        private readonly IProductService _productService;
        private readonly ICloudinaryService _cloudinaryService;

        private ObservableCollection<Product> _allProducts = new ObservableCollection<Product>();
        private ObservableCollection<Product> _filteredProducts = new ObservableCollection<Product>();
        private ObservableCollection<ProductCategory> _activeCategories = new ObservableCollection<ProductCategory>();
        private ObservableCollection<ProductCategory> _allCategories = new ObservableCollection<ProductCategory>();

        private Product _selectedProduct;
        private ProductCategory _selectedCategory;

        // Bộ lọc
        private string _searchKeyword = string.Empty;
        private int? _categoryFilterId;
        private string _statusFilter = "All"; // "All", "Active", "Inactive"

        // Form Sản phẩm (CRUD Products)
        private bool _isProductEditMode;
        private int _editingProductId;
        private string _formProductName = string.Empty;
        private int _formCategoryId = 1;
        private string _formUnit = "Lon";
        private string _formPriceText = "15000";
        private string _formImageUrl = string.Empty;
        private bool _formProductIsActive = true;
        private bool _isUploadingImage;

        // Form Danh mục (CRUD ProductCategories)
        private bool _isCategoryEditMode;
        private int _editingCategoryId;
        private string _formCategoryName = string.Empty;
        private string _formCategoryDescription = string.Empty;
        private bool _formCategoryIsActive = true;

        public event Action<string, string> NotificationRequested;

        public ProductManagementViewModel(IProductService productService = null, ICloudinaryService cloudinaryService = null)
        {
            _productService = productService ?? new ProductService();
            _cloudinaryService = cloudinaryService ?? new CloudinaryService();
            Title = "Quản lý Danh mục & Sản phẩm Dịch vụ";

            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            FilterCategoryCommand = new RelayCommand<object>(OnFilterCategory);
            FilterStatusCommand = new RelayCommand<string>(OnFilterStatus);

            PrepareNewProductCommand = new RelayCommand(ExecutePrepareNewProduct);
            SaveProductCommand = new AsyncRelayCommand(ExecuteSaveProductAsync);
            ToggleProductActiveCommand = new AsyncRelayCommand(ExecuteToggleProductActiveAsync);
            SelectAndUploadImageCommand = new AsyncRelayCommand(ExecuteSelectAndUploadImageAsync);
            ClearImageCommand = new RelayCommand(() => FormImageUrl = string.Empty);

            PrepareNewCategoryCommand = new RelayCommand(ExecutePrepareNewCategory);
            SaveCategoryCommand = new AsyncRelayCommand(ExecuteSaveCategoryAsync);

            _ = LoadDataAsync();
        }

        #region Properties - Lists & Filters

        public ObservableCollection<Product> FilteredProducts
        {
            get => _filteredProducts;
            set => SetProperty(ref _filteredProducts, value);
        }

        public ObservableCollection<ProductCategory> ActiveCategories
        {
            get => _activeCategories;
            set => SetProperty(ref _activeCategories, value);
        }

        public ObservableCollection<ProductCategory> AllCategories
        {
            get => _allCategories;
            set => SetProperty(ref _allCategories, value);
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    ApplyFilter();
                }
            }
        }

        public int TotalProductsCount => _allProducts.Count;
        public int ActiveProductsCount => _allProducts.Count(p => p.IsActive);
        public int InactiveProductsCount => _allProducts.Count(p => !p.IsActive);

        #endregion

        #region Properties - Product Form

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value) && value != null)
                {
                    ClearError();
                    _editingProductId = value.ProductId;
                    FormProductName = value.ProductName;
                    FormCategoryId = value.CategoryId;
                    FormUnit = value.Unit;
                    FormPriceText = value.Price.ToString("0.##", CultureInfo.InvariantCulture);
                    FormImageUrl = value.ImageUrl ?? string.Empty;
                    FormProductIsActive = value.IsActive;
                    IsProductEditMode = true;
                    OnPropertyChanged(nameof(ProductFormTitle));
                    OnPropertyChanged(nameof(ToggleProductActiveButtonText));
                }
            }
        }

        public bool IsProductEditMode
        {
            get => _isProductEditMode;
            set
            {
                if (SetProperty(ref _isProductEditMode, value))
                {
                    OnPropertyChanged(nameof(ProductFormTitle));
                    OnPropertyChanged(nameof(ProductSaveButtonText));
                }
            }
        }

        public string ProductFormTitle => IsProductEditMode
            ? $"Cập nhật Sản phẩm: #{_editingProductId} ({FormProductName})"
            : "Thêm Sản phẩm / Dịch vụ mới";
        public string ProductSaveButtonText => IsProductEditMode ? "LƯU CẬP NHẬT SẢN PHẨM" : "THÊM SẢN PHẨM MỚI";
        public string ToggleProductActiveButtonText => FormProductIsActive ? "Ngừng kinh doanh" : "Mở bán lại";

        public string FormProductName
        {
            get => _formProductName;
            set => SetProperty(ref _formProductName, value);
        }

        public int FormCategoryId
        {
            get => _formCategoryId;
            set => SetProperty(ref _formCategoryId, value);
        }

        public string FormUnit
        {
            get => _formUnit;
            set => SetProperty(ref _formUnit, value);
        }

        public string FormPriceText
        {
            get => _formPriceText;
            set => SetProperty(ref _formPriceText, value);
        }

        public string FormImageUrl
        {
            get => _formImageUrl;
            set
            {
                if (SetProperty(ref _formImageUrl, value))
                {
                    OnPropertyChanged(nameof(HasFormImageUrl));
                }
            }
        }

        public bool HasFormImageUrl => !string.IsNullOrWhiteSpace(_formImageUrl);

        public bool IsUploadingImage
        {
            get => _isUploadingImage;
            set
            {
                if (SetProperty(ref _isUploadingImage, value))
                {
                    OnPropertyChanged(nameof(UploadButtonText));
                }
            }
        }

        public string UploadButtonText => IsUploadingImage ? "Đang tải lên Cloudinary..." : "Tải ảnh lên Cloudinary";

        public bool FormProductIsActive
        {
            get => _formProductIsActive;
            set
            {
                if (SetProperty(ref _formProductIsActive, value))
                {
                    OnPropertyChanged(nameof(ToggleProductActiveButtonText));
                }
            }
        }

        #endregion

        #region Properties - Category Form

        public ProductCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value) && value != null)
                {
                    ClearError();
                    _editingCategoryId = value.CategoryId;
                    FormCategoryName = value.CategoryName;
                    FormCategoryDescription = value.Description ?? string.Empty;
                    FormCategoryIsActive = value.IsActive;
                    IsCategoryEditMode = true;
                    OnPropertyChanged(nameof(CategoryFormTitle));
                }
            }
        }

        public bool IsCategoryEditMode
        {
            get => _isCategoryEditMode;
            set
            {
                if (SetProperty(ref _isCategoryEditMode, value))
                {
                    OnPropertyChanged(nameof(CategoryFormTitle));
                    OnPropertyChanged(nameof(CategorySaveButtonText));
                }
            }
        }

        public string CategoryFormTitle => IsCategoryEditMode
            ? $"Cập nhật Danh mục: #{_editingCategoryId}"
            : "Thêm Danh mục Dịch vụ mới";
        public string CategorySaveButtonText => IsCategoryEditMode ? "CẬP NHẬT DANH MỤC" : "THÊM DANH MỤC";

        public string FormCategoryName
        {
            get => _formCategoryName;
            set => SetProperty(ref _formCategoryName, value);
        }

        public string FormCategoryDescription
        {
            get => _formCategoryDescription;
            set => SetProperty(ref _formCategoryDescription, value);
        }

        public bool FormCategoryIsActive
        {
            get => _formCategoryIsActive;
            set => SetProperty(ref _formCategoryIsActive, value);
        }

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }
        public ICommand FilterCategoryCommand { get; }
        public ICommand FilterStatusCommand { get; }

        public ICommand PrepareNewProductCommand { get; }
        public ICommand SaveProductCommand { get; }
        public ICommand ToggleProductActiveCommand { get; }
        public ICommand SelectAndUploadImageCommand { get; }
        public ICommand ClearImageCommand { get; }

        public ICommand PrepareNewCategoryCommand { get; }
        public ICommand SaveCategoryCommand { get; }

        #endregion

        #region Methods

        public async Task LoadDataAsync()
        {
            IsBusy = true;
            ClearError();

            try
            {
                var products = await _productService.GetProductsAsync(includeInactive: true);
                _allProducts = new ObservableCollection<Product>(products);

                var activeCats = await _productService.GetCategoriesAsync(includeInactive: false);
                ActiveCategories = new ObservableCollection<ProductCategory>(activeCats);

                var allCats = await _productService.GetCategoriesAsync(includeInactive: true);
                AllCategories = new ObservableCollection<ProductCategory>(allCats);

                ApplyFilter();
                UpdateSummaryCounts();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải danh sách sản phẩm: {ex.Message}";
                Notify(ErrorMessage, "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilter()
        {
            var query = _allProducts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                string kw = SearchKeyword.Trim().ToLowerInvariant();
                query = query.Where(p =>
                    (p.ProductName != null && p.ProductName.ToLowerInvariant().Contains(kw)) ||
                    (p.Unit != null && p.Unit.ToLowerInvariant().Contains(kw)) ||
                    (p.CategoryName != null && p.CategoryName.ToLowerInvariant().Contains(kw)));
            }

            if (_categoryFilterId.HasValue && _categoryFilterId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == _categoryFilterId.Value);
            }

            if (_statusFilter == "Active")
            {
                query = query.Where(p => p.IsActive);
            }
            else if (_statusFilter == "Inactive")
            {
                query = query.Where(p => !p.IsActive);
            }

            FilteredProducts = new ObservableCollection<Product>(query);
        }

        private void UpdateSummaryCounts()
        {
            OnPropertyChanged(nameof(TotalProductsCount));
            OnPropertyChanged(nameof(ActiveProductsCount));
            OnPropertyChanged(nameof(InactiveProductsCount));
        }

        private void OnFilterCategory(object param)
        {
            if (param == null || string.Equals(param.ToString(), "All", StringComparison.OrdinalIgnoreCase))
            {
                _categoryFilterId = null;
            }
            else if (int.TryParse(param.ToString(), out int catId))
            {
                _categoryFilterId = catId;
            }
            else
            {
                _categoryFilterId = null;
            }
            ApplyFilter();
        }

        private void OnFilterStatus(string status)
        {
            _statusFilter = string.IsNullOrWhiteSpace(status) ? "All" : status;
            ApplyFilter();
        }

        private void ExecutePrepareNewProduct()
        {
            ClearError();
            _selectedProduct = null;
            OnPropertyChanged(nameof(SelectedProduct));

            _editingProductId = 0;
            FormProductName = string.Empty;
            FormCategoryId = ActiveCategories.FirstOrDefault()?.CategoryId ?? 1;
            FormUnit = "Lon";
            FormPriceText = "15000";
            FormImageUrl = string.Empty;
            FormProductIsActive = true;
            IsProductEditMode = false;
            OnPropertyChanged(nameof(ProductFormTitle));
        }

        private async Task ExecuteSelectAndUploadImageAsync()
        {
            if (IsUploadingImage) return;
            ClearError();

            var dlg = new OpenFileDialog
            {
                Title = "Chọn ảnh sản phẩm để tải lên Cloudinary",
                Filter = "Tệp hình ảnh (*.jpg;*.jpeg;*.png;*.webp;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.webp;*.gif;*.bmp"
            };

            if (dlg.ShowDialog() != true) return;

            IsUploadingImage = true;
            try
            {
                var (success, uploadedUrl, msg) = await _cloudinaryService.UploadProductImageAsync(dlg.FileName);
                if (success && !string.IsNullOrWhiteSpace(uploadedUrl))
                {
                    FormImageUrl = uploadedUrl;
                    Notify(msg, "Success");
                }
                else
                {
                    ErrorMessage = msg;
                    Notify(msg, "Error");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi khi tải ảnh lên Cloudinary: {ex.Message}";
                Notify(ErrorMessage, "Error");
            }
            finally
            {
                IsUploadingImage = false;
            }
        }

        private async Task ExecuteSaveProductAsync()
        {
            if (IsBusy) return;
            ClearError();

            string rawPrice = (FormPriceText ?? string.Empty).Replace(",", "").Replace(".", "").Trim();
            if (!decimal.TryParse(FormPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedPrice) &&
                !decimal.TryParse(rawPrice, out parsedPrice))
            {
                ErrorMessage = "Đơn giá bán không hợp lệ! Vui lòng nhập số (vd: 20000).";
                Notify(ErrorMessage, "Error");
                return;
            }

            if (parsedPrice < 0)
            {
                ErrorMessage = "Giá bán sản phẩm không được phép là số âm!";
                Notify(ErrorMessage, "Error");
                return;
            }

            var (validUrl, urlMsg) = _cloudinaryService.ValidateImageUrl(FormImageUrl);
            if (!validUrl)
            {
                ErrorMessage = urlMsg;
                Notify(urlMsg, "Error");
                return;
            }

            var model = new Product
            {
                ProductId = _editingProductId,
                CategoryId = FormCategoryId,
                ProductName = FormProductName,
                Unit = FormUnit,
                Price = parsedPrice,
                ImageUrl = FormImageUrl,
                IsActive = FormProductIsActive
            };

            IsBusy = true;
            try
            {
                if (IsProductEditMode)
                {
                    var (success, msg) = await _productService.UpdateProductAsync(model);
                    if (success)
                    {
                        Notify(msg, "Success");
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = msg;
                        Notify(msg, "Error");
                    }
                }
                else
                {
                    var (success, msg, newId) = await _productService.CreateProductAsync(model);
                    if (success)
                    {
                        Notify(msg, "Success");
                        ExecutePrepareNewProduct();
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = msg;
                        Notify(msg, "Error");
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteToggleProductActiveAsync()
        {
            if (!IsProductEditMode || _editingProductId <= 0) return;

            bool targetActive = !FormProductIsActive;
            string actionDesc = targetActive ? "mở bán lại" : "ngừng kinh doanh";

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn {actionDesc} sản phẩm [{FormProductName}]?\n(Lưu ý: Các hóa đơn lịch sử đã có sản phẩm này vẫn được bảo toàn nguyên vẹn).",
                "Xác nhận thay đổi trạng thái kinh doanh",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var (success, msg) = await _productService.ToggleProductActiveAsync(_editingProductId, targetActive);
                if (success)
                {
                    FormProductIsActive = targetActive;
                    Notify(msg, "Success");
                    await LoadDataAsync();
                }
                else
                {
                    ErrorMessage = msg;
                    Notify(msg, "Error");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecutePrepareNewCategory()
        {
            ClearError();
            _selectedCategory = null;
            OnPropertyChanged(nameof(SelectedCategory));

            _editingCategoryId = 0;
            FormCategoryName = string.Empty;
            FormCategoryDescription = string.Empty;
            FormCategoryIsActive = true;
            IsCategoryEditMode = false;
            OnPropertyChanged(nameof(CategoryFormTitle));
        }

        private async Task ExecuteSaveCategoryAsync()
        {
            if (IsBusy) return;
            ClearError();

            var model = new ProductCategory
            {
                CategoryId = _editingCategoryId,
                CategoryName = FormCategoryName,
                Description = FormCategoryDescription,
                IsActive = FormCategoryIsActive
            };

            IsBusy = true;
            try
            {
                if (IsCategoryEditMode)
                {
                    var (success, msg) = await _productService.UpdateCategoryAsync(model);
                    if (success)
                    {
                        Notify(msg, "Success");
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = msg;
                        Notify(msg, "Error");
                    }
                }
                else
                {
                    var (success, msg, newId) = await _productService.CreateCategoryAsync(model);
                    if (success)
                    {
                        Notify(msg, "Success");
                        ExecutePrepareNewCategory();
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = msg;
                        Notify(msg, "Error");
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected void Notify(string message, string type = "Info")
        {
            NotificationRequested?.Invoke(message, type);
        }

        #endregion
    }
}
