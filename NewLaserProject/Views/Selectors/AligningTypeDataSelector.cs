using NewLaserProject.Classes;
using System.Windows;
using System.Windows.Controls;

namespace NewLaserProject.Views.Selectors
{
    public class AligningTypeDataSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            if (item is FileAlignment entityType)
            {
                switch (entityType)
                {
                    case FileAlignment.AlignByCorner:
                        return element?.FindResource("AlignByCornerDataTemplate") as DataTemplate;
                    case FileAlignment.AlignByThreePoint:
                        return element?.FindResource("AlignByThreePointDataTemplate") as DataTemplate;
                    case FileAlignment.AlignByTwoPoint:
                        return element?.FindResource("AlignByTwoPointDataTemplate") as DataTemplate;
                    default:
                        return null;
                }
            }
            return null;
        }
    }

}