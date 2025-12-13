# Inventory Alert Popup Window Implementation

## Overview

This implementation creates a popup window that displays detailed information about inventory alerts when users click on the warning cards in the Dashboard. The popup shows a list of products with low stock or slow-moving inventory in a clean, organized table format.

## Components Created

### 1. **InventoryAlertWindow.xaml**
A WPF Window that displays the alert details in a professional layout.

**Features:**
- Dynamic title and icon based on alert type
- Statistics bar showing:
  - Total number of products
  - Total stock quantity
  - Estimated total value
- DataGrid with product details:
  - Product ID
  - Product Name
  - Category
  - Unit
  - Stock Quantity (color-coded)
  - Price
- Close button

**Visual Design:**
```
???????????????????????????????????????????????
?  Icon  Title: Low Stock / Slow Moving      ?
?         Description                          ?
???????????????????????????????????????????????
?  ?? Total: 5  ?  ?? Stock: 25  ?  ?? Value: 2.5M ?
???????????????????????????????????????????????
?  ID  ?  Name  ?  Category  ?  Unit  ?  Stock  ?  Price  ?
??????????????????????????????????????????????????
?  SP001  ?  Coca...  ?  Drinks  ?  Can  ?   5  ?  15,000 ?
?  SP002  ?  Pepsi... ?  Drinks  ?  Can  ?   3  ?  14,000 ?
?  ...                                          ?
???????????????????????????????????????????????
?                          [Close] Button      ?
???????????????????????????????????????????????
```

### 2. **InventoryAlertWindow.xaml.cs**
Code-behind file that handles window events.

```csharp
public partial class InventoryAlertWindow : Window
{
    public InventoryAlertWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
```

### 3. **InventoryAlertWindowVM.cs**
ViewModel that manages the data and business logic.

**Properties:**
- `AlertType` - Type of alert (LowStock or SlowMoving)
- `Products` - Observable collection of products
- `TotalStockQuantity` - Sum of all stock quantities
- `TotalValue` - Total estimated value of inventory

**Methods:**
- `LoadProducts()` - Loads products based on alert type
- `CalculateStatistics()` - Calculates total stock and value

**Filtering Logic:**

**Low Stock:**
```csharp
const int MIN_STOCK_THRESHOLD = 10;
query = query.Where(p => p.StockQuantity > 0 && 
                          p.StockQuantity <= MIN_STOCK_THRESHOLD);
```

**Slow Moving:**
```csharp
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
```

### 4. **Updated Dashboard.xaml.cs**
Modified event handlers to show popup window instead of navigation.

```csharp
private void LowStockAlert_Click(object sender, MouseButtonEventArgs e)
{
    ShowInventoryAlertWindow(InventoryAlertType.LowStock);
}

private void SlowMovingAlert_Click(object sender, MouseButtonEventArgs e)
{
    ShowInventoryAlertWindow(InventoryAlertType.SlowMoving);
}

private void ShowInventoryAlertWindow(InventoryAlertType alertType)
{
    var window = new InventoryAlertWindow
    {
        DataContext = new InventoryAlertWindowVM(alertType),
        Owner = Window.GetWindow(this)
    };
    window.ShowDialog();
}
```

## User Flow

1. User is on Dashboard
2. User sees warning cards:
   - "Sap het hang" (Low Stock) - Red card
   - "Ton kho lau" (Slow Moving) - Orange card
3. User clicks on "Click de xem chi tiet" or anywhere on the alert card
4. Popup window opens showing:
   - Title based on alert type
   - Statistics (count, stock, value)
   - Detailed product list in table
5. User reviews the products
6. User clicks "Close" to return to Dashboard

## Visual Features

### Color Coding
- **Low Stock Alert:**
  - Icon: TrendingDown (Red #F44336)
  - Stock numbers: Red
  - Background: Light Red (#FFEBEE)

- **Slow Moving Alert:**
  - Icon: ClockAlert (Orange #FF9800)
  - Stock numbers: Orange
  - Background: Light Orange (#FFF3E0)

### DataGrid Styling
- Clean, modern look
- Alternating row colors for readability
- Color-coded stock quantities
- Right-aligned numbers
- Hover effects

### Statistics Bar
- Three key metrics displayed prominently
- Icons for visual clarity
- Large numbers for easy reading

## Benefits

? **Non-intrusive**
- Popup doesn't navigate away from Dashboard
- Modal window keeps focus
- Easy to dismiss

? **Comprehensive Information**
- Shows all relevant product details
- Statistics at a glance
- Sortable columns (inherent in DataGrid)

? **Professional Look**
- Material Design icons
- Consistent with app theme
- Color-coded for quick recognition

? **Performance**
- Only loads when needed
- Efficient queries
- Calculated statistics

## Advantages Over Navigation Approach

| Aspect | Popup Window | Navigation to Products |
|--------|--------------|------------------------|
| Context | Keeps Dashboard visible | Leaves Dashboard |
| Speed | Instant | Requires page load |
| Focus | Single purpose | General purpose view |
| Dismissal | Quick close | Must navigate back |
| Data | Alert-specific | Mixed with other products |

## Future Enhancements

### 1. Export Functionality
Add button to export alert list to Excel/PDF:

```csharp
public ICommand ExportCommand { get; set; }

private void ExportToExcel()
{
    // Export logic
}
```

### 2. Quick Actions
Add action buttons for each product:
- "Nhap hang" (Restock)
- "Giam gia" (Discount)
- "Xem chi tiet" (View Details)

### 3. Sorting and Filtering
Allow users to sort by:
- Stock quantity (lowest first)
- Value (highest first)
- Product name (A-Z)

### 4. Refresh Button
Add refresh capability to update data without closing window:

```csharp
public ICommand RefreshCommand { get; set; }

private void RefreshData()
{
    LoadProducts();
}
```

### 5. Print Function
Add print capability for inventory reports:

```csharp
public ICommand PrintCommand { get; set; }

private void PrintReport()
{
    // Print logic
}
```

### 6. Email Notification
Send alert list via email:

```csharp
public ICommand EmailCommand { get; set; }

private void SendEmailAlert()
{
    // Email logic
}
```

## Configuration

Current thresholds are hardcoded:

```csharp
const int MIN_STOCK_THRESHOLD = 10;
const int SLOW_MOVING_DAYS = 30;
```

**Recommended:** Move to app settings or database for admin configuration:

```csharp
public class InventorySettings
{
    public int MinStockThreshold { get; set; } = 10;
    public int SlowMovingDays { get; set; } = 30;
}
```

## Testing Checklist

- [ ] Popup opens when clicking Low Stock alert
- [ ] Popup opens when clicking Slow Moving alert
- [ ] Correct products are displayed
- [ ] Statistics are calculated correctly
- [ ] Close button works
- [ ] Window is modal (blocks Dashboard interaction)
- [ ] Window is centered on parent
- [ ] Color coding is correct
- [ ] DataGrid displays all columns
- [ ] Numbers are formatted correctly
- [ ] Window is resizable
- [ ] Theme colors are applied correctly

## Build Status

? Build successful
? All files created
? No compilation errors
