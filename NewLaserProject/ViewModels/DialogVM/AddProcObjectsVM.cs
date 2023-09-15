using System.Collections.ObjectModel;
using NewLaserProject.Data.Models;

namespace NewLaserProject.ViewModels.DialogVM
{
    internal class AddProcObjectsVM : CommonDialogResultable<ObjectForProcessing>
    {
        public ObjectForProcessing ObjectForProcessing
        {
            get;
            set;
        }
        public Material Material
        {
            get;
            set;
        }
        public ObservableCollection<Material> Materials
        {
            get;
            set;
        }
        public override void SetResult() => SetResult(ObjectForProcessing);
    }

}
