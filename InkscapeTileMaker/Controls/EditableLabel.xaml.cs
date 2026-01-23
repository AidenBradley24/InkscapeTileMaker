using System.Runtime.CompilerServices;

namespace InkscapeTileMaker.Controls;

public partial class EditableLabel : ContentView
{
	public static readonly BindableProperty TextProperty =
		BindableProperty.Create(nameof(Text), typeof(string), typeof(EditableLabel), string.Empty);

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set
		{
			SetValue(TextProperty, value);
			OnPropertyChanged(nameof(Text));
		}
	}

	private string oldValue = "";

	public static readonly BindableProperty TextPlaceholderProperty =
		BindableProperty.Create(nameof(TextPlaceholder), typeof(string), typeof(EditableLabel), "Enter text...");

	public string TextPlaceholder
	{
		get => (string)GetValue(TextPlaceholderProperty);
		set
		{
			SetValue(TextPlaceholderProperty, value);
			OnPropertyChanged(nameof(TextPlaceholder));
		}
	}

	private bool _isEditing = false;
	public bool IsEditing
	{
		get => _isEditing;
		set
		{
			_isEditing = value;
			OnPropertyChanged(nameof(IsEditing));
		}
	}

	public EditableLabel()
	{
		InitializeComponent();
	}

	private void StartEditing()
	{
		if (IsEditing) return;
		oldValue = Text;
		IsEditing = true;
		TextEntry.Focus();
	}

	private void FinishEditing()
	{
		IsEditing = false;
	}

	private void TextEntry_Unfocused(object sender, FocusEventArgs e)
	{
		FinishEditing();
	}

	private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
	{
		StartEditing();
	}

	private void TextEntry_Completed(object sender, EventArgs e)
	{
		FinishEditing();
	}

	protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		base.OnPropertyChanged(propertyName);

		if (propertyName == nameof(IsVisible))
		{
			OnVisibilityChanged(this, EventArgs.Empty);
		}
	}

	private void OnVisibilityChanged(object sender, EventArgs e)
	{
		if (!IsVisible && IsEditing)
		{
			Text = oldValue;
			IsEditing = false;
		}
	}
}