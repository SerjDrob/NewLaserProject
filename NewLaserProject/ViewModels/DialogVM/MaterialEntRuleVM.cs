using NewLaserProject.Data.Models;

namespace NewLaserProject.ViewModels.DialogVM
{
    public class MaterialEntRuleVM : CommonDialogResultable<MaterialEntRule>
    {
        public MaterialEntRule MaterialEntRule
        {
            get;
            set;
        }
        public override void SetResult() => SetResult(MaterialEntRule);
    }
}
