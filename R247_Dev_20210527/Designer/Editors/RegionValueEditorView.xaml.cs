using HalconDotNet;
using NOVisionDesigner.Designer.Windows;
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
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Editors
{
    /// <summary>
    /// Interaction logic for RegionValueEditorView.xaml
    /// </summary>
    public partial class RegionValueEditorView : UserControl, IViewFor<RegionValueEditorViewModel>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(RegionValueEditorViewModel), typeof(RegionValueEditorView), new PropertyMetadata(null));

        public RegionValueEditorViewModel ViewModel
        {
            get => (RegionValueEditorViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (RegionValueEditorViewModel)value;
        }
        #endregion
        public RegionValueEditorView()
        {
            InitializeComponent();
            //this.WhenActivated(d => d(
            //    this.OneWayBind(ViewModel, vm => vm.Value, v => v.valueUpDown.Text, HRegionToString)
            //));
        }
        public string HRegionToString(HRegion src)
        {
            return src.ToString();
        }
        private void btn_change_directory_Click(object sender, RoutedEventArgs e)
        {
            HImage image = new HImage();
            Nodes.CollectionOfregion regions = new Nodes.CollectionOfregion();
            image.GenEmptyObj();
            if(ViewModel.Parent.Parent.Inputs.Items.Where(p => p.PortType == "Image" | p.PortType == "HImage").Any())
            {
                image = (ViewModel.Parent.Parent.Inputs.Items.Where(p => p.PortType == "Image" | p.PortType == "HImage").First() as NodeNetwork.Toolkit.ValueNode.ValueNodeInputViewModel<HImage>).Value;
            }
            if (ViewModel.regions != null) 
            { 
                regions = ViewModel.regions;
            }
            WindowRegionWindowInteractive wd = new WindowRegionWindowInteractive(image, regions, new HHomMat2D());
            wd.ShowDialog();
            ViewModel.Value = regions.Region;
            ViewModel.regions = wd.regions;
        }
    }
}
