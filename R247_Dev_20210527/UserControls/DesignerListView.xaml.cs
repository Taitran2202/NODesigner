using DevExpress.Xpf.Core;
using DynamicData;
using NodeNetwork.Utilities;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Windows;
using NOVisionDesigner.Windows.HelperWindows;
using ReactiveUI;
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

namespace NOVisionDesigner.UserControls
{
    /// <summary>
    /// Interaction logic for DesignerListView.xaml
    /// </summary>
    public partial class DesignerListView : UserControl,IViewFor<NetworkViewModel>
    {
        public DesignerListView()
        {
            InitializeComponent();
        }
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
    typeof(NetworkViewModel), typeof(DesignerListView), new PropertyMetadata(null));
        public NetworkViewModel ViewModel
        {
            get => (NetworkViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (NetworkViewModel)value;
        }
        List<Button> buildCommandList(NodeViewModel node)
        {
            List<Button> list = new List<Button>();
            var commandList = node.GetType().GetProperties().Where(x => x.IsDefined(typeof(HMIProperty), true));
            foreach (var command in commandList)
            {
                if (command.PropertyType != typeof(IReactiveCommand))
                {
                    continue;
                }
                var description = (command.GetCustomAttributes(typeof(HMIProperty), false)[0] as HMIProperty).Description;
                Button commandButton = new Button() { Content = description,Margin=new Thickness(4),Padding=new Thickness(12,4,12,4) };
                Binding myBinding = new Binding(command.Name);
                myBinding.Source = node;
                commandButton.SetBinding(Button.CommandProperty, myBinding);
                Binding b = new Binding();
                b.RelativeSource = new RelativeSource(RelativeSourceMode.Self);
                commandButton.SetBinding(Button.CommandParameterProperty, b);
                list.Add(commandButton);
            }
            return list;
        }
        DesignerHost designer;
        public void SetDesigner(DesignerHost designer)
        {
            this.designer = designer;
            ViewModel = designer.Network;
            this.BindList(designer.Network, vm => vm.Nodes, v => v.lst_nodes.ItemsSource);
        }

        private void lst_command_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(lst_command.DataContext != null)
            {
                if(lst_command.DataContext is NodeViewModel)
                {
                    lst_command.Items.Clear();
                    var newlist = buildCommandList(lst_command.DataContext as NodeViewModel);
                    foreach (var item in newlist)
                    {
                        lst_command.Items.Add(item);
                    }
                    NodeInfo nodeInfo = (NodeInfo)Attribute.GetCustomAttribute(lst_command.DataContext.GetType(), typeof(NodeInfo));
                    txt_node_type.Text = nodeInfo.DisplayName;
                }
                else
                {
                    lst_command.Items.Clear();
                }
            }
            
        }

        private void btn_add_node_Click(object sender, RoutedEventArgs e)
        {
            AddToolWindow wd = new AddToolWindow();
            wd.SetDesigner(designer);
            wd.Owner = Window.GetWindow(this);
            wd.Show();
        }

        private void btn_remove_node_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as FrameworkElement).DataContext as NodeViewModel;
            //NodeViewModel selected = lst_nodes.SelectedItem as NodeViewModel;
            if (selected != null)
            {
                if (DXMessageBox.Show(this, "Do you want to remove " + selected.Name + "?", "warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    designer.RemoveNode(selected);
                }
            }
            
        }

        private void btn_rename_node_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as FrameworkElement).DataContext as NodeViewModel;
            if (selected != null)
            {
                SaveWindow wd = new SaveWindow(selected.Name, "Rename");
                wd.Owner = Window.GetWindow(this);
                if (wd.ShowDialog() == true)
                {
                    selected.Name = wd.Text;
                }
            }
            
        }

        private void lst_nodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = lst_nodes.SelectedItem as NodeViewModel;
            if (selected != null)
            {
                Task.Run(new Action(() =>
                {
                    foreach (var node in designer.Network.Nodes.Items)
                    {
                        node.IsHighLight = false;
                    }

                    foreach (var input in selected.Inputs.Items)
                    {
                        if (input.Connections.Count > 0)
                        {
                            input.Connections.Items.First().Output.Parent.IsHighLight = true;
                        }
                    }
                }));
                
            }
        }

        private void btn_open_image_Click(object sender, RoutedEventArgs e)
        {
            var data=  designer.Network.Nodes.Items.FirstOrDefault(x => x is AccquisitionNode);
            if (data != null)
            {
                (data as AccquisitionNode).Acq.OpenImage();
            }
        }

        private void btn_reload_Click(object sender, RoutedEventArgs e)
        {
            var data = designer.Network.Nodes.Items.FirstOrDefault(x => x is AccquisitionNode);
            if (data != null)
            {
                (data as AccquisitionNode).Acq.DisplaygImage();
            }
        }
    }
}
