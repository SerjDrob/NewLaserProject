using NewLaserProject.Data.Models.DTOs;

namespace NewLaserProject.ViewModels.DialogVM
{
    public class MaterialVM : CommonDialogResultable<MaterialDTO>
    {
        public MaterialDTO MaterialDTO
        {
            get;
            set;
        }
        public override void SetResult() => SetResult(MaterialDTO);
    }
}
