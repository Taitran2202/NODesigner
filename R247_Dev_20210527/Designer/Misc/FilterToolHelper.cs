using HalconDotNet;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Misc
{
    public class MultiImageFilter : HelperMethods, INotifyPropertyChanged
    {
        bool _is_enabled_tool = true;
        public bool IsEnabledTool
        {
            get
            {
                return _is_enabled_tool;
            }
            set
            {
                if (_is_enabled_tool != value)
                {
                    _is_enabled_tool = value;
                    RaisePropertyChanged("IsEnabledTool");
                }
            }
        }
        public void ClearAll()
        {
            tools.Clear();
        }

        //public InputBlock<HImage, HHomMat2D> InputBlock { get; set; }
        //public Input<HImage> InputImage { get { return InputBlock.Target1; } set { InputBlock.Target1 = value; } }
        //public Input<HHomMat2D> InputFixture { get { return InputBlock.Target2; } set { InputBlock.Target2 = value; } }
        public static List<int> tools = new List<int>();
        public ObservableCollection<IMultiImageFilter> filters = new ObservableCollection<IMultiImageFilter>();

        CollectionOfregion _regions = new CollectionOfregion();
        public CollectionOfregion Regions
        {
            get
            {
                return _regions;
            }
            set
            {
                if (_regions != value)
                {
                    _regions = value;
                    RaisePropertyChanged("Regions");
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }
        public int tool_num;
        public HWindow display;
        #region Properties  
        //public Output<HImage> OutputImage { get; set; }
        string _name;

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    //if (OutputImage != null)
                    //    OutputImage.Name = value + ".OutputImage";
                    RaisePropertyChanged("Name");

                }
            }
        }

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
            int temp = item.DeserializeTuple();
            filters.Clear();
            for (int i = 0; i < temp; i++)
            {
                HTuple temp2 = item.DeserializeTuple();
                IMultiImageFilter loaded = null;
                switch (temp2.S)
                {
                    case ("Gauss"): loaded = new GaussImage(); break;
                    case ("Deriche1"): loaded = new Deriche1(); break;
                    case ("Deriche2"): loaded = new Deriche2(); break;
                    case ("Shen"): loaded = new Shen(); break;
                    case ("SelectChannel"): loaded = new SelectChannel(); break;
                    case ("DerivateGauss"): loaded = new DerivateGauss(); break;
                    case ("ErodeRectImage"): loaded = new ErodeRectImage(); break;
                    case ("DilationRect"): loaded = new DilationRectImage(); break;
                    case ("DilationOctagon"): loaded = new DilationOctagonImage(); break;
                    case ("Binomial"): loaded = new BinomialImage(); break;
                    case ("Canny"): loaded = new CannyImage(); break;
                    case ("Laplace"): loaded = new Laplace(); break;
                    case ("LaplaceImage"): loaded = new LaplaceImage(); break;
                    case ("MedianRectangle"): loaded = new MedianRectangleImage(); break;
                    case ("Invert"): loaded = new InvertImage(); break;
                        //case ("ExtractColor"): loaded = new ExtractColor(); break;
                }
                if (loaded != null)
                {
                    loaded.Load(item);
                    filters.Add(loaded);
                }
            }
        }

        public void OnLoadComplete()
        {
            foreach (IMultiImageFilter item in filters)
            {
                item.OnLoadComplete();
            }
            // throw new NotImplementedException();
        }

        public void Remove()
        {
            // throw new NotImplementedException();
        }

        public void Run()
        {
            //  throw new NotImplementedException();
            //if (image_data!=null)
            //{
            //    if (fixture==null)
            //    {
            //        HImage smoothed = image_data.GaussFilter(_size);
            //        display?.DispObj(smoothed);
            //        OutputImage.Notify(smoothed);
            //    }
            //    else
            //    {
            //        HImage smoothed = image_data.AffineTransImage(fixture,"constant","false").GaussFilter(_size);
            //        display?.DispObj(smoothed);
            //        OutputImage.Notify(smoothed);
            //    }
            //}

        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
            (new HTuple(filters.Count)).SerializeTuple().FwriteSerializedItem(file);

            foreach (IMultiImageFilter filter_temp in filters)
            {
                new HTuple(filter_temp.Type).SerializeTuple().FwriteSerializedItem(file);
                filter_temp.Save(file);
            }

        }
        #region Constructor
        public MultiImageFilter()
        {

            for (int i = 1; i < 100; i++)
            {
                if (!tools.Contains(i))
                {
                    tools.Add(i);
                    tool_num = i;
                    this.Name = "ImageFilter" + i.ToString();
                    break;
                }
            }


            //OutputImage = new Output<HImage>(this.Name + ".OutputImage");
            //InputBlock = new InputBlock<HImage, HHomMat2D>(this.Name);
            //InputBlock.OnDataChanged = OnDataRecieve;
            //InputBlock.BlockReset += OnInputReset;
        }
        bool _display_result = true;
        public bool DisplayResult
        {
            get
            {
                return _display_result;
            }
            set
            {
                if (_display_result != value)
                {
                    _display_result = value;
                    RaisePropertyChanged("DisplayResult");
                }
            }
        }

        public void OnInputReset(object sender, EventArgs e)
        {
            //OutputImage.Reset();
        }
        public HImage UpdateSingle(HImage image, HHomMat2D transform, IMultiImageFilter filter)
        {

            if (filter == null)
                return image;
            if (transform == null)
            {
                HImage image_reduced = image.ReduceDomain(Regions.Region);


                return filter.Run(image_reduced);
            }
            else
            {
                HImage image_reduced = image.ReduceDomain(transform.AffineTransRegion(Regions.Region, "constant"));
                return filter.Run(image_reduced);
            }

        }
        public HImage Update(HImage image, HHomMat2D transform)
        {

            if (transform == null)
            {
                HImage image_reduced = image.ReduceDomain(Regions.Region);

                foreach (IMultiImageFilter filter in filters)
                {
                    image_reduced = filter.Run(image_reduced);
                }
                return image_reduced;
            }
            else
            {             
                HImage image_reduced = image.ReduceDomain(transform.AffineTransRegion(Regions.Region, "constant"));
                foreach (IMultiImageFilter filter in filters)
                {
                    image_reduced = filter.Run(image_reduced);
                }
                return image_reduced;
            }

        }
        public void OnDataRecieve(Tuple<HImage, HHomMat2D> data, NotifyMessage e)
        {
            // if (data.Item1)
            if (data.Item1 != null)
            {
                if (data.Item2 == null)
                {
                    HImage image_reduced = data.Item1.ReduceDomain(Regions.Region);

                    foreach (IMultiImageFilter filter in filters)
                    {
                        if (filter.IsEnabled)
                            image_reduced = filter.Run(image_reduced);
                    }
                    if (DisplayResult)
                        display?.DispObj(image_reduced);
                    //OutputImage.Notify(image_reduced, e);
                }
                else
                {
                    HImage image_reduced = data.Item1.ReduceDomain(data.Item2.AffineTransRegion(Regions.Region, "constant"));
                    foreach (IMultiImageFilter filter in filters)
                    {
                        if (filter.IsEnabled)
                            image_reduced = filter.Run(image_reduced);
                    }
                    if (DisplayResult)
                        display?.DispObj(image_reduced);
                    //OutputImage.Notify(image_reduced, e);
                }
            }
        }


        #endregion
    }
    public interface IMultiImageFilter
    {
        void CustomEditor(HImage image);
        //EventHandler ParameterChanged { get; set; }
        //UserControl View { get; set; }
        bool IsEnabled { get; set; }
        HImage Run(HImage image);
        void Save(HFile file);
        void Load(DeserializeFactory item);
        string Type { get; set; }
        string Name { get; set; }
        void OnLoadComplete();
    }
    public class FilterBase : HelperMethods
    {

        public virtual void CustomEditor(HImage image)
        {
            //DXMessageBox.Show("This filter does not support custom edit!");
        }
        public virtual void OnLoadComplete()
        {

        }
        public bool IsEnabled { get; set; } = true;
    }
    public class GaussImage : FilterBase, IMultiImageFilter
    {


        public string Type { get => "Gauss"; set { } }
        public string Name { get; set; } = "Gauss";

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
        [Description("The smoothing effect increases with increasing Size value. The following Size value are avaiable: 3, 5, 7, 9, 11.")]
        public int Size { get; set; } = 3;
        public HImage Run(HImage image)
        {
            return image.GaussImage(Size);
        }

        public void Save(HFile file)
        {

            SaveParam(file, this);
        }

    }
    public class Deriche1 : FilterBase, IMultiImageFilter
    {
        public string Type { get => "Deriche1"; set { } }
        public string Name { get; set; } = "Deriche1";

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }

        public double Alpha { get; set; } = 3;
        public HImage Run(HImage image)
        {
            return image.SmoothImage("deriche1", Alpha);
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
        }

    }
    public class Deriche2 : FilterBase, IMultiImageFilter
    {
        public string Type { get => "Deriche2"; set { } }
        public string Name { get; set; } = "Deriche2";

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }

        public double Alpha { get; set; } = 3;
        public HImage Run(HImage image)
        {
            return image.SmoothImage("deriche2", Alpha);
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
        }

    }
    public class Shen : FilterBase, IMultiImageFilter
    {
        public string Type { get => "Shen"; set { } }
        public string Name { get; set; } = "Shen";

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }

        public double Alpha { get; set; } = 3;
        public HImage Run(HImage image)
        {
            return image.SmoothImage("shen", Alpha);
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
        }

    }
    public class SelectChannel : FilterBase, IMultiImageFilter
    {
        public string Type { get => "SelectChannel"; set { } }
        public string Name { get; set; } = "SelectChannel";

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }

        public int Channel { get; set; } = 1;
        public HImage Run(HImage image)
        {
            return image.AccessChannel(Channel);
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
        }

    }
    public class DilationRectImage : FilterBase, IMultiImageFilter
    {
        public string Type { get => "DilationRect"; set { } }
        public string Name { get; set; } = "Dilation Rectangle";

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
        public int Width { get; set; } = 3;
        public int Height { get; set; } = 3;
        public HImage Run(HImage image)
        {
            return image.GrayDilationRect(Height, Width);
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
        }

    }
    public class ErodeRectImage : FilterBase, IMultiImageFilter
    {
        public string Type { get => "ErodeRectImage"; set { } }
        public string Name { get; set; } = "Erode Rectangle";

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
        public int Width { get; set; } = 3;
        public int Height { get; set; } = 3;
        public HImage Run(HImage image)
        {
            return image.GrayErosionRect(Height, Width);
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
        }

    }

    public class DilationOctagonImage : FilterBase, IMultiImageFilter
    {
        public string Type { get => "DilationOctagon"; set { } }
        public string Name { get; set; } = "Dilation Octagon";

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
        public double Width { get; set; } = 3;
        public double Height { get; set; } = 3;
        public HImage Run(HImage image)
        {
            return image.GrayDilationShape(Height, Width, "octagon");
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
        }

    }
    public class BinomialImage : FilterBase, IMultiImageFilter
    {
        public string Type { get => "Binomial"; set { } }
        public string Name { get; set; } = "Binomial";

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
        public int Width { get; set; } = 3;
        public int Height { get; set; } = 3;
        public HImage Run(HImage image)
        {
            return image.BinomialFilter(Height, Width);
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
        }

    }
    public class CannyImage : FilterBase, IMultiImageFilter
    {
        public string Type { get => "Canny"; set { } }
        public string Name { get; set; } = "Canny";

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
        public int Iterations { get; set; } = 10;
        public double Sigma { get; set; } = 1;
        public double Theta { get; set; } = 0.5;
        public HImage Run(HImage image)
        {
            return image.ShockFilter(Theta, Iterations, "canny", Sigma);
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
        }

    }
    public class LaplaceImage : FilterBase, IMultiImageFilter
    {
        public string Type { get => "LaplaceImage"; set { } }
        public string Name { get; set; } = "LaplaceImage";

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
        public int Iterations { get; set; } = 10;
        public double Sigma { get; set; } = 1;
        public double Theta { get; set; } = 0.5;
        public HImage Run(HImage image)
        {
            return image.ShockFilter(Theta, Iterations, "laplace", Sigma);
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
        }

    }
    public class MedianRectangleImage : FilterBase, IMultiImageFilter
    {
        public string Type { get => "MedianRectangle"; set { } }
        public string Name { get; set; } = "Median Rectangle";

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
        public int Width { get; set; } = 3;
        public int Height { get; set; } = 3;
        public HImage Run(HImage image)
        {
            return image.MedianRect(Width, Height);
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
        }

    }
    public class InvertImage : FilterBase, IMultiImageFilter
    {
        public string Type { get => "Invert"; set { } }
        public string Name { get; set; } = "Invert";

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
        public HImage Run(HImage image)
        {
            return image.InvertImage();
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
        }

    }
    public class DerivateGauss : FilterBase, IMultiImageFilter
    {
        public string Type { get => "DerivateGauss"; set { } }
        public string Name { get; set; } = "DerivateGauss";

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
        public double Sigma { get; set; } = 1;
        public string Component { get; set; } = "x";
        public HImage Run(HImage image)
        {
            return image.DerivateGauss(Sigma, Component);
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
        }

    }
    public enum LaplaceResultType
    {
        absolute, absolute_binomial
    }
    public enum LaplaceFilterMask
    {
        n_4, n_8, n_8_isotropic
    }
    public class Laplace : FilterBase, IMultiImageFilter
    {
        public string Type { get => "Laplace"; set { } }
        public string Name { get; set; } = "Laplace";
        public LaplaceResultType ResultType { get; set; } = LaplaceResultType.absolute;
        public LaplaceFilterMask FilterMask { get; set; } = LaplaceFilterMask.n_4;
        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
        public double MarkSize { get; set; } = 1;
        public HImage Run(HImage image)
        {
            return image.Laplace(ResultType.ToString(), MarkSize, FilterMask.ToString());
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);
        }

    }
    //public class ExtractColor : FilterBase, IMultiImageFilter
    //{

    //    public override void CustomEditor(HImage image)
    //    {
    //        if (ClassLUTColor == null)
    //            ClassLUTColor = new HClassLUT(ClassMLP, new HTuple(), new HTuple());
    //        TrainColorMlpWindow wd = new TrainColorMlpWindow(ClassMLP, ClassLUTColor, image, null);
    //        wd.ShowDialog();
    //    }
    //    public ExtractColor()
    //    {
    //        ClassMLP = new HClassMlp(3, 10, 2, "softmax", "normalization", 10, 42);
    //    }
    //    public override void OnLoadComplete()
    //    {
    //        if (ClassLUTColor == null)
    //        {

    //            ClassLUTColor = new HClassLUT(ClassMLP, new HTuple(), new HTuple());
    //        }
    //        else
    //        {

    //            ClassLUTColor.CreateClassLutMlp(ClassMLP, new HTuple(), new HTuple());
    //        }
    //    }
    //    public string Type { get => "ExtractColor"; set { } }
    //    public string Name { get; set; } = "ExtractColor";
    //    public void Load(DeserializeFactory item)
    //    {
    //        LoadParam(item, this);
    //    }
    //    public HClassLUT ClassLUTColor { get; set; }


    //    public HClassMlp ClassMLP
    //    {
    //        get; set;
    //    }
    //    public double MinSize { get; set; } = 5;
    //    public double ClosingCircle { get; set; } = 2.5;
    //    public HImage Run(HImage image)
    //    {
    //        if (ClassLUTColor == null)
    //            return image;
    //        HRegion region_color = ClassLUTColor.ClassifyImageClassLut(image).SelectObj(2).ClosingCircle(ClosingCircle).Connection();
    //        //HXLDCont lines = data.Item1.ReduceDomain(region_inspection).LinesColor(1.5, 15, 20, "true", "true").UnionCollinearContoursXld(10, 1, 2, 0.1, "attr_keep").SelectContoursXld("contour_length", 100, 999999999, 100, 200).GenRegionContourXld("filled").Connection();
    //        HRegion region_color_select = region_color.SelectShape("area", "and", MinSize, 999999999999).Union1();
    //        HRegion domain = image.GetDomain();
    //        HImage background = image.Rgb1ToGray().PaintRegion(domain, new HTuple(0), new HTuple("fill"));
    //        if (region_color_select.CountObj() > 0)
    //            return background.PaintRegion(region_color_select, new HTuple(255), new HTuple("fill"));
    //        else
    //            return background;


    //    }

    //    public void Save(HFile file)
    //    {
    //        SaveParam(file, this);
    //    }

    //}

}
