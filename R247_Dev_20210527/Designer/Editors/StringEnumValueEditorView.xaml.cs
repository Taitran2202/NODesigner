using Microsoft.Win32;
using NOVisionDesigner.Designer.Editors.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
    public partial class StringEnumValueEditorView : UserControl, IViewFor<StringEnumValueEditorViewModel>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(StringEnumValueEditorViewModel), typeof(StringEnumValueEditorView), new PropertyMetadata(null));

        public StringEnumValueEditorViewModel ViewModel
        {
            get => (StringEnumValueEditorViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (StringEnumValueEditorViewModel)value;
        }
        #endregion

        public StringEnumValueEditorView()
        {
            InitializeComponent();

            this.WhenActivated(
        disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel)
                .Where(x => x != null)
                .Do((x) =>
                {
                    valueUpDown.ItemsSource = x.Value.Items;
                })
                .Subscribe()
                .DisposeWith(disposables);
        });
            this.WhenActivated(d => d(
                this.Bind(ViewModel, vm => vm.Value.SelectedIndex, v => v.valueUpDown.SelectedIndex)
            ));
        }
        private void btn_edit_list_Click(object sender, RoutedEventArgs e)
        {
            StringListEditorWindow wd = new StringListEditorWindow(ViewModel.Value.Items);
            wd.Show();
        }
    }

}
