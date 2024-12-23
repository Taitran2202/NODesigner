using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DevExpress.Xpf.Core;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Extensions;
using ReactiveUI;

namespace NOVisionDesigner.Designer.Controls
{
    /// <summary>
    /// Interaction logic for AddImageForNodeControl.xaml
    /// </summary>
    public partial class AddImageForNodeControl : UserControl
    {
        RecordImageNodeViewModel Model;
        public AddImageForNodeControl(ValueNodeInputViewModel<HImage> Input, string RecordDirectory)
        {
            InitializeComponent();
            Model = new RecordImageNodeViewModel(Input, RecordDirectory);
            this.DataContext = Model;
        }
        public void Dispose()
        {
            Model.CancelRecordCommand.Execute(null);
        }
    }
    class RecordImageNodeViewModel:ReactiveObject
    {
        ValueNodeInputViewModel<HImage> Input;
        public string RecordDirectory { get; set; }
        public RecordImageNodeViewModel(ValueNodeInputViewModel<HImage> Input,string RecordDirectory)
        {
            this.Input = Input;
            this.RecordDirectory = RecordDirectory;
        }
        bool _is_loading;
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

        string _loading_message;
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

        int _max_image=10;
        public int MaxImage
        {
            get
            {
                return _max_image;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _max_image, value);
            }
        }
        int _current_image_count;
        public int CurrentImageCount
        {
            get
            {
                return _current_image_count;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _current_image_count, value);
            }
        }

        string _image_format="png";
        public string ImageFormat
        {
            get
            {
                return _image_format;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _image_format, value);
            }
        }

        public ICommand RecordCommand 
        { get
            {
                return ReactiveCommand.Create(() =>
                {
                    if (IsRecording)
                    {
                        return;
                    }
                    Task.Run(() =>
                    {
                        Record();
                    });
                    
                });
            } 
        }
        public ICommand CancelRecordCommand
        {
            get
            {
                return ReactiveCommand.Create(() =>
                {
                    IsRecording = false;
                    CancelToken?.OnNext(Unit.Default);                   
                });
            }
        }
        ConcurrentQueue<HImage> SaveImageList = new ConcurrentQueue<HImage>();
        bool _is_recording;
        public bool IsRecording
        {
            get
            {
                return _is_recording;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _is_recording, value);
            }
        }
        Subject<Unit> CancelToken;
        void Record()
        {
            IsLoading = true;
            LoadingMessage = "Recording image from input...";
            SaveImageList = new ConcurrentQueue<HImage>();
            IsRecording = true;
            CurrentImageCount = 0;
            CancelToken = new Subject<Unit>();
            try
            {
                var recordSub = Input.WhenAnyValue(x => x.Value).TakeUntil(CancelToken).TakeUntil((image)=> CurrentImageCount>=MaxImage).Subscribe(x =>
                {
                    if (x != null)
                    {
                        var saveImage = x.CopyImage();
                        SaveImageList.Enqueue(saveImage);
                        CurrentImageCount++;
                    }
               
                });

                int saveCount = 0;
                while (_is_recording)
                {
                    while (SaveImageList.Count > 0)
                    {
                        if(SaveImageList.TryDequeue(out HImage result))
                        {
                            saveCount++;
                            if (saveCount >= MaxImage)
                            {
                                IsRecording = false;
                            }
                            result.WriteImage(ImageFormat, 0, Functions.RandomFileName(RecordDirectory));
                            LoadingMessage = "Recording "+ saveCount.ToString()+"/"+MaxImage.ToString() + " image...";
                        }
                    }
                }
                    
                
                
            }
            catch(Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DXMessageBox.Show(ex.Message,"Error");
                });
                
            }
            IsRecording = false;
            IsLoading = false;
        }
    }
}
