using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.Views;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.PropertiesViews;
using NOVisionDesigner.ViewModel;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Record", "Record Image",visible:false)]
    public class RecordImageNode : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            (new HTuple(EnableInput.Value)).SerializeTuple().FwriteSerializedItem(file);
            recordImage.Save(file);

        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            EnableInput.Editor = new BoolValueEditorViewModel()
            {
                Value = item.DeserializeTuple()
            };
            recordImage.Load(item);

        }
        public override void Dispose()
        {
            try
            {
                designer.OnComplete -= OnComplete;

            }
            catch (Exception)
            {

            }
        }
        public Control PropertiesView
        {
            get
            {
                return new RecordImageView(this);
            }
        }

        public RecordImageTool recordImage { get; set; }
        Queue<string> deleteFilesQueue = new Queue<string>();
        string deletePath = "";
        CancellationTokenSource tokenSource = new CancellationTokenSource();
        static RecordImageNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<RecordImageNode>));
        }
        Queue<SaveImageInfo> SaveImageQueue = new Queue<SaveImageInfo>();
        public ValueNodeInputViewModel<bool> EnableInput { get; }
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public MainViewModel mvm;
        public override void Run(object context)
        {

        }

        public void RunInside(HImage image, InspectionContext context)
        {
            if (image == null || !EnableInput.Value) { return; }
            var image1 = image.Clone();
            SaveImageInfo imageInfo = recordImage.GetSaveImageInfo(image1, context.result, designer.NameCamera);
            mvm.SaveImageQueue.Enqueue(imageInfo);


            var image2 = designer.displayMainWindow.HalconWindow.DumpWindowImage();
            var imageScreeShotInfo = recordImage.GetSaveScreenShotInfo(image2, context.result, designer.NameCamera, imageInfo.fileName);
            mvm.SaveImageQueue.Enqueue(imageScreeShotInfo);
            //image2.Dispose();
        }

        public RecordImageNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            designer.OnComplete += OnComplete;
            //recordImage = new RecordImage();
            Name = "Record Image";
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(ImageInput);


            CancellationToken token = tokenSource.Token;
            EnableInput = new ValueNodeInputViewModel<bool>()
            {
                Name = "Enable",
                PortType = "Bool",
                Editor = new BoolValueEditorViewModel()
            };
            this.Inputs.Add(EnableInput);
            mvm = MainViewModel.Instance;
            recordImage = mvm.recordImage;

        }
        public void OnComplete(object sender, EventArgs e)
        {
            var context = sender as InspectionContext;
            RunInside(ImageInput.Value, context);
        }
    }

    public class RecordImageTool : INotifyPropertyChanged
    {
        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);

        }

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);

        }
        public enum SaveTypeEnum
        {
            SaveAll = 0,
            SaveOnlyNG = 1,
            SaveOnlyOK = 2,
            SaveAllSort = 3
        }

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public RecordImageTool()
        {

        }
        #region Properties



        SaveTypeEnum _saveType = SaveTypeEnum.SaveOnlyNG;
        public SaveTypeEnum SaveType
        {
            get
            {
                return _saveType;
            }
            set
            {
                if (_saveType != value)
                {
                    _saveType = value;
                    RaisePropertyChanged("SaveType");
                }
            }
        }

        string _recordPath = "D:\\record";
        public string RecordPath
        {
            get
            {
                return _recordPath;
            }
            set
            {
                if (_recordPath != value)
                {
                    _recordPath = value;
                    RaisePropertyChanged("RecordPath");
                }
            }
        }

        int _deleteDaySP = 3;
        public int DeleteDaySP
        {
            get
            {
                return _deleteDaySP;
            }
            set
            {
                if (_deleteDaySP != value)
                {
                    _deleteDaySP = value;
                    RaisePropertyChanged("DeleteDaySP");
                }
            }
        }

        bool _autoDeleteEnable = false;
        public bool AutoDeleteEnable
        {
            get
            {
                return _autoDeleteEnable;
            }
            set
            {
                if (_autoDeleteEnable != value)
                {
                    _autoDeleteEnable = value;
                    RaisePropertyChanged("AutoDeleteEnable");
                }
            }
        }


        #endregion
        #region Method
        public SaveImageInfo GetSaveImageInfo(HImage image, bool result, string nameCamera)
        {
            string fileName = DateTime.Now.ToString("HH-mm-ss.ffff");
            string savePath = GetFileSavePath(result, nameCamera, fileName);
            return new SaveImageInfo(image, savePath, fileName);
        }

        public SaveImageInfo GetSaveScreenShotInfo(HImage image, bool result, string nameCamera, string namefile)
        {
            string fileName = namefile + "_screenshot";
            string savePath = GetFileSavePath(result, nameCamera, fileName);
            return new SaveImageInfo(image, savePath, fileName);
        }

        public string GetFileSavePath(bool result, string nameCamera, string fileName)
        {
            var dt = DateTime.Now;
            string FolderPath = Path.Combine(RecordPath, nameCamera, dt.Year.ToString(), dt.ToString("MMMM"), dt.Day.ToString());
            switch (SaveType)
            {
                case SaveTypeEnum.SaveAll:
                    FolderPath = Path.Combine(FolderPath, "All");
                    break;
                case SaveTypeEnum.SaveAllSort:
                    FolderPath = result ? Path.Combine(FolderPath, "OK") : Path.Combine(FolderPath, "NG");
                    break;
                case SaveTypeEnum.SaveOnlyNG:
                    FolderPath = Path.Combine(FolderPath, "NG");
                    break;
                case SaveTypeEnum.SaveOnlyOK:
                    FolderPath = Path.Combine(FolderPath, "OK");
                    break;
            }
            if (fileName.Contains("_screenshot"))
            {
                FolderPath = Path.Combine(FolderPath, "ScreenShot");
            }
            else
            {

                FolderPath = Path.Combine(FolderPath, "images");
            }
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }
            return FolderPath;
        }
        #endregion


    }

    public class SaveImageInfo
    {
        public string fileName { get; set; }
        public string savePath { get; set; }
        public HImage image { get; set; }
        public SaveImageInfo(HImage image, string savePath, string fileName)
        {
            this.fileName = fileName;
            this.savePath = savePath;
            this.image = image;
        }
        public void Dispose()
        {
            this.image.Dispose();
        }
    }
}