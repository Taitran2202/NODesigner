using DevExpress.Xpf.Core;
using DynamicData;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    public partial class IOScriptingNodeEditor : ThemedWindow
    {
        public bool isChanged = false;
        public CSharpScriptingNode node;
        public ObservableCollection<IOItem> inputItems = new ObservableCollection<IOItem>();
        public ObservableCollection<IOItem> outputItems = new ObservableCollection<IOItem>();
        public IOScriptingNodeEditor(CSharpScriptingNode node)
        {
            this.node = node;
            InitializeComponent();
            this.DataContext= this;
            lst_inputs.ItemsSource = node.inputItems;
            lst_outputs.ItemsSource = node.outputItems;
            try
            {
                foreach (var input in node.inputItems)
                {
                    inputItems.Add(input);
                }
                foreach (var output in node.outputItems)
                {
                    outputItems.Add(output);
                }
                foreach (var item in node.TypesCollection)
                {
                    if (!ListType.Contains(item.Name))
                    {
                        ListType.Add(item.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
            ListType.Add("<Add more type...>");
            //foreach (var item in node.designer.nodeTypes)
            //{
            //    Type t = Type.GetType("NOVisionDesigner.Designer.Nodes." + item);
            //    var prop = t.GetProperties().Where(x => x.PropertyType.BaseType?.Name == "NodeOutputViewModel");
            //    foreach (var p in prop)
            //    {
            //        var name = p?.PropertyType.GetGenericArguments()[0];
            //        if (!ListType.Contains(name))
            //        {
            //            ListType.Add(name);
            //        }
            //    }
            //}
            //foreach (var item in node.Inputs.Items)
            //{
            //    var type = item.GetType().GetGenericArguments()[0];
            //    var name = item.Name;
            //    inputItems.Add(new IOItem() { Name = name, ItemType = type.Name });
            //}
            //foreach (var item in node.Outputs.Items)
            //{
            //    var type = item.GetType().GetGenericArguments()[0];
            //    var name = item.Name;
            //    outputItems.Add(new IOItem() { Name = name, ItemType = type.Name });
            //}
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
            int i = 0;
            string name = "input";
            while (node.inputItems.Where(x => x.Name == (name + i.ToString())).Any())
            {
                i++;
            }
            node.inputItems.Add(new IOItem() { Name = name + i.ToString(), ItemType = typeof(bool).Name });
            lst_inputs.SelectedIndex = lst_inputs.Items.Count - 1;
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            
            var list_io = node.inputItems.Concat(node.outputItems);
            if(list_io.GroupBy(x => x.Name).Any(g => g.Count() > 1))
            {
                MessageBox.Show("Input and Output names is duplicated");
                return;
            }
            //node.Inputs.Clear();
            //node.Outputs.Clear();
            isChanged = false;
            var remove_items = node.Inputs.Items.Where(x => !node.inputItems.Any(y => (y.Name == x.Name && y.ItemType == x.PortType))).Select(x => new IOItem() { ItemType = x.PortType, Name = x.Name });
            var add_items = node.inputItems.Where(x => !node.Inputs.Items.Any(y => (y.Name == x.Name && y.PortType == x.ItemType)));
            foreach (var item in add_items)
            {
                Type itemtype = node.TypesCollection.FirstOrDefault(x => x.Name == item.ItemType);
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
                isChanged = true;
            }
            foreach(var item in remove_items)
            {
                node.Inputs.Remove(node.Inputs.Items.FirstOrDefault(x => (x.Name == item.Name && x.PortType == item.ItemType)));
                isChanged = true;
            }
            var remove_output_items = node.Outputs.Items.Where(x => !node.outputItems.Any(y => (y.Name == x.Name && y.ItemType == x.PortType))).Select(x => new IOItem() { ItemType = x.PortType, Name = x.Name });
            var add_output_items = node.outputItems.Where(x => !node.Outputs.Items.Any(y => (y.Name == x.Name && y.PortType == x.ItemType)));
            foreach (var item in add_output_items)
            {
                Type itemtype = node.TypesCollection.FirstOrDefault(x => x.Name == item.ItemType);
                Type[] paramType = new Type[] { itemtype };
                Type classType = typeof(ValueNodeOutputViewModel<>);
                try
                {
                    Type consType = classType.MakeGenericType(paramType);
                    NodeOutputViewModel output = (NodeOutputViewModel)Activator.CreateInstance(consType);
                    output.Name = item.Name;
                    output.PortType = item.ItemType;
                    node.Outputs.Add(output);
                }
                catch
                {
                    bd_message.Visibility = Visibility.Visible;
                    return;
                }
                isChanged = true;
            }
            foreach (var item in remove_output_items)
            {
                node.Outputs.Remove(node.Outputs.Items.FirstOrDefault(x => (x.Name == item.Name && x.PortType == item.ItemType)));
                isChanged = true;
            }
            
            this.Close();
        }

        private void btn_remove_input_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as Button).DataContext as IOItem;
            if (selected != null)
            {
                node.inputItems.Remove(selected);
            }
            
        }

        private void btn_add_output_Click(object sender, RoutedEventArgs e)
        {
            int i = 0;
            string name = "output";
            while (node.outputItems.Where(x => x.Name == (name + i.ToString())).Any())
            {
                i++;
            }
            node.outputItems.Add(new IOItem() { Name = name + i.ToString(), ItemType = typeof(bool).Name });
            lst_outputs.SelectedIndex = lst_outputs.Items.Count - 1;
        }

        private void btn_remove_output_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as Button).DataContext as IOItem;
            if (selected!=null)
            {
                node.outputItems.Remove(selected);
            }
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            node.inputItems.Clear();
            node.outputItems.Clear();
            foreach (var input in inputItems)
            {
                node.inputItems.Add(input);
            }
            foreach (var output in outputItems)
            {
                node.outputItems.Add(output);
            }
            this.Close();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (sender as ComboBox).SelectedItem;
            if(selected as string == "<Add more type...>")
            {
                AddTypeWindow wd = new AddTypeWindow(this);
                wd.ShowDialog();
                (sender as ComboBox).SelectedItem = ListType.ElementAtOrDefault(ListType.Count - 2);
            }
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            var selected = (sender as ComboBox).SelectedItem;
            if (selected as string == "<Add more type...>")
            {
                AddTypeWindow wd = new AddTypeWindow(this);
                wd.ShowDialog();
                (sender as ComboBox).SelectedItem = ListType.ElementAtOrDefault(ListType.Count - 2);
            }
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

        private void txt_input_name_LostFocus(object sender, RoutedEventArgs e)
        {
            //var name = (sender as TextBox).Text;
            //if(node.inputItems.Where(x => x.Name == name).Count()>1|| node.outputItems.Any(x => x.Name == name))
            //{
            //    MessageBox.Show("Input name is already used.");
            //}
        }
    }
    public class VariableNameValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string strValue = Convert.ToString(value);
            if (string.IsNullOrEmpty(strValue))
                return new ValidationResult(false, $"Value cannot be empty.");
            if (strValue.Contains(" "))
                return new ValidationResult(false, $"Value cannot contain space characters.");
            Regex regex = new Regex("^[a-zA-Z_][a-zA-Z_0-9]*$");
            if (!regex.IsMatch(strValue))
                return new ValidationResult(false, $"Incorrect variable name declaration.");
            return new ValidationResult(true, null);
        }
    }
}
