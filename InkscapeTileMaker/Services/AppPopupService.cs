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
		private readonly Lock _popupSync = new();
		private readonly HashSet<IAppPopupViewModel> _activePopups = [];

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

		private bool TryRegisterPopup(IAppPopupViewModel popupViewModel)
		{
			lock (_popupSync)
			{
				if (Volatile.Read(ref _disposeState) == DISPOSAL)
				{
					return false;
				}

				return _activePopups.Add(popupViewModel);
			}
		}

		private void UnregisterPopup(IAppPopupViewModel popupViewModel)
		{
			lock (_popupSync)
			{
				_activePopups.Remove(popupViewModel);
			}
		}

		private IAppPopupViewModel[] GetActivePopupsSnapshot()
		{
			lock (_popupSync)
			{
				return [.. _activePopups];
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

			var vm = new ConfirmationPopupViewModel()
			{
				Title = title,
				Message = message,
				ConfirmButtonText = confirmText,
				CancelButtonText = cancelText
			};

			if (!TryRegisterPopup(vm))
			{
				EndOperation();
				return false;
			}

			try
			{
				await _windowProvider.NavPage.Dispatcher.DispatchAsync(async () =>
				{
					var view = new ConfirmationPopup(vm);
					var opts = new PopupOptions()
					{
						CanBeDismissedByTappingOutsideOfPopup = false
					};

					await PopupExtensions.ShowPopupAsync(_windowProvider.NavPage, view, opts, _disposeCts.Token);
				});

				return vm.Result;
			}
			catch (OperationCanceledException)
			{
				return false;
			}
			finally
			{
				await SafeClosePopupAsync(vm).ConfigureAwait(false);
				UnregisterPopup(vm);
				EndOperation();
			}
		}

		public async Task ShowTextAsync(string text)
		{
			if (!TryBeginOperation()) return;

			var vm = new TextPopupViewModel()
			{
				Text = text
			};

			if (!TryRegisterPopup(vm))
			{
				EndOperation();
				return;
			}

			try
			{
				await _windowProvider.NavPage.Dispatcher.DispatchAsync(async () =>
				{
					var view = new TextPopup(vm);
					await PopupExtensions.ShowPopupAsync(_windowProvider.NavPage, view, PopupOptions.Empty, _disposeCts.Token);
				});
			}
			catch (OperationCanceledException) { }
			finally
			{
				await SafeClosePopupAsync(vm).ConfigureAwait(false);
				UnregisterPopup(vm);
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

				if (!TryRegisterPopup(vm))
				{
					if (semaphoreHeld)
					{
						_progressSemaphore.Release();
					}

					throw new OperationCanceledException();
				}

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
						UnregisterPopup(vm);

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

			foreach (var popupViewModel in GetActivePopupsSnapshot())
			{
				await SafeClosePopupAsync(popupViewModel).ConfigureAwait(false);
			}

			if (Volatile.Read(ref _activeOperationCount) != 0)
			{
				await _disposeCompletion.Task.ConfigureAwait(false);
			}

			_disposeCts.Dispose();
		}
	}
}
