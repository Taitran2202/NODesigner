
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WindowsInput;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Threading;
using NOVisionDesigner.Designer.Misc;
using Newtonsoft.Json.Linq;
using NOVisionDesigner.Designer.Deeplearning.Windows;

namespace NOVisionDesigner.Designer.Nodes
{
    public class DrawRegion
    {
        HImage image;
        //public HRegion region = new HRegion();
        public HRegion region_select = new HRegion();
        public CollectionOfregion regions = new CollectionOfregion();
        public void AdapImageSize(HImage image, HWindowControlWPF window)
        {

            HTuple w, h;
            image.GetImageSize(out w, out h);
            window.ImagePart = new Rect(0, 0, w, h);
            if (window.ActualWidth > window.ActualHeight * w / h)
            {
                window.Width = window.ActualHeight * w / h;
            }
            else
            {
                window.Height = window.ActualWidth * h / w;
            }
        }
        HHomMat2D transform;

        HWindow display;
        public void ChangeRegion()
        {
            if (display == null)
                return;
            //region.Dispose();
            display.ClearWindow();
            //region.GenEmptyRegion();
            //foreach (Region item in regions.regions)
            //{
            //    if (!item.Minus)
            //    {
            //        region = region.Union2(item.region);
            //    }
            //}
            //foreach (Region item in regions.regions)
            //{
            //    if (item.Minus)
            //    {
            //        region = region.Difference(item.region);
            //    }
            //}
            DispOverlay();
            regions.MergeRegion();


        }
        public void DispOverlay()
        {
            display.SetDraw("fill");
            display.SetColor(regions.Color);
            if (transform != null)
                display.DispObj(regions.Region.AffineTransRegion(transform, "nearest_neighbor"));
            else
                display.DispObj(regions.Region);
        }

    }
    public class CollectionOfregion : INotifyPropertyChanged,IHalconDeserializable
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Region> regions = new ObservableCollection<Region>();

        string _color = "#00ff00ff";
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

        public void Save(HFile file)
        {
            //save the name 1st
            (new HTuple(Name)).SerializeTuple().FwriteSerializedItem(file);

            (new HTuple(_color)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(regions.Count)).SerializeTuple().FwriteSerializedItem(file);

            foreach (Region region_temp in regions)
            {
                region_temp.Save(file);
            }
        }

        public void Load(DeserializeFactory item)
        {
            HTuple temp = item.DeserializeTuple();
            _color = item.DeserializeTuple();
            Name = temp.S;
            temp = item.DeserializeTuple();
            regions.Clear();
            for (int i = 0; i < temp; i++)
            {
                HTuple temp2 = item.DeserializeTuple();
                switch (temp2.I)
                {
                    case ((int)RegionType.Nurbs): regions.Add(new Nurbs(item)); break;
                    case ((int)RegionType.Brush): regions.Add(new BrushRegion(item)); break;
                    case ((int)RegionType.Rectangle1): regions.Add(new Rectange1(item)); break;
                    case ((int)RegionType.Rectangle2): regions.Add(new Rectange2(item)); break;
                    case ((int)RegionType.Circle): regions.Add(new Circle(item)); break;
                    case ((int)RegionType.Ellipse): regions.Add(new Ellipse(item)); break;
                    default:
                        break;
                }

            }
            MergeRegion();

        }

        public CollectionOfregion()
        {
            Region.GenEmptyRegion();
        }

        public CollectionOfregion(DeserializeFactory item)
        {
            Load(item);
        }

        string _name = "ok";
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

        public HRegion Region { get => region;internal set => region = value; }

