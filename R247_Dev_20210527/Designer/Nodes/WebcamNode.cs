using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using NOVisionDesigner.Designer.Extensions;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Camera","Intergate Camera",visible:false)]
    public class WebcamNode : BaseNode
    {
        static WebcamNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new WebcamNodeView(), typeof(IViewFor<WebcamNode>));
        }

       
        public ValueNodeOutputViewModel<HImage> ImageOutput { get; }
        WebcamModel camera;
        public WebcamNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Webcam";
            this.CanBeRemovedByUser = true;
            camera = new WebcamModel();
            ImageOutput = new ValueNodeOutputViewModel<HImage>()
            {
                Name = "Image",
                //Value =camera
            };
            this.WhenAnyObservable(vm => vm.camera).Subscribe((image) =>
            {
                ImageOutput.OnNext(image);
                
                var context = new InspectionContext(null,image, 1, 1, ID);
                BaseRun(context);
                if (image != null & base.ShowDisplay)
                {
                    base.designer.display.HalconWindow.DispObj(image);
                }
                context.inspection_result.Display(designer.display);
                Reset();
            });
            this.Outputs.Add(ImageOutput);

        }
        public void Start()
        {
            Task.Run(new Action(() =>
            {
                camera.Start();
            }));           
            
        }

    }
    public class WebcamModel : IObservable<HImage>
    {
        List<IObserver<HImage>> obser = new List<IObserver<HImage>>();
        public IDisposable Subscribe(IObserver<HImage> observer)
        {
            lock(obserlock){
                obser.Add(observer);
            }
           
            return new Unsubscriber(obser, observer);
        }
        object obserlock = new  object();
        bool is_run = false;
        bool is_connected = false;
        HFramegrabber camera;
        public void Connect()
        {
            if (!is_connected | camera == null)
            {
                camera = new HFramegrabber("DirectShow", 1, 1, 0, 0, 0, 0, "default", 8, "rgb", -1, "false", "default", "[0] Integrated Camera", 0, -1);
                is_connected = true;
            }
                
        }
        public void Stop()
        {
            if (is_run)
            {
                if (is_connected & camera != null)
                {
                    is_run = false;
                    try
                    {
                        camera.SetFramegrabberParam("do_grab_abort", 1);
                    }catch(Exception ex)
                    {

                    }
                    
                }
                is_run = false;
            }
           
        }
        public void Start()
        {
            Connect();
            if (is_run)
            {
                Stop();
                return;
            }
            
            is_run = true;
            camera.GrabImageStart(-1);
            while (is_run)
            {

                var image = camera.GrabImageAsync(-1);
                
                lock (obserlock)
                {
                    foreach (var item in obser)
                    {
                        
                        item.OnNext(image);
                    }
                }
                //HOperatorSet.CountSeconds(out s2);
                

            }
        }
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
