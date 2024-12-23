using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{    
    [NodeInfo("Display","Draw Graphic",visible:false)]
    public class DrawGraphics : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            foreach (var item2 in ListParam)
            {
                switch (item2.Type)
                {

                    case "Image":
                        {
                            this.Inputs.Add(new ValueNodeInputViewModel<HImage>()
                            {
                                Name = item2.Name,
                                PortType = "Image"
                            });
                          

                            break;
                        }
                    case "Region":
                        {
                            this.Inputs.Add(new ValueNodeInputViewModel<HRegion>()
                            {
                                Name = item2.Name,
                                PortType = "Region"
                            });
                           
                            break;
                        }
                    case "String":
                        {
                            this.Inputs.Add(new ValueNodeInputViewModel<string>()
                            {
                                Name = item2.Name,
                                PortType = "String"
                            });
                       
                            break;
                        }
                    case "Int":
                        {
                            this.Inputs.Add(new ValueNodeInputViewModel<int>()
                            {
                                Name = item2.Name,
                                PortType = "Number"
                            });
                         
                            break;
                        }
                    case "Double":
                        {
                            this.Inputs.Add(new ValueNodeInputViewModel<double>()
                            {
                                Name = item2.Name,
                                PortType = "Number"
                            });

                           
                            break;
                        }
                    default:
                        break;
                }
            }
          
        }
        static DrawGraphics()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<DrawGraphics>));
        }


        public ObservableCollection<Inputparams> ListParam  { get; set; }

        public ValueNodeOutputViewModel<HRegion> RegionOutput { get; }
        #region Field
        double LowerValue = 0;
        double UpperValue = 255;
        int Min_Area { get; set; } = 100;
        int Max_Area { get; set; } = 9999999;
        bool IsFill { get; set; } = false;
        HRegion Search_Region { get; set; } = new HRegion(10, 10.0, 100, 100);
        #endregion

        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    {
                        DrawGraphicsWindow wd = new DrawGraphicsWindow(this.Inputs, ListParam);

                        wd.ShowDialog();
                        #region old
                        //foreach (var item in this.Inputs.Items)
                        //{
                        //    string A = item.ToString();
                        //    if (A.Contains("HImage"))
                        //    {
                        //        (item as ValueNodeInputViewModel<HImage>).WhenAnyValue(x => x.Value).Subscribe((x) =>
                        //        {
                        //            if (x != null)
                        //            {
                        //                if (ShowDisplay)
                        //                {

                        //                    var context = new InspectionContext(null, 1, 1);

                        //                    context.inspection_result.AddDisplay(x, "red");
                        //                    designer.recorder.Add(context.inspection_result);

                        //                }
                        //            }
                        //        });
                        //    }
                        //    else if (A.Contains("HRegion"))
                        //    {
                        //        (item as ValueNodeInputViewModel<HRegion>).WhenAnyValue(x => x.Value).Subscribe((x) =>
                        //        {
                        //            if (x != null)
                        //            {
                        //                if (ShowDisplay)
                        //                {
                        //                    var context = new InspectionContext(null, 1, 1);

                        //                    context.inspection_result.AddDisplay(x, "red");
                        //                    designer.recorder.Add(context.inspection_result);

                        //                }
                        //            }
                        //        });
                        //    }
                        //    else if (A.Contains("Int32"))
                        //    {
                        //        (item as ValueNodeInputViewModel<int>).WhenAnyValue(x => x.Value).Subscribe((x) =>
                        //        {
                        //            if (x != null)
                        //            {
                        //                if (ShowDisplay)
                        //                {
                        //                    //designer.display.HalconWindow?.ClearWindow();
                        //                    //    designer.display.HalconWindow?.DispObj(x);
                        //                }
                        //            }
                        //        });
                        //    }
                        //    else if (A.Contains("Double"))
                        //    {
                        //        (item as ValueNodeInputViewModel<double>).WhenAnyValue(x => x.Value).Subscribe((x) =>
                        //        {
                        //            if (x != null)
                        //            {
                        //                if (ShowDisplay)
                        //                {
                        //                    //designer.display.HalconWindow?.ClearWindow();
                        //                    //   designer.display.HalconWindow?.DispObj(x);
                        //                }
                        //            }
                        //        });
                        //    }
                        //    else if (A.Contains("String"))
                        //    {
                        //        (item as ValueNodeInputViewModel<string>).WhenAnyValue(x => x.Value).Subscribe((x) =>
                        //        {
                        //            if (x != null)
                        //            {
                        //                if (ShowDisplay)
                        //                {
                        //                    //designer.display.HalconWindow?.ClearWindow();
                        //                    //designer.display.HalconWindow?.DispObj(x);
                        //                }
                        //            }
                        //        });
                        //    }
                        //}
                        #endregion
                        break;
                    }
            }
        }


        public override void Run(object context)
        {
            RunInside((InspectionContext)context);
        }
        public bool RunInside(InspectionContext e)
        {

            HTuple message = new HTuple();     
           
            //e.inspection_result = new InspectionResult(null, 1, 1);
            InspectionResult inspection_result = e.inspection_result;
            foreach (var item in this.Inputs.Items)
            {
                int loc = this.Inputs.Items.IndexOf(item);
                var param = ListParam[loc];
                string A = item.ToString();
                if (A.Contains("HImage"))
                {

                    try
                    {
                        if ((item as ValueNodeInputViewModel<HImage>).Value != null)
                        {
                            if (ShowDisplay)
                            {                          
                                e.inspection_result.AddDisplay((item as ValueNodeInputViewModel<HImage>).Value, param.Color);
                                //designer.recorder.Add(e.inspection_result);
                            }
                        }

                    }
                    catch (Exception)
                    {

                        
                    }
                }
                else if (A.Contains("HRegion"))
                {
                    try
                    {
                        if ((item as ValueNodeInputViewModel<HRegion>).Value != null)
                        {
                            if (ShowDisplay)
                            {
                                var row = (item as ValueNodeInputViewModel<HRegion>).Value.Row + param.Row;
                                var col = (item as ValueNodeInputViewModel<HRegion>).Value.Column + param.Col;
                                string area = "Area: " + (item as ValueNodeInputViewModel<HRegion>).Value.Area.ToString();
                                string Text = param.Area? param.Header + " " + area : param.Header + " ";
                                e.inspection_result.AddRegion((item as ValueNodeInputViewModel<HRegion>).Value, param.Color,param.DrawType);
                                if (param.BackgroundColor == "transparent")
                                {
                                    e.inspection_result?.AddText(Text, param.Color, param.BackgroundColor, row, col, param.FontSize, false);
                                }
                                else
                                {
                                    e.inspection_result?.AddText(Text, param.Color, param.BackgroundColor, row, col, param.FontSize, true);
                                }

                                //designer.recorder.Add(e.inspection_result);
                            }
                        }

                    }
                    catch (Exception ex)
                    {

                    }

                }
                else if (A.Contains("Int32"))
                {
                    try
                    {
                        if ((item as ValueNodeInputViewModel<int>).Value != null)
                        {
                            if (ShowDisplay)
                            {
                                if (param.BackgroundColor == "transparent")
                                {
                                    e.inspection_result?.AddText(param.Header + "" + (item as ValueNodeInputViewModel<int>).Value.ToString(), param.Color, param.BackgroundColor, param.Row, param.Col, param.FontSize, false);
                                }
                                else
                                {
                                    e.inspection_result?.AddText(param.Header + "" + (item as ValueNodeInputViewModel<int>).Value.ToString(), param.Color, param.BackgroundColor, param.Row, param.Col, param.FontSize, true);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }



                }
                else if (A.Contains("Double"))
                {

                    try
                    {
                        if ((item as ValueNodeInputViewModel<double>).Value != null)
                        {
                            if (ShowDisplay)
                            {
                                if(param.BackgroundColor == "transparent")
                                {
                                    e.inspection_result?.AddText(param.Header + "" + (item as ValueNodeInputViewModel<double>).Value.ToString(), param.Color, param.BackgroundColor, param.Row, param.Col,param.FontSize,false);
                                }
                                else
                                {
                                    e.inspection_result?.AddText(param.Header + "" + (item as ValueNodeInputViewModel<double>).Value.ToString(), param.Color, param.BackgroundColor, param.Row, param.Col, param.FontSize, true);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }


                }
                else if (A.Contains("String"))
                {

                    try
                    {
                        if ((item as ValueNodeInputViewModel<string>).Value != null)
                        {
                            if (ShowDisplay)
                            {
                                if (param.BackgroundColor == "transparent")
                                {
                                    e.inspection_result?.AddText(param.Header + "" + (item as ValueNodeInputViewModel<string>).Value.ToString(), param.Color, param.BackgroundColor, param.Row, param.Col, param.FontSize, false);
                                }
                                else
                                {
                                    e.inspection_result?.AddText(param.Header + "" + (item as ValueNodeInputViewModel<string>).Value.ToString(), param.Color, param.BackgroundColor, param.Row, param.Col, param.FontSize, true);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }


                }


            }
            return true;
        }

        public DrawGraphics(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {

            ListParam = new ObservableCollection<Inputparams>();
            Name = "Graphics";
            this.CanBeRemovedByUser = true;



        }


    }




    public class Inputparams : HelperMethods, INotifyPropertyChanged
    {


        public  void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);

        }

        public  void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);

        }


        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private bool is_constant = false;
        public bool IsConstant
        {
            get
            {
                return is_constant;
            }
            set
            {
                if (is_constant != value)
                {
                    is_constant = value;
                    RaisePropertyChanged("IsConstant");
                }
            }
        }


        private string _name = "Input";
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }


        private string _type = "Image";
        public string Type
        {
            get
            {
                return _type;
            }
            set
            {
                if (_type != value)
                {
                    _type = value;
                    RaisePropertyChanged("Type");
                }
            }
        }



        private int _type_index = 0;
        public int TypeIndex
        {
            get
            {
                return _type_index;
            }
            set
            {
                if (_type_index != value)
                {
                    _type_index = value;
                    RaisePropertyChanged("TypeIndex");
                }
            }
        }



        private int _color_index = 0;
        public int ColorIndex
        {
            get
            {
                return _color_index;
            }
            set
            {
                if (_color_index != value)
                {
                    _color_index = value;
                    RaisePropertyChanged("ColorIndex");
                }
            }
        }


        private bool _show = true;
        public bool Show
        {
            get
            {
                return _show;
            }
            set
            {
                if (_show != value)
                {
                    _show = value;
                    RaisePropertyChanged("Show");
                }
            }
        }

        bool area = false;
        public bool Area
        {
            get
            {
                return area;
            }
            set
            {
                if (area != value)
                {
                    area = value;
                    RaisePropertyChanged("Area");
                }
            }
        }


        private string _color = "red";
        public string Color
        {
            get
            {
                return _color;
            }
            set
            {
                if (_color != value)
                {
                    _color = value;
                    RaisePropertyChanged("Color");
                }
            }
        }



        private string _header = "";
        public string Header
        {
            get
            {
                return _header;
            }
            set
            {
                if (_header != value)
                {
                    _header = value;
                    RaisePropertyChanged("Header");
                }
            }
        }


        private double row =100;
        public double Row
        {
            get
            {
                return row;
            }
            set
            {
                if (row != value)
                {
                    row = value;
                    RaisePropertyChanged("Row");
                }
            }
        }

        private double col = 100;
        public double Col
        {
            get
            {
                return col;
            }
            set
            {
                if (col != value)
                {
                    col = value;
                    RaisePropertyChanged("Col");
                }
            }
        }


        private string draw_type = "margin";
        public string DrawType
        {
            get
            {
                return draw_type;
            }
            set
            {
                if (draw_type != value)
                {
                    draw_type = value;
                    RaisePropertyChanged("DrawType");
                }
            }
        }

        private int font_size = 30;
        public int FontSize
        {
            get
            {
                return font_size;
            }
            set
            {
                if (font_size != value)
                {
                    font_size = value;
                    RaisePropertyChanged("FontSize");
                }
            }
        }


        private string _bgr_color = "transparent";
        public string BackgroundColor
        {
            get
            {
                return _bgr_color;
            }
            set
            {
                if (_bgr_color != value)
                {
                    _bgr_color = value;
                    RaisePropertyChanged("BackgroundColor");
                }
            }
        }



        public Inputparams()
        {

        }

    }
}

