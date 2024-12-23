using HalconDotNet;
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
    public partial class HObjectOutputValueEditorView<T> : HObjectOutputValueEditorView, IViewFor<HObjectOutputValueEditorViewModel<T>> where T : HObject
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(HObjectOutputValueEditorViewModel<T>), typeof(HObjectOutputValueEditorView), new PropertyMetadata(null));

        public HObjectOutputValueEditorViewModel<T> ViewModel
        {
            get => (HObjectOutputValueEditorViewModel<T>)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (HObjectOutputValueEditorViewModel<T>)value;
        }
        #endregion
        IObservable<HObject> ValueChanged;
        public HObjectOutputValueEditorView()
        {
            //this.WhenActivated(d => d(
            //    this.Bind(ViewModel, vm => vm.Value, v => v.text.Text)
            //));
            btn_view.Click += btn_view_Click;
            ValueChanged = this.WhenAnyValue(x => (HObject)x.ViewModel.Value);
        }
        private void btn_view_Click(object sender, RoutedEventArgs e)
        {
            HObjectStreamWindow wd = new HObjectStreamWindow(ValueChanged);
            wd.Owner = Window.GetWindow(this);
            wd.Show();
        }


    }
    public partial class HObjectOutputValueEditorView : UserControl
    {
        public HObjectOutputValueEditorView()
        {
            InitializeComponent();
        }

        
    }
}
