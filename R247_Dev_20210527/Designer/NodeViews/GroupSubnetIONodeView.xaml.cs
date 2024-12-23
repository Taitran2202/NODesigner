using NOVisionDesigner.Designer.GroupNodes;
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
    /// Interaction logic for GroupSubnetIONodeView.xaml
    /// </summary>
    public partial class GroupSubnetIONodeView : IViewFor<GroupSubnetIONodeViewModel>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(GroupSubnetIONodeViewModel), typeof(GroupSubnetIONodeView), new PropertyMetadata(null));

        public GroupSubnetIONodeViewModel ViewModel
        {
            get => (GroupSubnetIONodeViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (GroupSubnetIONodeViewModel)value;
        }
        #endregion

        public GroupSubnetIONodeView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                NodeView.ViewModel = this.ViewModel;
                Disposable.Create(() => NodeView.ViewModel = null).DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.NodeType, v => v.NodeView.Background, MyNodeView.ConvertNodeTypeToBrush).DisposeWith(d);

            });
        }
    }
}
