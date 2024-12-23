using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NOVisionDesigner.Designer.Data
{
    public class ImageData
    {
        string _full_path;
        public string FullPath
        {
            get
            {
                if (!File.Exists(_full_path))
                    return null;
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
        public DataRow Data { get; set; }
        public ImageData(string FullPath)
        {
            this.FullPath = FullPath;
        }
        public ImageData(string FullPath, DataRow data)
        {
            this.FullPath = FullPath;
            this.Data = data;
        }
    }
    public class CalibarionData
    {
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
        public CalibarionData(double x, double y)
        {
            this.ScaleX = x;
            this.ScaleY = y;
        }
    }
    public class DefectData
    {
        public double Size { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public CalibarionData calib { get; set; }
        public DefectData(double r1, double c1, double r2, double c2, string Name, string Type, CalibarionData calib, double Size)
        {
            this.calib = calib;

            Row = (int)(r2 + r1) / 2;
            Column = (int)(c2 + c1) / 2;
            Width = Math.Abs((int)(c2 - c1));
            Height = Math.Abs((int)(r2 - r1));
            this.Size = Size;
            this.Name = Name; this.Type = Type;
        }
        public string Name { get; set; }
        public string Type { get; set; }
    }
    public class CommandHandler : ICommand
    {
        private Action _action;
        private bool _canExecute;
        public CommandHandler(Action action, bool canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _action();
        }
    }
    public class TimeData24H
    {
        public List<TimeData> list = new List<TimeData>();

    }
    public class TimeData
    {
        public TimeData(DateTime X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }
        public DateTime X { get; set; }
        public double Y { get; set; }
    }
    public class TimeDataHours
    {
        public TimeDataHours(DateTime X, double Y)
        {

            this.X = X;
            this.Y = Y;
        }
        public DateTime X { get; set; }
        public double Y { get; set; }
    }
    public class DefectRegionInfo
    {
        public DefectRegionInfo(double r1, double c1, double r2, double c2, string Name, string Type, double Size)
        {
            R1 = r1;
            R2 = r2;
            C1 = c1;
            C2 = c2;
            this.Name = Name; this.Type = Type;
            this.Size = Size;
        }
        public double Size { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        public double R1 { get; set; }
        public double R2 { get; set; }
        public double C1 { get; set; }
        public double C2 { get; set; }
    }
}
