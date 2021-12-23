using MetadataLocal.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MetadataLocal.Views
{
    public partial class MetadataLocalSettingsView : UserControl
    {
        public MetadataLocalSettingsView()
        {
            InitializeComponent();
        }


        private void PART_BtUp_Click(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(((Button)sender).Tag.ToString());

            if (index > 0)
            {
                ((List<Store>)PART_LbStores.ItemsSource).Insert(index - 1, (Store)PART_LbStores.Items[index]);
                ((List<Store>)PART_LbStores.ItemsSource).RemoveAt(index + 1);

                PART_LbStores.Items.Refresh();
            }
        }

        private void PART_BtDown_Click(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(((Button)sender).Tag.ToString());

            if (index < PART_LbStores.Items.Count - 1)
            {
                ((List<Store>)PART_LbStores.ItemsSource).Insert(index + 2, (Store)PART_LbStores.Items[index]);
                ((List<Store>)PART_LbStores.ItemsSource).RemoveAt(index);

                PART_LbStores.Items.Refresh();
            }
        }
    }
}