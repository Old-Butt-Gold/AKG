using System.Collections.ObjectModel;
using AKG.Core.Objects;

namespace AKG.UI.MVVM.ViewModels;

public class MaterialListViewModel : BaseViewModel
{
    public ObservableCollection<ObjModel> Models { get; } = [];

    public MaterialListViewModel(IEnumerable<ObjModel> models)
    {
        foreach (var model in models.Where(m => m.Materials != null))
            Models.Add(model);
    }
}