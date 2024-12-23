using HalconDotNet;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using NOVisionDesigner.Designer.Misc;

namespace NOVisionDesigner.Designer.Windows
{
    public partial class FindEdgeEditorWindow : Window
    //,INotifyPropertyChanged
    {
        HWindow display;
        HImage image;
        public int w, h;
        //Add new by Minh
        //public delegate void DisplayResultsDelegate();
        public List<HTuple> drawing_objects;
        HDrawingObject.HDrawingObjectCallback cb;
        public HTuple rectangleParamName;
        public HTuple rectangleParamValue;
        public List<UserRectangle> RectBoxes;
        public delegate void InputThresholdChanged(int oldValue, int newValue);
        public InputThresholdChanged OnThresholdchanged { get; set; }
        public double id_isSelected;
        public HHomMat2D inputFixture;
        public List<ResultPoints> points;
        public List<ListID> lst_id = new List<ListID>();

        public class ResultPoints
        {
            public double id { get; set; }
            public double row { get; set; }
            public double column { get; set; }
            public void Save(HFile file)
            {
                (new HTuple(id)).SerializeTuple().FwriteSerializedItem(file);
                (new HTuple(row)).SerializeTuple().FwriteSerializedItem(file);
                (new HTuple(column)).SerializeTuple().FwriteSerializedItem(file);
            }
            public void Load(DeserializeFactory item)
            {

                id = item.DeserializeTuple();
                row = item.DeserializeTuple();
                column = item.DeserializeTuple();
            } 
        }
        public class UserRectangle
        {
            public void Save(HFile file)
            {
                //(new HTuple("update_manual")).SerializeTuple().FwriteSerializedItem(file);
                (new HTuple(id)).SerializeTuple().FwriteSerializedItem(file);
                (new HTuple(row)).SerializeTuple().FwriteSerializedItem(file);
                (new HTuple(column)).SerializeTuple().FwriteSerializedItem(file);
                (new HTuple(phi)).SerializeTuple().FwriteSerializedItem(file);
                (new HTuple(length1)).SerializeTuple().FwriteSerializedItem(file);
                (new HTuple(length2)).SerializeTuple().FwriteSerializedItem(file);
                (new HTuple(sigma)).SerializeTuple().FwriteSerializedItem(file);
                (new HTuple(threshold)).SerializeTuple().FwriteSerializedItem(file);
                (new HTuple(direction)).SerializeTuple().FwriteSerializedItem(file);
                (new HTuple(detectionMode)).SerializeTuple().FwriteSerializedItem(file);
                (new HTuple(selection)).SerializeTuple().FwriteSerializedItem(file);

            }
            public void Load(DeserializeFactory item)
            {
             
                id = item.DeserializeTuple();
                row = item.DeserializeTuple();
                column = item.DeserializeTuple();
                phi = item.DeserializeTuple();
                length1 = item.DeserializeTuple();
                length2 = item.DeserializeTuple();
                sigma = item.DeserializeTuple();
                threshold = item.DeserializeTuple();
                direction = item.DeserializeTuple();
                detectionMode = item.DeserializeTuple();
                selection = item.DeserializeTuple();


            }

            public double id { get; set; }
            public double row { get; set; }  // row is y coordinate center of rectangle
            public double column { get; set; }   // col is x coordinate center of rectangle
            public double phi { get; set; }
            public double length1 { get; set; }
            public double length2 { get; set; }

            public double sigma { get; set; }
            public double threshold { get; set; }
            public string direction { get; set; }
            public string detectionMode { get; set; }
            public string selection { get; set; }         
        }
        public class ListID
        {
            public double id { get; set; }
            public string name { get; set; }
        }
        
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public UserRectangle RecalculateRectBoxes(UserRectangle rect, HHomMat2D fixture)
        {
            
           
            HTuple newrow, newcol;
            HOperatorSet.AffineTransPoint2d(fixture, rect.column, rect.row, out newcol, out newrow);
            var newRectBoxes = new UserRectangle() { id = rect.id, row = newrow, column = newcol, phi = rect.phi, length1 = rect.length1, length2 = rect.length2,
                        sigma = rect.sigma, threshold = rect.threshold, detectionMode = rect.detectionMode, direction = rect.direction, selection =  rect.selection};
           
            return newRectBoxes;
        }


        public FindEdgeEditorWindow(HImage imageInput, List<UserRectangle> modelRectBoxes, HHomMat2D fixture)
        {
            InitializeComponent();
            drawing_objects = new List<HTuple>();
            //HImage gray = image.Rgb1ToGray();
            this.image = imageInput.Rgb1ToGray();
            RectBoxes = (modelRectBoxes.Count == 0) ? new List<UserRectangle>() : modelRectBoxes;
            this.inputFixture = fixture;
            window_display.HMoveContent = true;           
        }

        public void UpdateDisplayInput(double id)
        {
            if (RectBoxes == null | RectBoxes.Count == 0)
            {
                return;
            }
            var rect = RectBoxes.Find(x => x.id == id);
            sld_threshold.Value = (rect.threshold);
            sld_sigma.Value = (rect.sigma);
            cmb_transition.SelectedIndex = cmb_transition.Items.IndexOf(rect.direction);
            cmb_select.SelectedIndex = cmb_select.Items.IndexOf(rect.selection);
            cmb_detectionMode.SelectedIndex = cmb_detectionMode.Items.IndexOf(rect.detectionMode);

            lst_edge.SelectedIndex = lst_id.FindIndex(x => x.id == rect.id);

        }
        protected void DrawingCallback(IntPtr drawId, IntPtr windowHandle, string type)
        {
            int id = new HTuple(drawId);
            int windowId = new HTuple(windowHandle);
            rectangleParamName = new HTuple("row", "column", "phi", "length1", "length2");
            HOperatorSet.GetDrawingObjectParams(drawId, rectangleParamName, out rectangleParamValue);

            id_isSelected = id;   
            UpdateRectBoxes(id, rectangleParamValue);
            if(type == "on_select")
            {
                UpdateDisplayInput(id);
            }
       
        }

        private void UpdateRectBoxes(int id, HTuple paramValue)
        {
            double row = paramValue.DArr[0];
            double column = paramValue.DArr[1];
            double phi = paramValue.DArr[2];
            double length1 = paramValue.DArr[3];
            double length2 = paramValue.DArr[4];
            //if (this.inputFixture != null)
            //{
            //    HTuple newrow, newcol;
            //    var newFixture = this.inputFixture.HomMat2dInvert();
            //    HOperatorSet.AffineTransPoint2d(newFixture, column, row, out newcol, out newrow);
            //    row = (double)newrow;
            //    column = (double)newcol;
            //}
            var rect = RectBoxes.Find(x => x.id == id);
            UserRectangle newRect = new UserRectangle
            {
                id = id,
                row = row,
                column = column,
                phi = phi,
                length1 = length1,
                length2 = length2,
                sigma = rect.sigma,
                threshold = rect.threshold,
                detectionMode = rect.detectionMode,
                direction  = rect.direction,
                selection = rect.selection,
            };
            var index = RectBoxes.FindIndex(x => x.id == id);
            RectBoxes.RemoveAt(index);
            RectBoxes.Add(newRect);
            UpdateDisplayInput(id_isSelected);
        }
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.ClearWindow();
            display.AttachBackgroundToWindow(this.image);
            this.image.GetImageSize(out w, out h);
            cb = new HDrawingObject.HDrawingObjectCallback(DrawingCallback);
            if (RectBoxes.Count != 0)
            {
               
                var tempRectBoxes = new List<UserRectangle>();
                foreach (var obj in drawing_objects)
                {
                    HOperatorSet.ClearDrawingObject(obj);
                }

                foreach (var rect in RectBoxes)
                {
                    HTuple drawid = new HTuple();
                    //UserRectangle rect;
                    //if (inputFixture!=null)
                    //{
                    //    rect= RecalculateRectBoxes(rect1,inputFixture);
                      
                    //}
                    //else
                    //{
                    //    rect = rect1;
                    //}
                    
                    HOperatorSet.CreateDrawingObjectRectangle2(rect.row, rect.column, rect.phi, rect.length1, rect.length2, out drawid);
                    HOperatorSet.SetDrawingObjectParams(drawid, "color", "blue");
                    SetCallbacks(drawid);
                    tempRectBoxes.Add(new UserRectangle()
                    {
                        id = drawid,
                        row = rect.row,
                        column = rect.column,
                        phi = rect.phi,
                        length1 = rect.length1,
                        length2 = rect.length2,
                        sigma = rect.sigma,
                        threshold = rect.threshold,
                        detectionMode = rect.detectionMode,
                        direction = rect.direction,
                        selection = rect.selection
                    });
                    lst_id.Add(new ListID { id = drawid, name = "unknown" });
                    lst_edge.Items.Add(new ListID { id = drawid, name = "unknown" });
                    if (lst_edge.Items.Count == 1)
                    {
                        lst_edge.SelectedIndex = 0;
                    }
                }
                RectBoxes = tempRectBoxes;
                id_isSelected = RectBoxes.Min(x => x.id);
                UpdateDisplayInput(id_isSelected);
                Apply_Click();
            }
         

        }

