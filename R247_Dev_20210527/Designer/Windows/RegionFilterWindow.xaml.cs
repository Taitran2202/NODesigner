using DevExpress.Xpf.Grid;
using HalconDotNet;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for RegionFilterWindow.xaml
    /// </summary>
    public partial class RegionFilterWindow : Window
    {
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        int _color_opacity = 50;
        public int ColorOpacity
        {
            get
            {
                return _color_opacity;
            }
            set
            {
                if (_color_opacity != value)
                {
                    _color_opacity = value;
                    RaisePropertyChanged("ColorOpacity");
                }
            }
        }
        #region Field

        HWindow display = null;
        HImage image;
        public ObservableCollection<BlobResults> BlobResults { get; set; } = new ObservableCollection<BlobResults>();
        public HImage Image
        {
            get => image;
            set => image = value;
            
        }
        int _threshold;

        public int Threshold
        {
            get
            {
                return _threshold;
            }
            set
            {
                if (_threshold != value)
                {
                    if (value > 255)
                    {
                        value = 255;
                    }
                    _threshold = value;
                    RaisePropertyChanged("Threshold");
                }
            }
        }

        BlobResults _current_blob;
        public BlobResults CurrentBlob
        {
            get
            {
                return _current_blob;
            }
            set
            {
                if (_current_blob != value)
                {
                    _current_blob = value;
                    RaisePropertyChanged("CurrentBlob");
                }
            }
        }


        int regionType = 0;
        public int RegionType
        {
            get
            {
                return regionType;
            }
            set
            {
                if (regionType != value)
                {
                    regionType = value;
                    RaisePropertyChanged("RegionType");
                }
            }
        }
        RegionFilterNode node;
        HRegion region;
        //HRegion searchRegion;
        //HHomMat2D homMat2D;
        #endregion
        public RegionFilterWindow(RegionFilterNode node)
        {
            this.node = node;
            InitializeComponent();
            image = node.ImageInput.GetCurrentConnectionValue()?.Clone();
            region = node.RegionInput.GetCurrentConnectionValue()?.Clone();
            if (region == null)
            {
                region = new HRegion();
                region.GenEmptyRegion();
            }
            //searchRegion = node.Searchregion.GetCurrentConnectionValue()?.Clone();
            //homMat2D = node.HomInput.GetCurrentConnectionValue()?.Clone();
            DataContext = this;
            grid_filters.ItemsSource = node.Filters;
            
            
        }


        #region Method
        

        
        
        private void UpdatedDisplay()
        {
            display.ClearWindow();
            display.SetDraw("fill");
            display.SetColor(AddOpacity("#00ff00ff",(double)ColorOpacity/100));
            if(region!=null)
            display.DispRegion(region);
            display.SetColor(AddOpacity("#ff0000ff",(double)ColorOpacity/100));
            display.SetDraw("fill");
            foreach (var region in BlobResults)
            {
                display.DispRegion(region.Region);
            }
            if (CurrentBlob != null)
            {
                display.SetDraw("margin");
                display.SetLineWidth(2);
                display.SetColor("blue");
                display.DispRegion(CurrentBlob.Region);
                display.SetDraw("fill");
                display.SetLineWidth(1);
            }

        }
        public string AddOpacity(string color, double opacity)
        {
            string alpha = color.Substring(7);
            int alpha_int = int.Parse(alpha, System.Globalization.NumberStyles.HexNumber);
            string result = ((int)(alpha_int * opacity)).ToString("X2");
            return color.Remove(7) + result;
        }
        public void Run()
        {
            HRegion filtered_region = node.RunInside(region);
            BlobResults.Clear();
            for (int i = 0; i < filtered_region.CountObj(); i++)
            {

                BlobResults.Add(new Windows.BlobResults(filtered_region[i + 1], i));
                
            }
            UpdatedDisplay();
        }
        #endregion

        #region Event Handler
        private void OnBlobChanged()
        {
            UpdatedDisplay();
            if (CurrentBlob == null)
            {
                return;
            }
            //calculate blob shape features
            var features_array = node.Filters.Select(x => x.Feature).ToArray();
            if (features_array.Count() == 0)
            {
                return;
            }
            var result = new List<Feature>();
            var features = CurrentBlob.Region.RegionFeatures(features_array);
            for (int i = 0; i < features.Length; i++)
            {
                result.Add(new Feature() { FeatureName = features_array[i], Value = features[i] });
            }
            lst_features.ItemsSource = result;
        }
        public class Feature
        {
            public string FeatureName { get; set; }
            public double Value { get; set; }
        }

        private void OnResize(HDrawingObject drawid, HWindow window, string type)
        {
            Run();
        }
        #endregion

        #region Views

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetDraw("fill");
            if (image != null)
            {
                display.AttachBackgroundToWindow(image);
            }
            if (region != null)
            {
                UpdatedDisplay();
            }
            if (image != null)
            {
                window_display.SetFullImagePart(image);
            }

        }
        
        private void Setting_Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Tab_Grid.Focus();
        }

        #endregion

        private void TableView_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            Run();
        }

        private void lst_class_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            OnBlobChanged();
        }
        public static List<string> HalconSelectShapeFeatures { get; internal set; } = new List<string>()
        {
            "anisometry", "area", "area_holes", "bulkiness", "circularity", "column", "column1", "column2",
            "compactness", "connect_num", "contlength", "convexity", "dist_deviation", "dist_mean", "euler_number",
            "height", "holes_num", "inner_height", "inner_radius", "inner_width", "max_diameter", "moments_i1",
            "moments_i2", "moments_i3", "moments_i4", "moments_ia", "moments_ib", "moments_m02", "moments_m02_invar",
            "moments_m03", "moments_m03_invar", "moments_m11", "moments_m11_invar", "moments_m12", "moments_m12_invar",
            "moments_m20", "moments_m20_invar", "moments_m21", "moments_m21_invar", "moments_m30", "moments_m30_invar",
            "moments_phi1", "moments_phi2", "moments_psi1", "moments_psi2", "moments_psi3", "moments_psi4", "num_sides",
            "orientation", "outer_radius", "phi", "ra", "rb", "rect2_len1", "rect2_len2", "rect2_phi", "rectangularity",
            "roundness", "row", "row1", "row2", "struct_factor", "width"
        };

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selected = grid_filters.SelectedItem as SelectShape;
            if (selected != null)
            {
                node.Filters.Remove(selected);
            }
        }

        private void window_display_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (region != null)
            {
                var selected = region.SelectRegionPoint((int)e.Row, (int)e.Column);
                if (selected.CountObj() == 0)
                {
                    return;
                }
                if (CurrentBlob == null)
                {
                    CurrentBlob = new BlobResults(selected.SelectObj(1), -1);
                }
                else
                {
                    CurrentBlob = new BlobResults(selected.SelectObj(1), -1);
                }
                
                OnBlobChanged();
                
            }
        }
    }
}
