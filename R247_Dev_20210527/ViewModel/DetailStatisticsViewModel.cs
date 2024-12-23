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
    public class DetailStatisticsViewModel : BaseViewModel
    {
        #region Field

        public MainWindow main;
        public DesignerHost designer { get; set; }
        public string NameCamera { get; set; }


      

        #endregion

        #region ICommand
        public ICommand LoadedWindowCommand { get; set; }

        public ICommand OpenDetailStatistics { get; set; }


        #endregion

        #region Constructor Method
        public DetailStatisticsViewModel(StatisticsUC StatisticsUC)
        {

            //LoadedWindowCommand = new RelayCommand<object>((p) =>
            //{
            //    return true;
            //}, (p) =>
            //{
            //    //main = Application.Current.MainWindow as MainWindow;
            //    //designer = (StatisticsUC.DataContext as StatisticsUCViewModel).designer;
            //});

        }
        #endregion

        #region Event Handler

        #endregion

        #region Method

        #endregion
    }
}
