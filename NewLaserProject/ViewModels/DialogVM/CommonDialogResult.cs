namespace NewLaserProject.ViewModels.DialogVM
{
    public class CommonDialogResult<T>
    {
        public bool Success
        {
            get;
            set;
        }
        public T CommonResult
        {
            get;
            set;
        }
    }


}
