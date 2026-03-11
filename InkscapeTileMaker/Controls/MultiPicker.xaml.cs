using System.Collections;

namespace InkscapeTileMaker.Controls;

public partial class MultiPicker : ContentView
{
	bool suppressUpdate = false;

	public static readonly BindableProperty ItemsProperty = BindableProperty.Create(
		nameof(Items),
		typeof(IList),
		typeof(MultiPicker),
		defaultBindingMode: BindingMode.OneWay);

	public IList Items
	{
		get => (IList)GetValue(ItemsProperty);
		set => SetValue(ItemsProperty, value);
	}

	public static readonly BindableProperty SelectedItemsProperty = BindableProperty.Create(
		nameof(SelectedItems),
		typeof(IList),
		typeof(MultiPicker),
		defaultBindingMode: BindingMode.TwoWay,
		propertyChanged: OnSelectedItemsPropertyChanged);

	public IList? SelectedItems
	{
		get => (IList?)GetValue(SelectedItemsProperty);
		set => SetValue(SelectedItemsProperty, value);
	}

	private static void OnSelectedItemsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is not MultiPicker picker) return;

		picker.ApplySelectedItemsToCollectionView(newValue as IList);
	}

	private void ApplySelectedItemsToCollectionView(IList? value)
	{
		try
		{
			suppressUpdate = true;

			var cvSelected = m_CollectionView.SelectedItems;
			cvSelected.Clear();

			if (value == null || Items == null)
				return;

			foreach (var selectedValue in value)
			{
				object? match = null;

				// Map VM value to the corresponding item instance in ItemsSource
				foreach (var candidate in Items)
				{
					if (Equals(candidate, selectedValue))
					{
						match = candidate;
						break;
					}
				}

				cvSelected.Add(match ?? selectedValue);
			}
		}
		finally
		{
			suppressUpdate = false;
		}
	}

	public static readonly BindableProperty ItemDisplayBindingProperty = BindableProperty.Create(
		nameof(ItemDisplayBinding),
		typeof(BindingBase),
		typeof(MultiPicker),
		propertyChanged: OnItemDisplayBindingChanged);

	public BindingBase? ItemDisplayBinding
	{
		get => (BindingBase?)GetValue(ItemDisplayBindingProperty);
		set => SetValue(ItemDisplayBindingProperty, value);
	}

	public MultiPicker()
	{
		InitializeComponent();
		UpdateItemTemplate();
	}

	private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (suppressUpdate)
			return;

		var snapshot = new ArrayList(m_CollectionView.SelectedItems.Count);
		foreach (var item in m_CollectionView.SelectedItems)
		{
			snapshot.Add(item);
		}

		SelectedItems = snapshot;
	}

	private static void OnItemDisplayBindingChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is MultiPicker picker)
		{
			picker.UpdateItemTemplate();
		}
	}

	private void UpdateItemTemplate()
	{
		m_CollectionView.ItemTemplate = new DataTemplate(() =>
		{
			var grid = new Grid
			{
				Padding = new Thickness(8),
				HorizontalOptions = LayoutOptions.Fill,
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Star }
				}
			};

			var label = new Label
			{
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.Start,
				TextColor = (Color)Application.Current!.Resources["White"],
				Margin = new Thickness(8, 0)
			};

			BindingBase binding;

			if (ItemDisplayBinding is Binding b)
			{
				binding = new Binding
				{
					Path = b.Path,
					Mode = b.Mode,
					StringFormat = b.StringFormat,
					Converter = b.Converter,
					ConverterParameter = b.ConverterParameter,
					Source = b.Source
				};
			}
			else
			{
				binding = new Binding(".");
			}

			label.SetBinding(Label.TextProperty, binding);

			grid.Add(label);
			Grid.SetColumn(label, 0);

			return grid;
		});
	}
}