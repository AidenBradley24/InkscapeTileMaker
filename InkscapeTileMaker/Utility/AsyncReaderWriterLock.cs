using System.Threading;

namespace InkscapeTileMaker.Utility;

internal sealed class AsyncReaderWriterLock
{
	private readonly SemaphoreSlim _turnstile = new(1, 1);
	private readonly SemaphoreSlim _roomEmpty = new(1, 1);
	private readonly SemaphoreSlim _readerMutex = new(1, 1);

	private int _readerCount;

	public Releaser EnterReadLock()
	{
		_turnstile.Wait();
		_turnstile.Release();

		_readerMutex.Wait();
		try
		{
			if (++_readerCount == 1)
			{
				try
				{
					_roomEmpty.Wait();
				}
				catch
				{
					_readerCount--;
					throw;
				}
			}
		}
		finally
		{
			_readerMutex.Release();
		}

		return new Releaser(this, isWriter: false);
	}

	public async ValueTask<Releaser> EnterReadLockAsync(CancellationToken cancellationToken = default)
	{
		await _turnstile.WaitAsync(cancellationToken).ConfigureAwait(false);
		_turnstile.Release();

		await _readerMutex.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			if (++_readerCount == 1)
			{
				try
				{
					await _roomEmpty.WaitAsync(cancellationToken).ConfigureAwait(false);
				}
				catch
				{
					_readerCount--;
					throw;
				}
			}
		}
		finally
		{
			_readerMutex.Release();
		}

		return new Releaser(this, isWriter: false);
	}

	public Releaser EnterWriteLock()
	{
		_turnstile.Wait();
		try
		{
			_roomEmpty.Wait();
		}
		catch
		{
			_turnstile.Release();
			throw;
		}

		return new Releaser(this, isWriter: true);
	}

	public async ValueTask<Releaser> EnterWriteLockAsync(CancellationToken cancellationToken = default)
	{
		await _turnstile.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			await _roomEmpty.WaitAsync(cancellationToken).ConfigureAwait(false);
		}
		catch
		{
			_turnstile.Release();
			throw;
		}

		return new Releaser(this, isWriter: true);
	}

	private void ExitReadLock()
	{
		_readerMutex.Wait();
		try
		{
			if (--_readerCount == 0)
			{
				_roomEmpty.Release();
			}
		}
		finally
		{
			_readerMutex.Release();
		}
	}

	private void ExitWriteLock()
	{
		_roomEmpty.Release();
		_turnstile.Release();
	}

	internal readonly struct Releaser : IDisposable
	{
		private readonly AsyncReaderWriterLock _owner;
		private readonly bool _isWriter;

		public Releaser(AsyncReaderWriterLock owner, bool isWriter)
		{
			_owner = owner;
			_isWriter = isWriter;
		}

		public void Dispose()
		{
			if (_isWriter)
			{
				_owner.ExitWriteLock();
				return;
			}

			_owner.ExitReadLock();
		}
	}
}