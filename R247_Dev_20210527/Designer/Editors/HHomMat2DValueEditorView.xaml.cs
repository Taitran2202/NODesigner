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
    /// Interaction logic for HHomMat2DValueEditorView.xaml
    /// </summary>
    public partial class HHomMat2DValueEditorView : UserControl, IViewFor<HHomMat2DValueEditorViewModel>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(HHomMat2DValueEditorViewModel), typeof(HHomMat2DValueEditorView), new PropertyMetadata(null));

        public HHomMat2DValueEditorViewModel ViewModel
        {
            get => (HHomMat2DValueEditorViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (HHomMat2DValueEditorViewModel)value;
        }
        #endregion

        public HHomMat2DValueEditorView()
        {
            InitializeComponent();

            this.WhenActivated(d => d(
                this.OneWayBind(ViewModel, vm => vm.Value, v => v.valueUpDown.Text, HHomMat2DToString)
            )); 
        }
        public string HHomMat2DToString(HHomMat2D src)
        {
            
            return src.RawData.ToString();
        }
        private decimal ToDecimal(int src)
        {
            return (decimal)src;
        }

        private int ToInt(decimal src)
        {

            return (int)src;
        }
    }

}
