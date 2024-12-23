using Microsoft.Win32;
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
    /// Interaction logic for StringValueEditorView.xaml
    /// </summary>
    public partial class StringValueEditorView : UserControl, IViewFor<StringValueEditorViewModel>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(StringValueEditorViewModel), typeof(StringValueEditorView), new PropertyMetadata(null));

        public StringValueEditorViewModel ViewModel
        {
            get => (StringValueEditorViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (StringValueEditorViewModel)value;
        }
        #endregion

        public StringValueEditorView()
        {
            InitializeComponent();

            this.WhenActivated(d => d(
                this.Bind(ViewModel, vm => vm.Value, v => v.valueUpDown.Text,valueUpDown.Events().LostFocus)
            ));
            this.WhenActivated(d => d(
                this.OneWayBind(ViewModel, vm => vm.ShowDirectory, v => v.btn_change_directory.Visibility, BoolToVisibility)
            )) ;
        }
        public Visibility BoolToVisibility(bool src)
        {
            if (src)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        private void btn_change_directory_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image Files|*.BMP;*.JPG;*.GIF;*.bmp;*.jpg;*.gif;*.png;*.PNG;*.jpeg|All files|*.*";
            if (fileDialog.ShowDialog() == true)
            {
                var file = fileDialog.FileName;
                ViewModel.Value = file;
            }
            
        }
    }

}
