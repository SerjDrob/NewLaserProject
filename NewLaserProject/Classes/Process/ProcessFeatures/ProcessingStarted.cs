namespace NewLaserProject.Classes.Process.ProcessFeatures
{
    public record ProcessingStarted(bool underCamera) : IProcessNotify;
}
