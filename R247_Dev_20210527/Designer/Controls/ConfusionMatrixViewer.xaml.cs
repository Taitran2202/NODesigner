using Newtonsoft.Json;
using NOVisionDesigner.Designer.Windows;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Controls
{
    /// <summary>
    /// Interaction logic for ConfusionMatrixViewer.xaml
    /// </summary>
    public partial class ConfusionMatrixViewer : UserControl
    {
        //ConfusionMatrixViewModel ViewModel = new ConfusionMatrixViewModel();
        public SelectionChangedEventHandler SelectionChanged;
        public ConfusionMatrixViewer()
        {
            InitializeComponent();
            //this.DataContext = ViewModel;
            list_confusion_matrix.SelectionChanged += List_confusion_matrix_SelectionChanged;
        }

        private void List_confusion_matrix_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(sender, e);
        }

        

        public void CreateLayout(int num_sample,List<string> classes,List<ClassificationResult> results)
        {
           
            int numclasses = classes.Count;
            var ViewModel = new ConfusionMatrixViewModel() { Rows = numclasses, Columns = numclasses };
            grid_actual_label.Rows = numclasses;
            grid_predict_label.Columns=numclasses;
            grid_actual_label.Children.Clear();
            grid_predict_label.Children.Clear();
            int num_classes = classes.Count;
            for (int i = 0; i < numclasses; i++)
            {
                grid_predict_label.Children.Add(new Label() { Content = classes[i],
                    VerticalAlignment= VerticalAlignment.Center, 
                    HorizontalAlignment = HorizontalAlignment.Center });

                grid_actual_label.Children.Add(new Label() { Content = classes[i],
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

            }
            lb_num_sample.Content = num_sample;

            //fill confusion matrix data
            List<ConfusionMatrixPoint> confusionData = new List<ConfusionMatrixPoint>();
            for(int i=0;i< numclasses; i++)
            {
                for(int j = 0; j < numclasses; j++)
                {
                    string label = classes[i];
                    string predict = classes[j];
                    var data= results.Where(x => x.Label == label & x.Predict == predict);
                    ConfusionMatrixPoint points = new ConfusionMatrixPoint()
                    {
                        Count = data.Count(),
                        Data = data.ToList()
                    };
                    confusionData.Add(points);

                }
            }

            var diagsum = 0;
            for(int i = 0; i < numclasses; i++)
            {
                diagsum += confusionData[i * numclasses + i].Count;
            }
            Acc = (double)diagsum / confusionData.Sum(x => x.Count);

            PrecisionList = new List<ClassSummary>();
            for (int i = 0; i < numclasses; i++)
            {
                var sumrow = 0;
                var sumcol = 0;
                for(int j=0;j < numclasses; j++)
                {
                    sumrow += confusionData[i * numclasses + j].Count;
                    sumcol += confusionData[i + numclasses * j].Count;
                }
                var summary = new ClassSummary()
                {
                    Name = classes[i],
                    Precision = confusionData[i * numclasses + i].Count * 1.0 / sumrow,
                    Recall = confusionData[i * numclasses + i].Count * 1.0 / sumcol

                };
                summary.F1 = 2 * summary.Precision * summary.Recall / (summary.Precision + summary.Recall);
                PrecisionList.Add(summary);
                F1 = PrecisionList.Average(x => x.F1);
            }
            

            ViewModel.LstConfusionData = confusionData;
            this.DataContext = ViewModel;
        }
        public List<ClassSummary> PrecisionList;
        public double Acc;
        public double F1;

    }
    public class ClassCount
    {
        public string Name { get; set; }
        public int Value { get;set; }
    }
    public class ClassSummary
    {
        public string Name { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double F1 { get; set; }
    }
    public class ConfusionMatrixViewModel:INotifyPropertyChanged
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        int _Rows;
        public int Rows
        {
            get
            {
                return _Rows;
            }
            set
            {
                if (_Rows != value)
                {
                    _Rows = value;
                    RaisePropertyChanged("Rows");
                }
            }
        }
        int _columns;
        public int Columns
        {
            get
            {
                return _columns;
            }
            set
            {
                if (_columns != value)
                {
                    _columns = value;
                    RaisePropertyChanged("Columns");
                }
            }
        }
        List<ConfusionMatrixPoint> _lst_confusion_data;
        public List<ConfusionMatrixPoint> LstConfusionData
        {
            get
            {
                return _lst_confusion_data;
            }
            set
            {
                if (_lst_confusion_data != value)
                {
                    _lst_confusion_data = value;
                    RaisePropertyChanged("LstConfusionData");
                }
            }
        }

    }
    public class ClassificationResult:INotifyPropertyChanged
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public string OriginalImageSource { get; set; }
        public string FileName { get; set; }
        public string Label { get; set; }
        public string Predict { get; set; }
        public string Image { get; set; }
        public static object image_loader = new object();
        bool _is_loaded = false;

        private System.Windows.Media.ImageSource _image_bitmap;
        [JsonIgnore]
        public System.Windows.Media.ImageSource ImageBitmap
        {
            get
            {
                if (!_is_loaded)
                {
                    Task.Run(new Action(() =>
                    {
                        lock (image_loader)
                        {
                            if (System.IO.File.Exists(Image))
                            {
                                BitmapImage bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.DecodePixelHeight = 140;
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.UriSource = new Uri(Image);
                                bitmap.EndInit();
                                bitmap.Freeze();
                                //Thread.Sleep(500);
                                _is_loaded = true;
                                ImageBitmap = bitmap;
                            }
                            else
                            {
                                _is_loaded = true;
                                _image_bitmap = null;
                            }
                            

                        }
                    }));
                    return null;
                }

                return _image_bitmap;

            }
            internal set
            {
                _image_bitmap = value;
                RaisePropertyChanged("ImageBitmap");
            }

        }
        public string PredictImage { get; set; }
        public double Probability { get; set; }
    }
    public class ConfusionMatrixPoint
    {
        public int Count { get; set; }
        public List<ClassificationResult> Data { get; set; }
    }

}
