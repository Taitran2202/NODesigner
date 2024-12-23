using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.ViewModel;
using NOVisionDesigner.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Event", "FrameSynchronization")]
    public class FrameSynchronization : BaseNode
    {
        SynchronizationModel synchronizationModel;
        static FrameSynchronization()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<FrameSynchronization>));
        }
        public ValueNodeInputViewModel<bool> InspectionResult { get; }
        public ValueNodeOutputViewModel<bool> Result { get; set; }
        public override void Run(object context)
        {
            var frameResult = new FrameResult() ;
            frameResult.FrameID = (context as InspectionContext).frameId;
            frameResult.Result = InspectionResult.Value;
            var listContext = synchronizationModel.listContext;
            lock (synchronizationModel.lock_object)
            {

                listContext.RemoveMany(listContext.Where(x => x.FrameID < frameResult.FrameID));
                if (listContext.Count(x=>x.FrameID== frameResult.FrameID) < 3 - 1)
                {

                        listContext.Add(frameResult);
                    
                    //else
                    //{
                    //    listContext.Clear();
                    //    listContext.Add(frameResult);
                    //}
                }
                else
                {
                    listContext.Add(frameResult);
                    bool result;
                    if (listContext.Where(x => x.Result == false).Any())
                    {
                        result = false;
                    }
                    else result = true;
                    Result.OnNext(result);
                    Console.WriteLine(frameResult.FrameID.ToString() + ":\t\t" + result.ToString());
                    listContext.RemoveMany(listContext.Where(x=>x.FrameID== frameResult.FrameID));
                }

            }
            
            
        }
        public FrameSynchronization(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "FrameSynchronization";
            InspectionResult = new ValueNodeInputViewModel<bool>()
            {
                Name = "Inspection Result",
                PortType = "Boolean"
            };
            this.Inputs.Add(InspectionResult);
            Result = new ValueNodeOutputViewModel<bool>()
            {
                Name = "Result",
                PortType = "Boolean"
            };
            this.Outputs.Add(Result);
            synchronizationModel = MainViewModel.Instance.synchronizationModel;
        }
        
    }

}
