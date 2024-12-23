using DevExpress.Xpf.Core;
using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.BreadcrumbBar;
using NodeNetwork.Toolkit.Group;
using NodeNetwork.Toolkit.NodeList;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using NOVisionDesigner.Designer;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.GroupNodes;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Helper;
using NOVisionDesigner.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NOVisionDesigner.ViewModel
{
    public class DesignerViewModel : ReactiveObject
    {
        
        #region Field
        Designer.DesignerHost _designer = null;
        public Designer.DesignerHost Designer
        {
            get
            {
                return _designer;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _designer, value);
            }
        }
        bool _is_loading = false;
        public bool IsLoading
        {
            get
            {
                return _is_loading;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _is_loading, value);
            }
        }
        string _loading_message = "Loading...";
        public string LoadingMessage
        {
            get
            {
                return _loading_message;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _loading_message, value);
            }
        }
        public Point mousePosition { get; set; } = new Point(0, 0);

        #endregion

        #region ICommand
        public ICommand LoadedWindowCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        public ICommand TriggerCommand { get; set; }
        public ICommand groupCommand { get; set; }
        public ICommand ungroupCommand { get; set; }
        public ICommand opengroupCommand { get; set; }
        public ICommand copygroupCommand { get; set; }
        public ICommand pastegroupCommand { get; set; }

        public ICommand changeNameCommand { get; set; }


        

        #endregion

        #region Construction Method
        public DesignerViewModel(VisionModel subcam)
        {
            LoadedWindowCommand = new RelayCommand<object>((p) =>
            {
                return true;
            }, (p) =>
            {
                Designer = subcam.Designer;
                InitDesigner(p);
            });

            CloseCommand = new RelayCommand<object>((p) => { return true; }, (p) => {

                (p as Window).Close();

            });

            TriggerCommand = new RelayCommand<object>((p) => { return true; }, (p) => {
               // output.SoftTrigger();
            });
            groupCommand = new RelayCommand<object>((p) => { return true; }, (p) => {

                Designer.GroupNode();

            });

            ungroupCommand = new RelayCommand<object>((p) => { return true; }, (p) => {

                Designer.UnGroupNode();

            });
            opengroupCommand = new RelayCommand<object>((p) => { return true; }, (p) => {

                Designer.OpenGroupNode();

            });

            copygroupCommand = new RelayCommand<object>((p) => { return true; }, (p) => {

                Designer.CopyGroupNode();

            });

            changeNameCommand = new RelayCommand<object>((p) => { return true; }, (p) => {

                Designer.ChangeNodeName();

            });


            pastegroupCommand = new RelayCommand<object>((p) => { return true; }, (p) => {

                try
                {
                    Designer.PasteGroupNode(mousePosition);
                }catch(Exception ex)
                {
                    DXMessageBox.Show("Cannot paste: "+ex.Message,"Error",MessageBoxButton.OK,MessageBoxImage.Error);
                }
                

            });
            


        }
        #endregion

        #region Event Handler

        #endregion

        #region Method
        private void InitDesigner(object p)
        {
            //designer.SetDisplay((p as DesignerWindow).display, (p as DesignerWindow).displayCamera);
            Designer.SetDisplay((p as DesignerWindow).display);
            Designer.CopySelectedNodes = new List<NodeViewModel>();
            var nodeList = (p as DesignerWindow).nodeList;
            //networkView = (p as DesignerWindow).networkView;
            var breadcrumbBar = (p as DesignerWindow).breadcrumbBar;
            //foreach (var item in designer.AddNodeFunction)
            //{
            //    ListViewModel.AddNodeType(item.AddFunction);
            //}
            //nodeList.ViewModel = ListViewModel;
            nodeList.SetAddNodeFactory(Designer.AddNodeFunction);
            //networkView.ViewModel = designer.Network;
            breadcrumbBar.ViewModel = Designer.NetworkBreadcrumbBar;
            Designer.Network.SelectedNodes.Connect(x => x.IsSelected).Subscribe((x) =>
            {
                if (x.Count() > 0)
                {
                    var view = x.First().Item.Current;
                    PropertiesView = view;
                    //if (view != null)
                    //{
                    //    var property = view.GetType().GetProperty("PropertiesView");
                    //    if (property != null)
                    //    {
                            
                    //    }
                    //}
                    
                }
                //else
                //{
                //    PropertiesView = null;
                //}
            });
            //doan code nay de hien thi lai history bar sau khi mo lai window
            if (Designer.recorder.ResultRecoderQueue.Count != 0)
            {
                (p as DesignerWindow).recorder.Recorder = new Recorder();
                foreach (var item in Designer.recorder.ResultRecoderQueue)
                {
                    (p as DesignerWindow).recorder.Recorder.Add(item);
                }
            }
            //end code
            (p as DesignerWindow).recorder.Recorder = Designer.recorder;
            //End old code
        }
        object _PropertiesView;
        public object PropertiesView
        {
            get
            {
                return _PropertiesView;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _PropertiesView, value);
            }
        }

        #endregion


    }

}
