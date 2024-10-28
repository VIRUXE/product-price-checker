using System.Diagnostics;

using Microsoft.Data.Sqlite;

using ProductPriceChecker.Models;
using ProductPriceChecker.Services;

namespace ProductPriceChecker;

public partial class MainForm : Form {
	private readonly System.Windows.Forms.Timer refreshTimer = new();
	private readonly System.Windows.Forms.Timer clockTimer   = new();

	public MainForm() {
		InitializeComponent();

		_ = DatabaseManager.InitializeDatabase();
		LoadProducts();
		
		const int refreshMinutes = 30;
		refreshTimer.Interval = refreshMinutes * 60 * 1000;
		refreshTimer.Tick    += async (_,_) => await RefreshPrices();
		refreshTimer.Start();

		var secondsUntilRefresh = refreshMinutes * 60;
		clockTimer.Interval = 1000;
		clockTimer.Tick    += (_, _) => {
			if (Controls["nextRefreshLabel"] is not Label nextRefreshLabel) return;

			nextRefreshLabel.Text = $"Next Refresh: {secondsUntilRefresh / 60:00}:{secondsUntilRefresh % 60:00}";
			secondsUntilRefresh   = secondsUntilRefresh > 0 ? secondsUntilRefresh - 1 : 30 * 60;
		};
		clockTimer.Start();
	}

	private void InitializeComponent() {
		Text = "Amazon Price Tracker";
		Size = new(800, 600);

		var urlLabel = new Label {
			Text     = "Amazon Product URL:",
			Location = new(10, 20),
			AutoSize = true
		};

		var urlTextBox = new TextBox {
			Name     = "urlTextBox",
			Location = new(10, 45),
			Width    = 500
		};

		var addButton = new Button {
			Text     = "Add Product",
			Location = new(520, 43),
			Width    = 100
		};
		addButton.Click += async (s, e) => await AddButton_Click(s, e);

		var productList = new ListView {
			Name          = "productList",
			Location      = new(10, 80),
			Size          = new(760, 400),
			View          = View.Details,
			FullRowSelect = true
		};

		productList.Columns.AddRange([
			new ColumnHeader { Text = "Title", Width = 300 },
			new ColumnHeader { Text = "Current Price", Width = 70 },
			new ColumnHeader { Text = "Lowest Price", Width = 70 },
			new ColumnHeader { Text = "Highest Price", Width = 70 },
			new ColumnHeader { Text = "Save with Used", Width = 90 },
			new ColumnHeader { Text = "Last Updated", Width = 150 }
		]);

		var removeButton = new Button {
			Text     = "Remove Selected",
			Location = new(10, 490),
			Width    = 120
		};
		removeButton.Click += RemoveButton_Click;

		var refreshButton = new Button {
			Text     = "Refresh Now",
			Location = new(140, 490),
			Width    = 100
		};
		refreshButton.Click += async (_,_) => await RefreshPrices();

		var nextRefreshLabel = new Label {
			Name     = "nextRefreshLabel",
			Text     = "Next Refresh: 30:00",
			Location = new(250, 490),
			AutoSize = true
		};

		Controls.AddRange([ urlLabel, urlTextBox, addButton, productList, removeButton, refreshButton, nextRefreshLabel ]);
	}

	private async Task AddButton_Click(object? sender, EventArgs e) {
		if (Controls["urlTextBox"] is not TextBox urlTextBox) return;

		if (string.IsNullOrWhiteSpace(urlTextBox.Text)) {
			MessageBox.Show("Please enter a valid Amazon URL");
			return;
		}

		try {
			var product = await PriceTracker.TrackProduct(urlTextBox.Text);
			await DatabaseManager.AddProduct(product);
			LoadProducts();
			urlTextBox.Clear();
		} catch (Exception ex) {
			MessageBox.Show($"Error adding product: {ex.Message}");
		}
	}

	private async void RemoveButton_Click(object? sender, EventArgs e) {
		if (Controls["productList"] is not ListView productList) return;

		if (productList.SelectedItems.Count > 0) {
			var url = productList.SelectedItems[0].Tag?.ToString();
			if (url is not null) {
				await DatabaseManager.RemoveProduct(url);
				LoadProducts();
			}
		}
	}
	
	private async Task RefreshPrices() {
		foreach (var product in await DatabaseManager.GetAllProducts()) {
			try {
				var updatedProduct = await PriceTracker.TrackProduct(product.Url);

				bool isLowerPrice     = updatedProduct.CurrentPrice < product.LowestPrice;
				bool isLowerUsedPrice = updatedProduct.UsedPrice > 0 && updatedProduct.UsedPrice < product.LowestPrice;

				if (isLowerPrice || isLowerUsedPrice) NotifyPriceDrop(updatedProduct, isLowerPrice, isLowerUsedPrice);

				await DatabaseManager.UpdateProduct(updatedProduct);
			} catch (SqliteException ex) {
				Debug.WriteLine($"Error updating product: {ex.Message}");
			}
		}

		LoadProducts();
	}

	private static void NotifyPriceDrop(Product product, bool isLowerPrice, bool isLowerUsedPrice) {
		var notificationText = $"{product.Title} has a new lower price!";

		if (isLowerPrice) notificationText     += $"\nNew Price: {product.CurrentPrice:C}";
		if (isLowerUsedPrice) notificationText += $"\nNew Used Price: {product.UsedPrice:C}";

		var notifyIcon = new NotifyIcon {
			Visible         = true,
			Icon            = SystemIcons.Information,
			BalloonTipTitle = "Price Drop Alert",
			BalloonTipText  = notificationText
		};

		notifyIcon.ShowBalloonTip(30000);
	}

	private void LoadProducts() {
		if (Controls["productList"] is not ListView productList) return;
		
		productList.Items.Clear();
	
		foreach (var product in DatabaseManager.GetAllProducts().Result) 
			productList.Items.Add(new ListViewItem([
				product.Title,
				product.CurrentPrice.ToString("C"),
				product.LowestPrice.ToString("C"),
				product.HighestPrice.ToString("C"),
				product.UsedPrice.ToString("C"),
				product.LastUpdated.ToString("g")
			]) {
				Tag = product.Url
			});
	}
}
