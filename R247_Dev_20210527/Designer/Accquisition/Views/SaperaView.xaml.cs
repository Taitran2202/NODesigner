using Microsoft.Win32;
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

namespace NOVisionDesigner.Designer.Accquisition.Views
{
    /// <summary>
    /// Interaction logic for Sapera.xaml
    /// </summary>
    public partial class SaperaView : UserControl, IViewFor<Sapera>
    {
        public static readonly DependencyProperty ViewModelProperty =
       DependencyProperty.Register(nameof(ViewModel), typeof(Sapera), typeof(SaperaView), new PropertyMetadata(null));

        public Sapera ViewModel
        {
            get => (Sapera)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (Sapera)value;
        }
        public SaperaView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext).DisposeWith(d);
            });
        }

        private void Btn_select_camera_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "All files|*.*";
            if (fileDialog.ShowDialog() == true)
            {
                var file = fileDialog.FileName;
                ViewModel.ConfigurationPath = file;
            }
        }

        private void SpinEdit_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            //try
            //{
            //    ViewModel.Exposure = (double)e.NewValue;
            //}
            //catch(Exception ex)
            //{

            //}
        }
    }
}
