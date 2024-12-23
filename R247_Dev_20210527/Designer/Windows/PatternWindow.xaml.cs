using DevExpress.Xpf.Core;
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
using static NOVisionDesigner.Designer.Nodes.OCRNode;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for PatternWindow.xaml
    /// </summary>
    public partial class PatternWindow : ThemedWindow, INotifyPropertyChanged
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        #region Field
        //PatternParameters _param;
        //public PatternParameters param
        //{
        //    get
        //    {
        //        return _param;
        //    }
        //    set
        //    {
        //        if (_param != value)
        //        {
        //            _param = value;
        //            RaisePropertyChanged("param");
        //        }
        //    }
        //}
        public PatternParameters param { get; set; }
        PatternNode node;
        //public HNCCModel model;
        //public bool isTrained;
        //HDrawingObject Drawing_SearchObject; //train
        //HDrawingObject Drawing_SearchRegion;

        HImage image;
        HWindow display;
        HWindow display_model;
        public ObservableCollection<PatternResult> listResult { get; set; } = new ObservableCollection<PatternResult>();
        int _Current_Index = 0;
        public int Current_Index
        {
            get
            {
                return _Current_Index;
            }
            set
            {
                if (_Current_Index != value)
                {
                    _Current_Index = value;
                    OnIndexChanged(value);
                    RaisePropertyChanged("Current_Index");
                }
            }
        }

        #endregion
        public PatternWindow(PatternNode node)
        {
           
            InitializeComponent();
            this.node = node;
            this.param = node.Parameters;
            //param.trainParam.TrainRegion.AreaCenter(out origin_row, out origin_col);
            if (node.ImageInput.Value != null)
            {
                if (node.ImageInput.Value.IsInitialized())
                {
                    this.image = node.ImageInput.Value.Clone();
                }
                
            }
            UpdateRegionToMarker(this.param.RuntimeParam.SearchRegion, SearchRegion);
            UpdateRegionToMarker(this.param.TrainParam.TrainRegion, Pattern);
            UpdateResultToMarker(Pattern);
            
            CurrentRegionList.Add(Pattern);
            CurrentRegionList.Add(SearchRegion);
            this.DataContext = this;
            
        }
        public void UpdateResultToMarker(RegionMaker2 marker)
        {
            if (!param.TrainParam.IsTrained | image==null) { return; }
            if (node.Model == null) return;
            double angleStart = Math.PI * param.RuntimeParam.LowerAngle / 180;
            double angleExtent = Math.PI * (param.RuntimeParam.UpperAngle - param.TrainParam.LowerAngle) / 180;
            node.Model.FindNccModel(image.Rgb1ToGray().ReduceDomain(param.RuntimeParam.SearchRegion), angleStart, angleExtent, param.RuntimeParam.MinScore, param.RuntimeParam.NumMatches, param.RuntimeParam.MaxOverlap,
               "true", param.RuntimeParam.NumLevels, out HTuple row, out HTuple col, out HTuple angle, out HTuple score);
            int i = 0;
            if (row.Length > 0)
            {
                HHomMat2D hom = new HHomMat2D();
                hom.VectorAngleToRigid(param.TrainParam.OriginalRow, param.TrainParam.OriginalCol, 0.0, row.DArr[i], col.DArr[i], angle.DArr[i]);
                marker.Region.TransformRegion(hom);
            }
            else
            {
                marker.Region.TransformRegion(new HHomMat2D());
            }
            
        }
        public void UpdateRegionToMarker(HRegion region,RegionMaker2 marker)
        {
            if (region != null)
            {
                region.SmallestRectangle2(out double row, out double col, out double phi, out double length1, out double length2);
                marker.Region = new Rectange2(false, row,col,phi,length1,length2);
               
            }
            
        }
        bool _is_waiting=false;
        public bool IsWaiting
        {
            get
            {
                return _is_waiting;
            }
            set
            {
                if (_is_waiting != value)
                {
                    _is_waiting = value;
                    RaisePropertyChanged("IsWaiting");
                }
            }
        }

        #region Method
        public void TrainShapeModel()
        {
            Task.Run(new Action(() =>
            {
                IsWaiting = true;
                try
                {


                    double angleStart = Math.PI * param.TrainParam.LowerAngle / 180;
                    double angleExtent = Math.PI * (param.TrainParam.UpperAngle - param.TrainParam.LowerAngle) / 180;
                    string polarity = param.TrainParam.UsePolarity ? "use_polarity" : "ignore_global_polarity";
                    if (node.Model == null)
                    {
                        node.Model = new HNCCModel();
                    }
                    node.Model.CreateNccModel(image.Rgb1ToGray().ReduceDomain(Pattern.Region.region), param.TrainParam.NumLevels, angleStart, angleExtent, "auto", polarity);
                    param.TrainParam.IsTrained = true;
                    param.TrainParam.TrainRegion = Pattern.Region.region.Clone();
                    param.TrainParam.TrainRegion.AreaCenter(out double row, out double col);
                    param.TrainParam.OriginalRow = row;
                    param.TrainParam.OriginalCol = col;
                    HImage img = image.ReduceDomain(Pattern.Region.region).CropDomain();
                    param.TrainParam.ModelImage = img;
                    display_model.DispObj(img);
                }catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                IsWaiting = false;
            }));
            


        }
        public void ChangeSearchRegion()
        {
            param.RuntimeParam.SearchRegion = SearchRegion.Region.region.Clone();
        }
        public void FindShapeMode()
        {
            if(!param.TrainParam.IsTrained) { return; }
            //display.DetachDrawingObjectFromWindow(Drawing_SearchRegion);
            //HTuple p = Drawing_SearchRegion.GetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"));
            //param.runtimeParam.SearchRegion.GenRectangle1((double)p[0], p[1], p[2], p[3]);
            //TrainShapeModel();
            double angleStart = Math.PI * param.RuntimeParam.LowerAngle / 180;
            double angleExtent = Math.PI * (param.RuntimeParam.UpperAngle - param.TrainParam.LowerAngle) / 180;
            listResult.Clear();
            node.Model.FindNccModel(image.Rgb1ToGray().ReduceDomain(param.RuntimeParam.SearchRegion), angleStart, angleExtent, param.RuntimeParam.MinScore, param.RuntimeParam.NumMatches, param.RuntimeParam.MaxOverlap,
                "true", param.RuntimeParam.NumLevels, out HTuple row, out HTuple col, out HTuple angle, out HTuple score);
            if (row.Length == 0)
            {
                Console.WriteLine("Can't find pattern");
                return;
            }

            
            for(int i = 0; i < row.DArr.Length; i++)
            {
                listResult.Add(new PatternResult(i, row.DArr[i], col.DArr[i], (int)Math.Round(angle.DArr[i] * 180 / Math.PI), Math.Round(score.DArr[i],2)));
                HHomMat2D hom = new HHomMat2D();
                hom.VectorAngleToRigid(param.TrainParam.OriginalRow, param.TrainParam.OriginalCol, 0.0, row.DArr[i], col.DArr[i], angle.DArr[i]);
                HRegion region = hom.AffineTransRegion(param.TrainParam.TrainRegion, "nearest_neighbor");
                display.DispRegion(region);
            }

        }

       

        private void OnIndexChanged(int index)
        {
            if (index == -1) { return; }
            display.ClearWindow();
            display.DispObj(image);
            for (int i = 0; i < listResult.Count; i++)
            {
                HHomMat2D hom = new HHomMat2D();
                hom.VectorAngleToRigid(param.TrainParam.OriginalRow, param.TrainParam.OriginalCol, 0.0, listResult[i].Row, listResult[i].Col, Math.PI*listResult[i].Angle/180);
                HRegion region = hom.AffineTransRegion(param.TrainParam.TrainRegion, "nearest_neighbor");
                if(i == index)
                {
                    display.SetColor("blue");
                }
                else
                {
                    display.SetColor("green");
                }
                display.DispRegion(region);
            }

        }

        #endregion

        #region View

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            if (param.TrainParam.IsTrained)
            {
                //display.DetachDrawingObjectFromWindow(Drawing_SearchRegion);
                FindShapeMode();

            }
        }

        

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetDraw("fill");
            display.SetWindowParam("background_color", "white");
            display.ClearWindow();
            if (image != null)
            {
                display.AttachBackgroundToWindow(image);
                DispOverlay();
            }
            

        }





        #endregion
        bool _is_drawing=false;
        public bool IsDrawing
        {
            get
            {
                return _is_drawing;
            }
            set
            {
                if (_is_drawing != value)
                {
                    _is_drawing = value;
                    RaisePropertyChanged("IsDrawing");
                }
            }
        }

        RegionMaker2 editingRegion = null;
        RegionMaker2 Pattern = new RegionMaker2() { Region = new Rectange2(false) , Annotation = new ClassifierClass1() { Name = "Pattern Region" } };
        RegionMaker2 SearchRegion = new RegionMaker2() { Region = new Rectange2(false), Annotation = new ClassifierClass1() { Name = "Search Region", Color = "#0000ffff" } };
        List<RegionMaker2> CurrentRegionList = new List<RegionMaker2>();
        public void UpdateSelectBoxPosition()
        {
            if (IsDrawing & editingRegion!=null)
            {
                double winposx, winposy;
                var bb = ViewDisplayMode.BoundingBox(new Rect2(editingRegion.Region.row, editingRegion.Region.col, editingRegion.Region.phi, editingRegion.Region.length1, editingRegion.Region.length2));
                display.ConvertCoordinatesImageToWindow(bb.row1,bb.col1, out winposx, out winposy);
                box_pattern_accept.Margin = new Thickness(winposy, winposx - 25, 0, 0);
            }

        }
        private double _colorOpacity = 20;
        public double ColorOpacity
        {
            get { return _colorOpacity; }
            set
            {
                if (_colorOpacity != value)
                {
                    _colorOpacity = value;
                    DispOverlay();
                    RaisePropertyChanged("ColorOpacity");
                }
            }
        }

        public string AddOpacity(string color, double opacity)
        {
            string alpha = color.Substring(7);
            int alpha_int = int.Parse(alpha, System.Globalization.NumberStyles.HexNumber);
            string result = ((int)(alpha_int * opacity)).ToString("X2");
            return color.Remove(7) + result;
        }
        public void DispOverlay()
        {
            if (display == null)
                return;
            display.ClearWindow();
            display.SetDraw("fill");
            //display.SetColor("#00ff0025");
            DisplayMarker(SearchRegion,true);
            DisplayMarker(Pattern,true);
            
        }

        private void DisplayMarker(RegionMaker2 item,bool draw_bb=false)
        {
            if (item.Annotation != null)
            {
                display.SetColor(OCRWindow.AddOpacity(item.Annotation.Color, ColorOpacity / 100));
                display.DispObj(item.Region.region);
                
                var bb = ViewDisplayMode.BoundingBox(new Rect2(item.Region.row, item.Region.col, item.Region.phi, item.Region.length1, item.Region.length2));
                if (draw_bb)
                {
                    //display.SetColor(OCRWindow.AddOpacity(item.Annotation.Color, ColorOpacity / 100));
                    display.SetDraw("margin");
                    display.DispRectangle1(bb.row1, bb.col1, bb.row2, bb.col2);
                    display.SetDraw("fill");
                }
                
                display.DispText(item.Annotation.Name, "image", bb.row1, bb.col1, "black", new HTuple("box_color"), new HTuple("#ffffffff"));
            }
        }

        private void window_display_HInitWindow2(object sender, EventArgs e)
        {
            display_model = window_model.HalconWindow;
            display_model.SetWindowParam("background_color", "white");
            display_model.ClearWindow();
            if (param.TrainParam.ModelImage != null)
            {
                display_model.DispObj(param.TrainParam.ModelImage);

            }
            //if (param.trainParam.TrainImage != null)
            //{
            //    display.DispObj(param.trainParam.TrainImage);
            //}
            //if (param.trainParam.TrainImage != null && param.trainParam.IsTrained)
            //{
            //    HImage img = param.trainParam.TrainImage.ReduceDomain(param.trainParam.TrainRegion).CropDomain();
            //    display_model.DispObj(img);
            //}
        }

        private void btn_change_pattern_Click(object sender, RoutedEventArgs e)
        {
            editingRegion = Pattern;
            editingRegion.Attach(window_display.HalconWindow, null, PatternRegionParametersChangedCallback, PatternRegionSelectedCallback);
            pre_rect = new Rect2(editingRegion.Region.row, editingRegion.Region.col, editingRegion.Region.phi, editingRegion.Region.length1, editingRegion.Region.length2);
            IsDrawing = true;
            UpdateSelectBoxPosition();
        }
        public void PatternRegionParametersChangedCallback(RegionMaker2 sender, Region region)
        {
            DispOverlay();
            //UpdateSelectBoxPosition();
        }
        public void PatternRegionSelectedCallback(RegionMaker2 sender, Region region)
        {

        }

        private void btn_accept_pattern_Click(object sender, RoutedEventArgs e)
        {
            IsDrawing = false;
            editingRegion.Region.ClearDrawingData(window_display.HalconWindow);
            AcceptRegion();
        }
        public void AcceptRegion()
        {
            if (editingRegion != null)
            {
                if(editingRegion== Pattern)
                {
                    //create shape model
                    TrainShapeModel();
                }
                else
                {
                    ChangeSearchRegion();
                }
            }
        }

        private void btn_discard_pattern_Click(object sender, RoutedEventArgs e)
        {
            IsDrawing = false;
            editingRegion.Region.ClearDrawingData(window_display.HalconWindow);
            if (editingRegion== SearchRegion)
            {
                SearchRegion.Region = new Rectange2(false, pre_rect.row, pre_rect.col, pre_rect.phi, pre_rect.length1, pre_rect.length2);
                
            }
            else
            {
                Pattern.Region = new Rectange2(false, pre_rect.row, pre_rect.col, pre_rect.phi, pre_rect.length1, pre_rect.length2);
            }
            
            DispOverlay();
        }

        private void window_display_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            
        }

        private void window_display_HMouseWheel(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            UpdateSelectBoxPosition();
        }

        private void window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSelectBoxPosition();
            }
        }
        Rect2 pre_rect;
        private void btn_edit_search_region_Click(object sender, RoutedEventArgs e)
        {
            editingRegion = SearchRegion;
            pre_rect = new Rect2(SearchRegion.Region.row, SearchRegion.Region.col, SearchRegion.Region.phi, SearchRegion.Region.length1, SearchRegion.Region.length2);
            editingRegion.Attach(window_display.HalconWindow, null, PatternRegionParametersChangedCallback, PatternRegionSelectedCallback);
            IsDrawing = true;
            UpdateSelectBoxPosition();
        }

        private void window_display_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            
            UpdateSelectBoxPosition();
        }
    }

}
