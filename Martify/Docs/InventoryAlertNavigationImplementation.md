# Inventory Alert Navigation Implementation

## T?ng Quan

Tính nãng này cho phép ngý?i dùng click vào các c?nh báo t?n kho trong Dashboard và t? ð?ng chuy?n ð?n trang Products v?i b? l?c phù h?p ð? xem chi ti?t các s?n ph?m c?n chú ?.

## Lu?ng Ho?t Ð?ng

```
Dashboard (Click Alert) 
    ?
Dashboard.xaml.cs (Event Handler)
    ?
DashboardVM.NavigateToInventoryAlert()
    ?
NavigationVM.NavigateToProductsWithAlert()
    ?
ProductsVM.SetInventoryAlertFilter()
    ?
ProductsVM.FilterProducts()
    ?
Products View (Hi?n th? danh sách ð? l?c)
```

## Các Files Ð? Thay Ð?i

### 1. **InventoryAlertType.cs** (M?i)
```csharp
public enum InventoryAlertType
{
    None = 0,           // Không có b? l?c
    LowStock = 1,       // S?p h?t hàng
    SlowMoving = 2      // T?n kho lâu
}
```

**M?c ðích:** Ð?nh ngh?a các lo?i c?nh báo t?n kho

### 2. **Dashboard.xaml**

**Thay ð?i:**
```xaml
<!-- C?nh báo s?p h?t hàng -->
<Border Cursor="Hand" MouseLeftButtonDown="LowStockAlert_Click">
    ...
</Border>

<!-- C?nh báo t?n kho lâu -->
<Border Cursor="Hand" MouseLeftButtonDown="SlowMovingAlert_Click">
    ...
</Border>
```

**M?c ðích:** Thêm event handlers cho các alert cards

### 3. **Dashboard.xaml.cs**

**Thêm methods:**
```csharp
private void LowStockAlert_Click(object sender, MouseButtonEventArgs e)
{
    var viewModel = DataContext as DashboardVM;
    viewModel?.NavigateToInventoryAlert(InventoryAlertType.LowStock);
}

private void SlowMovingAlert_Click(object sender, MouseButtonEventArgs e)
{
    var viewModel = DataContext as DashboardVM;
    viewModel?.NavigateToInventoryAlert(InventoryAlertType.SlowMoving);
}
```

**M?c ðích:** X? l? s? ki?n click và g?i ViewModel

### 4. **DashboardVM.cs**

**Thêm method:**
```csharp
public void NavigateToInventoryAlert(InventoryAlertType alertType)
{
    var mainWindow = Application.Current.MainWindow;
    if (mainWindow?.DataContext is MainVM mainVM)
    {
        mainVM.Navigation.NavigateToProductsWithAlert(alertType);
    }
}
```

**M?c ðích:** Ði?u hý?ng ð?n Products view v?i alert filter

### 5. **NavigationVM.cs**

**Thêm method:**
```csharp
public void NavigateToProductsWithAlert(InventoryAlertType alertType)
{
    var productsVM = new ProductsVM();
    productsVM.SetInventoryAlertFilter(alertType);
    CurrentView = productsVM;
}
```

**M?c ðích:** T?o ProductsVM m?i v?i b? l?c c?nh báo

### 6. **ProductsVM.cs**

**Thêm property:**
```csharp
private InventoryAlertType _inventoryAlertFilter;
public InventoryAlertType InventoryAlertFilter
{
    get => _inventoryAlertFilter;
    set
    {
        _inventoryAlertFilter = value;
        OnPropertyChanged();
        FilterProducts();
    }
}
```

**Thêm method:**
```csharp
public void SetInventoryAlertFilter(InventoryAlertType alertType)
{
    // Clear other filters
    SearchText = string.Empty;
    SelectedCategory = null;
    SelectedUnit = null;

    // Set alert filter
    InventoryAlertFilter = alertType;

    // Reload products with alert filter
    FilterProducts();
}
```

**C?p nh?t FilterProducts():**
```csharp
// Filter by inventory alert type
if (InventoryAlertFilter != InventoryAlertType.None)
{
    if (InventoryAlertFilter == InventoryAlertType.LowStock)
    {
        const int MIN_STOCK_THRESHOLD = 10;
        query = query.Where(p => p.StockQuantity > 0 && 
                                  p.StockQuantity <= MIN_STOCK_THRESHOLD);
    }
    else if (InventoryAlertFilter == InventoryAlertType.SlowMoving)
    {
        const int SLOW_MOVING_DAYS = 30;
        var thirtyDaysAgo = DateTime.Today.AddDays(-SLOW_MOVING_DAYS);

        var recentlyOrderedProductIds = DataProvider.Ins.DB.InvoiceDetails
            .Include(id => id.Invoice)
            .Where(id => id.Invoice.CreatedDate >= thirtyDaysAgo)
            .Select(id => id.ProductID)
            .Distinct()
            .ToList();

        query = query.Where(p => p.StockQuantity > 0 && 
                                  !recentlyOrderedProductIds.Contains(p.ProductID));
    }
}
```

