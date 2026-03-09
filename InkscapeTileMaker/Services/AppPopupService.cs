using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using InkscapeTileMaker.ViewModels.Popups;
using InkscapeTileMaker.Views.Popups;

namespace InkscapeTileMaker.Services
{
	public class AppPopupService : IAppPopupService
	{
		private readonly IWindowProvider _windowProvider;
		private readonly SemaphoreSlim _progressSemaphore;

		public AppPopupService(IWindowProvider windowProvider)
		{
			_windowProvider = windowProvider;
			_progressSemaphore = new SemaphoreSlim(1);
		}

		public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
		{
			var vm = new ConfirmationPopupViewModel()
			{
				Title = title,
				Message = message,
				ConfirmButtonText = confirmText,
				CancelButtonText = cancelText
			};

			var view = new ConfirmationPopup(vm);
			var opts = new PopupOptions()
			{
				CanBeDismissedByTappingOutsideOfPopup = false
			};

			await _windowProvider.NavPage.Dispatcher.DispatchAsync(async () =>
			{
				await PopupExtensions.ShowPopupAsync(_windowProvider.NavPage, view, opts);
			});
			return vm.Result;
		}

		public async Task ShowTextAsync(string text)
		{
			var vm = new TextPopupViewModel()
			{
				Text = text
			};

			var view = new TextPopup(vm);
			await _windowProvider.NavPage.Dispatcher.DispatchAsync(async () =>
			{
				await PopupExtensions.ShowPopupAsync(_windowProvider.NavPage, view, PopupOptions.Empty);
			});
		}

		public async Task ShowProgressOnTaskAsync(string message, bool isIndeterminate, Func<IProgress<double>, Task> progressAction)
		{
			await _progressSemaphore.WaitAsync().ConfigureAwait(false);
			var vm = new ProgressPopupViewModel()
			{
				Message = message,
				IsIndeterminate = isIndeterminate
			};

			var view = new ProgressPopup(vm);
			var opts = new PopupOptions()
			{
				CanBeDismissedByTappingOutsideOfPopup = false
			};

			Task popupTask = _windowProvider.NavPage.Dispatcher.DispatchAsync(async () =>
			{
				await PopupExtensions
					.ShowPopupAsync(_windowProvider.NavPage, view, opts)
					.ConfigureAwait(false);
			});

			try
			{
				await progressAction.Invoke(vm.ProgressReporter)
					.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				await _windowProvider.NavPage.Dispatcher.DispatchAsync(() => _windowProvider.NavPage.DisplayAlertAsync("Error", $"An error occurred while performing the operation.\n\n{ex.Message}", "OK"));
			}
			finally
			{
				await _windowProvider.NavPage.Dispatcher
					.DispatchAsync(() => view.ClosePopup())
					.ConfigureAwait(false);

				await popupTask.ConfigureAwait(false);

				_progressSemaphore.Release();
			}
		}
	}
}
