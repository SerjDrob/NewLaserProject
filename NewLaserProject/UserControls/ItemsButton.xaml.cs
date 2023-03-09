using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NewLaserProject.UserControls
{
    /// <summary>
    /// Interaction logic for ItemsButton.xaml
    /// </summary>
    public partial class ItemsButton : UserControl
    {
        public ItemsButton()
        {
            InitializeComponent();
            MainGrid.DataContext = this;
        }

        private int _index = 0;



        public int SetIndex
        {
            get { return (int)GetValue(SetIndexProperty); }
            set { SetValue(SetIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SetIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SetIndexProperty =
            DependencyProperty.Register("SetIndex", typeof(int), typeof(ItemsButton), new PropertyMetadata(0,new PropertyChangedCallback(SetIndexChanged)));

        private static void SetIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var itemsButton = (ItemsButton)d;
            itemsButton._index = (int)e.NewValue;
            ChangeButtonContent(itemsButton);
        }

        public DataTemplateSelector ItemSelector
        {
            get { return (DataTemplateSelector)GetValue(ItemSelectorProperty); }
            set { SetValue(ItemSelectorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemSelector.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemSelectorProperty =
            DependencyProperty.Register("ItemSelector", typeof(DataTemplateSelector), typeof(ItemsButton), new PropertyMetadata(null));

        

        public IEnumerable<object> Items
        {
            get { return (IEnumerable<object>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(IEnumerable<object>), typeof(ItemsButton), 
                new PropertyMetadata(null, new PropertyChangedCallback(ItemsChanged)));

        private static void ItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var itemsButton = (ItemsButton)d;
            itemsButton._index = 0;
            ChangeButtonContent(itemsButton);
        }

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(ItemsButton), new PropertyMetadata(null));



        private void MainButton_Click(object sender, RoutedEventArgs e)
        {
            if (Items?.Any() ?? false)
            {
                _index = (_index + 1) % Items.Count();
                ChangeButtonContent(this);
            }
        }

        private static void ChangeButtonContent(ItemsButton itemsButton)
        {
            if (itemsButton.Items?.Any() ?? false)
            {
                var cc = new ContentControl();
                itemsButton.SelectedItem = itemsButton.Items.ElementAt(itemsButton._index);
                cc.ContentTemplate = itemsButton.ItemSelector.SelectTemplate(itemsButton.SelectedItem, cc);
                cc.Content = itemsButton.SelectedItem;
                itemsButton.MainButton.Content = cc;                 
            }
            else
            {
                itemsButton.MainButton.Content = null;
            }
        }
    }
}
