using HtmlAgilityPack;

namespace BookScraper
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var url = "https://books.toscrape.com/";
			var web = new HtmlWeb();

			VisitNode(web, url, "index.html").Wait();
		}

		private static async Task VisitNode(HtmlWeb web, string baseUrl, string relativePath)
		{
			var urlBuilder = new UriBuilder(baseUrl) { Path = relativePath };
			var html = await web.LoadFromWebAsync(urlBuilder.Uri.AbsoluteUri);
			SaveHtml(html, relativePath);
			var allLinks = GetAllLinks(html);
			foreach (var link in allLinks)
			{
				Console.WriteLine(link);
			}
		}

		private static void SaveHtml(HtmlDocument html, string relativePath)
		{
			var folder = ".\\LocalVersion\\" + string.Join('\\', relativePath.Split('/').Reverse().Skip(1).Reverse());
			Directory.CreateDirectory(folder);
			File.WriteAllText(Path.Join(folder, relativePath.Split('/').Last()), html.DocumentNode.WriteContentTo());
		}

		private static IEnumerable<string> GetAllLinks(HtmlDocument html)
		{
			var linkCarryingNodes = new[]
			{
				("a", "href"),
				("img", "src"),
				("link", "href"),
				("script", "src")
			};


			return linkCarryingNodes.SelectMany(nodeSpec => GetAllNodes(html, nodeSpec.Item1, nodeSpec.Item2));
		}

		private static IEnumerable<string> GetAllNodes(HtmlDocument html, string nodeType, string attributeIdentifier)
		{
			return html.DocumentNode.SelectNodes($"//{nodeType}[@{attributeIdentifier}]")
				.Select(node => node.Attributes[attributeIdentifier].Value);
		}
	}
}
