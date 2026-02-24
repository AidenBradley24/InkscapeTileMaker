using System.Collections;
using System.ComponentModel;

namespace InkscapeTileMaker.Behaviors;

public partial class NotifyDataErrorBehavior : Behavior<View>
{
	private INotifyDataErrorInfo? _notifyDataErrorInfo;
	private View? _associatedObject;

	public static readonly BindableProperty PropertyNameProperty =
		BindableProperty.Create(
			nameof(PropertyName),
			typeof(string),
			typeof(NotifyDataErrorBehavior),
			propertyChanged: OnPropertyNameChanged);

	public string? PropertyName
	{
		get => (string?)GetValue(PropertyNameProperty);
		set => SetValue(PropertyNameProperty, value);
	}

	public static readonly BindableProperty ErrorLabelProperty =
		BindableProperty.Create(
			nameof(ErrorLabel),
			typeof(Label),
			typeof(NotifyDataErrorBehavior),
			default(Label),
			propertyChanged: OnErrorLabelChanged);

	public Label? ErrorLabel
	{
		get => (Label?)GetValue(ErrorLabelProperty);
		set => SetValue(ErrorLabelProperty, value);
	}

	protected override void OnAttachedTo(View bindable)
	{
		base.OnAttachedTo(bindable);

		_associatedObject = bindable;
		bindable.BindingContextChanged += OnBindingContextChanged;

		AttachToErrors(bindable.BindingContext);
	}

	protected override void OnDetachingFrom(View bindable)
	{
		base.OnDetachingFrom(bindable);

		bindable.BindingContextChanged -= OnBindingContextChanged;
		DetachFromErrors();
		_associatedObject = null;
	}

	private void OnBindingContextChanged(object? sender, EventArgs e)
	{
		if (sender is not BindableObject bo)
			return;

		DetachFromErrors();
		AttachToErrors(bo.BindingContext);
	}

	private void AttachToErrors(object? bindingContext)
	{
		_notifyDataErrorInfo = bindingContext as INotifyDataErrorInfo;
		if (_notifyDataErrorInfo != null)
		{
			_notifyDataErrorInfo.ErrorsChanged += OnErrorsChanged;
			UpdateError();
		}
		else
		{
			ClearError();
		}
	}

	private void DetachFromErrors()
	{
		if (_notifyDataErrorInfo != null)
		{
			_notifyDataErrorInfo.ErrorsChanged -= OnErrorsChanged;
			_notifyDataErrorInfo = null;
		}
	}

	private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
	{
		if (string.IsNullOrEmpty(PropertyName) ||
			!string.Equals(e.PropertyName, PropertyName, StringComparison.Ordinal))
		{
			return;
		}

		UpdateError();
	}

	private static void OnPropertyNameChanged(BindableObject bindable, object? oldValue, object? newValue)
	{
		var behavior = (NotifyDataErrorBehavior)bindable;
		behavior.UpdateError();
	}

	private static void OnErrorLabelChanged(BindableObject bindable, object? oldValue, object? newValue)
	{
		var behavior = (NotifyDataErrorBehavior)bindable;
		behavior.UpdateError();
	}

	private void UpdateError()
	{
		if (_notifyDataErrorInfo == null ||
			string.IsNullOrEmpty(PropertyName) ||
			ErrorLabel == null)
		{
			ClearError();
			return;
		}

		var errors = _notifyDataErrorInfo.GetErrors(PropertyName);
		string? firstError = null;

		if (errors is IEnumerable enumerable)
		{
			foreach (var e in enumerable)
			{
				if (e is string s)
				{
					firstError = s;
					break;
				}

				if (e != null)
				{
					firstError = e.ToString();
					break;
				}
			}
		}

		if (string.IsNullOrEmpty(firstError))
		{
			ClearError();
		}
		else
		{
			ErrorLabel.Text = firstError;
			ErrorLabel.IsVisible = true;
		}
	}

	private void ClearError()
	{
		if (ErrorLabel == null)
			return;

		ErrorLabel.Text = string.Empty;
		ErrorLabel.IsVisible = false;
	}
}