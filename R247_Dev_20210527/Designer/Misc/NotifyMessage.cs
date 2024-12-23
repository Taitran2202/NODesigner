using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalconDotNet;

namespace NOVisionDesigner.Designer.Misc
{
    public class NotifyMessage
    {
        public void AddOutput(int output)
        {
            if (!outputs.Contains(output))
            {
                outputs.Add(output);
            }
        }
        public List<int> outputs = new List<int>();
        public List<string> DefectTag = new List<string>();
        public HTuple message = new HTuple();
        public HTuple tools = new HTuple();
        public bool result = true;
        public InspectionResult inspection_result;
    }
    public class ResultTable
    {
        public string Name { get; set; }
        public bool Result { get; set; }
    }
    public class InspectionContext
    {
        
        public string SavedName { get; set; } = string.Empty;
        public void AddOutput(int output)
        {
            if (!outputs.Contains(output))
            {
                outputs.Add(output);
            }
        }
        public int total, pass, fail;
        public double FPS;
        public bool ByPassReject { get; set; } = false;
        public Accquisition.Accquisition Accquisition;
        public InspectionContext(Accquisition.Accquisition accquisition, HImage image, double scale_x, double scale_y,string NodeId,ulong frameId=0,int total=0,int pass=0,int fail=0)
        {
            this.Accquisition = accquisition;
            inspection_result = new InspectionResult(image, scale_x, scale_y, NodeId);
            this.frameId = frameId;
            this.total = total;
            this.pass = pass;
            this.fail = fail;
        }
        public string SaveImagePath { get; set; }
        public string SaveScreenShotPath { get; set; }
        public List<int> outputs = new List<int>();
        public List<string> DefectTag = new List<string>();
        public bool result = true;
        public ulong frameId;
        public double PixelValueToCalibrationValue(double value)
        {
            return value / (inspection_result.scale_y * inspection_result.scale_x);
        }
        public InspectionResult inspection_result { get; }
        public delegate void OnCompleted(InspectionContext context);
        public OnCompleted EventOnCompleted { get; set; }
        public void AddGraphics(IEnumerable<IDisplayable> graphics)
        {
            inspection_result.lst_display.AddRange(graphics);
        }
        public void SetResult(string name,bool value)
        {
            if (inspection_result.ResultTable.ContainsKey(name))
            {
                inspection_result.ResultTable[name]&= value;
            }
            else
            {
                inspection_result.ResultTable[name] = value;
            }
            
        }
        public void DisplayResultTable()
        {

        }

    }

    public class Statistics : INotifyPropertyChanged  
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;


        int listOK;
        public int List_OK
        {
            get
            {
                return listOK;
            }
            set
            {
                if (listOK != value)
                {
                    listOK = value;
                    RaisePropertyChanged("List_OK");
                    RaisePropertyChanged("List_Total");
                }
            }
        }

        int listNG;
        public int  List_NG
        {
            get
            {
                return listNG;
            }
            set
            {
                if (listNG != value)
                {
                    listNG = value;
                    RaisePropertyChanged("List_NG");
                    RaisePropertyChanged("List_Total");
                }
            }
        }
        int listTotal;
        public int List_Total
        {
            get
            {
                return (List_NG + List_OK);
            }
            
        }

        int _list_me_co;
        public int List_me_co
        {
            get
            {
                return _list_me_co;
            }
            set
            {
                if (_list_me_co != value)
                {
                    _list_me_co = value;
                    RaisePropertyChanged("List_me_co");
                }
            }
        }

        int _list_me_canh;
        public int List_me_canh
        {
            get
            {
                return _list_me_canh;
            }
            set
            {
                if (_list_me_canh != value)
                {
                    _list_me_canh = value;
                    RaisePropertyChanged("List_me_canh");
                }
            }
        }

        int _list_xuoc;
        public int List_xuoc
        {
            get
            {
                return _list_xuoc;
            }
            set
            {
                if (_list_xuoc != value)
                {
                    _list_xuoc = value;
                    RaisePropertyChanged("List_xuoc");
                }
            }
        }

        int _list_do_co;
        public int List_do_co
        {
            get
            {
                return _list_do_co;
            }
            set
            {
                if (_list_do_co != value)
                {
                    _list_do_co = value;
                    RaisePropertyChanged("List_do_co");
                }
            }
        }

        int _list_dom_dong;
        public int List_dom_dong
        {
            get
            {
                return _list_dom_dong;
            }
            set
            {
                if (_list_dom_dong != value)
                {
                    _list_dom_dong = value;
                    RaisePropertyChanged("MyProperty");
                }
            }
        }

        int _list_me_goc;
        public int List_me_goc
        {
            get
            {
                return _list_me_goc;
            }
            set
            {
                if (_list_me_goc != value)
                {
                    _list_me_goc = value;
                    RaisePropertyChanged("List_me_goc");
                }
            }
        }

        int _list_keo_to;
        public int List_keo_to
        {
            get
            {
                return _list_keo_to;
            }
            set
            {
                if (_list_keo_to != value)
                {
                    _list_keo_to = value;
                    RaisePropertyChanged("List_keo_to");
                }
            }
        }


    }
    //public class SubCamStatistics
    //{
    //    public int rowindex { get; set; }
    //    public int colindex { get; set; }
    //    public Statistics statistics { get; set; }
    //}
}
