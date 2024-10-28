using Microsoft.Data.Sqlite;

using ProductPriceChecker.Models;

namespace ProductPriceChecker.Services;

public static class DatabaseManager {
	private const string ConnectionString = "Data Source=products.db";

	public static async Task InitializeDatabase() {
		await using var conn = new SqliteConnection(ConnectionString);
		await conn.OpenAsync();

		var command = conn.CreateCommand();
		command.CommandText = """
			CREATE TABLE IF NOT EXISTS Products (
				Url TEXT PRIMARY KEY,
				Title TEXT,
				CurrentPrice DECIMAL,
				LowestPrice DECIMAL,
				HighestPrice DECIMAL,
				UsedPrice DECIMAL,
				LastUpdated DATETIME
			)
			""";
		await command.ExecuteNonQueryAsync();
	}

	public static async Task AddProduct(Product product) {
		await using var conn = new SqliteConnection(ConnectionString);
		await conn.OpenAsync();

		var command = conn.CreateCommand();
		command.CommandText = """
			INSERT OR REPLACE INTO Products (Url, Title, CurrentPrice, LowestPrice, HighestPrice, UsedPrice, LastUpdated)
			VALUES (@url, @title, @currentPrice, @lowestPrice, @highestPrice, @usedPrice, @updated)
		""";
		command.Parameters.AddWithValue("@url", product.Url);
		command.Parameters.AddWithValue("@title", product.Title);
		command.Parameters.AddWithValue("@currentPrice", product.CurrentPrice);
		command.Parameters.AddWithValue("@lowestPrice", product.LowestPrice);
		command.Parameters.AddWithValue("@highestPrice", product.HighestPrice);
		command.Parameters.AddWithValue("@usedPrice", product.UsedPrice);
		command.Parameters.AddWithValue("@updated", product.LastUpdated);
		
		await command.ExecuteNonQueryAsync();
	}

	public static async Task UpdateProduct(Product product) {
		await using var conn = new SqliteConnection(ConnectionString);
		await conn.OpenAsync();

		var command = conn.CreateCommand();
		command.CommandText = """
			UPDATE Products 
			SET Title = @title,
				CurrentPrice = @currentPrice,
				LowestPrice  = CASE WHEN @currentPrice < LowestPrice THEN @currentPrice ELSE LowestPrice END,
				HighestPrice = CASE WHEN @currentPrice > HighestPrice THEN @currentPrice ELSE HighestPrice END,
				UsedPrice    = CASE WHEN @currentPrice < UsedPrice THEN @currentPrice ELSE UsedPrice END,
				LastUpdated  = @updated
			WHERE Url = @url
		""";
		command.Parameters.AddWithValue("@url", product.Url);
		command.Parameters.AddWithValue("@title", product.Title);
		command.Parameters.AddWithValue("@currentPrice", product.CurrentPrice);
		command.Parameters.AddWithValue("@usedPrice", product.UsedPrice);
		command.Parameters.AddWithValue("@updated", product.LastUpdated);

		await command.ExecuteNonQueryAsync();
	}

	public static async Task RemoveProduct(string url) {
		await using var conn = new SqliteConnection(ConnectionString);
		await conn.OpenAsync();

		var command = conn.CreateCommand();
		command.CommandText = "DELETE FROM Products WHERE Url = @url";
		command.Parameters.AddWithValue("@url", url);

		await command.ExecuteNonQueryAsync();
	}

	public static async Task<List<Product>> GetAllProducts() {
		var products = new List<Product>();

		await using var conn = new SqliteConnection(ConnectionString);
		await conn.OpenAsync();

		var command = conn.CreateCommand();
		command.CommandText = "SELECT * FROM Products";

		await using var reader = await command.ExecuteReaderAsync();
		while (await reader.ReadAsync()) {
			products.Add(new() {
				Url          = reader.GetString(0),
				Title        = reader.GetString(1),
				CurrentPrice = reader.GetDecimal(2),
				LowestPrice  = reader.GetDecimal(3),
				HighestPrice = reader.GetDecimal(4),
				UsedPrice    = reader.GetDecimal(5),
				LastUpdated  = reader.GetDateTime(6)
			});
		}

		return products;
	}
}