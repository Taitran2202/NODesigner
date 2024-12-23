using DynamicData;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// Interaction logic for IOScriptingNodeEditor.xaml
    /// </summary>
    public partial class IOMeasurementLinesEditor : Window
    {
        public bool isChanged = false;
        public MeasureLinesNode node;
        public ObservableCollection<IOItem> inputItems = new ObservableCollection<IOItem>();
        public IOMeasurementLinesEditor(MeasureLinesNode node)
        {
            this.node = node;
            InitializeComponent();
            foreach (var input in node.inputItems)
            {
                inputItems.Add(input);
            }
            lst_inputs.ItemsSource = node.inputItems;
            
            
        }
        ObservableCollection<string> _listType = new ObservableCollection<string>();
        public ObservableCollection<string> ListType
        {
            get
            {
                return _listType;
            }
            set
            {
                if (_listType != value)
                {
                    _listType = value;
                    RaisePropertyChanged("ListType");
                }
            }
        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void btn_add_input_Click(object sender, RoutedEventArgs e)
        {
            int i = 2;
            string name = "Lines Input ";
            while (node.inputItems.Where(x => x.Name == (name + i.ToString())).Any())
            {
                i++;
            }
            node.inputItems.Add(new IOItem() { Name = name + i.ToString(), ItemType = typeof(LineValue[]).Name });
            lst_inputs.SelectedIndex = lst_inputs.Items.Count - 1;
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            node.Inputs.RemoveMany(node.Inputs.Items.Where(x => ((x.PortType == "LineValue[]") && (x.Name != "Lines Input 1"))));
            foreach (var item in node.inputItems)
            {
                Type itemtype = typeof(LineValue[]);
                Type[] paramType = new Type[] { itemtype };
                Type classType = typeof(ValueNodeInputViewModel<>);
                try
                {
                    Type consType = classType.MakeGenericType(paramType);
                    NodeInputViewModel input = (NodeInputViewModel)Activator.CreateInstance(consType, new object[] { null, null });
                    input.Name = item.Name;
                    input.PortType = item.ItemType;
                    node.Inputs.Add(input);
                }
                catch
                {
                    bd_message.Visibility = Visibility.Visible;
                    return;
                }

            }
            isChanged = true;
            this.Close();
        }

        private void btn_remove_input_Click(object sender, RoutedEventArgs e)
        {
            int index = lst_inputs.SelectedIndex;
            if (index != -1)
            {
                node.inputItems.RemoveAt(index);
                lst_inputs.SelectedIndex = index > 0 ? index - 1 : 0;
            }

        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            node.inputItems.Clear();
            foreach (var input in inputItems)
            {
                node.inputItems.Add(input);
            }

            this.Close();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //var selected = (sender as ComboBox).SelectedItem;
            //if (selected as string == "<Add more type...>")
            //{
            //    AddTypeWindow wd = new AddTypeWindow(this);
            //    wd.ShowDialog();
            //    (sender as ComboBox).SelectedItem = ListType.ElementAtOrDefault(ListType.Count - 2);
            //}
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            //var selected = (sender as ComboBox).SelectedItem;
            //if (selected as string == "<Add more type...>")
            //{
            //    AddTypeWindow wd = new AddTypeWindow(this);
            //    wd.ShowDialog();
            //    (sender as ComboBox).SelectedItem = ListType.ElementAtOrDefault(ListType.Count - 2);
            //}
        }

        private void btn_ok_message_Click(object sender, RoutedEventArgs e)
        {
            bd_message.Visibility = Visibility.Hidden;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (!isChanged)
            {
                btn_cancel_Click(null, null);
            }
        }
    }

}
