using System.Collections.Concurrent;

using HtmlAgilityPack;

namespace BookScraper
{
	internal class Program
	{
		// priva
		static void Main(string[] args)
		{
			var url = "https://books.toscrape.com/";
			var web = new HtmlWeb();

			_workerPool.AddWork(() => VisitNode(web, url, "index.html"));
			while (_workerPool.HasWork)
			{
				_workerPool.GetResult();
			}
			// VisitNode(web, url, "/static/oscar/css/styles.css").Wait();
			// VisitNode(web, url, "catalogue/sharp-objects_997/index.html").Wait();
			// VisitNode(web, url, "media/cache/26/0c/260c6ae16bce31c8f8c95daddd9f4a1c.jpg").Wait();
		}

		private static readonly ConcurrentDictionary<string, byte> VisitedNodes = new ConcurrentDictionary<string, byte>();
		private static WorkerPool<bool> _workerPool = new WorkerPool<bool>(8);

		private static async Task<bool> VisitNode(HtmlWeb web, string baseUrl, string relativePath)
		{
			var nodeAlreadyVisited = !VisitedNodes.TryAdd(relativePath, 0);
			if (nodeAlreadyVisited)
			{
				return false;
			}

			Console.WriteLine("Visiting: " + relativePath);

			var urlBuilder = new UriBuilder(baseUrl) { Path = relativePath };
			if (IsBinary(relativePath))
			{
				await SaveBinary(urlBuilder.Uri);
			}
			else
			{
				var html = await web.LoadFromWebAsync(urlBuilder.Uri.AbsoluteUri);
				SaveHtml(html, relativePath);

				if (relativePath.EndsWith(".html"))
				{
					var allLinks = GetAllLinks(html).Where(IsRelative);

					foreach (var link in allLinks)
					{
						_workerPool.AddWork(() => VisitNode(web, baseUrl, NormalizeLink(relativePath, link)));
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

		private static async Task SaveBinary(Uri uri)
		{
			var folder = ".\\LocalVersion\\" + string.Join('\\', uri.LocalPath.Split('/').Reverse().Skip(1).Reverse());
			Console.WriteLine($"Writing {uri.LocalPath} (binary)");
			Directory.CreateDirectory(folder);

			using (var client = new System.Net.Http.HttpClient()) // WebClient
			{
				// Download the image and write to the file
				var imageBytes = await client.GetByteArrayAsync(uri);
				Console.WriteLine("Bytes: " + imageBytes.Length);
				var fileName = Path.Join(folder, uri.LocalPath.Split('/').Last());
				await File.WriteAllBytesAsync(fileName, imageBytes);
			}
		}

		private static void SaveHtml(HtmlDocument html, string relativePath)
		{
			var folder = GetLocalFolder(relativePath);
			Console.WriteLine($"Writing {relativePath}");
			Directory.CreateDirectory(folder);
			var fileName = relativePath.Split('/').Last();
			File.WriteAllText(Path.Join(folder, fileName), html.DocumentNode.WriteContentTo());
		}

		private static string GetLocalFolder(string relativePath)
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

		private static IEnumerable<string> GetAllNodes(HtmlDocument html, string nodeType, string attributeIdentifier)
		{
			var nodes = html.DocumentNode.SelectNodes($"//{nodeType}[@{attributeIdentifier}]").ToArray();
			return html.DocumentNode.SelectNodes($"//{nodeType}[@{attributeIdentifier}]")
				.Select(node => node.Attributes[attributeIdentifier].Value);
		}
	}
}
