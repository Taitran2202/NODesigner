using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace NOVisionDesigner.Designer.ValueConverters
{

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
    public class TriggerModeToBoolConverter : IValueConverter
    {


        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool?))
                throw new InvalidOperationException("The target must be a boolean");

            return ((string)value == "On");
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(string))
                throw new InvalidOperationException("The target must be a int");

            return (bool?)value == true ? "On" : "Off";
        }


    }
    public class IntToBoolConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool?))
                throw new InvalidOperationException("The target must be a boolean");

            return ((int)value == 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(int))
                throw new InvalidOperationException("The target must be a int");

            return (bool?)value == true ? 1 : 0;
        }

        #endregion
    }
    [ValueConversion(typeof(bool), typeof(bool?))]
    public class NullableBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool?))
                throw new InvalidOperationException("The target must be a boolean");

            return (bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
    [ValueConversion(typeof(double), typeof(string))]
    public class DoubleToIntConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(string))
                throw new InvalidOperationException("The target must be a boolean");

            return ((double)value).ToString("0.00");
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }


    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityCollapse : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
                throw new InvalidOperationException("The target must be a visible");

            if ((bool)value)
            {
                return Visibility.Collapsed;
            }
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InvertBoolToVisibilityCollapse : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
                throw new InvalidOperationException("The target must be a visible");

            if ((bool)value)
            {
                return Visibility.Visible;
            }
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InvertBoolToVisibilityVisible : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
                throw new InvalidOperationException("The target must be a visible");

            if ((bool)value)
            {
                return Visibility.Visible;
            }
            else
                return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }


    public class StringToColor : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(Color?) | targetType == typeof(Color))
            {
                Color temp = (Color)ColorConverter.ConvertFromString((string)value);
                string convert = string.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", temp.B, temp.A, temp.R, temp.G);
                return (Color)ColorConverter.ConvertFromString(convert);
            }
            throw new InvalidOperationException("The target must be a color");


        }


        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(string))
                throw new InvalidOperationException("The target must be a color");
            Color e = (Color)value;
            return string.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", e.R, e.G, e.B, e.A);
        }

        #endregion
    }
    public class StringToColor1 : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(Color?) | targetType == typeof(Color))
            {
                Color temp = (Color)ColorConverter.ConvertFromString((string)value);
                string convert = string.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", temp.B, temp.A, temp.R, temp.G);
                return (Color)ColorConverter.ConvertFromString(convert);
            }
            throw new InvalidOperationException("The target must be a color");


        }


        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            //if (targetType != typeof(string))
            //    throw new InvalidOperationException("The target must be a color");
            Color e = (Color)value;
            if (e != null)
                return string.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", e.R, e.G, e.B, e.A);
            else
                return "#000000ff";
        }

        #endregion
    }
    [ValueConversion(typeof(object), typeof(bool))]
    public class ObjectBool : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a bool");

            if ((object)value != null)
            {
                return true;
            }
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    [ValueConversion(typeof(Color), typeof(Brush))]
    public class ColorToBrush : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;
            if (targetType != typeof(Brush))
                throw new InvalidOperationException("The target must be a brush");

            return new SolidColorBrush((Color)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
    [ValueConversion(typeof(string), typeof(BitmapImage))]
    public class UriToBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.DecodePixelWidth = 100;
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.UriSource = new Uri(value.ToString());
            bi.EndInit();
            return bi;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    public class UnionModeConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {


            if (targetType != typeof(object))
            {
                throw new InvalidOperationException("The target must be a string");
            }
            if ((bool)value)
            {
                return "Minus";
            }
            return "Sum";

        }


        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a bool");
            if ((string)value == "Minus")
            {
                return true;
            }
            else
                return false;

        }
        #endregion
    }

    [ValueConversion(typeof(string), typeof(string))]
    public class AnomalyModelNameConverter : IValueConverter
    {
        static Dictionary<string, string> ModelDictionary = new Dictionary<string, string>()
        {
            {"MemSeg","Memory Segmentation" },
            {"MemSeg_v2","Memory Segmentation 2" },
            {"MemSeg_Pytorch","Memory Segmentation 3" },
            {"DiffusionAD","Diffusion AD" },
            {"FAPM","Fast Adaptive Patches Memory" },
            {"FAPM_Pytorch","Fast Adaptive Patches Memory 2" },
            {"tf_fastflow","Fast Flow" },
            {"RD","RD" },
            {"AESeg_Pytorch","Auto Encoder" },
            {"cfa","CFA" },
            {"EfficientAD","Efficient AD" }
            


        };
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            //if (targetType != typeof(string))
            //    throw new InvalidOperationException("The target must be a string");
            if (ModelDictionary.ContainsKey((string)value))
            {
                return ModelDictionary[(string)value];
            }
            else
            {
                return "Invalid Model Name";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (ModelDictionary.ContainsValue((string)value))
            {
                return ModelDictionary.FirstOrDefault(x=>x.Value == (string)value).Key;
            }
            else
            {
                return "Invalid Model Name";
            }
        }

        #endregion
    }
}
