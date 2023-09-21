namespace NewLaserProject.Classes.Process.ProcessFeatures
{
    internal class ScopedGeomsRequest : IProcessNotify
    {
        public ScopedGeomsRequest(double width, double height, double x, double y)
        {
            Width = width;
            Height = height;
            X = x;
            Y = y;
        }

        public double Width
        {
            get; init;
        }
        public double Height
        {
            get; init;
        }
        public double X
        {
            get; init;
        }
        public double Y
        {
            get; init;
        }
    }
}
