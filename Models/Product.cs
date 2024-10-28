namespace ProductPriceChecker.Models;

public record Product {
	public required string Url { get; init; }
	public required string Title { get; init; }
	public decimal CurrentPrice { get; init; }
	public decimal LowestPrice { get; init; }
	public decimal HighestPrice { get; init; }
	public decimal UsedPrice { get; init; }
	public DateTime LastUpdated { get; init; }
}