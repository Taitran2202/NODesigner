using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.Windows;
using NOVisionDesigner.Windows.HelperWindows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
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

namespace NOVisionDesigner.Designer.NodeViews
{
    /// <summary>
    /// Interaction logic for NodeViewWithEditor.xaml
    /// </summary>
    public partial class NodeViewWithEditor : UserControl, IViewFor<BaseNode>
    {
        public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(BaseNode), typeof(NodeViewWithEditor), new PropertyMetadata(null));

        public BaseNode ViewModel
        {
            get => (BaseNode)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (BaseNode)value;
        }
        public NodeViewWithEditor()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(v => v.ViewModel).BindTo(this, v => v.NodeView.ViewModel).DisposeWith(d);
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OnCommand("editor",this);
        }

        private void btn_expression_Click(object sender, RoutedEventArgs e)
        {
            //ExpressionWindow wd = new ExpressionWindow(ViewModel);
            ////wd.Owner = Application.Current.MainWindow;
            //wd.Show();
        }

        private void btn_run_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RunOnce(null);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            PropertiesWindow wd = new PropertiesWindow(ViewModel);
            wd.Owner = Window.GetWindow(this);
            wd.Show();
        }

        private void MenuItemFolder_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", ViewModel.Dir);
        }

        private void MenuItemRename_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            SaveWindow wd = new SaveWindow(ViewModel.Name, "Rename");
            if(wd.ShowDialog() == true)
            {
                ViewModel.Name = wd.Text;
            }

        }
        private void btn_comand_list_ContextMenuOpening_1(object sender, ContextMenuEventArgs e)
        {

        }

        private void BarSubItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }
        bool listBuild = false;
        List<BarButtonItem> buildCommandList()
        {
            List<BarButtonItem> list = new List<BarButtonItem>();
            var commandList = ViewModel.GetType().GetProperties().Where(x => x.IsDefined(typeof(HMIProperty), true));
            foreach(var command in commandList)
            {
                if(command.PropertyType != typeof(IReactiveCommand))
                {
                    continue;
                }
                var description = (command.GetCustomAttributes(typeof(HMIProperty), false)[0] as HMIProperty).Description;
                BarButtonItem commandButton = new BarButtonItem() { Content = description };
                Binding myBinding = new Binding(command.Name);
                myBinding.Source = ViewModel;
                commandButton.SetBinding(BarButtonItem.CommandProperty, myBinding);
                Binding b = new Binding();
                b.RelativeSource = new RelativeSource(RelativeSourceMode.Self);
                commandButton.SetBinding(Button.CommandParameterProperty, b);
                list.Add(commandButton);
            }
            return list;
        }
        private void BarSubItem_Popup(object sender, EventArgs e)
        {
            
            
            

        }

        private void BarSubItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            InitializeComponent();
        }

        private void PopupMenu_Opened(object sender, EventArgs e)
        {
            
            if (!listBuild)
            {
                var control = (sender as PopupMenu);
                if (control != null)
                {
                    var BarSubItem = new BarSubItem() { Content="Command"};
                    BarSubItem.Glyph = WpfSvgRenderer.CreateImageSource(DXImageHelper.GetImageUri("SvgImages/Spreadsheet/PivotTableCalculationsFieldsItemsSetsGroup.svg")); 
                  
                    var list = buildCommandList();
                    foreach (var item in list)
                    {
                        BarSubItem.Items.Add(item);
                    }
                    listBuild = true;
                    control.Items.Add(BarSubItem);
                }
                
            }
        }

        private void MenuItem_edit_connection_Click(object sender, ItemClickEventArgs e)
        {
            EditNodeConnectionWindow wd = new EditNodeConnectionWindow(ViewModel);
            wd.Owner = Window.GetWindow(this);
            wd.Show();
        }
    }
}