## Logic L?c S?n Ph?m

### Low Stock Alert (S?p h?t hàng)
```
Ði?u ki?n: StockQuantity > 0 AND StockQuantity <= 10
```

### Slow Moving Alert (T?n kho lâu)
```
Ði?u ki?n: 
1. StockQuantity > 0
2. Không có InvoiceDetail nào trong 30 ngày qua
```

## Cách S? D?ng

1. **T? Dashboard:**
   - Click vào "S?p h?t hàng" ? Xem danh sách s?n ph?m có t?n kho ? 10
   - Click vào "T?n kho lâu" ? Xem danh sách s?n ph?m không bán trong 30 ngày

2. **Trong Products View:**
   - Danh sách s? t? ð?ng ðý?c l?c theo lo?i c?nh báo
   - Các b? l?c khác (Category, Unit, SearchText) s? b? xóa
   - Có th? ti?p t?c áp d?ng thêm các b? l?c khác

## Ki?n Trúc MVVM

### View (Dashboard.xaml)
- Hi?n th? UI
- X? l? s? ki?n click

### Code-Behind (Dashboard.xaml.cs)
- Event handlers
- G?i ViewModel methods

### ViewModel (DashboardVM)
- Business logic
- Ði?u hý?ng

### Navigation Service (NavigationVM)
- Qu?n l? navigation
- T?o ViewModels m?i

### Model (InventoryAlertType)
- Ð?nh ngh?a data types

## Ýu Ði?m

? **Tuân th? MVVM Pattern**
- View ch? x? l? UI events
- Logic n?m trong ViewModel
- Model ð?nh ngh?a r? ràng

? **D? b?o tr?**
- Code tách bi?t r? ràng
- D? test t?ng component
- D? m? r?ng thêm alert types

? **Tr?i nghi?m ngý?i dùng t?t**
- Click tr?c ti?p t? Dashboard
- T? ð?ng l?c products
- Có th? ti?p t?c l?c thêm

## Tính Nãng Týõng Lai

### 1. Thêm Alert Indicator
Hi?n th? badge/tag trong Products view ð? ngý?i dùng bi?t ðang ? ch? ð? l?c c?nh báo:

```xaml
<Border Visibility="{Binding HasAlertFilter}">
    <TextBlock Text="?? S?p h?t hàng" />
</Border>
```

### 2. Clear Alert Filter Button
Thêm nút ð? xóa b? l?c c?nh báo:

```csharp
public ICommand ClearAlertFilterCommand { get; set; }

private void ClearAlertFilter()
{
    InventoryAlertFilter = InventoryAlertType.None;
    FilterProducts();
}
```

### 3. Persist Alert Filter
Lýu tr?ng thái filter khi ngý?i dùng chuy?n tab và quay l?i:

```csharp
// Trong MainVM ho?c singleton service
public InventoryAlertType CurrentAlertFilter { get; set; }
```

### 4. Multiple Alert Types
H? tr? l?c nhi?u lo?i c?nh báo cùng lúc:

```csharp
public ObservableCollection<InventoryAlertType> SelectedAlertTypes { get; set; }
```

### 5. Export Alert Data
Cho phép xu?t danh sách s?n ph?m c?nh báo ra Excel/PDF:

```csharp
public ICommand ExportAlertCommand { get; set; }
```

## Testing Checklist

- [ ] Click "S?p h?t hàng" hi?n th? ðúng s?n ph?m
- [ ] Click "T?n kho lâu" hi?n th? ðúng s?n ph?m
- [ ] Các b? l?c khác b? xóa khi click alert
- [ ] Có th? áp d?ng thêm b? l?c sau khi click alert
- [ ] Clear filter ho?t ð?ng ðúng
- [ ] Build thành công
- [ ] Không có memory leak
- [ ] Performance t?t v?i nhi?u s?n ph?m

## Ngý?ng C?u H?nh

Hi?n t?i các ngý?ng ðý?c hard-code:

```csharp
const int MIN_STOCK_THRESHOLD = 10;      // Low stock
const int SLOW_MOVING_DAYS = 30;         // Slow moving
```

**Ð? xu?t c?i ti?n:** Chuy?n sang AppSettings ho?c Database ð? admin có th? c?u h?nh:

```csharp
public class InventorySettings
{
    public int MinStockThreshold { get; set; } = 10;
    public int SlowMovingDays { get; set; } = 30;
}
```
