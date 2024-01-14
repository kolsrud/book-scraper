using HtmlAgilityPack;

namespace BookScraper
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var url = "https://books.toscrape.com/";
			var web = new HtmlWeb();
			var html = web.LoadFromWebAsync(url).Result;
			Console.WriteLine(html.DocumentNode.WriteContentTo());
		}
	}
}
