using HalconDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Misc
{
    public class Calibration :  INotifyPropertyChanged,IHalconDeserializable
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private double actual_height;
        public Calibration()
        {
            //TextureRegion.GenEmptyRegion();
        }
        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public void LoadOldVersion2(DeserializeFactory item)
        {
            _scale_x = item.DeserializeTuple();
            length_pixel = item.DeserializeTuple();
            length_mm = item.DeserializeTuple();
            _scale_y = _scale_x;
        }
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        public HTuple row_base, col_base;
        public HTuple row_area, col_area;

        double _scale_y = 1;
        public double ScaleY
        {
            get
            {
                return _scale_y;
            }
            set
            {
                if (_scale_y != value)
                {
                    _scale_y = value;
                    ImageHeightReal = ActualHeight / ScaleY;
                    RaisePropertyChanged("ScaleY");
                }
            }
        }

        public double _scale_x = 1;
        public double ScaleX
        {
            get
            {
                return _scale_x;
            }
            set
            {
                if (_scale_x != value)
                {
                    _scale_x = value;
                    ImageWidthReal = ActualWidth / ScaleX;

                    RaisePropertyChanged("ScaleX");
                }
            }
        }
        double length_pixel;
        public double LengthPixel
        {
            get
            {
                return length_pixel;
            }
            set
            {
                if (length_pixel != value)
                {
                    length_pixel = value;
                    RaisePropertyChanged("LengthPixel");
                }
            }
        }
        double length_pixely;
        public double LengthPixelY
        {
            get
            {
                return length_pixely;
            }
            set
            {
                if (length_pixely != value)
                {
                    length_pixely = value;
                    RaisePropertyChanged("LengthPixelY");
                }
            }
        }
        double _image_width_real = 1692;
        public double ImageWidthReal
        {
            get
            {
                return _image_width_real;
            }
            set
            {
                if (_image_width_real != value)
                {
                    _image_width_real = value;
                    RaisePropertyChanged("ImageWidthReal");
                }
            }
        }
        double _image_height_real = 1262;
        public double ImageHeightReal
        {
            get
            {
                return _image_height_real;
            }
            set
            {
                if (_image_height_real != value)
                {
                    _image_height_real = value;
                    RaisePropertyChanged("ImageHeightReal");
                }
            }
        }

        double length_mm;
        public double Lengthmm
        {
            get
            {
                return length_mm;
            }
            set
            {
                if (length_mm != value)
                {
                    length_mm = value;
                    RaisePropertyChanged("Lengthmm");
                }
            }
        }
        double length_mmy;
        public double LengthmmY
        {
            get
            {
                return length_mmy;
            }
            set
            {
                if (length_mmy != value)
                {
                    length_mmy = value;
                    RaisePropertyChanged("LengthmmY");
                }
            }
        }

        public double ActualWidth { get => actual_width; set => actual_width = value; }
        public double ActualHeight { get => actual_height; set => actual_height = value; }
        private double actual_width;

    }
}
