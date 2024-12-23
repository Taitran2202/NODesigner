using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows.GigeCameraUserControl;
using NOVisionDesigner.ViewModel;
using NOVisionDesigner.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Camera","GigE Vision Camera",visible:false)]
    public class GigeCamera : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            acq.Save(file);
            acq.Interface?.Dispose();

        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            //acq.InterfaceChanged += Acq_InterfaceChanged;
            acq.Load(item);
            try
            {
                //acq.Interface.Connect();
                acq.Interface?.Start();
            }
            catch
            { }

        }
        #region Field
        //GigeCameraModel Camera;
        public Designer.Windows.GigeCameraUserControl.Accquisition acq { set; get; } = new Designer.Windows.GigeCameraUserControl.Accquisition();
        public CameraInterface Camera { get; set; }
        #endregion
        static GigeCamera()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<GigeCamera>));
        }
        //public override void Run(object context)
        //{
        //    ImageOutput.Value.OnNext(RunInside(ImageInput.Value, HomInput.Value, filter));
        //}
        //public HImage RunInside(HImage image, HHomMat2D hom, MultiImageFilter filter)
        //{
        //    HImage result;
        //    try
        //    {
        //        result = filter.Update(image, hom);
        //    }
        //    catch (Exception ex)
        //    {
        //        result = null;
        //    }
        //    return result;
        //}

        public AcqussitionView PropertiesView
        {
            get
            {

                return new AcqussitionView(acq);
            }
        }
        public override void Dispose()
        {
            acq.InterfaceChanged -= Acq_InterfaceChanged;
            acq.Interface?.Dispose();
        }
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    //if (Camera.Connect())
                    //{
                    //    SettingCameraWindow wd = new SettingCameraWindow(Camera);
                    //    wd.ShowDialog();
                    //    if (Camera.isPause)
                    //    {
                    //        Camera.Pause();
                    //    }
                    //}
                    //else
                    //{
                    //    System.Windows.Forms.MessageBox.Show("No Camera");
                    //}
                    
                    //AcqussitionView view = new AcqussitionView(acq);
                    //DesignerWindow window = Window.GetWindow(base.designer.display) as DesignerWindow;
                   // window.togge_toolview.IsChecked = false;
                   // window.togge_camera.IsChecked = true;
                    //window.PropertiesGrid.Children.Clear();
                    //window.PropertiesGrid.Children.Add(view);
                    //window.LayoutProperties.IsActive = true;
                    // (window.DataContext as DesignerViewModel).SetCameraUC(acq);


                    break;
                case "start":
                    //Camera.Start();
                    break;
            }
        }
        public ValueNodeOutputViewModel<HImage> ImageOutput { get; }
        public ValueNodeOutputViewModel<bool> BoolOutput { get; }

        public ValueNodeInputViewModel<bool> TriggerInput { get; }
        public override void Run(object context)
        {
            if(this.TriggerInput.Value == true)
            {
                acq.Interface.Trigger();
            }
        }

        public GigeCamera(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "CameraInterface";
            ImageOutput = new ValueNodeOutputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            BoolOutput = new ValueNodeOutputViewModel<bool>()
            {
                Name = "BoolOutput",
                PortType = "Bool"
            };
            this.Outputs.Add(BoolOutput);

            TriggerInput = new ValueNodeInputViewModel<bool>()
            {
                Name = "Trigger Input",
                PortType = "Bool"
            };
            this.Inputs.Add(TriggerInput);
            TriggerInput.ValueChanged.Subscribe((x) =>
            {
                if (x)
                {
                    acq.Interface.Trigger();
                }
            });
            this.Outputs.Add(ImageOutput);

            //this.ImageOutput.WhenAnyValue(x => x.CurrentValue).Subscribe((x) =>
            //{
            //    if (x != null)
            //    {
            //        if (ShowDisplay)
            //        {
            //            //designer.display.HalconWindow?.ClearWindow(); // no need clear
            //            designer.display.HalconWindow?.DispObj(x);
            //        }
            //    }
            //});
            //this.Outputs.Add(ImageOutput);
            //acq.InterfaceChanged += Acq_InterfaceChanged;
            //if(acq.Interface!= null)
            //{
            //    try
            //    {
            //        acq.Interface.Start();
            //    }
            //    catch (Exception)
            //    {

            //    }
            //}
            
        }

        private void Acq_InterfaceChanged(object sender, EventArgs e)
        {
            if(acq.Interface != null)
            {
                this.Outputs.Add(ImageOutput);
                //acq.Interface.ImageAcquired = (image) =>
                //{
                //    ImageOutput.OnNext(image);
                //    var context = new InspectionContext(null, 1, 1);
                //    BaseRun(context);
                //    context.inspection_result.Display(designer.display);
                //    Reset();
                //};
                SetImageOutputValue();
            }
            else
            {
                this.Outputs.Clear();
            }
        }
        private void SetImageOutputValue()
        {
            switch (acq.Interface.Type)
            {
                case "GigEVision2":
                    {
                        ImageOutput.Value = (GigeCameraModel)acq.Interface;
                        try
                        {
                            //acq.Interface.Start();
                        }
                        catch { }
                        acq.Interface.ImageAcquired = (image) =>
                        {
                            ImageOutput.OnNext(image);
                            BoolOutput.OnNext(true);
                            string direct = @"D:\IMG\Camera " + (designer.indexCamera + 1).ToString() + @"\";
                            if (!Directory.Exists(direct))
                            {
                                Directory.CreateDirectory(direct);
                            }
                            string name = @"D:\IMG\Camera " + (designer.indexCamera +1).ToString() + @"\" + DateTime.Now.ToString("HH-mm-ss.ffff");
                            //designer.displayMainWindow.HalconWindow.ClearWindow();
                            if (ShowDisplay)
                            {

                                designer.display?.HalconWindow.DispObj(image);
                                designer.displayMainWindow.HalconWindow.DispObj(image);
                                //Task.Run(() =>
                                //{

                                //    image.WriteImage("bmp", 0, name);
                                //});
                            }

                            var context = new InspectionContext(null,null, 1, 1, ID);
                            //context.inspection_result.lst_display?.Clear();

                           
                            DateTime t = DateTime.Now;
                            BaseRun(context);
                            var time = (DateTime.Now - t).TotalMilliseconds.ToString();
                            //Thread.Sleep(800);
                            context.result = false;
                            if(designer.EventOnCompleted != null)
                            {
                                designer.EventOnCompleted(context);
                            }                            
                            context.inspection_result.Display(designer.display);
                            context.inspection_result.Display(designer.displayMainWindow);
                            designer.displayMainWindow.HalconWindow.SetPart(0, 0, -2, -2);
                            if (context.result)
                            {
                                designer.ListStatistics.List_OK += 1;
                            }
                            else
                            {
                                designer.ListStatistics.List_NG += 1;
                            }
                            Reset();
                            if (context.result)
                            {
                                //pass++;
                                
                            }
                            else
                            {
                                context.inspection_result.image = ImageOutput.CurrentValue.CopyImage();
                                designer.recorder.Add(context.inspection_result);
                                //fail++;
                            }
                        };
                        break;
                    }
            }
        }


       

    }

   

}