        public void UpdateDisplayImage(HImage image)
        {
            display.ClearWindow();
            display.AttachBackgroundToWindow(image);
        }

        private void window_display_HMouseUp(object sender, HalconDotNet.HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
        }
        private void window_display_MouseLeave(object sender, MouseEventArgs e)
        {

        }
        private void window_display_HMouseDown(object sender, HalconDotNet.HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {

        }
        private void window_display_HMouseMove(object sender, HalconDotNet.HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {

        }

        public enum FindEdgeState
        {
            Pan, Draw
        }
        private void window_display_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Apply_Click();
        }
        //Add new code for draw rectangle by Minh
        private void window_display_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void SetCallbacks(HTuple draw_id)
        {
            // Set callbacks for all relevant interactions
            drawing_objects.Add(draw_id);
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(cb);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_resize", ptr);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_drag", ptr);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_select", ptr);
         
            HOperatorSet.AttachDrawingObjectToWindow(window_display.HalconID, draw_id);
        }

        private void Apply_Click()
        {
      
            if(display == null)
            {
                return;
            }    
            display.ClearWindow();
            display.AttachBackgroundToWindow(this.image);

            if (RectBoxes.Count == 0)
            {

                return;
            }
            else
            {
                points = new List<ResultPoints>();
                foreach (UserRectangle rect in RectBoxes)
                {
                    var result = FindEdge(rect,rect.sigma, rect.threshold, rect.direction, rect.selection, rect.detectionMode);
                    if (result.Count == 0)
                    {                       
                        display.SetDraw("margin");
                        display.SetColor("red");
                        display.SetLineWidth(2.0);
                        display.DispRectangle2(rect.row, rect.column, rect.phi, rect.length1, rect.length2);
                        continue;
                    }
                    else
                    {                       
                        points.Add(new ResultPoints() { id = rect.id, row = result[0], column = result[1] });
                        drawLine(rect, new ResultPoints() { id = rect.id, row = result[0], column = result[1] }, "green");
                        display.SetDraw("margin");
                        display.SetColor("green");
                        display.SetLineWidth(2.0);
                        display.DispRectangle2(rect.row, rect.column, rect.phi, rect.length1, rect.length2);
                    }
                }

                if (points.Count != 0)
                {
                    foreach (var p in points)
                    {
                        display.SetDraw("margin");
                        display.SetColor("green");
                        display.DispCircle(p.row, p.column, 2.0);
                    }
                }
            }

        }
        public HMeasure edges;
        public List<double> FindEdge(UserRectangle rect, double sigma, double threshold, string direction, string selection ,string detectionMode)
        {
            List<double> result = new List<double>();
            if (detectionMode == "Manual")
            {
                result.Add(rect.row);
                result.Add(rect.column);
                return result;
            }
            edges = new HMeasure();
            edges.GenMeasureRectangle2(rect.row, rect.column, rect.phi, rect.length1, rect.length2, w, h, "nearest_neighbor");
            HTuple row, column, amp, dis;
            try
            {
                edges.MeasurePos(this.image,sigma, threshold, direction, selection , out row, out column, out amp, out dis);
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

        private void btn_new_edge_Click(object sender, RoutedEventArgs e)
        {
            if (RectBoxes.Count >= 2)
            {
                MessageBox.Show("The maximum of number of tools is only 2", "Help");
                return;
            }
            HTuple draw_id = new HTuple();

            HOperatorSet.CreateDrawingObjectRectangle2(50, 50, 0, 50, 50, out draw_id);
            HOperatorSet.SetDrawingObjectParams(draw_id, "color", "blue");
            UserRectangle rect = new UserRectangle { id = draw_id, row = 50, column = 50, phi = 0, length1 = 50, length2 = 50 , threshold = 1, sigma=1, direction = "negative", detectionMode ="Auto", selection ="first"};

            RectBoxes.Add(rect);
            lst_id.Add(new ListID { id = draw_id, name = "unknown" });
            lst_edge.Items.Add(new ListID{ id = draw_id, name = "unknown"});
            if (lst_edge.Items.Count == 1)
            {
                lst_edge.SelectedIndex = 0;          
            }
            SetCallbacks(draw_id);
            Apply_Click();
            id_isSelected = RectBoxes.Min(x=>x.id);
            UpdateDisplayInput(id_isSelected);

        }

        private void btn_remove_edge_Click(object sender, RoutedEventArgs e)
        {
            foreach (HTuple dobj in drawing_objects)
            {
                HOperatorSet.ClearDrawingObject(dobj);
            }
            //Clear in HDrawingObject and RectBoxes
            drawing_objects.Clear();
            RectBoxes.Clear();
            lst_id.Clear();
            lst_edge.Items.Clear();
            sld_threshold.Value = 1;
            sld_sigma.Value = 1;
            cmb_detectionMode.SelectedIndex = 0;
            cmb_select.SelectedIndex = 1;
            cmb_transition.SelectedIndex = 1;
            //End 
            UpdateDisplayImage(this.image);
        }

        private void lst_edge_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void Sld_Sigma_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RectBoxes == null || RectBoxes.Count == 0)
            {
                return;
            }
            if (sld_sigma == null)
            {
                return;
            }
            
            if(RectBoxes.Find(x => x.id == id_isSelected) == null)
            {
                return;
            }
        
            RectBoxes.Find(x => x.id == id_isSelected).sigma = Convert.ToDouble(e.NewValue);
            Apply_Click();
        }
        private void Sld_Threshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RectBoxes == null || RectBoxes.Count == 0)
            {
                return;
            }
            if(sld_threshold == null)
            {
                return;
            }
            if (RectBoxes.Find(x => x.id == id_isSelected) == null)
            {
                return;
            }
            RectBoxes.Find(x => x.id == id_isSelected).threshold = Convert.ToDouble(e.NewValue);
            Apply_Click();
        }

        private void cmb_detectionMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RectBoxes == null || RectBoxes.Count ==0)
            {
                return;
            };
            if (cmb_detectionMode.SelectedItem == null)
            {
                return;
            }
            if (RectBoxes.Find(x => x.id == id_isSelected) == null)
            {
                return;
            }
            RectBoxes.Find(x => x.id == id_isSelected).detectionMode = cmb_detectionMode.SelectedItem.ToString();
            Apply_Click();
        }

        private void cmb_select_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RectBoxes == null || RectBoxes.Count == 0)
            {
                return;
            };
            if (cmb_select.SelectedItem == null)
            {
                return;
            }
            if (RectBoxes.Find(x => x.id == id_isSelected) == null)
            {
                return;
            }
            RectBoxes.Find(x => x.id == id_isSelected).selection = cmb_select.SelectedItem.ToString();
            Apply_Click();
        }


        private void cmb_transtion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RectBoxes == null || RectBoxes.Count == 0)
            {
                return;
            };
            if (cmb_transition.SelectedItem == null)
            {
                return;
            }
            if (RectBoxes.Find(x => x.id == id_isSelected) == null)
            {
                return;
            }
            RectBoxes.Find(x => x.id == id_isSelected).direction = cmb_transition.SelectedItem.ToString() ;
            Apply_Click();
        }

        public void drawLine(UserRectangle rect, ResultPoints point, string color)
        {
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
    }
}
