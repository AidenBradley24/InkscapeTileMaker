using Microsoft.Maui.Controls;
using System.Windows.Input;

namespace InkscapeTileMaker.Controls;

public partial class ToolIcon : ContentView
{
	public static readonly BindableProperty ToolNameProperty =
		BindableProperty.Create(nameof(ToolName), typeof(string), typeof(ToolIcon), string.Empty);

	public static readonly BindableProperty IsSelectedProperty =
		BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(ToolIcon), false);

	public static readonly BindableProperty SourceProperty =
		BindableProperty.Create(nameof(Source), typeof(ImageSource), typeof(ToolIcon));

	public static readonly BindableProperty CommandProperty =
		BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ToolIcon));

	public static readonly BindableProperty CommandParameterProperty =
		BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(ToolIcon));

	public static readonly BindableProperty SelectedOutlineStrokeProperty =
		BindableProperty.Create(nameof(SelectedOutlineStroke), typeof(Color), typeof(ToolIcon), Brush.White.Color);

	public static readonly BindableProperty UnselectedOutlineStrokeProperty =
		BindableProperty.Create(nameof(UnselectedOutlineStroke), typeof(Color), typeof(ToolIcon), Brush.Transparent.Color);

	public string ToolName
	{
		get => (string)GetValue(ToolNameProperty);
		set
		{
			SetValue(ToolNameProperty, value);
			OnPropertyChanged(nameof(ToolName));
		}
	}

	public bool IsSelected
	{
		get => (bool)GetValue(IsSelectedProperty);
		set
		{
			SetValue(IsSelectedProperty, value);
		}
	}

	public ImageSource Source
	{
		get => (ImageSource)GetValue(SourceProperty);
		set
		{
			SetValue(SourceProperty, value);
		}
	}

	public ICommand Command
	{
		get => (ICommand)GetValue(CommandProperty);
		set
		{
			SetValue(CommandProperty, value);
		}
	}

	public object CommandParameter
	{
		get => GetValue(CommandParameterProperty);
		set
		{
			SetValue(CommandParameterProperty, value);
		}
	}

	public Color SelectedOutlineStroke
	{
		get => (Color)GetValue(SelectedOutlineStrokeProperty);
		set
		{
			SetValue(SelectedOutlineStrokeProperty, value);
		}
	}

	public Color UnselectedOutlineStroke
	{
		get => (Color)GetValue(UnselectedOutlineStrokeProperty);
		set
		{
			SetValue(UnselectedOutlineStrokeProperty, value);
		}
	}


	public ToolIcon()
	{
		InitializeComponent();
	}
}