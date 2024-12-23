using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Windows;
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
    public partial class NodeViewRegionFilter : UserControl, IViewFor<RegionFilterNode>
    {
        public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(RegionFilterNode), typeof(NodeViewRegionFilter), new PropertyMetadata(null));

        public RegionFilterNode ViewModel
        {
            get => (RegionFilterNode)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (RegionFilterNode)value;
        }
        public NodeViewRegionFilter()
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
    }
}
