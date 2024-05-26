using MachineControlsLibrary.CommonDialog;

namespace NewLaserProject.ViewModels.DialogVM
{
    internal class DownloadDbVM : CommonDialogResultable<DownloadDbVM>
    {
        public string DatabasePath { get; set; }
        public bool RewriteDatabase { get; set; } = false;
        public bool MergeChangeOnNew { get; set; }
        public bool MergeNotSave { get; set; }
        public bool MergeSaveBoth { get; set; } = true;

        public override void SetResult() => SetResult(this);
    }
}
