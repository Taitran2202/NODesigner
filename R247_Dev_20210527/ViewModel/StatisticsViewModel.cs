using DynamicData;
using NodeNetwork.ViewModels;
using NOVisionDesigner.UserControls;
using NOVisionDesigner.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;

namespace NOVisionDesigner.ViewModel
{


    public class StatisticsViewModel : BaseViewModel
    {
        #region Field
        StatisticsWindow mw;
        public MainViewModel mvm;
    
        public UserModel currentUser;
        public bool isAdmin;

        public CurrentUserRole CurrentRole;

        bool _isEditJob;
        public bool IsEditJob
        {
            get
            {
                return _isEditJob;
            }
            set
            {
                if (_isEditJob != value)
                {
                    _isEditJob = value;
                    RaisePropertyChanged("IsEditJob");
                }
            }
        }

        bool _isEditTool;
        public bool IsEditTool
        {
            get
            {
                return _isEditTool;
            }
            set
            {
                if (_isEditTool != value)
                {
                    _isEditTool = value;
                    RaisePropertyChanged("IsEditTool");
                }
            }
        }
        bool _isPrivilege;
        public bool IsPrivilege
        {
            get
            {
                return _isPrivilege;
            }
            set
            {
                if (_isPrivilege != value)
                {
                    _isPrivilege = value;
                    RaisePropertyChanged("IsPrivilege");
                }
            }
        }
        bool _isUserManagement;
        public bool IsUserManagement
        {
            get
            {
                return _isUserManagement;
            }
            set
            {
                if (_isUserManagement != value)
                {
                    _isUserManagement = value;
                    RaisePropertyChanged("IsUserManagement");
                }
            }
        }


        PrivilegeDatabase privilegeDb = new PrivilegeDatabase();

        bool isOnline = true;
        private bool _FullScreenChecked = false;
        public bool FullScreenChecked
        {
            get => _FullScreenChecked; set
            {
                _FullScreenChecked = value;
                if (value)
                {

                    mw.WindowState = WindowState.Maximized;
                }
                else
                {
                    mw.Width = MainWindow.pixelWidth * MainWindow.pixelDPI;
                    mw.Height = MainWindow.pixelHeight * MainWindow.pixelDPI;
                    mw.Left = 0;
                    mw.Top = 0;
                    mw.WindowState = WindowState.Normal;
                    mw.ResizeMode = ResizeMode.CanMinimize;
                }
                OnPropertyChanged();
            }
        }
        public List<NodeViewModel> CopySelectedNodes { get; set; }
        public string CopyDir { get; set; }
        public ISourceList<ConnectionViewModel> CopyConnections { get; set; }
        #endregion

        #region Command
        public ICommand LoadedWindowCommand { get; set; }

        public ICommand ClosedCommand { get; set; }


        #endregion

        
        #region Construction Method
        public StatisticsViewModel()
        {
            LoadedWindowCommand = new RelayCommand<object>((p) =>
            {
                return true;
            }, (p) =>
            {
                mw = (p as StatisticsWindow);

                CreateGrid();

            });


            ClosedCommand = new RelayCommand<object>((p) =>
            {
                return true;
            }, (p) =>
            {
                (p as Window).Hide();

            });


        }

        #endregion

        #region Event Handler
        //private void Update_Image(object sender, PropertyChangedEventArgs e)
        //{
        //    (sender as System.Windows.Forms.PictureBox).Image = 
        //}
        #endregion

        #region Method
        private void CreateGrid(int number = 0)
        {
            Grid t = new Grid();
            t.Name = "subGrid";
            int oGridNumber = 9;
            for (int i = 0; i < Math.Sqrt(oGridNumber); i++)
            {
                if (i < Math.Sqrt(oGridNumber) - 1)
                {
                    t.RowDefinitions.Add(new RowDefinition());
                    t.RowDefinitions[i].Height = new GridLength(10, GridUnitType.Star);
                }
                t.ColumnDefinitions.Add(new ColumnDefinition());
                t.ColumnDefinitions[i].Width = new GridLength(10, GridUnitType.Star);
            }
            Grid.SetColumn(t, 0);
            Grid.SetRow(t, 0);
            mw.MainStatisticsWindow.Children.Add(t);
            for (int i = 0; i < Math.Sqrt(oGridNumber) - 1; i++)
            {
                int j = 0;
                while (j < Math.Sqrt(oGridNumber))
                {
                    StatisticsUC sc = new StatisticsUC(i, j);
                    (sc.DataContext as StatisticsUCViewModel).main = mw;
        
                    Grid.SetColumn(sc, j);
                    Grid.SetRow(sc, i);
                    (sc.DataContext as ViewModel.StatisticsUCViewModel).Total = (i*j).ToString();
                    (sc.DataContext as ViewModel.StatisticsUCViewModel).OK = "100";
                    (sc.DataContext as ViewModel.StatisticsUCViewModel).NG = "100";
                    t.Children.Add(sc);
                    j++;
                }
            }
        }


        public void CreateGridV2(int i, int j,StatisticsWindow mw, StatisticsUCViewModel vm)
        {
            
            var t = mw.MainStatisticsWindow;
            StatisticsUC sc = new StatisticsUC(i, j);
            sc.DataContext = vm;
            (sc.DataContext as StatisticsUCViewModel).main = mw;
            Grid.SetColumn(sc, j);
            Grid.SetRow(sc, i);
            //(sc.DataContext as ViewModel.StatisticsUCViewModel).Total = "100";
            //(sc.DataContext as ViewModel.StatisticsUCViewModel).OK = "100";
            //(sc.DataContext as ViewModel.StatisticsUCViewModel).NG = "100";
            t.Children.Add(sc);
            j++;

        }

        internal void ShowManualDialog(StatisticsWindow StatisticsWindow)
        {
            StatisticsWindow.ShowDialog();
        }

        #endregion
    }
}
