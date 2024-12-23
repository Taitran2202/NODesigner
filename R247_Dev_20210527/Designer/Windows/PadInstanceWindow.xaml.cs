using DevExpress.Data;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Docking.VisualElements;
using DevExpress.Xpf.Editors;
using DevExpress.XtraEditors;
using DynamicData;
using DynamicData.Experimental;
using HalconDotNet;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NOVisionDesigner.Designer.Accquisition;
using NOVisionDesigner.Designer.Data;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Python;
using NOVisionDesigner.Windows;
using Org.BouncyCastle.Bcpg.Sig;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
    /// Interaction logic for PadInstanceWindow.xaml
    /// </summary>
    public partial class PadInstanceWindow : ThemedWindow,INotifyPropertyChanged
    {
        private FileSystemWatcher watcher1;
        private FileSystemWatcher watcher2;
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        bool _is_trainning;
        public bool IsTrainning
        {
            get
            {
                return _is_trainning;
            }
            set
            {
                if (_is_trainning != value)
                {
                    _is_trainning = value;
                    RaisePropertyChanged("IsTrainning");
                }
            }
        }
        bool _is_loading;
        public bool IsLoading
        {
            get
            {
                return _is_loading;
            }
            set
            {
                if (_is_loading != value)
                {
                    _is_loading = value;
                    RaisePropertyChanged("IsLoading");
                }
            }
        }
        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();
        PadInspection node;
        ObservableCollection<PadSegmentationFilmStrip> _list_image= new ObservableCollection<PadSegmentationFilmStrip>();
        public ObservableCollection<PadSegmentationFilmStrip> ListImage
        {
            get
            {
                return _list_image;
            }
            set
            {
                if (_list_image != value)
                {
                    _list_image = value;
                    RaisePropertyChanged("ListImage");
                }
            }
        }
        PadSegmentationFilmStrip _pre_image;
        PadSegmentationFilmStrip _selected_image;
        public PadSegmentationFilmStrip SelectedImage
        {
            get
            {
                return _selected_image;
            }
            set
            {
                if (_selected_image != value)
                {
                    
                    _selected_image = value;
                    
                    RaisePropertyChanged("SelectedImage");
                    SetSelectedImage(value);
                    _pre_image = value;
                }
            }
        }
        public PadInstanceWindow(PadInspection node)
        {
            this.node = node;
            InitializeComponent();
            this.DataContext = this;
            LoadImageList(node.InstanceConfig.ImageDir);
        }
        void LoadImageList(string dir)
        {
            var result = new ObservableCollection<PadSegmentationFilmStrip>();
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
                foreach (string file in files)
                {
                    result.Add(new PadSegmentationFilmStrip(file,node.InstanceConfig.ImageDir,node.InstanceConfig.AnnotationDir));
                }

            }
            ListImage = result;
        }

        
        public void SaveResult(PadSegmentationFilmStrip target)
        {
            if(target == null)
            {
                return;
            }
            target.SaveData();
        }
        private void SetSelectedImage(PadSegmentationFilmStrip selected)
        {
            if (selected != null)
            {
                //save previous result
                try
                {
                    SaveResult(_pre_image);
                }
                catch (Exception ex)
                {

                }
                //load current image
                try
                {
                    SelectedImage.LoadData();
                    window_display.HalconWindow?.AttachBackgroundToWindow(SelectedImage.RawImage);
                    SelectedImage.DisplayGT(window_display.HalconWindow, "#0000ff20");
                }
                catch (Exception ex)
                {
                    //image = new HImage();
                }
                //load current annotation
                


            }
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as PadSegmentationFilmStrip;
            if (vm != null)
            {
                try
                {
                    System.IO.File.Delete(vm.FullPath);
                    ListImage.Remove(vm);
                    if (System.IO.File.Exists(vm.MaskPath))
                    {
                        System.IO.File.Delete(vm.MaskPath);
                    }
                }
                catch (Exception ex)
                {

                }

            }

        }

        private void window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {

        }

        private void window_display_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {

        }

        private void window_display_MouseLeave(object sender, MouseEventArgs e)
        {

        }

        private void window_display_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {

        }

        private void window_display_HInitWindow(object sender, EventArgs e)
        {

        }

        private void btn_add_image_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image Files|*.BMP;*.JPG;*.GIF|All files|*.*";
            fileDialog.Multiselect = true;
            if (fileDialog.ShowDialog() == true)
            {
                Task.Run(new Action(() =>
                {
                    var files = fileDialog.FileNames;
                    IsLoading = true;
                    int count = 0;
                    int length = files.Length;
                    foreach (var file in files)
                    {
                        try
                        {
                            var Image = new HImage(file);
                            if (count == length - 1)
                            {
                                AddImage(Image,true);
                            }
                            else
                            {
                                AddImage(Image);
                            }
                           
                            count++;
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    IsLoading = false;
                }));
                

            }
        }

        private void btn_add_image_camera_Click(object sender, RoutedEventArgs e)
        {
            if (node.Image.Value != null)
            {
                try
                {
                    var image = Extensions.Functions.GetNoneEmptyHImage(node.Image);
                    AddImage(image,true);

                }
                catch (Exception ex)
                {

                }
            }
        }
        
        private void AddImage(HImage image,bool Prepend=false)
        {
            if (image != null)
            {
                var newFileName = Extensions.Functions.NewFileName(node.InstanceConfig.ImageDir);
                HRegion inspected_region = node.Regions.Region;
                HImage InspectImage;
                if (inspected_region.Area.Length == 0)
                {
                    InspectImage = image;
                }
                else if (inspected_region.Area == 0)
                {
                    InspectImage = image;

                }
                else
                {
                    inspected_region.Union1().SmallestRectangle1(out _, out _, out int r2, out int c2);
                    var image_croped = Extensions.Functions.CropImageWithRegionTranslate(image, inspected_region.Union1());
                    InspectImage = image_croped;

                }
                var newitem = new PadSegmentationFilmStrip(newFileName, node.InstanceConfig.ImageDir, node.InstanceConfig.AnnotationDir);


                var pad = node.ExtractPad(InspectImage);
                if (pad.weights!=null)
                {
                    HRegion padRegion = pad.GenRegion();
                    InspectImage.GetImageSize(out int w, out int h);
                    var mask = new HImage("byte", w, h);
                    mask.OverpaintRegion(padRegion, new HTuple(1), new HTuple("fill"));
                    newitem.MaskImage= mask;
                }
                newitem.AnnotationData = pad;
                InspectImage.WriteImage("png", 0, newFileName);
                newitem.RawImage = InspectImage;
                newitem.SaveData();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Prepend)
                    {
                        ListImage.Insert(0,newitem);
                    }
                    else
                    {
                        ListImage.Add(newitem);
                    }
                    
                });
                
            }
        }
        Nurbs RegionToSmoothNurbs(HRegion region)
        {
            //return region;
            region.GetRegionPolygon(5.0, out HTuple rows, out HTuple cols);
            //HXLDCont cont = new HXLDCont();
            //cont.GenContourNurbsXld(rows, cols, "auto", "auto", node.FitAngle, node.FitError, node.FitDistance);
            HTuple weights = new HTuple();
            if (weights.Length == 0)
            {
                weights = new HTuple(15.0);
                for (int i = 0; i < rows.Length - 2; i++)
                {
                    weights = weights.TupleConcat(15.0);
                }
            }
            var nurbs =  new Nurbs(false) { rows = rows,cols = cols,weights=weights };
            nurbs.CreateRegion();
            return nurbs;
        }
        private void btn_edit_region_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedImage != null)
            {
                var Pad = new Nurbs(false) { rows= SelectedImage.AnnotationData.rows, cols = SelectedImage.AnnotationData.cols,
                    weights = SelectedImage.AnnotationData.weights
                };
                WindowNurbs wd = new WindowNurbs(SelectedImage.RawImage,
                    Pad, new HHomMat2D(), this, true);
                if(wd.ShowDialog() == true)
                {
                    SelectedImage.AnnotationData = new NurbData() { rows = Pad.rows, cols = Pad.cols,weights=Pad.weights };
                    SelectedImage.UpdateMask();
                }
                
            }
            
        }

        private void btn_clear_region_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_zoom_in_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_zoom_out_Click(object sender, RoutedEventArgs e)
        {

        }
        TrainNOVISION train;
        BitmapImage LoadImage(string path)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        private void DisplayImage(HSmartWindowControlWPF imageControl, string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    imageControl.HalconWindow.DispObj(new HImage(path));
                }
                else
                {
                    // Handle case when the image file is missing
                    imageControl.HalconWindow.ClearWindow();
                }
            }catch(Exception ex)
            {

            }
            
        }
        private void btn_train_Click(object sender, RoutedEventArgs e)
        {
            ConvertToCOCO();
            Task.Run(new Action(() =>
            {
                FileSystemWatcher watcher = new FileSystemWatcher(node.InstanceConfig.ModelDir);
                watcher.NotifyFilter = NotifyFilters.LastWrite; 
                watcher.Changed += (o1, e1) =>
                {
                    if (e1.Name == "original_img_with_epoch.jpg")
                    {
                        DisplayImage(im1,e1.FullPath);
                    }
                    else
                    {
                        if (e1.Name == "predict_img_with_epoch.jpg")
                        {
                            DisplayImage(im2,e1.FullPath);
                        }
                    }
                    

                };
                watcher.Created += (o1, e1) =>
                {
                    if (e1.Name == "original_img_with_epoch.jpg")
                    {
                        DisplayImage(im1, e1.FullPath);
                    }
                    else
                    {
                        if (e1.Name == "predict_img_with_epoch.jpg")
                        {
                            DisplayImage(im2, e1.FullPath);
                        }
                    }


                };



                // Start watching for changes
                watcher.EnableRaisingEvents = true;

                train = new TrainNOVISION();
                IsTrainning = true;
                node.InstanceConfig.ModelType = "pad-segmentation";
                node.InstanceConfig.Save();
                
                train.TrainPython("segmentation",node.InstanceConfig.ConfigDir, "yolo_nas_seg", (trainargs) =>
                {
                    if (trainargs.State == Python.TrainState.Cancel | trainargs.State == Python.TrainState.Error)
                    {
                        IsTrainning = false;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this, "Training was cancel because of error!", 
                                "Warning", MessageBoxButton.OK, icon: MessageBoxImage.Warning);
                        });

                    }
                    if (trainargs.State == TrainState.OnGoing)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Logs.Add(trainargs.Log);
                            if (log_box.VerticalOffset == log_box.ScrollableHeight)
                            { log_box.ScrollToEnd(); }
                        });

                    }
                    if (trainargs.State == Python.TrainState.Completed)
                    {
                        IsTrainning = false;
                        node.AnomalyRuntime.State = ModelState.Unloaded;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this, "Train complete!", "Info", MessageBoxButton.OK, icon: MessageBoxImage.Information);
                        });

                    }
                });
                IsTrainning = false;
            }));
        }
        void ConvertToCOCO()
        {
            int imageID = 0;
            int annotationID = 0;
            var categories = new COCOCategory[1];
            var images = new List<COCOImage>();
            var annotations = new List<COCOAnnotation>();
            categories[0] = new COCOCategory() { id = 0,name="pad",supercategory="" };
            var count = ListImage.Count;
            int duplicates = 100 / count;
            foreach (var item in ListImage)
            {
                var AnnotationPath = item.AnnotationPath;
                NurbData AnnotationData=null;
                if (System.IO.File.Exists(AnnotationPath))
                {
                    try
                    {
                        string json = File.ReadAllText(AnnotationPath);
                        // Deserialize JSON to Person object
                        AnnotationData = JsonConvert.DeserializeObject<NurbData>(json);
                    }
                    catch (Exception ex)
                    {
                        
                    }

                }
                if (AnnotationData != null)
                {
                    AnnotationData.GenRegion().GetRegionPolygon(1.0, out HTuple rows, out HTuple cols);
                    
                    double[,] segmentation = new double[1,rows.Length*2];
                    for (int i = 0; i < rows.Length; i++)
                    {
                        segmentation[0,i*2] = cols[i];
                        segmentation[0,i*2+1] = rows[i];
                    }
                    for (int j = 0;j < duplicates+1; j++)
                    {
                        images.Add(new COCOImage()
                        {
                            file_name = item.FileName,
                            width = AnnotationData.image_width,
                            height = AnnotationData.image_height,
                            id = imageID,
                            licence = 0
                        });
                        annotations.Add(new COCOAnnotation()
                        {
                            id = annotationID,
                            image_id = imageID,
                            category_id = 0,
                            bbox = new double[4] { cols.TupleMin(), rows.TupleMin(), cols.TupleMax() - cols.TupleMin(), rows.TupleMax() - rows.TupleMin() },
                            segmentation = segmentation
                        });
                        annotationID++;
                        imageID++;
                    }
                    
                    

                }
            }
            JObject COCOjson = new JObject(
            new JProperty("categories", JArray.FromObject(categories)),
            new JProperty("annotations", JArray.FromObject(annotations)),
            new JProperty("images", JArray.FromObject(images))
            );
            string jsonString = COCOjson.ToString(Formatting.None);
            File.WriteAllText(node.InstanceConfig.AnnotationFile, jsonString);
        }

        private void btn_train_config_Click(object sender, RoutedEventArgs e)
        {
            PropertiesWindow wd = new PropertiesWindow(node.InstanceConfig);
            wd.Show(); 
        }

        private void btn_cancel_train_Click(object sender, RoutedEventArgs e)
        {
            train?.Cancel();
        }

        private void btn_add_image_recorder_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    public class COCOSegmentation
    {
        public COCOCategory[] categories;

    }
    public class COCOImage
    {
        public string file_name { get; set; }
        public int id { get; set; }
        public int height { get; set; }
        public int width { get; set; }
        public int licence { get; set; } = 1;
    }
    public class COCOAnnotation
    {
        public int id { get; set; }
        public int image_id { get; set; }
        public int category_id { get; set; }
        public double[] bbox { get; set; }
        public double area { get; set; }
        public double[,] segmentation { get; set; }
    }
    public class COCOCategory
    {
        public string name { get; set; }
        public int id { get; set; }
        public string supercategory { get; set; }
    }
    public class NurbData
    {
        public int image_width { get; set; }
        public int image_height { get; set; }
        public double[] rows { get; set; }
        public double[] cols { get; set; }
        public double[] weights { get; set; }
        public HRegion GenRegion()
        {
            HXLDCont cont = new HXLDCont();
            cont.GenContourNurbsXld(rows, cols, "auto", "auto", 3, 1, 5);
            return cont.GenRegionContourXld("filled");
        }
    }
    public class PadSegmentationFilmStrip : INotifyPropertyChanged
    {
        public HImage MaskImage;
        public HImage RawImage;
        HRegion thresh;
        public int im_w, im_h;
        public NurbData AnnotationData { get; set; } = new NurbData();
        public void UpdateMask()
        {
            RawImage.GetImageSize(out  im_w, out im_h);
            MaskImage = new HImage("byte",im_w,im_h);
            try
            {
                var region = AnnotationData.GenRegion();
                MaskImage.OverpaintRegion(region, 1.0, "fill");
            }catch(Exception ex)
            {

            }
            
        }
        public void SaveData()
        {
            UpdateMask();
            RawImage.GetImageSize(out im_w, out im_h);
            AnnotationData.image_height = im_h;
            AnnotationData.image_width = im_w;
            MaskImage?.WriteImage("png", 0, MaskPath);
            string json = JsonConvert.SerializeObject(AnnotationData, Formatting.Indented);
            File.WriteAllText(AnnotationPath, json);
        }
        public void DisplayGT(HWindow display,string color="#200000ff")
        {
            if (RawImage == null)
                return;
            if (display == null)
            {
                return;
            }
            display.ClearWindow();
            //if (thresh == null)
            //{
                thresh = MaskImage.Threshold(1.0, 255.0);
            //}
            
            display.SetColor(color);
            display.SetDraw("fill");
            display.DispObj(thresh);
            display.SetColor("#0000ff90");
            display.SetDraw("margin");
            display.SetLineWidth(2);
            display.DispObj(thresh);
        }
        public void LoadData()
        {
            RawImage = new HImage(FullPath);
            RawImage.GetImageSize(out im_w, out im_h);

            try
            {
                if (System.IO.File.Exists(MaskPath))
                {
                    try
                    {
                        MaskImage = new HImage(MaskPath);
                    }
                    catch (Exception ex)
                    {
                        MaskImage = new HImage("byte", im_w, im_h);
                    }

                }
                else
                {
                    MaskImage = new HImage("byte", im_w, im_h);
                }
                if (System.IO.File.Exists(AnnotationPath))
                {
                    try
                    {
                        string json = File.ReadAllText(AnnotationPath);

                        // Deserialize JSON to Person object
                        AnnotationData = JsonConvert.DeserializeObject<NurbData>(json);
                    }
                    catch (Exception ex)
                    {
                        AnnotationData = new NurbData();
                    }

                }
                else
                {
                    AnnotationData = new NurbData();
                }


            }
            catch (Exception ex)
            {

            }
        }
        public static object image_loader = new object();
        public void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        string _mask_path;


        public string MaskPath
        {
            get
            {
                return _mask_path;
            }
            set
            {
                if (_mask_path != value)
                {
                    _mask_path = value;

                }
            }
        }
        string _annotation_path;


        public string AnnotationPath
        {
            get
            {
                return _annotation_path;
            }
            set
            {
                if (_annotation_path != value)
                {
                    _annotation_path = value;

                }
            }
        }

        string _full_path;


        public string FullPath
        {
            get
            {
                return _full_path;
            }
            set
            {
                if (_full_path != value)
                {
                    _full_path = value;

                }
            }
        }
        string file_name;
        public string FileName
        {
            get
            {
                return file_name;
            }
            set
            {
                if (file_name != value)
                {
                    file_name = value;
                    RaisePropertyChanged("FileName");
                }
            }
        }

        bool _is_loaded = true;
        public bool IsLoaded
        {
            get
            {
                return _is_loaded;
            }
            set
            {
                if (_is_loaded != value)
                {
                    _is_loaded = value;
                    RaisePropertyChanged("IsLoaded");
                }
            }
        }
        private System.Windows.Media.ImageSource _image;
        public System.Windows.Media.ImageSource Image
        {
            get
            {
                if (IsLoaded)
                {
                    Task.Run(new Action(() =>
                    {
                        lock (image_loader)
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.DecodePixelHeight = 140;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(_full_path);
                            bitmap.EndInit();
                            bitmap.Freeze();
                            //Thread.Sleep(500);
                            IsLoaded = false;
                            Image = bitmap;

                        }
                    }));
                    return null;
                }

                return _image;

            }
            internal set
            {
                _image = value;
                RaisePropertyChanged("Image");
            }

        }
        public PadSegmentationFilmStrip(string FullPath, string ImageDir,string AnnotationDir)
        {
            this.FullPath = FullPath;
            
            try
            {
                FileName = System.IO.Path.GetFileName(FullPath);
                this.MaskPath = System.IO.Path.Combine(AnnotationDir, FileName + ".png");
                this.AnnotationPath = System.IO.Path.Combine(AnnotationDir, FileName + ".json");
            }
            catch (Exception ex)
            {
                FileName = "Error File Name";
            }

        }
        public static BitmapImage Convert(System.Drawing.Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}
