using NOVisionDesigner.Designer.Nodes;
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
    /// Interaction logic for CameraNodeView.xaml
    /// </summary>
    public partial class WebcamNodeView : UserControl, IViewFor<WebcamNode>
    {
        public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(WebcamNode), typeof(WebcamNodeView), new PropertyMetadata(null));

        public WebcamNode ViewModel
        {
            get => (WebcamNode)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (WebcamNode)value;
        }
        public WebcamNodeView()
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

        private void btn_play_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Start();
        }
    }
}
