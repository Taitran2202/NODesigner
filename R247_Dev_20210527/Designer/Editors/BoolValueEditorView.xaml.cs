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
    public partial class BoolValueEditorView : UserControl, IViewFor<BoolValueEditorViewModel>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(BoolValueEditorViewModel), typeof(BoolValueEditorView), new PropertyMetadata(null));

        public BoolValueEditorViewModel ViewModel
        {
            get => (BoolValueEditorViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (BoolValueEditorViewModel)value;
        }
        #endregion

        public BoolValueEditorView()
        {
            InitializeComponent(); 
            this.WhenActivated(d => d(
                this.Bind(ViewModel, vm => vm.Value, v => v.btn_value.Content)
            ));
            
        }


        private void btn_value_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Value = !ViewModel.Value;
            if (ViewModel.Value)
            {
                btn_value.Content = "True";
            }
            else
            {
                btn_value.Content = "False";
            }
        }
    }

}
