using MachineControlsLibrary.CommonDialog;

namespace NewLaserProject.ViewModels.DialogVM
{
    public class ClusterVM:CommonDialogResultable<ClusterVM>
    {
        public int XParts { get; set; }
        public int YParts { get; set; }
        public bool Enable { get; set; }
        public override void SetResult() => SetResult(this);
        
    }
}
