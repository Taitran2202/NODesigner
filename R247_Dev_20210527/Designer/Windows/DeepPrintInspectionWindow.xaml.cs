using DevExpress.Xpf.Core;
using HalconDotNet;
using Microsoft.Win32;
using NOVisionDesigner.Designer.Extensions;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Python;
using NOVisionDesigner.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for PrintInspectionWindow.xaml
    /// </summary>
    public partial class DeepPrintInspectionWindow : Window,INotifyPropertyChanged
    {
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        DeepPrintInspectionFolder _selected_category = DeepPrintInspectionFolder.good;
        public DeepPrintInspectionFolder SelectedCategory
        {
            get
            {
                return _selected_category;
            }
            set
            {
                if (_selected_category != value)
                {
                    _selected_category = value;
                    RaisePropertyChanged("SelectedCategory");
                    switch (value)
                    {
                        case DeepPrintInspectionFolder.good_aligned: CurrentDir = GoodAlignDir;break;
                        case DeepPrintInspectionFolder.bad_aligned: CurrentDir = BadAlignDir; break;
                        case DeepPrintInspectionFolder.good: CurrentDir = GoodDir; break;
                        case DeepPrintInspectionFolder.bad: CurrentDir = BadDir; break;
                    }
                    LoadImageList(CurrentDir);
                }
            }
        }
        public string CurrentDir { get; set; }
        private ObservableCollection<ImageFilmstrip> _listImage;

        public ObservableCollection<ImageFilmstrip> ListImage
        {
            get
            {
                return _listImage;
            }
            set
            {
                if (_listImage != value)
                {
                    _listImage = value;
                    RaisePropertyChanged("ListImage");
                }
            }
        }

        bool _show_message = false;
        public bool ShowMessage
        {
            get
            {
                return _show_message;
            }
            set
            {
                if (_show_message != value)
                {
                    _show_message = value;
                    RaisePropertyChanged("ShowMessage");
                }
            }
        }
        string _message = "";
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                if (_message != value)
                {
                    _message = value;
                    RaisePropertyChanged("Message");
                }
            }
        }

        int _image_count = 1;
        public int ImageCount
        {
            get
            {
                return _image_count;
            }
            set
            {
                if (_image_count != value)
                {
                    _image_count = value;
                    RaisePropertyChanged("ImageCount");
                }
            }
        }
        int _image_index = 1;
        public int ImageIndex
        {
            get
            {
                return _image_index;
            }
            set
            {
                if (_image_index != value)
                {
                    _image_index = value;
                    RaisePropertyChanged("ImageIndex");
                }
            }
        }
        double _color_opacity = 80;
        public double ColorOpacity
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
        bool _is_drawing = false;
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
        public string ResultDir { get; set; }
        public string RootDir { get; set; }
        public string GoodDir { get; set; }
        public string BadDir { get; set; }
        public string GoodAlignDir { get; set; }
        public string BadAlignDir { get; set; }
        RegionMaker2 editingRegion = null;
        RegionMaker2 ReferenceRegion = new RegionMaker2() { Region = new Rectange2(false), Annotation = new ClassifierClass1() { Name = "Reference Region" } };
        RegionMaker2 Pattern = new RegionMaker2() { Region = new Rectange2(false), Annotation = new ClassifierClass1() { Name = "Pattern Region" } };
        RegionMaker2 SearchRegion = new RegionMaker2() { Region = new Rectange2(false), Annotation = new ClassifierClass1() { Name = "Search Region", Color = "#0000ffff" } };
        public void Initialize(DeepPrintInspectionNode node)
        {
            RootDir = node.Dir;
            GoodDir = node.GoodDir;
            GoodAlignDir = node.GoodAlignDir;
            BadAlignDir = node.BadAlignDir;
            BadDir = node.BadDir;
            CurrentDir = GoodDir;
            LoadImageList(CurrentDir);

            //pattern setting
            UpdateRegionToMarker(node.Options.Parameters.RuntimeParam.SearchRegion, SearchRegion);
            UpdateRegionToMarker(node.Options.Parameters.TrainParam.TrainRegion, Pattern);
            UpdateResultToMarker(Pattern);
        }
        public void UpdateResultToMarker(RegionMaker2 marker)
        {
            var param = node.Options.Parameters;
            if (!node.Options.Parameters.TrainParam.IsTrained | image == null) { return; }
            if (node.Options.Model == null) return;
            double angleStart = Math.PI * param.RuntimeParam.LowerAngle / 180;
            double angleExtent = Math.PI * (param.RuntimeParam.UpperAngle - param.TrainParam.LowerAngle) / 180;
            node.Options.Model.FindNccModel(image.Rgb1ToGray().ReduceDomain(param.RuntimeParam.SearchRegion), angleStart, angleExtent, param.RuntimeParam.MinScore, param.RuntimeParam.NumMatches, param.RuntimeParam.MaxOverlap,
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
        public void UpdateRegionToMarker(HRegion region, RegionMaker2 marker)
        {
            if (region != null)
            {
                region.SmallestRectangle2(out double row, out double col, out double phi, out double length1, out double length2);
                marker.Region = new Rectange2(false, row, col, phi, length1, length2);

            }

        }

        private void btn_edit_search_region_Click(object sender, RoutedEventArgs e)
        {
            editingRegion = SearchRegion;
            pre_rect = new Rect2(SearchRegion.Region.row, SearchRegion.Region.col, SearchRegion.Region.phi, SearchRegion.Region.length1, SearchRegion.Region.length2);
            editingRegion.Attach(window_display.HalconWindow, null, PatternRegionParametersChangedCallback, PatternRegionSelectedCallback);
            IsDrawing = true;
            UpdateSelectBoxPosition();
        }
        private void btn_change_pattern_Click(object sender, RoutedEventArgs e)
        {
            editingRegion = Pattern;
            editingRegion.Attach(window_display.HalconWindow, null, PatternRegionParametersChangedCallback, PatternRegionSelectedCallback);
            pre_rect = new Rect2(editingRegion.Region.row, editingRegion.Region.col, editingRegion.Region.phi, editingRegion.Region.length1, editingRegion.Region.length2);
            IsDrawing = true;
            UpdateSelectBoxPosition();
        }
        private void lst_view_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {

                if (e.AddedItems.Count == 0)
                {

                    return;
                }
                var selected = e.AddedItems[0] as ImageFilmstrip;
                SetSelectedImage(selected);

            }
            catch
            {
                return;
            }
        }
        int imgW, imgH;
        private void SetSelectedImage(ImageFilmstrip selected)
        {
            if (selected != null)
            {

                try
                {
                    image = new HImage(selected.FullPath);
                    image.GetImageSize(out imgW, out imgH);
                    window_display.HalconWindow.AttachBackgroundToWindow(image);
                    window_display.HalconWindow.ClearWindow();
                    lst_view.SelectedItem = selected;
                    
                }
                catch (Exception ex)
                {
                    //image = new HImage();
                }
            }
        }

        void LoadImageList(string dir)
        {
            var result = new ObservableCollection<ImageFilmstrip>();
            if (Directory.Exists(dir))
            {
                var imageFiles = Directory.GetFiles(dir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
                foreach (var goodImageFile in imageFiles)
                {
                    result.Add(new ImageFilmstrip(goodImageFile));
                }
                

            }
            ListImage = result;

        }


        Rect2 pre_rect;
        DeepPrintInspectionNode node;
        HImage image;
        HHomMat2D fixture;
        
        
        public void AttachReferenceImage(HSmartWindowControlWPF sender)
        {
            if (node.Options.OpticalFlowImage != null)
            {
                sender.HalconWindow.AttachBackgroundToWindow(node.Options.OpticalFlowImage);
                UpdateMask(sender);
                sender.SetFullImagePart();
            }

        }
        public void AttachAligmentImage(HSmartWindowControlWPF sender)
        {
            if (node.Options.Parameters.TrainParam.ModelImage != null)
            {
                sender.HalconWindow.AttachBackgroundToWindow(node.Options.Parameters.TrainParam.ModelImage);
                //UpdateMask(sender);
                sender.SetFullImagePart();
            }

        }

        private void UpdateMask(HSmartWindowControlWPF sender)
        {
            if (node.Options.ReferenceMask != null)
            {
                sender.HalconWindow.ClearWindow();
                node.Options.OpticalFlowImage.GetImageSize(out int w, out int h);
                HRegion hRegion = new HRegion();
                hRegion.GenRectangle1(0.0, 0, h, w);
                var regionMask = hRegion.Difference(node.Options.ReferenceMask.Region);
                sender.HalconWindow.SetColor("#00ff00aa");
                sender.HalconWindow.SetDraw("fill");
                sender.HalconWindow.DispObj(regionMask);
                sender.HalconWindow.SetColor("#00ff00ff");
                sender.HalconWindow.SetDraw("margin");
                sender.HalconWindow.DispObj(regionMask);
            }
        }

        public DeepPrintInspectionWindow(DeepPrintInspectionNode node)
        {
            this.node = node;
            InitializeComponent();
            if(node.ImageInput.Value != null)
            {
                image = node.ImageInput.Value.Clone();
            }

            fixture = new HHomMat2D();
            
            if (node.Options.InspectionRegion != null)
            {
                ReferenceRegion = new RegionMaker2()
                {
                    Region = new Rectange2(false, node.Options.InspectionRegion.row,
                node.Options.InspectionRegion.col,
                node.Options.InspectionRegion.phi,
                node.Options.InspectionRegion.length1,
                node.Options.InspectionRegion.length2
                ),
                    Annotation = new ClassifierClass1() { Name = "Reference Region" }
                };
            }
            else
            {
                ReferenceRegion = new RegionMaker2()
                {
                    Region = new Rectange2(false, 0,
                0,
                0,
                100,
                100
                ),
                    Annotation = new ClassifierClass1() { Name = "Reference Region" }
                };
            }
            
            propertiesGrid.SelectedObject = node.Options;
            Initialize(node);

            btn_train.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Visible;
            };
            btn_train_option.Click += (sender, e) =>
            {
                PropertiesWindow wd = new PropertiesWindow(node.TrainConfig);
                wd.ShowDialog();
                node.TrainConfig.Save();
            };

            btn_step_cancel.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Hidden;
            };
            btn_step_ok.Click += (sender, e) =>
            {
                var step = spin_step.Value;
                node.TrainConfig.Epoch = (int)step;
                //node.TrainConfig.ModelName = 
                box_step.Visibility = Visibility.Hidden;
                Task.Run(new Action(() =>
                {
                    Train();
                }));
            };
            this.DataContext = this;
            box_step.DataContext = this.node.TrainConfig;
            //spin_step.Value = this.node.TrainConfig.Epoch;

        }
        public void UpdateRegionToMarker(Rect2 region, RegionMaker2 marker)
        {
                
           marker.Region = new Rectange2(false, region.row, region.col, region.phi, region.length1, region.length2);


        }
        private void HSmartWindowControlWPF_HInitWindow(object sender, EventArgs e)
        {
            var display = (sender as HSmartWindowControlWPF);
            display.HalconWindow.SetWindowParam("background_color", "white");
            display.HalconWindow.ClearWindow();
            if(image != null)
            {
                display.HalconWindow.AttachBackgroundToWindow(image);
            }
        }

        private void HSmartWindowControlWPF_HInitWindow_1(object sender, EventArgs e)
        {
            (sender as HSmartWindowControlWPF).HalconWindow.SetWindowParam("background_color", "white");
            (sender as HSmartWindowControlWPF).HalconWindow.ClearWindow();
            AttachReferenceImage(sender as HSmartWindowControlWPF);
        }


        private void btn_change_region_Click(object sender, RoutedEventArgs e)
        {
            editingRegion = ReferenceRegion;
            ReferenceRegion.Attach(window_display.HalconWindow, fixture, PatternRegionParametersChangedCallback, PatternRegionSelectedCallback);
            pre_rect = new Rect2(ReferenceRegion.Region.row, ReferenceRegion.Region.col, ReferenceRegion.Region.phi, ReferenceRegion.Region.length1, ReferenceRegion.Region.length2);
            IsDrawing = true;
            
        }
        public void UpdateSelectBoxPosition()
        {
            if (IsDrawing& editingRegion!=null)
            {
                double winposx, winposy;
                var rect2 = new Rect2(editingRegion.Region.row, editingRegion.Region.col, editingRegion.Region.phi, editingRegion.Region.length1, editingRegion.Region.length2);
                var bb = ViewDisplayMode.BoundingBox(PrintInspectionNode.AffineTranRect2(rect2,fixture));
                window_display.HalconWindow.ConvertCoordinatesImageToWindow(bb.row1, bb.col1, out winposx, out winposy);
                box_pattern_accept.Margin = new Thickness(winposy, winposx - 25, 0, 0);
            }

        }
        public void DispOverlay()
        {
            if (window_display.HalconWindow == null)
                return;
            window_display.HalconWindow.ClearWindow();
            //display.SetColor("#00ff0025");
            DisplayMarker(ReferenceRegion, true);
        }
        public void PatternRegionParametersChangedCallback(RegionMaker2 sender, Region region)
        {
            DispOverlay();
            //UpdateSelectBoxPosition();
        }
        public void PatternRegionSelectedCallback(RegionMaker2 sender, Region region)
        {

        }

        private void btn_accept_reference_Click(object sender, RoutedEventArgs e)
        {
            IsDrawing = false;
            editingRegion.Region.ClearDrawingData(window_display.HalconWindow);
            AcceptRegion();
        }
        public void AcceptRegion()
        {
            if (editingRegion != null)
            {
                if (editingRegion == Pattern)
                {
                    //create shape model
                    TrainShapeModel();
                }
                else if(editingRegion == SearchRegion)
                {
                    ChangeSearchRegion();
                }
                else
                {
                    AcceptReferenceRegion();
                }
                

            }
        }
        public void AcceptReferenceRegion()
        {
            if (ReferenceRegion != null)
            {
                if (image != null)
                {
                    var rect2Trans = PrintInspectionNode.AffineTranRect2(new Rect2(ReferenceRegion.Region.row, ReferenceRegion.Region.col, ReferenceRegion.Region.phi, ReferenceRegion.Region.length1, ReferenceRegion.Region.length2),fixture);
                    HImage referenceImage = CropRect2(rect2Trans.row, rect2Trans.col, rect2Trans.phi, rect2Trans.length1, rect2Trans.length2);
                    node.Options.ReferenceImage = referenceImage.Clone();
                    node.Options.OpticalFlowImage = referenceImage.Clone();
                    node.Options.InspectionRegion = new Rect2(ReferenceRegion.Region.row, ReferenceRegion.Region.col, ReferenceRegion.Region.phi, ReferenceRegion.Region.length1, ReferenceRegion.Region.length2);
                    node.IsPrepared = false;
                    node.Prepare();
                    node.IsPrepared = true;
                    AttachReferenceImage(window_reference);
                }
            }
        }
        public void TrainShapeModel()
        {
            Task.Run(new Action(() =>
            {
                ShowMessage = true;
                try
                {
                    var param = node.Options.Parameters;

                    double angleStart = Math.PI * param.TrainParam.LowerAngle / 180;
                    double angleExtent = Math.PI * (param.TrainParam.UpperAngle - param.TrainParam.LowerAngle) / 180;
                    string polarity = param.TrainParam.UsePolarity ? "use_polarity" : "ignore_global_polarity";
                    if (node.Options.Model == null)
                    {
                        node.Options.Model = new HNCCModel();
                    }
                    node.Options.Model.CreateNccModel(image.Rgb1ToGray().ReduceDomain(Pattern.Region.region), param.TrainParam.NumLevels, angleStart, angleExtent, "auto", polarity);
                    param.TrainParam.IsTrained = true;
                    param.TrainParam.TrainRegion = Pattern.Region.region.Clone();
                    param.TrainParam.TrainRegion.AreaCenter(out double row, out double col);
                    param.TrainParam.OriginalRow = row;
                    param.TrainParam.OriginalCol = col;
                    HImage img = image.ReduceDomain(Pattern.Region.region).CropDomain();
                    param.TrainParam.ModelImage = img;
                    window_reference1.HalconWindow.DispObj(img);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                ShowMessage = false;
            }));



        }
        public void ChangeSearchRegion()
        {
            node.Options.Parameters.RuntimeParam.SearchRegion = SearchRegion.Region.region.Clone();
        }

        private HImage CropRect2(double row,double col,double phi,double length1,double length2)
        {
            HHomMat2D hHomMat2D = new HHomMat2D();
            hHomMat2D.VectorAngleToRigid(row, col, phi, length2,length1, 0);
            var referenceImage = hHomMat2D.AffineTransImageSize(image, "constant", (int)(length1 * 2), (int)(length2 * 2));
            return referenceImage;
        }
        
        private void DisplayMarker(RegionMaker2 item, bool draw_bb = false)
        {
            if (item.Annotation != null)
            {
                window_display.HalconWindow.SetColor(OCRWindow.AddOpacity(item.Annotation.Color, ColorOpacity / 100));
                window_display.HalconWindow.DispObj(item.Region.region.AffineTransRegion(fixture,"constant"));
                var rect2 = new Rect2(item.Region.row, item.Region.col, item.Region.phi, item.Region.length1, item.Region.length2);
                var transRect = PrintInspectionNode.AffineTranRect2(rect2, fixture);
                var bb = ViewDisplayMode.BoundingBox(transRect);
                if (draw_bb)
                {
                    //display.SetColor(OCRWindow.AddOpacity(item.Annotation.Color, ColorOpacity / 100));
                    window_display.HalconWindow.SetDraw("margin");
                    window_display.HalconWindow.DispRectangle1(bb.row1, bb.col1, bb.row2, bb.col2);
                    window_display.HalconWindow.SetDraw("fill");
                }

                window_display.HalconWindow.DispText(item.Annotation.Name, "image", bb.row1, bb.col1, "black", new HTuple("box_color"), new HTuple("#ffffffff"));
            }
        }
        private void btn_discard_reference_Click(object sender, RoutedEventArgs e)
        {
            IsDrawing = false;
            ReferenceRegion.Region.ClearDrawingData(window_display.HalconWindow);
            ReferenceRegion.Region = new Rectange2(false, pre_rect.row, pre_rect.col, pre_rect.phi, pre_rect.length1, pre_rect.length2);
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
            UpdateSelectBoxPosition();
        }

        private void btn_add_image_Click(object sender, RoutedEventArgs e)
        {
            AddFromFiles(CurrentDir);
        }
        

        public HImage UpdateAverageImage(string dir)
        {
            try
            {
                HImage Imageresult=null;
                var imageFiles = Directory.GetFiles(dir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
                if (!System.IO.Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var count = imageFiles.Count();
                int index = 1;

                foreach (var imageFile in imageFiles)
                {
                    HImage addImage = new HImage(imageFile).ConvertImageType("float");
                    if (Imageresult != null)
                    {
                        Imageresult += addImage;

                    }
                    else
                    {
                        Imageresult = addImage;
                    }
                    index++;
                }
                Imageresult /= count;
                return Imageresult.ConvertImageType("byte");
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public ImageFilmstrip AddImages(HImage image, string directory, bool Prepend = false)
        {
            try
            {

                var filename = Functions.RandomFileName(directory);
                //var newfile = System.IO.Path.Combine(ImageDir, filename);
                image.WriteImage("png", 0, filename);
                var imageadded = new ImageFilmstrip(filename + ".png");
                //if (Prepend)
                //{
                //    ListImage.Insert(0, imageadded);
                //}
                //else
                //{
                //    ListImage.Add(imageadded);
                //}
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (Prepend)
                    {
                        ListImage.Insert(0, imageadded);
                    }
                    else
                    {
                        ListImage.Add(imageadded);
                    }
                }));

                return imageadded;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public void AddFromFiles(string directory)
        {
            //ShowMessage = true;
            //Message = "Loading ... ";
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image Files|*.bmp;*.jpg;*.gif;*.png|All files|*.*";
            fileDialog.Multiselect = true;
            if (fileDialog.ShowDialog() == true)
            {
                var files = fileDialog.FileNames;
                //var count = files.Count();
                //int index = 1;
                //for (int i = 0; i < files.Length; i++)
                //{
                //    var file = files[i];                   
                //    try
                //    {
                //        //Message = "Loading " + index.ToString() + "/" + count.ToString();
                //        HImage image = new HImage(file);
                //        if (i == files.Length - 1)
                //        {
                //            var added = AddImages(image, directory, true);
                //            SetSelectedImage(added);
                //        }
                //        else
                //        {
                //            AddImages(image, directory);
                //        }

                //        index++;
                //    }
                //    catch (Exception ex)
                //    {
                //        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                //        {
                //            DXMessageBox.Show(this, ex.ToString(), "Error");
                //        }));
                //    }
                //}
                Task.Run(new Action(() =>
                {
                    ShowMessage = true;
                    try
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            Message = "Add image " + (i + 1).ToString() + "/" + files.Length;
                            var file = files[i];
                            try
                            {
                                HImage image = new HImage(file);
                                if (i == files.Length - 1)
                                {
                                    var added = AddImages(image, directory, true);
                                    SetSelectedImage(added);
                                }
                                else
                                {
                                    AddImages(image, directory);
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                    ShowMessage = false;
                }));


                //ShowMessage = false;
            }

        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as ImageFilmstrip;
            if (vm != null)
            {
                try
                {
                    System.IO.File.Delete(vm.FullPath);
                    ListImage.Remove(vm);
                }
                catch (Exception ex)
                {

                }

            }

        }

        //private void btn_train_Click(object sender, RoutedEventArgs e)
        //{
        //    //Task.Run(new Action(() =>
        //    //{
        //    //    Train();
        //    //}));
        //    btn_train.Click += (sender, e) =>
        //    {
        //        box_step.Visibility = Visibility.Visible;


        //    };

        //}
        public void ExtractAlignedImage(string srcDir,string targetDir,string category)
        {
            //ShowMessage = true;
            try
            {
                var imageFiles = Directory.GetFiles(srcDir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
                if (!System.IO.Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
                var count = imageFiles.Count();
                int index = 1;
                
                foreach (var imageFile in imageFiles)
                {
                    Message = "Extracting "+ category+" alignment image: " + index.ToString() + "/" + count.ToString();
                    HImage nonAlignedImage = new HImage(imageFile);
                    if (nonAlignedImage != null)
                    {
                        var transform = node.RunPatternNode(nonAlignedImage);
                        node.FindAlignedImage(nonAlignedImage, transform, out Rect2 _, out HImage im1, out HImage im2, out HImage im3);
                        var alignedImage = im1.Compose3(im2, im3);
                        var imageName = System.IO.Path.GetFileNameWithoutExtension(imageFile);
                        alignedImage.GetImageSize(out int w, out int h);
                        HRegion alignedRegion = new HRegion(0,0.0,h,w);
                        var subRg = alignedRegion.Difference(node.Options.ReferenceMask.Region);
                        alignedImage.OverpaintRegion(subRg, new HTuple(0,0,0), "fill");
                        alignedImage.WriteImage("png", 0, System.IO.Path.Combine(targetDir, imageName + ".png"));
                    }
                    index++;
                }
            }catch (Exception ex)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action( () =>
                {
                    MessageBox.Show(this, ex.ToString(), "Error");
                }));
                
            }
            //ShowMessage = false;
        }
        TrainDeepPrintInspection train = new TrainDeepPrintInspection();
        public void Train()
        {
            ShowMessage = true;
            ExtractAlignedImage(GoodDir, GoodAlignDir,"good");
            HImage AvrImage = UpdateAverageImage(GoodAlignDir);
            node.Options.ReferenceImage = AvrImage;
            node.Prepare();
            ExtractAlignedImage(BadDir, BadAlignDir,"bad");
            node.TrainConfig.Save();
            Message = "Now trainning...";
            try
            {
                train.TrainPython(node.TrainConfig.ConfigDir, node.TrainConfig.ModelName.ToString(), (trainargs) =>
                {
                    if (trainargs.State == Python.TrainState.Cancel | trainargs.State == Python.TrainState.Error)
                    {
                        //ShowMessage = false;
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MessageBox.Show(this, "Train error!");
                        }));
                    }
                    if (trainargs.State == Python.TrainState.Completed)
                    {
                        //ShowMessage = false;
                        //node.runtime.State = ModelState.Unloaded;

                        PostTrain();
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MessageBox.Show(this, "Train Complete!");
                        }));

                    }
                    Message = "Trainning..." + trainargs.Progress + "%";
                    //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    //{
                    //    //this.progress.Value = (trainargs.Progress);
                    //    //this.lb_loss.Content = trainargs.Loss;

                    //}));
                });
            }catch(Exception ex)
            {

            }
            
            ShowMessage = false;

        }

        public void PostTrain()
        {
            node.runtime.LoadRecipe();          
        }

        private void btn_discard_pattern_Click(object sender, RoutedEventArgs e)
        {
            IsDrawing = false;
            editingRegion.Region.ClearDrawingData(window_display.HalconWindow);
            editingRegion.Region = new Rectange2(false, pre_rect.row, pre_rect.col, pre_rect.phi, pre_rect.length1, pre_rect.length2);
        }

        private void btn_open_image_folder_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", CurrentDir);
        }

        private void btn_change_mask_click(object sender, RoutedEventArgs e)
        {
            WindowRegionWindowInteractive wd = new WindowRegionWindowInteractive(node.Options.ReferenceImage, node.Options.ReferenceMask, null);
            wd.ShowDialog();
            UpdateMask(window_reference);
        }
    }


    public enum DeepPrintInspectionFolder
    {
        good,bad,good_aligned,bad_aligned
    }
    public class TrainDeepPrintInspection : TrainPythonBase
    {
        public async void TrainPython(string configDir, string model, Action<TrainingArgs> TrainUpdate)
        {
            if (IsTrainning)
                return;
            IsTrainning = true;
            try
            {
                var yamlDir = System.IO.Path.Combine(MainWindow.AppPath, "Designer/Python/AnomalyV3/tools/anomalib/models/" + model + "/config.yaml");
                var trainFile = System.IO.Path.Combine(MainWindow.AppPath, "Designer/Python/AnomalyV3/tools/train_printing.py");
                run_cmd("python.exe",
                  String.Format("\"{0}\"", trainFile) + " --model " + model + " --config " + String.Format("\"{0}\"", yamlDir) + " --config2 "

                               + String.Format("\"{0}\"", configDir), TrainUpdate);
            }
            catch (Exception ex)
            {
                TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.Error });
            }
            IsTrainning = false;
        }
        public void TrainConsole(string configDir, string model)
        {
            var yamlDir = System.IO.Path.Combine(MainWindow.AppPath, "Designer/Python/AnomalyV3/tools/anomalib/models/" + model + "/config.yaml");
            System.Diagnostics.Process.Start("cmd.exe", "/k python " + System.IO.Path.Combine(MainWindow.AppPath, "Designer/Python/AnomalyV3/tools/train_printing.py")+ " --model " + model + " --config " + yamlDir + " --config2 "
            + String.Format("\"{0}\"", configDir));
        }
        private void run_cmd(string cmd, string args, Action<TrainingArgs> TrainUpdate)
        {

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = cmd;//cmd is full path to python.exe
            start.Arguments = args;//args is path to .py file and any cmd line args
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = true;
            process = new Process();

            Regex regex = new Regex("=> STEP [0-9]+/[0-9]+");
            double LossDataUpdate = 999999;
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                    return;
                Console.WriteLine(e.Data);
                if (e.Data.Contains("ETA"))
                {

                    int index_loss = e.Data.IndexOf("loss: ");
                    if (index_loss >= 0)
                    {

                        var LossData = e.Data.Substring(index_loss + 6);
                        if (double.TryParse(LossData, out LossDataUpdate))
                        {

                        }


                    }
                }
                if (e.Data.Contains("=> STEP "))
                {

                    int index_loss = e.Data.IndexOf("total_loss: ");
                    if (index_loss >= 0)
                    {

                        var LossData = e.Data.Substring(index_loss + 12);
                        if (double.TryParse(LossData, out LossDataUpdate))
                        {

                        }


                    }
                    var match = regex.Match(e.Data);
                    if (match.Success)
                    {

                        var result = match.Value.Substring(8);
                        var epochdata = result;
                        var index = epochdata.IndexOf('/');
                        if (index >= 0)
                        {
                            var epoch = epochdata.Substring(0, index);
                            var epochtotal = epochdata.Substring(index + 1);

                            var epochint = int.Parse(epoch);
                            var epochtotalint = int.Parse(epochtotal);
                            if (int.TryParse(epoch, out epochint))
                            {
                                if (int.TryParse(epochtotal, out epochtotalint))
                                {
                                    if (epochint == 0 && epochtotalint == 0)
                                    {
                                        epochtotalint = 1;
                                        epochint = 1;
                                    }
                                    var TrainProgress = (double)epochint * 100 / epochtotalint;
                                    TrainUpdate?.Invoke(new TrainingArgs() { Progress = TrainProgress, State = TrainState.OnGoing, Loss = LossDataUpdate });
                                }
                            }

                        }

                    }
                }


            };
            process.ErrorDataReceived += (sender, e) =>
            {
                Console.WriteLine(e);
            };
            process.StartInfo = start;
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.Completed });
        }
    }
}
