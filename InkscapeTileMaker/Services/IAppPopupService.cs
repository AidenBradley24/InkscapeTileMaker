namespace InkscapeTileMaker.Services
{
	public interface IAppPopupService : IAsyncDisposable
	{
		public Task ShowTextAsync(string text);

		public Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel");

		public Task ShowProgressOnTaskAsync(string message, bool isIndeterminate, Func<IProgress<double>?, Task> progressAction);
	}

	public interface IAppPopupViewModel
	{
		public Task ClosePopup();
	}
}
