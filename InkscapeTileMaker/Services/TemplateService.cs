using InkscapeTileMaker.Utility;
using InkscapeTileMaker.ViewModels;
using System.Xml.Linq;

namespace InkscapeTileMaker.Services
{
	public class TemplateService : ITemplateService
	{
		public async Task<IEnumerable<TemplateRecord>> GetTemplatesAsync()
		{
			var manifestStream = await FileSystem.Current.OpenAppPackageFileAsync("Templates/manifest.txt");
			var reader = new StreamReader(manifestStream);
			List<TemplateRecord> templates = [];
			do
			{
				string? line = await reader.ReadLineAsync();
				if (line is null) break;
				using var templateStream = await FileSystem.Current.OpenAppPackageFileAsync($"Templates/{line}");
				if (templateStream is null) continue;
				templates.Add(GetTemplateFromSvgStream(templateStream));
			} while (true);
			return templates;
		}

		public async Task<TemplateRecord?> GetTemplateByNameAsync(string name)
		{
			using var templateStream = await FileSystem.Current.OpenAppPackageFileAsync($"Templates/{name}");
			if (templateStream is null) return null;
			return GetTemplateFromSvgStream(templateStream);
		}

		public async Task<Stream> OpenTemplateStreamAsync(TemplateRecord template)
		{
			return await FileSystem.Current.OpenAppPackageFileAsync($"Templates/{template.Name}.svg");
		}
		
		public TemplateRecord GetTemplateFromSvgStream(Stream stream)
		{
			var svg = new InkscapeSvg(stream);
			string path = svg.SvgRoot?.Attribute(InkscapeSvg.sodipodiNamespace + "docname")?.Value ?? throw new Exception("no");
			
			return new TemplateRecord(Path.GetFileNameWithoutExtension(path), path)
			{
				TileSize = svg.GetTileSize(),
				TilesetSize = svg.GetSvgSize()
			};
		}
	}
}
