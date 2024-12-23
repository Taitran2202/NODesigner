using DevExpress.Xpf.Editors;
using NOVisionDesigner.Designer.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Accquisition.Windows
{
    /// <summary>
    /// Interaction logic for ListFeaturesControl.xaml
    /// </summary>
    public partial class ListFeaturesControl : UserControl
    {
        public BaseVimbaInterface ViewModel;
        public ListFeaturesControl()
        {
            InitializeComponent();
        }

        private void tb_int_value_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            var f = (sender as NumericUpDownWithKeyboard).DataContext as CustomVimbaFeature;
            if (!ViewModel.SetAndUpdateFeatureValue(f, (long)e.NewValue))
                return;// (sender as NumericUpDownWithKeyboard).Value = e.OldValue;
        }

        private void tb_float_value_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            var f = (sender as NumericUpDownWithKeyboard).DataContext as CustomVimbaFeature;
            if (!ViewModel.SetAndUpdateFeatureValue(f, e.NewValue))
                return;//(sender as NumericUpDownWithKeyboard).Value = e.OldValue;
        }

        private void cmb_enum_value_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var f = (sender as ComboBoxEdit).DataContext as CustomVimbaFeature;
            if (!ViewModel.SetAndUpdateFeatureValue(f, e.NewValue))
                return;//(sender as ComboBoxEdit).SelectedItem = e.OldValue;
        }

        private void cb_bool_value_Checked(object sender, RoutedEventArgs e)
        {
            var f = (sender as ToggleSwitch).DataContext as CustomVimbaFeature;
            if (!ViewModel.SetAndUpdateFeatureValue(f, true))
                return;//cb_bool_value_Unchecked(null, null);
        }

        private void cb_bool_value_Unchecked(object sender, RoutedEventArgs e)
        {
            var f = (sender as ToggleSwitch).DataContext as CustomVimbaFeature;
            if (!ViewModel.SetAndUpdateFeatureValue(f, false))
                return;//cb_bool_value_Checked(null, null);
        }

        private void btn_command_value_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SetFeature(((sender as System.Windows.Controls.Button).DataContext as CustomVimbaFeature).Name, 1);
        }

        private void tb_string_value_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
