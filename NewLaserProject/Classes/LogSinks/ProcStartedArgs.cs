using Newtonsoft.Json;

namespace NewLaserProject.Classes.LogSinks
{
    public class ProcStartedArgs
    {
        public string FileName { get; set; }
        public string MaterialName { get; set; }
        public string TechnologyName { get; set; }
        public double MaterialThickness { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
