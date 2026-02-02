namespace InkscapeTileMaker.Services
{
	public interface IAppPopupService
	{
		public Task ShowText(string text);

		public Task<bool> ShowConfirmation(string message);
	}
}
