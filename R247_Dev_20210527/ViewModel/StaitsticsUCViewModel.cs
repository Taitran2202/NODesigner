using DynamicData;
using HalconDotNet;
using Newtonsoft.Json.Linq;
using NodeNetwork.Toolkit.NodeList;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.SimpleView;
using NOVisionDesigner.Helper;
using NOVisionDesigner.UserControls;
using NOVisionDesigner.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NOVisionDesigner.ViewModel
{
    public class StatisticsUCViewModel : BaseViewModel
    {
        #region Field

        public StatisticsWindow main;
        public StatisticsUCViewModel mvm { get; set; }
        public DesignerHost designer { get; set; }
        public DetailStatisticsWindow detailStatisticsWindow { get; set; }

        public Statistics ListStatistics { get; set; }
        string _total;
        public string Total
        {
            get
            {
                return _total;
            }
            set
            {
                if (_total != value)
                {
                    _total = value;
                    RaisePropertyChanged("Total");
                }
            }
        }
        string _ok;
        public string OK
        {
            get
            {
                return _ok;
            }
            set
            {
                if (_ok != value)
                {
                    _ok = value;
                    RaisePropertyChanged("OK");
                }
            }
        }

        string _ng;
        public string NG
        {
            get
            {
                return _ng;
            }
            set
            {
                if (_ng != value)
                {
                    _ng = value;
                    RaisePropertyChanged("NG");
                }
            }
        }

        HImage _image;
        public HImage Image
        {
            get
            {
                return _image;
            }
            set
            {
                if (_image != value)
                {
                    _image = value;
                    RaisePropertyChanged("Image");
                }
            }
        }




        #endregion

        #region ICommand
        public ICommand LoadedWindowCommand { get; set; }

        public ICommand OpenDetailStatistics { get; set; }
        

        #endregion

        #region Constructor Method
        public StatisticsUCViewModel()
        {

            OpenDetailStatistics = new RelayCommand<object>((p) =>
            {
                return true;
            }, (p) => {
                detailStatisticsWindow = new DetailStatisticsWindow(p as StatisticsUC);
                (detailStatisticsWindow.DataContext as DetailStatisticsViewModel).designer =  designer;
                (detailStatisticsWindow.DataContext as DetailStatisticsViewModel).NameCamera = designer.NameCamera;
                detailStatisticsWindow.Show();
            });
        }
        #endregion

        #region Event Handler

        #endregion

        #region Method

        #endregion
    }
}
