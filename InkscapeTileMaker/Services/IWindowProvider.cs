namespace InkscapeTileMaker.Services
{
	public interface IWindowProvider
	{
		public IAppPopupService PopupService { get; }
		public NavigationPage NavPage { get; }
		public Page CurrentPage { get; }
		public void CloseWindow();
	}
}
