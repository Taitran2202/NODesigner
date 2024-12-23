using Fluent;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using AVT.VmbAPINET;
using System.Web.UI.WebControls;

namespace NOVisionDesigner.Designer.Misc
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool)& targetType != typeof(bool?))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
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

            return ((uint)value == 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(uint))
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
    [ValueConversion(typeof(bool), typeof(RibbonGroupBoxState))]
    public class BoolToRibbonGroupBoxState : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(RibbonGroupBoxState))
                throw new InvalidOperationException("The target must be a groupboxstate");

            if ((bool)value)
            {
                return RibbonGroupBoxState.Collapsed;
            }
            else
                return RibbonGroupBoxState.Large;
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
    public class StringToEditValueColor : IValueConverter
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
            
            if (value != null & value!="")
            {
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.DecodePixelWidth = 100;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(value.ToString());
                bi.EndInit();
                return bi;
            }
            return null;
            
            
            
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
    public class EnumToBoolConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;
            
            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            return Enum.Parse(targetType, parameterString);
        }
        #endregion
    }
    public class EnumToVisibilityCollapseConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);
            if (parameterValue.Equals(value))
                return Orientation.Vertical;
            else return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            return Enum.Parse(targetType, parameterString);
        }
        #endregion
    }
    public class VmbFeatureFlagsToBoolConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //string parameterString = parameter as string;
            //if (parameterString == null)
            //    return DependencyProperty.UnsetValue;
            foreach(var val in (value as List<VmbFeatureFlagsType>))
            {
                if (Enum.IsDefined(val.GetType(), val) == false)
                    return DependencyProperty.UnsetValue;

                //object parameterValue = Enum.Parse(val.GetType(), parameterString);

                if (val==VmbFeatureFlagsType.VmbFeatureFlagsWrite||val==VmbFeatureFlagsType.VmbFeatureFlagsModifyWrite) return true;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            return Enum.Parse(targetType, parameterString);
        }
        #endregion
    }
    [ValueConversion(typeof(string), typeof(Uri))]
    public class StringToImageSource : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(ImageSource))
            {
                try
                {
                    var imageSource = (ImageSource)new ImageSourceConverter().ConvertFromString((string)value);
                    return imageSource;
                }catch(Exception ex)
                {

                }
                return null;
            }
            throw new InvalidOperationException("The target must be a uri");


        }


        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return null;
        }

        #endregion
    }

}
