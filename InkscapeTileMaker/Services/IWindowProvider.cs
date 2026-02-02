namespace InkscapeTileMaker.Services
{
	public interface IWindowProvider
	{
		public IAppPopupService PopupService { get; }
		public INavigation Navigation { get; }
		public Page CurrentPage { get; }
		public void CloseWindow();
	}
}
