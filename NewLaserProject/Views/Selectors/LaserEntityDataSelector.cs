using MachineClassLibrary.Laser.Entities;
using NewLaserProject.Classes;
using System.Windows;
using System.Windows.Controls;

namespace NewLaserProject.Views.Selectors
{
    public class LaserEntityDataSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            if (item is LaserEntity entityType)
            {
                switch (entityType)
                {
                    case LaserEntity.Circle:
                        return element?.FindResource("LaserCircleDataTemplate") as DataTemplate;
                    case LaserEntity.Curve:
                        return element?.FindResource("LaserCurveDataTemplate") as DataTemplate;
                    case LaserEntity.Line:
                        return element?.FindResource("LaserLineDataTemplate") as DataTemplate;
                    case LaserEntity.Point:
                        return element?.FindResource("LaserPointDataTemplate") as DataTemplate;
                    default:
                        return null;
                } 
            }
            return null;
        }
    }
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