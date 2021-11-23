using NewLaserProject.Classes;
using NewLaserProject.Classes.ProgBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows;
using System.Windows.Controls;

namespace NewLaserProject.Views.Selectors
{
    public class AdornerSelector:DataTemplateSelector
    {
        public DataTemplate LoopDataTemplate { get; set; }
        public DataTemplate AddZDataTemplate { get; set; }
        public DataTemplate PierceDataTemplate { get; set; }
        public DataTemplate AddDiameterDataTemplate { get; set; }
        public DataTemplate DelayTimeDataTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            switch (((ProgModuleItemVM)item).ModuleType)
            {
                case ModuleType.Loop:
                    return LoopDataTemplate;
                case ModuleType.Pierce:
                    return PierceDataTemplate;
                case ModuleType.AddDiameter:
                    return AddDiameterDataTemplate;
                case ModuleType.AddZ:
                    return AddZDataTemplate;
                case ModuleType.Delay:
                    return DelayTimeDataTemplate;
                default:
                    return null;
                    break;
            }

            //return item switch
            //{
            //    LoopBlock => LoopDataTemplate;
            //    PierceBlock => PierceDataTemplate,

            //};
        }
    }
}
