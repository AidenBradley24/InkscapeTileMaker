using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using InkscapeTileMaker.ViewModels.Popups;
using InkscapeTileMaker.Views.Popups;

namespace InkscapeTileMaker.Services
{
	public sealed class AppPopupService : IAppPopupService
	{
		private readonly IWindowProvider _windowProvider;
		private readonly SemaphoreSlim _progressSemaphore;

		private int _activeOperationCount;

		const int ACTIVE = 1, DISPOSAL = 2;
		private readonly CancellationTokenSource _disposeCts = new();
		private int _disposeState = ACTIVE;
		private readonly TaskCompletionSource<object?> _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public AppPopupService(IWindowProvider windowProvider)
		{
			_windowProvider = windowProvider;
			_progressSemaphore = new SemaphoreSlim(1);
		}

		private bool TryBeginOperation()
		{
			if (Volatile.Read(ref _disposeState) == DISPOSAL)
			{
				return false;
			}

			Interlocked.Increment(ref _activeOperationCount);

			if (Volatile.Read(ref _disposeState) == DISPOSAL)
			{
				EndOperation();
				return false;
			}

			return true;
		}

		private void EndOperation()
		{
			if (Interlocked.Decrement(ref _activeOperationCount) == 0 &&
				Volatile.Read(ref _disposeState) == DISPOSAL)
			{
				_disposeCompletion.TrySetResult(null);
			}
		}

		private static async Task SafeClosePopupAsync(IAppPopupViewModel popupViewModel)
		{
			try
			{
				await popupViewModel.ClosePopup().ConfigureAwait(false);
			}
			catch (OperationCanceledException) { }
			catch (ObjectDisposedException) { }
			catch (InvalidOperationException) { }
		}

		public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
		{
			if (!TryBeginOperation()) return false;

			try
			{
				bool result = await _windowProvider.NavPage.DisplayAlertAsync(title, message, confirmText, cancelText, FlowDirection.LeftToRight);
				return result;
			}
			finally
			{
				EndOperation();
			}
		}

		public async Task ShowTextAsync(string text, string title = "")
		{
			if (!TryBeginOperation()) return;

			try
			{
				await _windowProvider.NavPage.DisplayAlertAsync(title, text, "OK");
			}
			finally
			{
				EndOperation();
			}
		}

		public async Task ShowProgressOnTaskAsync(string message, bool isIndeterminate, Func<IProgress<double>?, Task> progressAction)
		{
			if (!TryBeginOperation())
			{
				throw new OperationCanceledException();
			}

			try
			{
				bool semaphoreHeld = false;
				await _progressSemaphore.WaitAsync(_disposeCts.Token).ConfigureAwait(false);
				semaphoreHeld = true;
				_disposeCts.Token.ThrowIfCancellationRequested();

				var vm = new ProgressPopupViewModel()
				{
					Message = message,
					IsIndeterminate = isIndeterminate
				};

				Task popupTask = _windowProvider.NavPage.Dispatcher.DispatchAsync(async () =>
				{
					var view = new ProgressPopup(vm);
					var opts = new PopupOptions()
					{
						CanBeDismissedByTappingOutsideOfPopup = false
					};

					await PopupExtensions
						.ShowPopupAsync(_windowProvider.NavPage, view, opts, _disposeCts.Token)
						.ConfigureAwait(false);
				});

				try
				{
					await progressAction.Invoke(vm.ProgressReporter)
						.ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (OperationCanceledException) { }
				catch (Exception ex)
				{
					await _windowProvider.NavPage.Dispatcher.DispatchAsync(() => _windowProvider.NavPage.DisplayAlertAsync("Error", $"An error occurred while performing the operation.\n\n{ex.Message}", "OK"));
				}
				finally
				{
					await SafeClosePopupAsync(vm).ConfigureAwait(false);

					try
					{
						await popupTask.ConfigureAwait(false);
					}
					catch (OperationCanceledException) { }
					finally
					{
						if (semaphoreHeld)
						{
							_progressSemaphore.Release();
						}
					}
				}
			}
			catch (OperationCanceledException) { }
			finally
			{
				EndOperation();
			}
		}

		public async ValueTask DisposeAsync()
		{
			if (Interlocked.Exchange(ref _disposeState, DISPOSAL) != ACTIVE)
			{
				return;
			}

			_disposeCts.Cancel();

			if (Volatile.Read(ref _activeOperationCount) != 0)
			{
				await _disposeCompletion.Task.ConfigureAwait(false);
			}

			await Task.Delay(500).ConfigureAwait(false);

			_disposeCts.Dispose();
		}
	}
}
