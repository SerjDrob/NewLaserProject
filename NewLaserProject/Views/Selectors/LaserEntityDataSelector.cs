using MachineClassLibrary.Laser.Entities;
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
}