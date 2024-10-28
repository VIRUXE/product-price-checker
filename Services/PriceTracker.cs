using ProductPriceChecker.Models;

using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace ProductPriceChecker.Services;

public static class PriceTracker {
	private static readonly HttpClient client = new() {
		DefaultRequestHeaders = {
			{ "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36" }
		}
	};

	public static async Task<Product> TrackProduct(string url) {
		var html = await client.GetStringAsync(url);
		var doc  = new HtmlDocument();

		doc.LoadHtml(html);

		var title     = doc.DocumentNode.SelectSingleNode("//span[@id='productTitle']")?.InnerText.Trim() ?? throw new Exception("Could not find product title");
		var priceNode = doc.DocumentNode.SelectSingleNode("//span[@class='a-price-whole']") ?? throw new Exception("Could not find product price");
		var priceText = priceNode.InnerText.Replace(".", "").Replace(",", "").Trim();

		if (!decimal.TryParse(priceText, out decimal currentPrice)) throw new Exception("Could not parse price");

		var usedPriceNode = doc.DocumentNode.SelectSingleNode("//span[@class='a-price-used']"); // Adjust the XPath as needed
		var usedPriceText = usedPriceNode?.InnerText.Replace(".", "").Replace(",", "").Trim();
		decimal.TryParse(usedPriceText, out decimal usedPrice);

		return new() {
			Url          = url,
			Title        = title,
			CurrentPrice = currentPrice,
			UsedPrice    = usedPrice,
			LastUpdated  = DateTime.Now
		};
	}
}