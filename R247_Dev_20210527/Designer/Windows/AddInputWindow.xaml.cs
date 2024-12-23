using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
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
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for AddInputWindow.xaml
    /// </summary>
    public partial class AddInputWindow : Window
    {

        DynamicData.ISourceList<NodeNetwork.ViewModels.NodeInputViewModel> list_input;
        ObservableCollection<Nodes.Inputparams> listParam;
        bool is_edit;
        int edit_index;
        Nodes.Inputparams edit_param;
        public AddInputWindow(DynamicData.ISourceList<NodeNetwork.ViewModels.NodeInputViewModel> list_input, ObservableCollection<Nodes.Inputparams> listParam, bool is_edit, int edit_index)
        {
            InitializeComponent();
            this.list_input = list_input;
            this.listParam = listParam;
            this.is_edit = is_edit;
            this.edit_index = edit_index;
            if (is_edit == true)
            {
                this.DataContext = listParam[edit_index];
                edit_param = listParam[edit_index];
                cbx_type.IsEnabled = false;
            }
            else
            {
                edit_param = new Nodes.Inputparams();
                this.DataContext = edit_param;

            }
            UpdateUI();

        }
        public void UpdateUI()
        {
            try
            {
                control_bgr_color.Visibility = Visibility.Visible;
                control_draw_type.Visibility = Visibility.Visible;
                control_font_size.Visibility = Visibility.Visible;
                control_header.Visibility = Visibility.Visible;
                control_area.Visibility = Visibility.Collapsed;
                if (edit_param != null)
                {
                    switch (edit_param.Type)
                    {
                        case "Image":
                        case "Region":
                            {
                                //control_bgr_color.Visibility = Visibility.Collapsed;
                                control_area.Visibility = Visibility.Visible;
                                break;
                            }
                        case "String":
                        case "Int":
                        case "Double":
                            {
                                control_draw_type.Visibility = Visibility.Collapsed;
                                break;
                            }

                    }
                }
             
            }
            catch (Exception)
            {
                 
            }
          
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (is_edit == false)
            {
              
            }
            UpdateUI();

        }


        private void Update()
        {
            if (is_edit == false)
            {
                if (cbx_type.SelectedItem != null)
                {

                    Nodes.Inputparams param = edit_param;
                  
                    switch (cbx_type.SelectedItem)
                    {


                        case "Image":
                            {
                                list_input.Add(new ValueNodeInputViewModel<HImage>()
                                {
                                    Name = txt_name.Text,
                                    PortType = "Image"

                                });
                                listParam.Add(param);

                                break;
                            }
                        case "Region":
                            {
                                list_input.Add(new ValueNodeInputViewModel<HRegion>()
                                {
                                    Name = txt_name.Text,
                                    PortType = "Region"
                                });
                                listParam.Add(param);
                                break;
                            }
                        case "String":
                            {
                                list_input.Add(new ValueNodeInputViewModel<string>()
                                {
                                    Name = txt_name.Text,
                                    PortType = "String"

                                });
                                listParam.Add(param);
                                break;
                            }
                        case "Int":
                            {
                                list_input.Add(new ValueNodeInputViewModel<int>()
                                {
                                    Name = txt_name.Text,
                                    PortType = "Number"

                                });
                                listParam.Add(param);
                                break;
                            }
                        case "Double":
                            {
                                list_input.Add(new ValueNodeInputViewModel<double>()
                                {
                                    Name = txt_name.Text,
                                    PortType = "Number"
                                });

                                listParam.Add(param);
                                break;
                            }
                        default:
                            break;
                    }
                }
            }
            else
            {
                
                 
            }
        }



        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Update();
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
          
        }
    }
}
