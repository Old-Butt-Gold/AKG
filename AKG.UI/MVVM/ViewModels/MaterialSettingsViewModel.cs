using System.Windows.Input;
using AKG.Core.Parser;
using AKG.UI.MVVM.Commands;

namespace AKG.UI.MVVM.ViewModels
{
    public class MaterialSettingsViewModel
    {
        public Material Material { get; set; }

        public ICommand SaveCommand { get; }

        public MaterialSettingsViewModel(Material material)
        {
            Material = material;
            SaveCommand = new RelayCommand(Save);
        }

        private void Save(object? parameter)
        {
            // Сохраняем изменения в материале
            // Здесь можно добавить логику для обновления сцены или других действий
        }
    }
}