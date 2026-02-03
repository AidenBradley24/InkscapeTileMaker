using CommunityToolkit.Mvvm.ComponentModel;

namespace InkscapeTileMaker.ViewModels
{
	public partial class TextPopupViewModel : ObservableObject
	{
		[ObservableProperty]
		public partial string Text { get; set; } = "(text)";
	}
}
