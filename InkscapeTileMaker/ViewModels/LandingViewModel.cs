using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkscapeTileMaker.Services;

namespace InkscapeTileMaker.ViewModels
{
    public partial class LandingViewModel : ObservableObject
    {
        private readonly IWindowService _windowService;

        public LandingViewModel(IWindowService windowService)
        {
            _windowService = windowService;
        }

        [RelayCommand]
        public async Task OpenDesigner()
        {
            await Task.Yield();
            _windowService.OpenDesignerWindow();
        }

        [RelayCommand]
        public async Task CreateNewDesign()
        {
            await OpenDesigner();
        }
    }
}
