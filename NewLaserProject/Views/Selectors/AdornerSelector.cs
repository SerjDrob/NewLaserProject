using NewLaserProject.Classes;
using System.Windows;
using System.Windows.Controls;

namespace NewLaserProject.Views.Selectors;

public class AdornerSelector : DataTemplateSelector
{
    public DataTemplate? LoopDataTemplate { get; set; }
    public DataTemplate? AddZDataTemplate { get; set; }
    public DataTemplate? PierceDataTemplate { get; set; }
    public DataTemplate? AddDiameterDataTemplate { get; set; }
    public DataTemplate? DelayTimeDataTemplate { get; set; }
    public override DataTemplate? SelectTemplate(object item, DependencyObject container) => ((ProgModuleItemVM)item).ModuleType switch
    {
        ModuleType.Loop => LoopDataTemplate,
        ModuleType.Pierce => PierceDataTemplate,
        ModuleType.AddDiameter => AddDiameterDataTemplate,
        ModuleType.AddZ => AddZDataTemplate,
        ModuleType.Delay => DelayTimeDataTemplate,
        _ => null
    };    
}
