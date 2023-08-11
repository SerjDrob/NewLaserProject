﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandyControl.Controls;
using HandyControl.Properties;
using System.ComponentModel;
using HandyControl.Tools.Extension;
using System.Runtime.CompilerServices;
using NewLaserProject.Views.Dialogs;

namespace NewLaserProject.ViewModels
{
    internal class MaterialVM
    {
        [Category("Размеры")]
        [DisplayName("Ширина")]
        public double Width { get; set; }
        [Category("Размеры")]
        [DisplayName("Высота")]
        public double Height { get; set; }
        [Category("Размеры")]
        [DisplayName("Толщина")]
        public double Thickness { get; set; }
    }


    internal class AskLearnFocus : IDialogResultable<AskLearnFocus>
    {
        [DisplayName("Начальная высота")]
        public double Height { get; set; }
        [DisplayName("Скорость")]
        public double Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                if (_speed < 1000)
                {
                    CloseAction?.Invoke();
                }
            }
        }
        private double _speed;
        public AskLearnFocus Result { get => this; set { } }
        public Action CloseAction { get; set; }
    }
}
namespace HandyControl.Tools.Extension
{
    public static class DialogExtensions2
    {
        public static Dialog SetDialogTitle(this Dialog dialog, string title)
        {
            if (dialog.Content is CommonDialog commonDialog) commonDialog.SetTitle(title);
            return dialog;
        }
        public static Dialog SetDataContext<TContext>(this Dialog dialog, Action<TContext> action) where TContext : new()
        {
            
            dialog.DataContext = new TContext();
            dialog.Initialize(action);
            return dialog;
        }

        public static Dialog SetDataContext<TContext>(this Dialog dialog, TContext instance, Action<TContext> action)
        {
            dialog.DataContext = instance;
            dialog.Initialize(action);
            return dialog;
        }
    }
}
