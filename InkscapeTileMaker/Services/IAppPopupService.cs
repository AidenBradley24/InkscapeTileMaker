namespace InkscapeTileMaker.Services
{
	public interface IAppPopupService
	{
		public Task ShowTextAsync(string text);

		public Task<bool> ShowConfirmationAsync(string message);
	}
}
