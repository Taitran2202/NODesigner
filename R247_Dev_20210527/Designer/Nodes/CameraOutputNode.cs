using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Helper;
using NOVisionDesigner.ViewModel;
using NOVisionDesigner.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Camera","Camera Output",visible:false)]
    public class CameraOutputNode: BaseNode
    {
        
        static CameraOutputNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new CameraNodeView(), typeof(IViewFor<CameraOutputNode>));
        }
        #region Field

        bool isConnected { get; set; } = false;
        #endregion
        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    if (Camera.Connect())
                    {
                        SettingCameraWindow wd = new SettingCameraWindow(Camera);
                        wd.ShowDialog();
                        if (Camera.isPause)
                        {
                            Camera.Pause();
                        }
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("No Camera");
                    }

                    break;
                case "start":
                    Camera.Start();
                    break;
            }
        }

        public ValueNodeOutputViewModel<HImage> ImageOutput { get; }
        public CameraModel Camera;
        public CameraOutputNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Camera";
            this.CanBeRemovedByUser = false;
            Camera = new CameraModel();

            ImageOutput = new ValueNodeOutputViewModel<HImage>()
            {
                Name = "Image",
                //Value = Camera
            };

            //this.ImageOutput.WhenAnyValue(x => x.CurrentValue).Subscribe((x) => {
            //    if (x != null)
            //    {
            //        if (ShowDisplay)
            //        {
            //            designer.display.HalconWindow?.ClearWindow();
            //            designer.display.HalconWindow?.DispObj(x);
            //        }
            //    }
            //});
            Camera.Subscribe(x =>
            {
                ImageOutput.OnNext(x);
            });
            this.Outputs.Add(ImageOutput);
        }

        public void SoftTrigger()
        {
            Camera.SoftTrigger();
        }


    }

    public class CameraModel : IObservable<HImage>
    {
        #region Field
        public VimbaHelper m_VimbaHelper = new VimbaHelper();

        public bool isStart { get; set; } = false;
        public bool isPause { get; set; } = false;
        public bool isConnected { get; set; } = false;
        #endregion

        List<IObserver<HImage>> obser = new List<IObserver<HImage>>();
        HFramegrabber camera = null;
        public IDisposable Subscribe(IObserver<HImage> observer)
        {
            lock (obserlock)
            {
                obser.Add(observer);
            }

            return new Unsubscriber(obser, observer);
        }
        object obserlock = new object();
        public void OnImageAquired(object sender, HImage image)
        {
            foreach(var item in obser)
            {
                item.OnNext(image);
            }
        }


        #region Method


        public void Start()
        {
            Connect();
            if (m_VimbaHelper != null && m_VimbaHelper.m_Camera != null)
            {
                if (!m_VimbaHelper.m_Acquiring)
                {
                    m_VimbaHelper.StartContinuousImageAcquisition(this.OnFrameReceived);
                }
                else
                {
                    m_VimbaHelper.StopContinuousImageAcquisition();
                    isConnected = false;

                }
                isStart = m_VimbaHelper.m_Acquiring;
            }
            else
            {
                if (!isStart)
                {
                    Task.Run(new Action(() =>
                    {
                        isStart = true;
                        camera = new HFramegrabber("DirectShow", 1, 1, 0, 0, 0, 0, "default", 8, "rgb", -1, "false", "default", "[0] Integrated Camera", 0, -1);
                        camera.GrabImageStart(-1);
                        while (isStart)
                        {

                            var image = camera.GrabImageAsync(-1);
                            lock (obserlock)
                            {
                                foreach (var item in obser)
                                {
                                    item.OnNext(image);
                                }
                            }
                        }
                    }));
                    }
                else
                {
                    isStart = false;
                    camera.Dispose();
                }
                return;
            }

        }

        public bool Connect()
        {
            if (!isConnected)
            {
                m_VimbaHelper.Startup();
                try
                {
                    m_VimbaHelper.OpenCamera();
                    //System.Windows.Forms.MessageBox.Show("Connect successful");
                    isConnected = true;
                }
                catch (Exception)
                {
                    System.Windows.Forms.MessageBox.Show("Can't connect to camera");
                    return false;
                }

            }
            return true;
        }

        public void Stop()
        {
            this.Start(); //stop
            this.Connect(); //connect
        }

        public void Pause()
        {
            if (isPause)
            {
                m_VimbaHelper.m_Camera.StartContinuousImageAcquisition(3);
                isPause = false;
            }
            else
            {
                m_VimbaHelper.m_Camera.StopContinuousImageAcquisition();
                isPause = true;
            }

        }

        public void SoftTrigger()
        {
            if (m_VimbaHelper == null || m_VimbaHelper.m_Camera == null)
            {
                var id = new Random().Next(1, 5);
                HImage image = new HImage($"{id}.bmp");
                foreach (var item in obser)
                {
                    item.OnNext(image);
                }
            }
            else
            {
                m_VimbaHelper.TriggerSoftwareTrigger();
            }
        }
        #endregion

        #region Event Handler
        private void OnFrameReceived(object sender, FrameEventArgs args)
        {
            // Start an async invoke in case this method was not
            // called by the GUI thread.

            if (true == m_VimbaHelper.m_Acquiring)
            {
                HImage image = args.Image;
                foreach (var item in obser)
                {
                    item.OnNext(image);
                }

            }
        }
        #endregion


        private class Unsubscriber : IDisposable
        {
            private List<IObserver<HImage>> _observers;
            private IObserver<HImage> _observer;

            public Unsubscriber(List<IObserver<HImage>> observers, IObserver<HImage> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
        }

    }

}
