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
    /// Interaction logic for StringColorValueEditorView.xaml
    /// </summary>
    public partial class StringColorValueEditorView : IViewFor<StringColorValueEditorViewModel>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(StringColorValueEditorViewModel), typeof(StringColorValueEditorView), new PropertyMetadata(null));

        public StringColorValueEditorViewModel ViewModel
        {
            get => (StringColorValueEditorViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (StringColorValueEditorViewModel)value;
        }
        #endregion

        public StringColorValueEditorView()
        {
            InitializeComponent();

            this.WhenActivated(d => d(
                this.Bind(ViewModel, vm => vm.Value, v => v.valueUpDown.Color,StringToColor, ColorToString)
            ));;
        }
        public Color StringToColor(string value)
        {
            Color temp = (Color)ColorConverter.ConvertFromString((string)value);
            string convert = string.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", temp.B, temp.A, temp.R, temp.G);
            return (Color)ColorConverter.ConvertFromString(convert);
        }
        public string ColorToString(Color e)
        {
            return string.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", e.R, e.G, e.B, e.A);
        }

    }
}
