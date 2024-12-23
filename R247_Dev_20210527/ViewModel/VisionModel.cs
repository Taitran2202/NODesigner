using DevExpress.Xpf.Docking;
using DynamicData;
using HalconDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodeNetwork.Toolkit.NodeList;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.SimpleView;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.Helper;
using NOVisionDesigner.UserControls;
using NOVisionDesigner.Windows;
using NOVisionDesigner.Windows.HelperWindows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NOVisionDesigner.ViewModel
{
    public class VisionModel : ReactiveObject, IDisposable
    {
        private int _show_last_fail =1;
        public int ShowLastFail
        {
            get
            {
                return _show_last_fail;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _show_last_fail, value);
            }
        }
        private int _rows = 1;
        public int Rows
        {
            get
            {
                return _rows;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _rows, value);
            }
        }
        private int _columns = 1;
        public int Columns
        {
            get
            {
                return _columns;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _columns, value);
            }
        }
        private string _caption = "Camera";
        [JsonProperty]
        public string Caption
        {
            get
            {
                return _caption;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _caption, value);
            }
        } 
        public string Content
        {
            get; set;
        }
        public string TargetName
        {
            get; set;
        } = "root";
        public ICommand ChangeCaptionCommand
        {
            get 
            {
                return ReactiveCommand.Create(() =>
                ChangeCaption());
            }
        }
        public ICommand OpenDisplaySettingCommand
        {
            get
            {
                return ReactiveCommand.Create(() =>
                OpenDisplaySetting());
            }
        }
        
        public ICommand OpenTagManagerCommand
        {
            get;set;
        }
        #region Field
        public DesignerHost Designer { get; set; }
        public HSmartWindowControlWPF display;
        public MainViewModel mainViewModel { get; set; }
        //bool _isEditTool;
        //public bool IsEditTool
        //{
        //    get
        //    {
        //        return _isEditTool;
        //    }
        //    set
        //    {
        //        this.RaiseAndSetIfChanged(ref _isEditTool, value);
        //    }
        //}

        #endregion

        #region ICommand
        public ICommand LoadedWindowCommand { get; set; }

        public ICommand OpenDesigner 
        {
            get;set;
        }
        public ICommand OpenDesignerList
        {
            get;set;
        }
        
        public ICommand OpenSimpleView { get; set; }
        public ICommand OpenRecorderWiew { get; set; }
        #endregion
        public string Id { get; set; }
        public string BaseDir { get; set; }
        public string ConfigDir { get; set; }
        #region Constructor Method
        public VisionModel()
        {

        }
        public VisionModel(string dir, string Id)
        {
            this.Id = Id;
            BaseDir = System.IO.Path.Combine(dir, Id);
            ConfigDir = System.IO.Path.Combine(BaseDir, "config.json");
            Designer = new DesignerHost(BaseDir);
            mainViewModel = MainViewModel.Instance;
            //Designer.SetDisplayMainWindow(display);
            this.WhenAnyValue(x => x.Designer.displayData.ShowLastFail).Subscribe(x => {
                if (x)
                {
                    if(Designer.displayData.Layout==DisplayLayout.Vertical)
                    {
                        Rows = 2; Columns= 1;
                    }
                    else
                    {
                        Rows = 1; Columns = 2;
                    }
                    ShowLastFail = 2;
                }
                else
                {
                    Rows = 1; Columns = 1;
                    ShowLastFail = 1;
                }
            });
            this.WhenAnyValue(x => x.Designer.displayData.Layout).Subscribe(x => {
                if (Designer.displayData.ShowLastFail)
                {
                    if (x == DisplayLayout.Vertical)
                    {
                        Rows = 2; Columns = 1;
                    }
                    else
                    {
                        Rows = 1; Columns = 2;
                    }
                    ShowLastFail = 2;
                }
                else
                {
                    Rows = 1; Columns = 1;
                    ShowLastFail = 1;
                }
            });

            OpenSimpleView = new RelayCommand<object>((p) =>
            {
                return true;
            }, (p) => {
                var simpleviewwindow = new SimpleViewWindow(Designer);
                simpleviewwindow.Show();
            });
            OpenRecorderWiew = new RelayCommand<object>((p) =>
            {
                return ViewModel.PrivilegeViewModel.Instance.CanOpenRecord;
            }, (p) =>
            {

                var recoderwindow = new RecoderViewerWindow(Designer.recorder);
                recoderwindow.Owner = Application.Current.MainWindow;
                recoderwindow.view.Designer = Designer;
                recoderwindow.Show();
            });

            OpenDesigner = new RelayCommand<object>((p) =>
            {
                return ViewModel.PrivilegeViewModel.Instance.CanOpenDesigner;
            }, (p) =>
            {

                var designerWindow = new DesignerWindow(this);
                designerWindow.Show();
            });
            OpenDesignerList = new RelayCommand<object>((p) =>
            {
                return ViewModel.PrivilegeViewModel.Instance.CanOpenDesignerList;
            }, (p) =>
            {

                var designerListWindow = new DesignerListViewWindow(Designer);
                designerListWindow.Owner = Application.Current.MainWindow;
                designerListWindow.Show();
            });
            OpenTagManagerCommand = new RelayCommand<object>((p) =>
            {
                return ViewModel.PrivilegeViewModel.Instance.CanOpenTagManager;
            }, (p) =>
            {
                OpenTagManager();
            });


        }
        public void OpenTagManager()
        {
            TagManagerWindow wd = new TagManagerWindow(Designer.TagManager);
            wd.Owner = Application.Current.MainWindow;
            wd.ShowDialog(); 

        }
        public void OpenDisplaySetting()
        {
            DisplaySettingWindow wd = new DisplaySettingWindow(Designer);
            wd.ShowDialog();
        }
        public void ChangeCaption()
        {
            SaveWindow wd = new SaveWindow(Caption);
            if (wd.ShowDialog() == true)
            {
                Caption = wd.Text;
            }
        }
        public JObject Serialize()
        {
            return JObject.FromObject(this);
        }
        public void Load()
        {
            Designer.Load();
            var data = JsonConvert.DeserializeObject<VisionModel>(File.ReadAllText(ConfigDir));
            Caption = data.Caption;

        }
        public void Save(bool SaveNodeData=true)
        {
            //Designer.Serialize
            using (StreamWriter file = File.CreateText(ConfigDir))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, Serialize());
            }
            Designer?.Save(SaveNodeData); 
        }
        
        public void Dispose()
        {
            this.Designer.Dispose();
        }
        #endregion

        #region Event Handler

        #endregion

        #region Method
       
        #endregion
    }
}
