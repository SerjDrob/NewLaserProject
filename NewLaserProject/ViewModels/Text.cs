using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    public partial class Text
    {
        public string LayerName
        {
            get;set;
        }
        public string Value { get; set; }
        public bool IsProcessed { get; set; }
        public int Count { get; set; }

        public static implicit operator Text(string str)
        {
            return new Text { Value = str };
        }
        public static implicit operator string(Text text)
        {
            return text.Value;
        }
    }
}
