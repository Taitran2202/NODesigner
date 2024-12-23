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

namespace NOVisionDesigner.Designer.Editors
{
    /// <summary>
    /// Interaction logic for IntegerValueEditorView.xaml
    /// </summary>
    public partial class DoubleValueEditorView : UserControl, IViewFor<DoubleValueEditorViewModel>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(DoubleValueEditorViewModel), typeof(DoubleValueEditorView), new PropertyMetadata(null));

        public DoubleValueEditorViewModel ViewModel
        {
            get => (DoubleValueEditorViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (DoubleValueEditorViewModel)value;
        }
        #endregion

        public DoubleValueEditorView()
        {
            InitializeComponent();
            this.WhenActivated(d => d(
                this.Bind(ViewModel, vm => vm.Value, v => v.valueUpDown.Value)
            )) ;
            this.WhenActivated(
            disposables =>
            {
                valueUpDown.Interval = ViewModel.DefaultStep;
            });
        }
        private decimal ToDecimal(double src)
        {
            return (decimal)src;
        }

        private double ToDouble(decimal src)
        {

            return (double)src;
        }
    }

}