        /// <summary>
        /// sum and add of all region
        /// </summary>
        private HRegion region = new HRegion();
        public void MergeRegion()
        {
            HRegion newregion=null;
            foreach(var item in regions)
            {
                if (!item.Minus)
                {
                    if (newregion == null)
                    {
                        newregion = item.region;
                        continue;
                    }
                    newregion = newregion.ConcatObj(item.region);
                }
                
            }
            HRegion newregionMinus = new HRegion();
            newregionMinus.GenEmptyRegion();
            foreach (var item in regions)
            {
                if (item.Minus)
                {
                    newregionMinus = newregionMinus.Union2(item.region);
                }

            }
            if (newregion == null)
            {
                newregion = new HRegion();
                newregion.GenEmptyRegion();
            }
            region = newregion.Union1().Difference(newregionMinus);
        }
        public void GenRegionBorder()
        {
            region = region.GenContourRegionXld("border").GenRegionContourXld("margin");

        }
        

    }
    public enum RegionType
    {
        Nurbs = 1, Rectangle1 = 2, Rectangle2 = 3, Brush = 4, Circle = 5, Ellipse = 6
    }
    public delegate void UpdateRegionParam(Region sender);
    public class Region
    {
        public virtual void TransformRegion(HHomMat2D transform)
        {
            region = transform.AffineTransRegion(region, "constant");
        }
        public virtual void ClearDrawingData(HWindow display)
        {
            if (current_draw != null)
                display.DetachDrawingObjectFromWindow(current_draw);
            current_draw = null;
        }
        internal HDrawingObject current_draw;
        public UpdateRegionParam OnUpdated;
        public UpdateRegionParam OnSelected;
        public virtual void OnResize(HDrawingObject drawid, HWindow window, string type)
        {

            //   drawid.GetDrawingObjectParams()

            //RefreshGraphic();
        }
        public virtual HRegion GenContourRegion()
        {
            if (region != null)
            {
                var border = region.GenContourRegionXld("border").GenRegionContourXld("margin").DilationCircle(2.5);
                return border;
            }
            else
            {
                var emptyobj= new HRegion();
                emptyobj.GenEmptyObj();
                return emptyobj;
            }
        }
        public virtual void OnSelect(HDrawingObject drawid, HWindow window, string type)
        {

            //   drawid.GetDrawingObjectParams()

            //RefreshGraphic();
        }
        public virtual HDrawingObject CreateDrawingObject(HHomMat2D transform)
        {
            return null;
            //throw new Exception("Method is not declare");
        }

        public virtual void Initial(int row, int col)
        {

            //throw new Exception("Method is not declare");
        }
        private bool minus = false;
        public HRegion region = new HRegion();
        public virtual void CreateRegion()
        {

        }
        public bool Minus { get => minus; set => minus = value; }

        public virtual void Draw(HWindow display, HHomMat2D transform, int w, int h)
        {
            MessageBox.Show("This draw method wasn't implement!!");
        }
        public virtual void UpdateDrawingObject(HDrawingObject drawid, HHomMat2D transform)
        {

        }
        public virtual void Edit(HWindow display, HHomMat2D transform)
        {
            throw new Exception("Method is not declare");
        }
        public virtual void Draw(HWindow display, HHomMat2D transform)
        {
            throw new Exception("Method is not declare");
        }
        public virtual void Save(HFile file)
        {
            MessageBox.Show("Error Saving Region. Save method was not declare");
        }
    }
    public class Nurbs : Region
    {
        public override void CreateRegion()
        {
            try
            {

                HXLDCont cont = new HXLDCont();
                cont.GenContourNurbsXld(rows, cols, new HTuple("auto"), weights, 3, new HTuple(1), new HTuple(5));
                region = cont.GenRegionContourXld("filled");
            }
            catch (Exception ex)
            {
                region.GenEmptyRegion();
            }
        }
        public override void TransformRegion(HHomMat2D transform)
        {
            transform.AffineTransPixel(rows, cols, out rows, out cols);
            if (current_draw != null)
                current_draw.SetDrawingObjectParams(new HTuple("row", "column"), new HTuple(rows, cols));

            base.TransformRegion(transform);
        }
        public override HRegion GenContourRegion()
        {
            
            HXLDCont cont = new HXLDCont();
            cont.GenContourNurbsXld(rows, cols, "auto", "auto", 3, 1, 5);
            cont.GetContourXld(out HTuple rows_cont, out HTuple cols_cont);
            HRegion region = new HRegion();
            region.GenRegionPolygon(rows_cont, cols_cont);
            return region;
        }
        public override void OnResize(HDrawingObject drawid, HWindow window, string type)
        {

            //   drawid.GetDrawingObjectParams()
            UpdateDrawingObject(drawid, transform);
            OnUpdated?.Invoke(this);
            //RefreshGraphic();
        }
        public override void Initial(int row, int col)
        {
            rows = new HTuple(row - 100, row, row + 100, row);
            cols = new HTuple(col, col + 100, col, col - 100);
        }
        public HHomMat2D transform;
        public override HDrawingObject CreateDrawingObject(HHomMat2D transform)
        {
            current_draw = null;
            this.transform = transform;
            HTuple row_t, col_t;
            if (rows.Length == 0)
            {
                rows = new HTuple(200, 400);
                cols = new HTuple(200, 400);
            }
            if (transform != null)
            {


                transform.AffineTransPixel(rows, cols, out row_t, out col_t);


                current_draw = new HDrawingObject();
                current_draw.CreateDrawingObjectXld(row_t, col_t);
            }
            else
            {
                current_draw = new HDrawingObject();
                current_draw.CreateDrawingObjectXld(rows, cols);
            }
            current_draw.SetDrawingObjectParams(new HTuple("line_width"), 3);

            return current_draw;


        }
        public override void UpdateDrawingObject(HDrawingObject drawid, HHomMat2D transform)
        {
            HTuple row, col;
            row = drawid.GetDrawingObjectParams(new HTuple("row"));
            col = drawid.GetDrawingObjectParams(new HTuple("column"));

            if (transform != null)
            {


                transform.HomMat2dInvert().AffineTransPixel(row, col, out rows, out cols);


            }
            else
            {
                rows = row;
                cols = col;

            }
            try
            {
                HXLDCont cont = new HXLDCont();
                cont.GenContourNurbsXld(rows, cols, "auto", "auto", 3, 1, 5);
                region = cont.GenRegionContourXld("filled");
            }
            catch (Exception ex)
            {
                region.GenEmptyRegion();
            }

        }

        private RegionType type = RegionType.Nurbs;
        public override void Save(HFile file)
        {
            //save the type of region 1st
            (new HTuple((int)type)).SerializeTuple().FwriteSerializedItem(file);


            //save faram
            (new HTuple(rows)).SerializeTuple().FwriteSerializedItem(file);

            (new HTuple(cols)).SerializeTuple().FwriteSerializedItem(file);

            (new HTuple(weights)).SerializeTuple().FwriteSerializedItem(file);

            if (Minus)
            {
                (new HTuple(1)).SerializeTuple().FwriteSerializedItem(file);
            }
            else
            {
                (new HTuple(0)).SerializeTuple().FwriteSerializedItem(file);
            }
            //save the region
            region.SerializeRegion().FwriteSerializedItem(file);
        }
        public override string ToString()
        {
            if (Minus)
                return " - " + name;
            else
                return " + " + name;
        }
        private string name = "Curve";
        public string Name
        {
            get
            {
                if (Minus)
                    return " - " + name;
                else
                    return " + " + name;
            }

            set
            {
                name = value;
            }
        }
        public HTuple rows, cols, weights = 0;
        public Nurbs(bool minus)
        {
            this.Minus = minus;
            region.GenEmptyRegion();
            rows = new HTuple();
            cols = new HTuple();
            weights = new HTuple();
        }

        public Nurbs(DeserializeFactory item)
        {

            rows = item.DeserializeTuple();

            cols = item.DeserializeTuple();

            weights = item.DeserializeTuple();

            HTuple temp = item.DeserializeTuple();

            if (temp == 1)
            {
                Minus = true;
            }
            else
                Minus = false;
            region = item.DeserializeRegion();

        }
        public override void Draw(HWindow display, HHomMat2D transform, int w, int h)
        {
            this.Draw(display, transform);
        }
        public override void Draw(HWindow display, HHomMat2D transform)
        {
            if (transform == null)
                region = display.DrawNurbs("true", "true", "true", "true", 3, out rows, out cols, out weights).GenRegionContourXld("filled");
            else
            {
                HTuple rows_t, cols_t;
                HRegion temp = display.DrawNurbs("true", "true", "true", "true", 3, out rows_t, out cols_t, out weights).GenRegionContourXld("filled");
                transform.HomMat2dInvert().AffineTransPixel(rows_t, cols_t, out rows, out cols);
                region = transform.HomMat2dInvert().AffineTransRegion(temp, "nearest_neighbor");
            }
        }
        public override void Edit(HWindow display, HHomMat2D transform)
        {
            if (transform == null)
                region = display.DrawNurbsMod("true", "true", "true", "true", "true", 3, rows, cols, weights, out rows, out cols, out weights).GenRegionContourXld("filled");
            else
            {
                HTuple r_t, c_t, rows_t = new HTuple(), cols_t = new HTuple();
                transform.AffineTransPixel(rows, cols, out r_t, out c_t);
                if (weights.Length == 0)
                {
                    weights = new HTuple(15.0);
                    for (int i = 0; i < rows.Length - 2; i++)
                    {
                        weights = weights.TupleConcat(15.0);
                    }
                }
                HRegion temp = display.DrawNurbsMod("true", "true", "true", "true", "true", 3, r_t, c_t, weights.Clone(), out rows_t, out cols_t, out weights).GenRegionContourXld("filled");
                transform.HomMat2dInvert().AffineTransPixel(rows_t, cols_t, out rows, out cols);
                region = transform.HomMat2dInvert().AffineTransRegion(temp, "nearest_neighbor");
            }
        }
    }
    public class Rectange1 : Region
    {
        public override void CreateRegion()
        {
            try
            {
                region.GenRectangle1(row1, col1, row2, col2);
            }
            catch (Exception ex)
            {
                region.GenEmptyRegion();
            }
        }
        public override void TransformRegion(HHomMat2D transform)
        {
            transform.AffineTransPixel(row1, col1, out row1, out col1);
            transform.AffineTransPixel(row2, col2, out row2, out col2);
            if (current_draw != null)
                current_draw.SetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"), new HTuple((int)row1, (int)col1, (int)row2, (int)col2));

            base.TransformRegion(transform);
        }
        public override void Initial(int row, int col)
        {
            this.row1 = row;
            this.col1 = col;
            this.row2 = row1 + 100;
            this.col2 = col1 + 100;
        }
        public override void OnResize(HDrawingObject drawid, HWindow window, string type)
        {
            UpdateDrawingObject(drawid, transform);
            OnUpdated?.Invoke(this);
        }
        public override void OnSelect(HDrawingObject drawid, HWindow window, string type)
        {
            UpdateDrawingObject(drawid, transform);
            OnSelected?.Invoke(this);
        }
        private RegionType type = RegionType.Rectangle1;

        public HHomMat2D transform;


        public override HDrawingObject CreateDrawingObject(HHomMat2D transform)
        {
            current_draw = null;
            this.transform = transform;
            HTuple row_t, col_t, row2_t, col2_t;

            if (transform != null)
            {
                transform.AffineTransPixel(row1, col1, out row_t, out col_t);
                transform.AffineTransPixel(row2, col2, out row2_t, out col2_t);
                current_draw = new HDrawingObject((int)row_t.D, (int)col_t.D, (int)row2_t.D, (int)col2_t.D);
            }
            else
            {
                current_draw = new HDrawingObject((int)row1, (int)col1, (int)row2, (int)col2);
            }
            UpdateDrawingObject(current_draw, transform);
            return current_draw;
        }
        public override void UpdateDrawingObject(HDrawingObject drawid, HHomMat2D transform)
        {
            HTuple param;
            param = drawid.GetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"));

            if (transform != null)
            {


                transform.HomMat2dInvert().AffineTransPixel(param[0], param[1], out row1, out col1);
                transform.HomMat2dInvert().AffineTransPixel(param[2], param[3], out row2, out col2);


            }
            else
            {
                row1 = (int)param[0];
                col1 = (int)param[1];
                row2 = (int)param[2];
                col2 = (int)param[3];
            }
            region.GenRectangle1(row1, col1, row2, col2);

        }
        public override void Save(HFile file)
        {
            //save the type of region 1st
            (new HTuple((int)type)).SerializeTuple().FwriteSerializedItem(file);

            //save faram
            (new HTuple(row1)).SerializeTuple().FwriteSerializedItem(file);

            (new HTuple(col1)).SerializeTuple().FwriteSerializedItem(file);


            (new HTuple(row2)).SerializeTuple().FwriteSerializedItem(file);

            (new HTuple(col2)).SerializeTuple().FwriteSerializedItem(file);

            if (Minus)
            {
                (new HTuple(1)).SerializeTuple().FwriteSerializedItem(file);
            }
            else
            {
                (new HTuple(0)).SerializeTuple().FwriteSerializedItem(file);
            }

            region.SerializeRegion().FwriteSerializedItem(file);
        }
        public double row1 = 0, col1 = 0, row2 = 100, col2 = 100;
        public double Row1
        {
            get { return row1; }
            set { row1 = value; }
        }
        public double Row2
        {
            get { return row2; }
            set { row2 = value; }
        }
        public double Col1
        {
            get { return col1; }
            set { col1 = value; }
        }
        public double Col2
        {
            get { return col2; }
            set { col2 = value; }
        }

        public Rectange1(bool minus)
        {
            this.Minus = minus;
            region.GenEmptyRegion();
        }
        public Rectange1(DeserializeFactory item)
        {
            row1 = item.DeserializeTuple();

            col1 = item.DeserializeTuple();

            row2 = item.DeserializeTuple();

            col2 = item.DeserializeTuple();

            HTuple temp = item.DeserializeTuple();

            if (temp == 1)
            {
                Minus = true;
            }
            else
                Minus = false;
            region = item.DeserializeRegion();
        }

        public override void Draw(HWindow display, HHomMat2D transform, int w, int h)
        {
            //base.Draw(display, transform, default_parameters);
            double rowin1, colin1, rowin2, colin2;
            rowin1 = h / 4;
            colin1 = w / 4;
            rowin2 = h * 3 / 4;
            colin2 = w * 3 / 4;

            if (transform != null)
            {
                display.DrawRectangle1Mod(rowin1, colin1, rowin2, colin2, out row1, out col1, out row2, out col2);

                transform.HomMat2dInvert().AffineTransPixel(row1, col1, out row1, out col1);
                transform.HomMat2dInvert().AffineTransPixel(row2, col2, out row2, out col2);

                region.GenRectangle1(row1, col1, row2, col2);
            }
            else
            {
                display.DrawRectangle1Mod(rowin1, colin1, rowin2, colin2, out row1, out col1, out row2, out col2);
                region.GenRectangle1(row1, col1, row2, col2);
            }
        }
        public override void Draw(HWindow display, HHomMat2D transform)
        {
            if (transform != null)
            {
                display.DrawRectangle1(out row1, out col1, out row2, out col2);

                transform.HomMat2dInvert().AffineTransPixel(row1, col1, out row1, out col1);
                transform.HomMat2dInvert().AffineTransPixel(row2, col2, out row2, out col2);

                region.GenRectangle1(row1, col1, row2, col2);
            }
            else
            {
                display.DrawRectangle1(out row1, out col1, out row2, out col2);
                region.GenRectangle1(row1, col1, row2, col2);
            }
        }
        public override void Edit(HWindow display, HHomMat2D transform)
        {

            if (transform != null)
            {
                double rt1, rt2, cl1, cl2;
                transform.AffineTransPixel(row1, col1, out rt1, out cl1);
                transform.AffineTransPixel(row2, col2, out rt2, out cl2);
                display.DrawRectangle1Mod(rt1, cl1, rt2, cl2, out row1, out col1, out row2, out col2);

                transform.HomMat2dInvert().AffineTransPixel(row1, col1, out row1, out col1);
                transform.HomMat2dInvert().AffineTransPixel(row2, col2, out row2, out col2);

                region.GenRectangle1(row1, col1, row2, col2);
            }
            else
            {
                display.DrawRectangle1Mod(row1, col1, row2, col2, out row1, out col1, out row2, out col2);
                region.GenRectangle1(row1, col1, row2, col2);
            }

        }
        public override string ToString()
        {
            if (Minus)
                return " - " + name;
            else
                return " + " + name;
        }
        private string name = "Rectangle1";
        public string Name
        {
            get
            {
                if (Minus)
                    return " - " + name;
                else
                    return " + " + name;
            }

            set
            {
                name = value;
            }
        }
    }
    public class Rectange2 : Region
    {
        public override void CreateRegion()
        {
            try
            {
                region.GenRectangle2(row, col, phi, length1, length2);
            }
            catch (Exception ex)
            {
                region.GenEmptyRegion();
            }
        }
        public override void TransformRegion(HHomMat2D transform)
        {

            double sy, phi_t, theta, tx, ty;
            transform.HomMat2dToAffinePar(out sy, out phi_t, out theta, out tx, out ty);
            transform.AffineTransPixel(row, col, out row, out col);
            phi = phi + phi_t;
            if (current_draw != null)
                current_draw.SetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"), new HTuple(row, col, phi, length1, length2));

            base.TransformRegion(transform);
        }
        public override void OnResize(HDrawingObject drawid, HWindow window, string type)
        {

            //   drawid.GetDrawingObjectParams()

            UpdateDrawingObject(drawid, transform);
            OnUpdated?.Invoke(this);
            //RefreshGraphic();
        }
        public override void OnSelect(HDrawingObject drawid, HWindow window, string type)
        {
            UpdateDrawingObject(drawid, transform);
            OnSelected?.Invoke(this);
        }

        public HHomMat2D transform;

        public override void Initial(int row, int col)
        {
            this.row = row;
            this.col = col;
        }
        public override HDrawingObject CreateDrawingObject(HHomMat2D transform)
        {
            current_draw = null;
            this.transform = transform;
            HTuple row_t, col_t;
            double sy, phi_t, theta, tx, ty;


            if (transform != null)
            {

                transform.HomMat2dToAffinePar(out sy, out phi_t, out theta, out tx, out ty);
                transform.AffineTransPixel(row, col, out row_t, out col_t);
                //transform.AffineTransPixel(row2, col2, out row2_t, out col2_t);

                current_draw = new HDrawingObject((int)row_t.D, (int)col_t.D, phi + phi_t, length1, length2);
            }
            else
            {
                current_draw = new HDrawingObject((int)row, (int)col, phi, length1, length2);
            }
            UpdateDrawingObject(current_draw, transform);
            return current_draw;


        }
        public override void UpdateDrawingObject(HDrawingObject drawid, HHomMat2D transform)
        {
            HTuple param;
            param = drawid.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));
            if (transform != null)
            {
                double r_t, c_t, p_t, sy, phi_t, theta, tx, ty;
                transform.HomMat2dToAffinePar(out sy, out phi_t, out theta, out tx, out ty);
                transform.HomMat2dInvert().AffineTransPixel(param[0], param[1], out row, out col);
                length1 = param[3];
                length2 = param[4];
                phi = param[2] - phi_t;
            }
            else
            {
                row = param[0];
                col = param[1];
                phi = param[2];
                length1 = param[3];
                length2 = param[4];

            }
            // HRegion temp = new HRegion();
            region.GenRectangle2(row, col, phi, length1, length2);
        }

        private RegionType type = RegionType.Rectangle2;
        public double row = 100, col = 100, length1 = 50, length2 = 50, phi = 0;
        public override void Save(HFile file)
        {
            //save the type of region 1st
            (new HTuple((int)type)).SerializeTuple().FwriteSerializedItem(file);

            //save faram
            (new HTuple(row)).SerializeTuple().FwriteSerializedItem(file);

            (new HTuple(col)).SerializeTuple().FwriteSerializedItem(file);


            (new HTuple(phi)).SerializeTuple().FwriteSerializedItem(file);

            (new HTuple(length1)).SerializeTuple().FwriteSerializedItem(file);

            (new HTuple(length2)).SerializeTuple().FwriteSerializedItem(file);





            if (Minus)
            {
                (new HTuple(1)).SerializeTuple().FwriteSerializedItem(file);
            }
            else
            {
                (new HTuple(0)).SerializeTuple().FwriteSerializedItem(file);
            }
            region.SerializeRegion().FwriteSerializedItem(file);

        }
        public Rectange2(bool minus)
        {
            this.Minus = minus;
            region.GenEmptyRegion();
        }
        public Rectange2(bool minus,double row, double col, double phi,double length1, double length2)
        {
            this.Minus = minus;
            //region.GenEmptyRegion();
            this.row = row;
            this.col = col;
            this.phi = phi;
            this.length1 = length1;
            this.length2 = length2;
            region.GenRectangle2(row, col, phi, length1, length2);
        }
        public Rectange2(DeserializeFactory item)
        {
            row = item.DeserializeTuple();

            col = item.DeserializeTuple();

            phi = item.DeserializeTuple();

            length1 = item.DeserializeTuple();


            length2 = item.DeserializeTuple();

            HTuple temp = item.DeserializeTuple();

            if (temp == 1)
            {
                Minus = true;
            }
            else
                Minus = false;
            region = item.DeserializeRegion();
        }
        public override string ToString()
        {
            if (Minus)
                return " - " + name;
            else
                return " + " + name;
        }
        private string name = "Rectangle2";
        public string Name
        {
            get
            {
                if (Minus)
                    return " - " + name;
                else
                    return " + " + name;
            }

            set
            {
                name = value;
            }
        }
        public override void Draw(HWindow display, HHomMat2D transform, int w, int h)
        {
            //base.Draw(display, transform, default_parameters);
            double rowIn, columnIn, phiIn, length1In, length2In;
            rowIn = h / 2;
            columnIn = w / 2;
            length1In = h / 4;
            length2In = w / 4;
            phiIn = 0;
            display.DrawRectangle2Mod(rowIn, columnIn, phiIn, length1In, length2In, out row, out col, out phi, out length1, out length2);

        }
        public override void Draw(HWindow display, HHomMat2D transform)
        {

            display.DrawRectangle2(out row, out col, out phi, out length1, out length2);
            region.GenRectangle2(row, col, phi, length1, length2);
        }
        public override void Edit(HWindow display, HHomMat2D transform)
        {
            double r_t, c_t, p_t, sy, phi_t, theta, tx, ty;
            transform.HomMat2dToAffinePar(out sy, out phi_t, out theta, out tx, out ty);
            transform.AffineTransPixel(row, col, out r_t, out c_t);
            display.DrawRectangle2Mod(r_t, c_t, phi + phi_t, length1, length2, out row, out col, out phi, out length1, out length2);
            transform.HomMat2dInvert().AffineTransPixel(row, col, out row, out col);
            phi = phi - phi_t;
            // HRegion temp = new HRegion();
            region.GenRectangle2(row, col, phi, length1, length2);
            // region =  temp.AffineTransRegion(transform.HomMat2dInvert(), "constant");

        }
    }
    public class Circle : Region
    {
        public override void TransformRegion(HHomMat2D transform)
        {

            double sy, phi_t, theta, tx, ty;
            transform.HomMat2dToAffinePar(out sy, out phi_t, out theta, out tx, out ty);
            transform.AffineTransPixel(row, col, out row, out col);

            if (current_draw != null)
                current_draw.SetDrawingObjectParams(new HTuple("row", "column", "radius"), new HTuple(row, col, radius));

            base.TransformRegion(transform);
        }
        public override void OnResize(HDrawingObject drawid, HWindow window, string type)
        {

            //   drawid.GetDrawingObjectParams()

            UpdateDrawingObject(drawid, transform);
            OnUpdated?.Invoke(this);
            //RefreshGraphic();
        }


        public HHomMat2D transform;

        public override void Initial(int row, int col)
        {
            this.row = row;
            this.col = col;
        }
        public override HDrawingObject CreateDrawingObject(HHomMat2D transform)
        {
            current_draw = null;
            this.transform = transform;
            HTuple row_t, col_t;
            double sy, phi_t, theta, tx, ty;
            //transform.HomMat2dToAffinePar(out sy, out phi_t, out theta, out tx, out ty);

            if (transform != null)
            {


                transform.AffineTransPixel(row, col, out row_t, out col_t);
                //transform.AffineTransPixel(row2, col2, out row2_t, out col2_t);

                current_draw = new HDrawingObject((int)row_t.D, (int)col_t.D, radius);
            }
            else
            {
                current_draw = new HDrawingObject((int)row, (int)col, radius);
            }
            UpdateDrawingObject(current_draw, transform);
            return current_draw;


        }
        public override void UpdateDrawingObject(HDrawingObject drawid, HHomMat2D transform)
        {
            HTuple param;
            param = drawid.GetDrawingObjectParams(new HTuple("row", "column", "radius"));
            if (transform != null)
                transform.HomMat2dInvert().AffineTransPixel(param[0], param[1], out row, out col);
            else
            {
                row = param[0];
                col = param[1];
            }
            radius = param[2];
            region.GenCircle(row, col, param[2]);
        }

        private RegionType type = RegionType.Circle;
        public double row = 100, col = 100, radius = 50;
        public override void Save(HFile file)
        {
            //save the type of region 1st
            (new HTuple((int)type)).SerializeTuple().FwriteSerializedItem(file);

            //save faram
            (new HTuple(row)).SerializeTuple().FwriteSerializedItem(file);

            (new HTuple(col)).SerializeTuple().FwriteSerializedItem(file);


            (new HTuple(radius)).SerializeTuple().FwriteSerializedItem(file);
            if (Minus)
            {
                (new HTuple(1)).SerializeTuple().FwriteSerializedItem(file);
            }
            else
            {
                (new HTuple(0)).SerializeTuple().FwriteSerializedItem(file);
            }
            region.SerializeRegion().FwriteSerializedItem(file);

        }
        public Circle(bool minus)
        {
            this.Minus = minus;
            region.GenEmptyRegion();
        }
        public Circle(bool minus,double row,double col,double radius)
        {
            this.Minus = minus;
            this.row = row;
            this.col = col;
            this.radius = radius;
            region.GenCircle(row,col,radius); 
        }
        public Circle(DeserializeFactory item)
        {
            row = item.DeserializeTuple();

            col = item.DeserializeTuple();

            radius = item.DeserializeTuple();

            HTuple temp = item.DeserializeTuple();

            if (temp == 1)
            {
                Minus = true;
            }
            else
                Minus = false;
            region = item.DeserializeRegion();
        }
        public override string ToString()
        {
            if (Minus)
                return " - " + name;
            else
                return " + " + name;
        }
        private string name = "Circle";
        public string Name
        {
            get
            {
                if (Minus)
                    return " - " + name;
                else
                    return " + " + name;
            }

            set
            {
                name = value;
            }
        }
        public override void Draw(HWindow display, HHomMat2D transform, int w, int h)
        {
            MessageBox.Show("Circle not supported");

        }
        public override void Draw(HWindow display, HHomMat2D transform)
        {

            MessageBox.Show("Circle not supported");
        }
        public override void Edit(HWindow display, HHomMat2D transform)
        {
            MessageBox.Show("Circle not supported");
        }
        public override void OnSelect(HDrawingObject drawid, HWindow window, string type)
        {
            UpdateDrawingObject(drawid, transform);
            OnSelected?.Invoke(this);
        }
    }
    public class Ellipse : Region
    {
        public override void TransformRegion(HHomMat2D transform)
        {

            double sy, phi_t, theta, tx, ty;
            transform.HomMat2dToAffinePar(out sy, out phi_t, out theta, out tx, out ty);
            transform.AffineTransPixel(row, col, out row, out col);
            phi = phi + phi_t;
            if (current_draw != null)
                current_draw.SetDrawingObjectParams(new HTuple("row", "column", "phi", "radius1", "radius2"), new HTuple(row, col, phi, radius1, radius2));

            base.TransformRegion(transform);
        }
        public override void OnResize(HDrawingObject drawid, HWindow window, string type)
        {

            //   drawid.GetDrawingObjectParams()

            UpdateDrawingObject(drawid, transform);
            OnUpdated?.Invoke(this);
            //RefreshGraphic();
        }


        public HHomMat2D transform;

        public override void Initial(int row, int col)
        {
            this.row = row;
            this.col = col;
        }
        public override HDrawingObject CreateDrawingObject(HHomMat2D transform)
        {
            current_draw = null;
            this.transform = transform;
            HTuple row_t, col_t;
            double sy, phi_t, theta, tx, ty;
            // 

            if (transform != null)
            {
                transform.HomMat2dToAffinePar(out sy, out phi_t, out theta, out tx, out ty);
                transform.AffineTransPixel(row, col, out row_t, out col_t);
                //transform.AffineTransPixel(row2, col2, out row2_t, out col2_t);
                current_draw = new HDrawingObject();
                current_draw.CreateDrawingObjectEllipse((int)row_t.D, (int)col_t.D, phi + phi_t, radius1, radius2);
            }
            else
            {
                current_draw = new HDrawingObject();
                current_draw.CreateDrawingObjectEllipse((int)row, (int)col, phi, radius1, radius2);
            }
            UpdateDrawingObject(current_draw, transform);
            return current_draw;


        }
        public override void UpdateDrawingObject(HDrawingObject drawid, HHomMat2D transform)
        {
            HTuple param;
            param = drawid.GetDrawingObjectParams(new HTuple("row", "column", "phi", "radius1", "radius2"));

            double r_t, c_t, p_t, sy, phi_t, theta, tx, ty;
            if (transform != null)
            {
                transform.HomMat2dToAffinePar(out sy, out phi_t, out theta, out tx, out ty);
                transform.HomMat2dInvert().AffineTransPixel(param[0], param[1], out row, out col);
                phi = param[2] - phi_t;
            }
            else
            {
                row = param[0];
                col = param[1];
                phi = param[2];
            }
            radius1 = param[3];
            radius2 = param[4];

            region.GenEllipse(row, col, phi, radius1, radius2);
        }

        private RegionType type = RegionType.Ellipse;
        public double row = 100, col = 100, radius1 = 50, radius2 = 100, phi = 0;
        public override void Save(HFile file)
        {
            //save the type of region 1st
            (new HTuple((int)type)).SerializeTuple().FwriteSerializedItem(file);

            //save faram
            (new HTuple(row)).SerializeTuple().FwriteSerializedItem(file);

            (new HTuple(col)).SerializeTuple().FwriteSerializedItem(file);


            (new HTuple(phi)).SerializeTuple().FwriteSerializedItem(file);

            (new HTuple(radius1)).SerializeTuple().FwriteSerializedItem(file);

            (new HTuple(radius2)).SerializeTuple().FwriteSerializedItem(file);





            if (Minus)
            {
                (new HTuple(1)).SerializeTuple().FwriteSerializedItem(file);
            }
            else
            {
                (new HTuple(0)).SerializeTuple().FwriteSerializedItem(file);
            }
            region.SerializeRegion().FwriteSerializedItem(file);

        }
        public Ellipse(bool minus)
        {
            this.Minus = minus;
            region.GenEmptyRegion();
        }
        public Ellipse(bool minus,double row,double col,double phi,double radius1,double radius2)
        {
            this.Minus = minus;
            
            this.row = row;
            this.col = col;
            this.phi = phi;
            this.radius1 = radius1;
            this.radius2 = radius2;
            region.GenEllipse(row, col, phi, radius1, radius2);
        }
        public Ellipse(DeserializeFactory item)
        {
            row = item.DeserializeTuple();

            col = item.DeserializeTuple();

            phi = item.DeserializeTuple();

            radius1 = item.DeserializeTuple();


            radius1 = item.DeserializeTuple();

            HTuple temp = item.DeserializeTuple();

            if (temp == 1)
            {
                Minus = true;
            }
            else
                Minus = false;
            region = item.DeserializeRegion();
        }
        public override string ToString()
        {
            if (Minus)
                return " - " + name;
            else
                return " + " + name;
        }
        private string name = "Ellipse";
        public string Name
        {
            get
            {
                if (Minus)
                    return " - " + name;
                else
                    return " + " + name;
            }

            set
            {
                name = value;
            }
        }
        public override void Draw(HWindow display, HHomMat2D transform, int w, int h)
        {
            MessageBox.Show("Ellipse not supported!");

        }
        public override void Draw(HWindow display, HHomMat2D transform)
        {

            MessageBox.Show("Ellipse not supported!");
        }
        public override void Edit(HWindow display, HHomMat2D transform)
        {
            MessageBox.Show("Ellipse not supported!");
        }
    }
    public class BrushRegion : Region    
    {
        private RegionType type = RegionType.Brush;
        public override void Save(HFile file)
        {
            //save the type of region 1st
            (new HTuple((int)type)).SerializeTuple().FwriteSerializedItem(file);
            if (Minus)
            {
                (new HTuple(1)).SerializeTuple().FwriteSerializedItem(file);
            }
            else
            {
                (new HTuple(0)).SerializeTuple().FwriteSerializedItem(file);
            }
            region.SerializeRegion().FwriteSerializedItem(file);

        }
        public override string ToString()
        {
            if (Minus)
                return " - " + name;
            else
                return " + " + name;
        }
        private string name = "Brush";
        public string Name
        {
            get
            {
                if (Minus)
                    return " - " + name;
                else
                    return " + " + name;
            }

            set
            {
                name = value;
            }
        }
        public BrushRegion(DeserializeFactory item)
        {



            HTuple temp = item.DeserializeTuple();

            if (temp == 1)
            {
                Minus = true;
            }
            else
                Minus = false;
            region = item.DeserializeRegion();

        }
        public BrushRegion(bool minus)
        {
            this.Minus = minus;
            region.GenEmptyRegion();
        }
        public override void Edit(HWindow display, HHomMat2D transform)
        {
            HImage image=null;
            try
            {
                image = display.GetWindowBackgroundImage();
            }
            catch(Exception ex)
            {

            }
            if (image != null)
            {
                BrushWindow wd = new BrushWindow(display.GetWindowBackgroundImage(), transform, this);
                wd.ShowDialog();
            }
            else{
                image = new HImage("byte",512,512);
                BrushWindow wd = new BrushWindow(image, transform, this);
                wd.ShowDialog();
            }
            
        }
    }
}
