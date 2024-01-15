using System.Collections.Concurrent;

using HtmlAgilityPack;

namespace BookScraper
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var url = "https://books.toscrape.com/";

			using (var client = new HttpClient())
			{
				var web = new HtmlWeb();


				_workerPool.AddWork(() => VisitNode(web, client, url, "index.html"));
				var visited = 0;
				while (_workerPool.HasWork)
				{
					_workerPool.GetResult();
					visited++;
					if (visited % 100 == 0)
						Console.WriteLine(
							$"Visits completed: {visited}, Remaining nodes to visit: {_workerPool.CurrentLoad}");
				}
				Console.WriteLine($"Visits completed: {visited}, Remaining nodes to visit: {_workerPool.CurrentLoad}");
			}

		}

		private static readonly ConcurrentDictionary<string, byte> VisitedNodes = new ConcurrentDictionary<string, byte>();
		private static readonly WorkerPool _workerPool = new WorkerPool(8);

		private static async Task<bool> VisitNode(HtmlWeb web, HttpClient client, string baseUrl,
			string relativePath)
		{
			var nodeAlreadyVisited = !VisitedNodes.TryAdd(relativePath, 0);
			if (nodeAlreadyVisited)
			{
				return false;
			}

			var urlBuilder = new UriBuilder(baseUrl) { Path = relativePath };
			if (IsBinary(relativePath))
			{
				await SaveBinary(client, urlBuilder.Uri);
			}
			else
			{
				var html = await web.LoadFromWebAsync(urlBuilder.Uri.AbsoluteUri);
				SaveHtml(html, relativePath);

				if (relativePath.EndsWith(".html"))
				{
					var allLinks = GetAllLinks(html).Where(IsRelative);

					foreach (var link in allLinks.Except(VisitedNodes.Keys))
					{
						_workerPool.AddWork(() => VisitNode(web, client, baseUrl, NormalizeLink(relativePath, link)));
					}
				}
			}

			return true;
		}

		private static bool IsBinary(string relativePath)
		{
			var fileEnding = relativePath.Split('.').Last();
			var binaryFiles = new string[] { "jpg", "ico" };
			return binaryFiles.Contains(fileEnding);
		}

		private static string NormalizeLink(string relativePath, string link)
		{
			var linkSegments = link.Split("/");
			var upDirs= linkSegments.TakeWhile(segment => segment == "..").Count();
			if (upDirs == 0)
				return link;

			var relSegments = relativePath.Split('/').Reverse().Skip(upDirs+1).Reverse();
			return string.Join('/', relSegments.Concat(linkSegments.Skip(upDirs)));
		}

		private static bool IsRelative(string url)
		{
			return !Uri.TryCreate(url, UriKind.Absolute, out _);
		}

		private static async Task SaveBinary(HttpClient client, Uri uri)
		{
			var folder = ".\\LocalVersion\\" + string.Join('\\', uri.LocalPath.Split('/').Reverse().Skip(1).Reverse());
			Directory.CreateDirectory(folder);

			// Download the image and write to the file
			var imageBytes = await client.GetByteArrayAsync(uri);
			var fileName = Path.Join(folder, uri.LocalPath.Split('/').Last());
			await File.WriteAllBytesAsync(fileName, imageBytes);
		}

		private static void SaveHtml(HtmlDocument html, string relativePath)
		{
			var folder = GetLocalFolderName(relativePath);
			Directory.CreateDirectory(folder);
			var fileName = relativePath.Split('/').Last();
			File.WriteAllText(Path.Join(folder, fileName), html.DocumentNode.WriteContentTo());
		}

		private static string GetLocalFolderName(string relativePath)
		{
			return ".\\LocalVersion\\" + string.Join('\\', relativePath.Split('/').Reverse().Skip(1).Reverse());
		}

		private static IEnumerable<string> GetAllLinks(HtmlDocument html)
		{
			var linkCarryingNodes = new[]
			{
				("img", "src"),
				("a", "href"),
				("link", "href"),
				("script", "src")
			};
			
			return linkCarryingNodes.SelectMany(nodeSpec => GetAllNodes(html, nodeSpec.Item1, nodeSpec.Item2));
		}

		// Get the list of all nodes referred to by an html document.
		private static IEnumerable<string> GetAllNodes(HtmlDocument html, string nodeType, string attributeIdentifier)
		{
			var nodes = html.DocumentNode.SelectNodes($"//{nodeType}[@{attributeIdentifier}]").ToArray();
			return html.DocumentNode.SelectNodes($"//{nodeType}[@{attributeIdentifier}]")
				.Select(node => node.Attributes[attributeIdentifier].Value);
		}
	}
}
