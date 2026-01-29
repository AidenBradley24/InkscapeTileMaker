using InkscapeTileMaker.ViewModels;

namespace InkscapeTileMaker.Services
{
	public interface ITemplateService
	{
		public Task<IEnumerable<TemplateRecord>> GetTemplatesAsync();

		public Task<TemplateRecord?> GetTemplateByNameAsync(string name);

		public Task<Stream> OpenTemplateStreamAsync(TemplateRecord template);
	}
}
