using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Concurrency;
using static NOVisionDesigner.Designer.Windows.FindEdgeEditorWindow;
using NOVisionDesigner.Designer.Misc;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{      
    [NodeInfo("Fixture","Edge Aligment (old)", visible: false)]
    public class FindEdgeNode : BaseNode
    {
        static FindEdgeNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<FindEdgeNode>));
        }

        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            (new HTuple(RectBoxes.Count)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(OriginPoints.Count)).SerializeTuple().FwriteSerializedItem(file);
            foreach (var rect in RectBoxes)
            {
                rect.Save(file);
            }
            foreach (var orgP in OriginPoints)
            {
                orgP.Save(file);
            }

        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
         
            HTuple countRects = item.DeserializeTuple();
            HTuple countOrigins = item.DeserializeTuple();
            RectBoxes.Clear();
            OriginPoints.Clear();
            for (int i = 0; i < countRects; i++)
            {
                var id = item.DeserializeTuple();

                var row = item.DeserializeTuple();
                var column = item.DeserializeTuple();
                var phi = item.DeserializeTuple();
                var length1 = item.DeserializeTuple();
                var length2 = item.DeserializeTuple();
                var sigma = item.DeserializeTuple();
                var threshold = item.DeserializeTuple();
                var direction = item.DeserializeTuple();
                var detectionMode = item.DeserializeTuple();
                var selection = item.DeserializeTuple();
              

                RectBoxes.Add(new UserRectangle() { 
                    id= id,
                    row = row,
                    column = column,
                    phi = phi,
                    length1 = length1,
                    length2 = length2,
                    sigma = sigma,
                    threshold = threshold,
                    direction = direction,
                    detectionMode = detectionMode,
                    selection = selection,
                    
                });   

              
            }
            for (int i = 0; i < countOrigins; i++)
            {
                OriginPoints.Add(new ResultPoints()
                {
                    id = item.DeserializeTuple(),
                    row = item.DeserializeTuple(),
                    column = item.DeserializeTuple()
                });
            }
            newRectBoxes = RectBoxes.ToList() ;
        }

        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<HHomMat2D> FixtureInput { get; }
        public ValueNodeOutputViewModel<HHomMat2D> FixtureOutput { get; }

        public List<UserRectangle> RectBoxes = new List<UserRectangle>();
        public List<UserRectangle> newRectBoxes = new List<UserRectangle>();
        public List<ResultPoints> OriginPoints = new List<ResultPoints>();
        public List<ResultPoints> newOriginPoints = new List<ResultPoints>();
        
        public bool isTrained = true;
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    isTrained = false;
                    if (ImageInput.Value != null)
                    {

                        Windows.FindEdgeEditorWindow wd = new Windows.FindEdgeEditorWindow(ImageInput.Value.CopyImage(), newRectBoxes, FixtureInput.Value);
                        wd.ShowDialog();
                        List<UserRectangle> tempRectBoxes;
                        List<ResultPoints> tempOriginPoints;
                        if (FixtureInput.Value!=null)
                        {
                            (tempRectBoxes, tempOriginPoints) = RecalculateRectBoxes(wd.RectBoxes,wd.points ,FixtureInput.Value.HomMat2dInvert());
                        }
                        else
                        {
                            tempRectBoxes = wd.RectBoxes;
                            tempOriginPoints = wd.points;
                        }


                        if (!CompareListRect(RectBoxes, tempRectBoxes))
                        {
                            OriginPoints = tempOriginPoints;

                            RectBoxes = tempRectBoxes;
                            newRectBoxes = tempRectBoxes;
                        }
                       
                        //RectBoxes = tempRectBoxes;
                        //newRectBoxes = tempRectBoxes;
                        //OriginPoints = tempOriginPoints;
                    }
                    
                    isTrained = true;      
                    break;
            }
        }
        public override void Run(object context)
        {
            FixtureOutput.OnNext(RunInside(ImageInput.Value, FixtureInput.Value,context as InspectionContext));
        }

        public FindEdgeNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "Find Edge";
            this.CanBeRemovedByUser = true;

            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"

            };
            this.Inputs.Add(ImageInput);

            FixtureInput = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "Fixture"

            };
            this.Inputs.Add(FixtureInput);

            FixtureOutput = new ValueNodeOutputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "Fixture"
            };
            this.Outputs.Add(FixtureOutput);
        }
        public HHomMat2D preFixture;
        public (List<UserRectangle>, List<ResultPoints>) RecalculateRectBoxes(List<UserRectangle> RectBoxes, List<ResultPoints> OriginPoints, HHomMat2D fixture)
        {
            var newRectBoxes = new List<UserRectangle>();
            var newOriginPoints = new List<ResultPoints>();
            foreach (var rect in RectBoxes)
            {
                HTuple newrow, newcol;
                
                HOperatorSet.AffineTransPoint2d(fixture, rect.column, rect.row, out newcol, out newrow);

                newRectBoxes.Add(new UserRectangle()
                {
                    id = rect.id,
                    row = newrow,
                    column = newcol,
                    phi = rect.phi,
                    length1 = rect.length1,
                    length2 = rect.length2,
                    sigma = rect.sigma,
                    threshold = rect.threshold,
                    detectionMode = rect.detectionMode,
                    direction = rect.direction,
                    selection = rect.selection
                });
       
            }
  
            foreach (var P in OriginPoints)
            {
                HTuple newrowP, newcolP;

                HOperatorSet.AffineTransPoint2d(fixture, P.column, P.row, out newcolP, out newrowP);
                newOriginPoints.Add(new ResultPoints { 
                    column=newcolP,
                    row = newrowP,
                    id = P.id
                });
            }
            
           
            return (newRectBoxes,newOriginPoints);
        }
        public List<ResultPoints> newPoints;

        public HHomMat2D RunInside(HImage imageinput, HHomMat2D fixture, InspectionContext e)
        {
          

            if (!isTrained)
            {
                return null;
            } 
            if(imageinput == null)
            {
                return null;
            }
            //var display = base.designer.display.HalconWindow;
            //display.ClearWindow();
            //display.AttachBackgroundToWindow(imageinput);
            if (RectBoxes.Count == 0)
            {
                return null;
            }
            else
            {
                var image = imageinput.Rgb1ToGray();
                HHomMat2D hom = new HHomMat2D();
                HHomMat2D homResult = new HHomMat2D();
                //newPoints = new List<ResultPoints>();
                hom.HomMat2dIdentity();
                double Tx = 0, Ty = 0;

                if (fixture !=null)
                {
                    (newRectBoxes, newOriginPoints) = RecalculateRectBoxes(RectBoxes,OriginPoints ,fixture);
                    
                }
                else
                {
                    newRectBoxes = RectBoxes;
                    newOriginPoints = OriginPoints;

                }
                foreach (var rect in newRectBoxes)
                {
                    
                    var result = FindEdge(image, rect, rect.sigma, rect.threshold,rect.direction, rect.selection, rect.detectionMode);
                    if (result.Count == 0)
                    {
                        continue;
                    }
                    else
                    {
                        //Calculate output fixture & draw box
                        if (ShowDisplay)
                        {
                            //HRegion test = new HRegion();
                            //test.GenRectangle2(rect.row, rect.column, rect.phi, rect.length1, rect.length2);
                            //HRegion testResult = new HRegion();
                            //testResult.GenCircle((HTuple)result[0], (HTuple)result[1], (HTuple)2);

                            //e.inspection_result.AddDisplay(test, "blue");
                            //e.inspection_result.AddDisplay(testResult, "red");
                        }
                        if (newOriginPoints.Find(x=>x.id ==  rect.id)!=null)
                        {
                            var orgP = newOriginPoints.Find(x => x.id == rect.id);
                            //Tx = Tx + (-orgP.column + result[1]);
                            //Ty = Ty + (-orgP.row + result[0]);

                            //new code
                            HHomMat2D homTemp = new HHomMat2D();
                            homTemp.HomMat2dIdentity();
                            homTemp.VectorAngleToRigid(orgP.row, orgP.column, 0.0, result[0], result[1], 0.0);
                            hom = hom.HomMat2dCompose(homTemp);
                            //end new code
                        }         
                    }
                }
                //hom[2] = Ty;
                //hom[5] = Tx;

                if (fixture != null)
                {
                    homResult = fixture.HomMat2dCompose(hom);


                }
                else
                {
                    homResult = hom;
                }
                return homResult;  
            }
         
        }
        public void drawLine(UserRectangle rect, ResultPoints point, string color)
        {
            var display = base.designer.display.HalconWindow;
            double[] pt1 = { point.column, point.row - rect.length2 };
            double[] pt2 = { point.column, point.row + rect.length2 };
            HHomMat2D hom = new HHomMat2D();
            HTuple homRotate, newpt1x, newpt1y, newpt2x, newpt2y;
            hom.HomMat2dIdentity();
            HOperatorSet.HomMat2dRotate(hom, Math.PI - rect.phi, point.column, point.row, out homRotate);
            HOperatorSet.AffineTransPoint2d(homRotate, pt1[0], pt1[1], out newpt1x, out newpt1y);
            HOperatorSet.AffineTransPoint2d(homRotate, pt2[0], pt2[1], out newpt2x, out newpt2y);
            HTuple temp1x, temp1y, temp2x, temp2y;
            HOperatorSet.TupleInt(newpt1x, out temp1x);
            HOperatorSet.TupleInt(newpt1y, out temp1y);
            HOperatorSet.TupleInt(newpt2x, out temp2x);
            HOperatorSet.TupleInt(newpt2y, out temp2y);
            display.SetDraw("margin");
            display.SetColor(color);
            display.SetLineWidth(2.0);
            display.DispLine(temp1y, temp1x, temp2y, temp2x);
        }

        //public HMeasure edges;
        public List<double> FindEdge(HImage image, UserRectangle rect, double sigma, double threshold, string direction, string selection, string detectionMode)
        {
            List<double> result = new List<double>();
            HTuple w, h;
            image.GetImageSize(out w,out h);
            if (detectionMode == "Manual")
            {
                result.Add(rect.row);
                result.Add(rect.column);
                return result;
            }

            HMeasure edges = new HMeasure();
            edges.GenMeasureRectangle2(rect.row, rect.column, rect.phi, rect.length1, rect.length2, w, h, "nearest_neighbor");
            HTuple row, column, amp, dis;
            try
            {
                edges.MeasurePos(image, sigma, threshold, direction, selection, out row, out column, out amp, out dis);
            }
            catch
            {
                return result;
            }
            if (row.Length == 0)
            {
                return result;
            }
      
            result.Add(row); //Add row first
            result.Add(column);
            return result;
        }

        public bool CompareListRect(List<UserRectangle>list1, List<UserRectangle> list2)
        {
            
            if (list1.Count != list2.Count)
            {
                return false;
            }

            bool result = true;
            for(int i= 0;i<list1.Count; i++)
            {
                var rect1 = list1[i];
                var rect2 = list2[i];
          
                if (!(rect1.row == rect2.row && rect1.column == rect2.column && rect1.phi == rect2.phi && rect1.length1 == rect2.length1
                    && rect1.length2 == rect2.length2 && rect1.sigma==rect2.sigma && rect1.threshold==rect2.threshold 
                    &&rect1.detectionMode==rect2.detectionMode && rect1.direction==rect2.direction && rect1.selection==rect2.selection)  )
                {
                    result = false;
                }
            }
            return result;
          
        }
    }
}
