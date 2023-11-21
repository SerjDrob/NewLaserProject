using NewLaserProject.ViewModels.DialogVM;
using System.Windows;
using System.Windows.Controls;

namespace NewLaserProject.Views.Selectors
{
    public class HatchLoopDirectionDataSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            if (item is HatchLoopDirection entityType)
            {
                var template = entityType switch
                {
                    HatchLoopDirection.Hatch_IN => element?.FindResource("HatchLoopInTemplate") as DataTemplate,
                    HatchLoopDirection.Hatch_OUT => element?.FindResource("HatchLoopOutTemplate") as DataTemplate,
                    HatchLoopDirection.CrossHatch => element?.FindResource("CrossHatchTemplate") as DataTemplate,
                    _ => null
                };
                return template;
            }
            return null;
        }
    }
}