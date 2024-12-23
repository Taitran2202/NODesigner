using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for DrawGraphicsWindow.xaml
    /// </summary>
    /// 
    class StringMatchConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (values.Length < 2)
            {
                return false;
            }
        
            for (int i = 1; i < values.Length; i++)
            {
                if (!(values[0] as string).Equals(values[i] as string))
                {
                    return false;
                }
            }

            return true;

        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public partial class DrawGraphicsWindow : Window
    {
        ISourceList<NodeInputViewModel> list_input;

        ObservableCollection<Nodes.Inputparams> listParam;
        public DrawGraphicsWindow(ISourceList<NodeInputViewModel> inputs, ObservableCollection<Nodes.Inputparams> listParam)
        {
     
            InitializeComponent();
            this.list_input = inputs;
            this.listParam = listParam;
            lst_input.ItemsSource = this.listParam;
        }

        public delegate void Update();
        public Update UpdateList;
    
 

        private void Button_remove_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                list_input.RemoveAt(lst_input.SelectedIndex);
                listParam.RemoveAt(lst_input.SelectedIndex);
                lst_input.ItemsSource = listParam;
            }
            catch (Exception)
            {


            }

        }

        private void btn_up_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_down_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_edit_click(object sender, RoutedEventArgs e)
        {
            if(lst_input.SelectedIndex >=0)
            {
                try
                {
                    AddInputWindow wd = new AddInputWindow(list_input, listParam, true, lst_input.SelectedIndex);
                    wd.ShowDialog();
                    lst_input.ItemsSource = listParam;
                }
                catch (Exception)
                {

                }
            }
        
        }

        private void btn_add_filter_Click(object sender, RoutedEventArgs e)
        {
          
            try
            {
                AddInputWindow wd = new AddInputWindow(list_input, listParam, false, 0 );
                wd.ShowDialog();
                lst_input.ItemsSource = (listParam);
            }
            catch (Exception)
            {

            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            for (int i = 0; i < list_input.Items.Count(); i++)
            {
                list_input.Items.ElementAt(i).Name = listParam[i].Name;
            }
        }
    }
}
