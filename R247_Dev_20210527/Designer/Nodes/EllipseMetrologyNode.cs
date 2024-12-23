using DynamicData;
using HalconDotNet;
using Newtonsoft.Json;
using NodeNetwork.Toolkit.ValueNode;
using NumSharp;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using static NOVisionDesigner.Designer.Windows.AugmentationEditorWindow;
using Microsoft.ML.OnnxRuntime;
using System.Linq;
using System.Windows;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Measurement", "Ellipse Metrology")]
    public class EllipseMetrologyNode : BaseNode
    {
        
        public override void OnLoadComplete()
        {
            //segmentation.ReloadRecipe();
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);    
        }
        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item,this);
        }
        #region Properties
        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    EllipseMetrologyWindow wd = new EllipseMetrologyWindow(this);
                    wd.Owner = Window.GetWindow(sender);
                    wd.Show();
                    break;
            }

        }
        [HMIProperty("Metrology Editor")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor", sender)); }
        }
        public EllipseMetrology Model { get; set; }
        
        public ValueNodeInputViewModel<HImage> Image { get; }
        public ValueNodeInputViewModel<HHomMat2D> Fixture { get; }
        public ValueNodeOutputViewModel<EllipseData[]> Ellipse { get; set; }


        #endregion

        static EllipseMetrologyNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<EllipseMetrologyNode>));
        }

        public EllipseMetrologyNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
           
            this.Name = "Ellipse Metrology";
            Image = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(Image);

            Fixture = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "HHomMat2D"
            };
            this.Inputs.Add(Fixture);

            Ellipse = new ValueNodeOutputViewModel<EllipseData[]>()
            {
                Name = "Ellipse",
                PortType = "EllipseData[]"
            };
            this.Outputs.Add(Ellipse);


        }
        public override void OnInitialize()
        {
            Model = new EllipseMetrology();
        }
        
        public override void Run(object context)
        {
            if (Image.Value == null)
            {
                return;
            }
            var result = Model.Run(Image.Value, Fixture.Value);
            if (ShowDisplay)
            {
                if (result != null)
                {
                    (context as InspectionContext).inspection_result.AddEllipse("green",result.row, result.col, result.phi, result.radius1, result.radius2);
                }
            }
            if (result != null)
            {
                Ellipse.OnNext(new[] { result });
            }
            else
            {
                Ellipse.OnNext(new EllipseData[] {  });
            }
            
            
            
        }

        
    }
    public class EllipseMetrology: IHalconDeserializable
    {
        
        HMetrologyModel Model;
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            try
            {
                Create();
            }catch (Exception ex)
            {
                App.GlobalLogger.LogError("EllipseMetrology", ex.Message);
            }
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public EllipseMetrologyParameter Parameter { get; set; }
        public EllipseMetrology()
        {
            Parameter = new EllipseMetrologyParameter();
            
            
        }
        public EllipseData Run(HImage image,HHomMat2D fixture)
        {
            if (!Created)
            {
                return null;
            }
            if(fixture == null)
            {
                fixture = new HHomMat2D();
            }
            double sx= fixture.HomMat2dToAffinePar(out double sy, out double phi, out double theta, out double row, out double col);
            Model.AlignMetrologyModel(row, col, phi);
            Model.ApplyMetrologyModel(image);
            HTuple result = Model.GetMetrologyObjectResult(HandleIndex, "all", new HTuple("result_type"), new HTuple("row","column","phi","radius1","radius2"));
            if (result.Length > 0)
            {
                return new EllipseData()
                {
                    row = result[0],
                    col = result[1],
                    phi = result[2],
                    radius1 = result[3],
                    radius2 = result[4]
                };
            }
            else
            {
                return null;
            }
        }
        public (HXLDCont measure, HXLDCont cross) GetMeasureResult(double size=10)
        {
            var result= Model.GetMetrologyObjectMeasures(HandleIndex, "all",out HTuple rows,out HTuple cols);
            HXLDCont cross = new HXLDCont();
            cross.GenCrossContourXld(rows, cols,new HTuple(size), Parameter.Phi+Math.PI/4);
            return (result,cross);
        }
        public HXLDCont GetContourResult()
        {
            return Model.GetMetrologyObjectResultContour(HandleIndex,"all",1.5);
        }
        int HandleIndex;
        private bool _created=false;
        public bool Created
        {
            get { return _created; }
        }
        public void Create()
        {
            if (Model == null)
            {
                Model = new HMetrologyModel();               
            }
            if (!Created)
            {
                try
                {
                    HandleIndex = Model.AddMetrologyObjectEllipseMeasure(Parameter.Row,
                    Parameter.Column,
                    Parameter.Phi,
                    Parameter.Radius1,
                    Parameter.Radius2,
                    Parameter.MeasureLength1,
                    Parameter.MeasureLength2,
                    Parameter.MeasureSigma,
                    Parameter.MeasureThreshold, new HTuple(), new HTuple());
                    _created = true;
                }
                catch(Exception ex)
                {

                }
                    SetParameter();
                                                 
            }
            
        }
        public void UpdateEllise(Ellipse rect)
        {
            Parameter.Row = rect.row;
            Parameter.Column = rect.col;
            Parameter.Phi = rect.phi;
            Parameter.Radius1 = rect.radius1;
            Parameter.Radius2 = rect.radius2;

            if (Model != null)
            {
                if (Created)
                {
                    Model.ClearMetrologyObject(HandleIndex);
                    _created = false;
                }
            }
            Create();
        }
        //public void UpdateRect(Rect2 rect)
        //{
        //    Parameter.Row = rect.row;
        //    Parameter.Column = rect.col;
        //    Parameter.Phi = rect.phi;
        //    Parameter.Radius1 = rect.length1;
        //    Parameter.Radius2 = rect.length2;

        //    if (Model != null)
        //    {
        //        if (Created)
        //        {
        //            Model.ClearMetrologyObject(HandleIndex);
        //            _created = false;
        //        }
        //    }
        //    Create();
        //}
        public void SetParameter()
        {
            try
            {
                Model.SetMetrologyObjectParam(HandleIndex,"measure_length1", Parameter.MeasureLength1);
                Model.SetMetrologyObjectParam(HandleIndex,"measure_length1", Parameter.MeasureLength1);
                Model.SetMetrologyObjectParam(HandleIndex,"measure_length2", Parameter.MeasureLength2);
                Model.SetMetrologyObjectParam(HandleIndex, "measure_distance", Parameter.MeasureDistance);
                Model.SetMetrologyObjectParam(HandleIndex, "num_measures", Parameter.NumMeasures);
                Model.SetMetrologyObjectParam(HandleIndex, "measure_sigma", Parameter.MeasureSigma);
                Model.SetMetrologyObjectParam(HandleIndex, "measure_threshold", Parameter.MeasureThreshold);
                Model.SetMetrologyObjectParam(HandleIndex, "measure_select", Parameter.MeasureSelect.ToString());
                Model.SetMetrologyObjectParam(HandleIndex, "measure_transition", Parameter.MeasureTransition.ToString());
                Model.SetMetrologyObjectParam(HandleIndex, "min_score", Parameter.MinScore);
                Model.SetMetrologyObjectParam(HandleIndex, "num_instances", Parameter.NumInstances);
                Model.SetMetrologyObjectParam(HandleIndex, "distance_threshold", Parameter.DistanceThreshold);
                Model.SetMetrologyObjectParam(HandleIndex, "max_num_iterations", Parameter.MaxMumIterations);
                Model.SetMetrologyObjectParam(HandleIndex, "rand_seed", Parameter.RandSeed);
                Model.SetMetrologyObjectParam(HandleIndex, "instances_outside_measure_regions", Parameter.InstancesOutsideMeasureRegions.ToString().ToLower());
            }catch(Exception ex)
            {

            }
            
        }
    }

    public class EllipseMetrologyParameter : IHalconDeserializable
    {
        [ReadOnly(true)]
        public double Row { get; set; }
        [ReadOnly(true)]
        public double Column { get; set; }
        [ReadOnly(true)]
        public double Phi { get; set; }
        [ReadOnly(true)]
        public double Radius1 { get; set; }
        [ReadOnly(true)]
        public double Radius2 { get; set; }
        public EllipseMetrologyParameter()
        {
            Row = 0;
            Column = 0;
            Phi = 0;
            Radius1 = 100;
            Radius2 = 100;

            //default values
            MeasureLength1 = 20;
            MeasureLength2 = 5;
            MeasureDistance = 10;
            NumMeasures = 2;

            MeasureSigma = 1;
            MeasureThreshold = 30;
            MeasureSelect = MeasureSelectEnum.all;
            MeasureTransition = MeasureTransitionEnum.all;
            MeasureInterpolation = MeasureInterpolationEnum.bilinear;

            MinScore = 0.7;
            NumInstances = 1;
            DistanceThreshold = 3.5;
            MaxMumIterations = -1;
            RandSeed = 42;
            InstancesOutsideMeasureRegions = true;
        }
        public double MeasureLength1 { get; set; }
        public double MeasureLength2 { get; set; }
        public double MeasureDistance { get; set; }
        public int NumMeasures { get; set; }
        public double MeasureSigma { get; set; }
        public double MeasureThreshold { get; set; }
        public MeasureSelectEnum MeasureSelect { get; set; }
        public MeasureTransitionEnum MeasureTransition { get; set; }
        public MeasureInterpolationEnum MeasureInterpolation { get; set; }
        public double MinScore { get; set; }
        public int NumInstances { get; set; }
        public double DistanceThreshold { get; set; }
        public int MaxMumIterations { get; set; }
        public int RandSeed { get; set; }
        public bool InstancesOutsideMeasureRegions { get; set; }

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }

}
