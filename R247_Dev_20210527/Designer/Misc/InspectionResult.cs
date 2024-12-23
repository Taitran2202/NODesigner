using HalconDotNet;
using MoreLinq;
using NOVisionDesigner.Designer.Extensions;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Misc
{
    public class RegionInfo
    {
        public HRegion region;
        public string Name;
        public string Type;
        public RegionInfo(HRegion region, string name, string type)
        {
            this.region = region;
            this.Name = name;
            this.Type = type;
        }
    }
    public interface IDisplayable : IDisposable
    {
        void Display(HWindow display);
    }
    public class DisplayImageRegion : IDisplayable
    {
        static HTuple WindowHandleBuffer;//buffer to draw image then copy this image to the real display at a specific location
        static DisplayImageRegion()
        {
            HOperatorSet.OpenWindow(0, 0, -1, -1, "root", "buffer", "", out WindowHandleBuffer);
        }
        public void Dispose()
        {
            //display_object?.Dispose();
        }
        public int row, col;
        public HImage image = null;
        public HRegion region = null;
        int MaxRegion ;
        int Margin,MaxWidth,MaxHeight;
        int Padding ;
        bool Resize = true;
        bool Vertical = false;
        int TilesColumn;
        public DisplayImageRegion(HImage image,HRegion region,int row,int col,int MaxRegion=4,
            int Padding = 50,int Margin =10,int TilesColumn=1,int MaxWidth=100,int MaxHeight=100,bool Resize=true,bool Vertical=false)
        {
            this.MaxHeight = MaxHeight;
            this.MaxWidth = MaxWidth;
            this.TilesColumn = TilesColumn;
            this.row = row;
            this.col = col;
            this.image = image;
            this.region = region;
            this.MaxRegion = MaxRegion;
            this.Margin = Margin;
            this.Resize = Resize;
            this.Vertical = Vertical;
            this.Padding = Padding;
        }
        public void Display(HWindow display)
        {
            if (image != null & WindowHandleBuffer != null)
            {
                if (!image.IsInitialized())
                {
                    return;
                }
                try
                {


                    if (!Resize)
                    {
                        HOperatorSet.DispObj(image, WindowHandleBuffer);
                        region.SmallestRectangle1(out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                        HTuple PositionRow = new HTuple();
                        HTuple PositionCol = new HTuple();
                        int currentCol = col;
                        int currentRow = row;
                        for (int i = 0; i < Math.Min(row1.Length, MaxRegion); i++)
                        {
                            row1[i] = row1[i] - Padding;
                            row2[i] = row2[i] + Padding;
                            col1[i] = col1[i] - Padding;
                            col2[i] = col2[i] + Padding;
                            if (!Vertical)
                            {
                                PositionRow.Append(row);
                                PositionCol.Append(currentCol);
                                currentCol = currentCol + col2[i] - col1[i] + Margin;
                            }
                            else
                            {
                                PositionCol.Append(col);
                                PositionRow.Append(currentRow);
                                currentRow = currentRow + row2[i] - row1[i] + Margin;
                            }

                        }
                        HOperatorSet.CopyRectangle(WindowHandleBuffer, display, row1, col1, row2, col2, PositionRow, PositionCol);
                    }
                    else
                    {
                        region.SmallestRectangle1(out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                        if (row1.Length > 0)
                        {
                            HImage images = null;
                            for (int i = 0; i < Math.Min(row1.Length, MaxRegion); i++)
                            {
                                row1[i] = row1[i] - Padding;
                                row2[i] = row2[i] + Padding;
                                col1[i] = col1[i] - Padding;
                                col2[i] = col2[i] + Padding;
                                double rW = (double)MaxWidth / (col2[i] - col1[i]);
                                double rH = (double)MaxHeight / (row2[i] - row1[i]);
                                HImage part = image.CropRectangle1((int)Math.Max(0, row1[i]), (int)Math.Max(0, col1[i]), (int)row2[i], (int)col2[i]);
                                part = part.ZoomImageFactor(Math.Min(rW, rH), Math.Min(rW, rH), "bilinear");
                                if (images == null)
                                {
                                    images = part;
                                }
                                else
                                {
                                    images = images.ConcatObj(part);
                                }

                            }
                            image.GetImageSize(out int w, out int h);
                            HHomMat2D hHomMat2D = new HHomMat2D();
                            hHomMat2D = hHomMat2D.HomMat2dTranslate((HTuple)row, col);
                            HImage tiles = images.TileImages(Math.Min(row1.Length, MaxRegion), Vertical ? "vertical" : "horizontal");
                            tiles.GetImageSize(out int tilew, out int tileh);
                            tiles.AffineTransImageSize(hHomMat2D, "bilinear", w, h).Rectangle1Domain(row, col, row + tileh, col + tilew).DispObj(display);
                            display.SetColor("blue");
                            var numRegion = Math.Min(row1.Length, MaxRegion);
                            for (int i = 0; i < numRegion; i++)
                            {
                                
                                display.DispRectangle1((double)row, col + i * tilew/ numRegion, row + tileh, col + (i + 1) * tilew/numRegion);
                            }
                        }
                        
                    }
                }catch (Exception ex)
                {

                }
                
                //display.SetWindowExtents(row, col, 100, 100);
                //display.MoveRectangle(row, col, 100, 100, 0, 0);
                //display.DispObj(image);
            }
        }
    }
    public class DisplayImage: IDisplayable
    {
        static HTuple WindowHandleBuffer;//buffer to draw image then copy this image to the real display at a specific location
        static DisplayImage()
        {
            HOperatorSet.OpenWindow(0, 0, -1, -1, "root", "buffer", "", out WindowHandleBuffer);
        }
        public void Dispose()
        {
            //display_object?.Dispose();
        }
        public int row, col;
        public HImage image = null;
        public DisplayImage(HImage image)
        {
            this.image = image;
        }
        public DisplayImage(HImage image, int row,int col)
        {
            this.image = image;
            this.row = row;
            this.col = col;
        }
        public void Display(HWindow display)
        {
            if (image != null & WindowHandleBuffer!=null)
            {
                //HOperatorSet.OpenWindow(0, 0, -1, -1, "root", "buffer", "", out HTuple WindowHandleBuffer);
                HOperatorSet.DispObj(image, WindowHandleBuffer);
                image.GetImageSize(out int w, out int h);
                //display.GetOsWindowHandle(out IntPtr OSHandle);
                HOperatorSet.CopyRectangle(WindowHandleBuffer, display, 0,0, h, w,row,col);
                //display.SetWindowExtents(row, col, 100, 100);
                //display.MoveRectangle(row, col, 100, 100, 0, 0);
                //display.DispObj(image);
            }
        }
    }
    public class DisplayObject : IDisplayable
    {
        public void Dispose()
        {
            //display_object?.Dispose();
        }
        public string color = "red";
        public HObject display_object = null;
        bool is_region = false;
        string draw;
        public DisplayObject(string color, HObject display_object)
        {
            this.color = color;
            this.display_object = display_object;
        } 
        public DisplayObject(string color, HRegion display_object, string draw = "margin")
        {
            this.color = color;
            this.display_object = display_object;
            is_region = true;
            this.draw = draw;
        }
        public void Display(HWindow display)
        {
            if (display_object != null)
            {
                if (is_region == true)
                {
                    var a = display.GetDraw();
                    display.SetDraw(draw);
                    display.SetColor(color);
                    display.DispObj(display_object);
                    display.SetDraw(a);

                }
                else
                {
                    display.SetDraw("margin");
                    display.SetColor(color);
                    display.DispObj(display_object);
                }

            }
        }
    }
    public class DisplayText : IDisplayable
    {
        public void Dispose()
        {

        }
        public string color = "black";
        public string box_color = "white";
        public string message = "";
        double row, col;
        double fontsize = 12;
        bool usebox = true;
        public DisplayText(string color, string message, string box_color, double row, double col, double fontsize,bool box = true)
        {
            this.fontsize = fontsize;
            this.color = color;
            this.box_color = box_color;
            this.message = message;
            this.row = row; this.col = col;
            usebox = box;
        }
        public void Display(HWindow display)
        {
            display.SetFont("default-Normal-" + fontsize.ToString());
            if(usebox == true)
            {
                display.DispText(message, "image", row, col, color, "box_color", box_color);
            }
           else
            {
                display.DispText(message, "image", row, col, color, "box", "false");
            }
            display.SetFont("default-Normal-12");
        }
    }
    public class DisplayMultiText : IDisplayable
    {
        public void Dispose()
        {

        }
        public string color = "black";
        public string box_color = "white";
        public string[] messages ;
        double row, col;
        double fontsize = 12;
        bool usebox = true;
        public DisplayMultiText(string color, string[] messages, string box_color, double row, double col, double fontsize, bool box = true)
        {
            this.fontsize = fontsize;
            this.color = color;
            this.box_color = box_color;
            this.messages = messages;
            this.row = row; this.col = col;
            usebox = box;
        }
        public void Display(HWindow display)
        {
            display.SetFont("default-Normal-" + fontsize.ToString());
            if (usebox == true)
            {
                
                display.DispText(new HTuple(messages), "image", row, col, color, "box_color", box_color);
            }
            else
            {
                display.DispText(new HTuple(messages), "image", row, col, color, "box", "false");
            }
            display.SetFont("default-Normal-12");
        }
    }
    public class DisplayCircle : IDisplayable
    {
        public string color = "green";
        double row, col, phi, radius;
        public DisplayCircle(string color, double row, double col, double radius)
        {
            this.color = color;
            this.row = row;
            this.col = col;
            this.radius= radius;
        }
        public void Display(HWindow display)
        {
            display.SetColor(color);
            display.DispCircle(row, col, radius);
        }

        public void Dispose()
        {

        }
    }
    public class DisplayEllipse : IDisplayable
    {
        public string color = "green";
        double row, col, phi, radius1, radius2;
        public DisplayEllipse(string color, double row, double col, double phi, double radius1, double radius2)
        {
            this.color = color;
            this.row = row;
            this.col = col;
            this.phi = phi;
            this.radius1 = radius1;
            this.radius2 = radius2;
        }
        public void Display(HWindow display)
        {
            display.SetColor(color);
            display.DispEllipse(row, col, phi, radius1, radius2);
        }

        public void Dispose()
        {

        }
    }
    public class DisplayRect2 : IDisplayable
    {
        public string color = "green";
        double row, col, phi, length1,length2;
        public DisplayRect2(string color, double row, double col, double phi, double length1,double length2)
        {
            this.color = color;
            this.row = row;
            this.col = col;
            this.phi = phi;
            this.length1 = length1;
            this.length2 = length2;
        }
        public void Display(HWindow display)
        {
            display.SetColor(color);
            display.DispRectangle2(row, col, phi, length1,length2);
        }

        public void Dispose()
        {

        }
    }
    public class DisplayRect1 : IDisplayable
    {
        public string color = "green";
        double row1, col1, row2, col2;
        public DisplayRect1(string color, double row1, double col1, double row2, double col2)
        {
            this.color = color;
            this.row1 = row1;
            this.row2 = row2;
            this.col1 = col1;
            this.col2 = col2;
        }
        public void Display(HWindow display)
        {
            display.SetDraw("margin");
            display.SetColor(color);
            display.DispRectangle1(row1, col1, row2, col2);
        }

        public void Dispose()
        {
            
        }
    }
    public class DisplayLine : IDisplayable
    {
        public void Dispose()
        {

        }
        public string color = "green";
        double row1, col1, row2, col2;
        double size = 2;
        public DisplayLine(string color, double row1, double col1, double row2, double col2, double size)
        {
            this.size = size;
            this.color = color;
            this.row1 = row1;
            this.row2 = row2;
            this.col1 = col1;
            this.col2 = col2;
        }
        public void Display(HWindow display)
        {
            display.SetLineWidth(size);
            display.SetColor(color);
            display.DispLine(row1, col1, row2, col2);
            display.SetLineWidth(1);
        }
    }
    public class InspectionResult
    {
        public Dictionary<string, bool> ResultTable { get; set; } = new Dictionary<string, bool>();
        public List<IDisplayable> lst_display = new List<IDisplayable>();
        public HImage image;
        public double scale_x = 1;
        public double scale_y = 1;
        public List<string> ColorCodes;
        public List<RegionInfo> regions;
        public int ImageID { get; set; }
        public string ID { get; set; }
        public HHomMat2D Transform { get; set; }
        public InspectionResult(HImage image, double scale_x, double scale_y,string ID)
        {
            this.image = image;
            this.scale_x = scale_x;
            this.scale_y = scale_y;
            ColorCodes = new List<string>();
            regions = new List<RegionInfo>();
            this.ID = ID;
        }
        public void DisplayResultTable(HWindow window)
        {
            if (ResultTable.Count > 0)
            {
                window.SetFont("default-Normal-18");
                double startRow = 10, startCol = 10;
                var list = ResultTable.ToList();
                //var maxLength = list.Max(x => x.Key.Length);
                window.GetFontExtents(out _,out _ ,out HTuple maxHeight);
                var texts = list.Select(x => x.Key).ToArray();
                HTuple rows = new HTuple();
                HTuple cols = new HTuple();
                HTuple result_cols = new HTuple();
                HTuple colors = new HTuple();
                HTuple textParam = new HTuple();
                HTuple resultText = new HTuple();
                for (int i=0;i<texts.Length;i++)
                {
                    if (!list[i].Value)
                    {
                        window.DispText("NG", "window", startRow + i * (maxHeight + 5), 
                            startCol, new HTuple("black"), new HTuple("box_color", "border_radius"),new HTuple("#ff0000ff",0));
                    }
                    else
                    {
                        window.DispText("OK", "window", startRow + i * (maxHeight + 5), 
                            startCol, new HTuple("black"), new HTuple("box_color", "border_radius"), new HTuple("#00ff00ff",0));
                    }
                    textParam.Append("box_color");
                    rows.Append(startRow + i * (maxHeight +5));
                    cols.Append(startCol+37);
                    
                }
                window.DispText(texts, "window", rows, cols, "white", new HTuple("box_color", "border_radius"), new HTuple("#0000ffff" +
                    "",0));
                
            }
            
            
        }
        public void Display(HSmartWindowControlWPF display)
        {
            if (display != null)
            {
                if (display.HalconWindow != null)
                {
                    ///Add by Minh
                
                    display.HalconWindow.SetWindowParam("graphics_stack_max_element_num", 100);
                    //display.HalconWindow.SetDraw("margin");
                    //display.HalconWindow.SetLineWidth(2);
                    foreach (IDisplayable disp in lst_display)
                    {

                        disp?.Display(display.HalconWindow);           
                    }

                    DisplayResultTable(display.HalconWindow);
                }
            }
                
        }
        public void AddRect1(string color,double row1,double col1,double row2,double col2)
        {
            if(Transform != null)
            {
                Transform.AffineTransPixel(new HTuple(row1, row2), new HTuple(col1, col2), out HTuple rowTrans, out HTuple colTrans);
                lst_display.Add(new DisplayRect1(color, rowTrans[0], colTrans[0], rowTrans[1], colTrans[1]));
            }
            else
            {
                lst_display.Add(new DisplayRect1(color, row1, col1, row2, col2));
            }   
            
        }
        public void AddRect2(string color, double row, double col, double phi, double length1,double length2)
        {
            if (Transform != null)
            {
                Transform.AffineTransPixel(new HTuple(row),new HTuple(col),out HTuple rowTrans, out HTuple colTrans);
                
                lst_display.Add(new DisplayRect2(color, rowTrans[0], colTrans[0], phi, length1,length2)); //not finish
            }
            else
            {
                lst_display.Add(new DisplayRect2(color, row, col, phi, length1,length2));
            }

        }
        public void AddCircle(string color, double row, double col, double radius)
        {
            if (Transform != null)
            {
                Transform.AffineTransPixel(new HTuple(row), new HTuple(col), out HTuple rowTrans, out HTuple colTrans);

                lst_display.Add(new DisplayCircle(color, rowTrans[0], colTrans[0], radius)); //not finish
            }
            else
            {
                lst_display.Add(new DisplayCircle(color, row, col,radius));
            }

        }
        public void AddEllipse(string color, double row, double col, double phi, double radius1, double radius2)
        {
            if (Transform != null)
            {
                Transform.AffineTransPixel(new HTuple(row), new HTuple(col), out HTuple rowTrans, out HTuple colTrans);

                lst_display.Add(new DisplayEllipse(color, rowTrans[0], colTrans[0], phi, radius1, radius2)); //not finish
            }
            else
            {
                lst_display.Add(new DisplayEllipse(color, row, col, phi, radius1, radius2));
            }

        }
        public void AddDisplay(HObject disp_object, string color)
        {
            if (Transform != null)
            {
                //disp_object.AffineTransImage(Transform,"constant",)
                lst_display.Add(new DisplayObject(color, disp_object));
            }
            else
            {
                lst_display.Add(new DisplayObject(color, disp_object));
            }
            
        }
        public void AddImage(HImage image, int row, int col)
        {
            if (Transform != null)
            {
                lst_display.Add(new DisplayImage(image, row, col));
            }
            else
            {
                lst_display.Add(new DisplayImage(image, row, col));
            }
            
        }
        public void DisplayImageRegion(HImage image, HRegion region, int row, int col, int MaxRegion = 4,
            int Padding = 50, int Margin = 10, int TilesColumn = 1, int MaxWidth = 100, 
            int MaxHeight = 100, bool Resize = true, bool Vertical = false)
        {
            if (Transform != null)
            {
                lst_display.Add(new DisplayImageRegion(image, region,row,col,MaxRegion,Padding,
                    Margin, TilesColumn, MaxWidth, MaxHeight, Resize, Vertical));
            }
            else
            {
                lst_display.Add(new DisplayImageRegion(image, region, row, col, MaxRegion, Padding,
                    Margin, TilesColumn, MaxWidth, MaxHeight, Resize, Vertical));
            }

        }

        public void AddRegion(HRegion disp_object, string color, string draw_type = "margin")
        {
            if (Transform != null)
            {
                //disp_object.AffineTransImage(Transform,"constant",)
                lst_display.Add(new DisplayObject(color, disp_object.AffineTransRegion(Transform,"constant"), draw_type));
            }
            else
            {
                lst_display.Add(new DisplayObject(color, disp_object, draw_type));
            }
            
        }
        public void AddLine(double row1, double col1, double row2, double col2, string color, double size)
        {
            if (Transform != null)
            {
                Transform.AffineTransPixel(new HTuple(row1, row2), new HTuple(col1, col2), out HTuple rowTrans, out HTuple colTrans);
                lst_display.Add(new DisplayLine(color, rowTrans[0], colTrans[0], rowTrans[1], colTrans[1], size));
            }
            else
            {
                lst_display.Add(new DisplayLine(color, row1, col1, row2, col2, size));
            }
            
        }

        public void AddText(string message, string color, string box_color, double row, double col, double fontsize = 12, bool box = true)
        {
            if (Transform != null)
            {
                Transform.AffineTransPixel(row,col, out double rowTrans, out double colTrans);
                lst_display.Add(new DisplayText(color, message, box_color, rowTrans, colTrans, fontsize, box));
            }
            else
            {
                lst_display.Add(new DisplayText(color, message, box_color, row, col, fontsize, box));
            }
            
        }
        public void AddTextMulti(string[] message, string color, string box_color, double row, double col, double fontsize = 12, bool box = true)
        {
            if (Transform != null)
            {
                Transform.AffineTransPixel(row, col, out double rowTrans, out double colTrans);
                lst_display.Add(new DisplayMultiText(color, message, box_color, rowTrans, colTrans, fontsize, box));
            }
            else
            {
                lst_display.Add(new DisplayMultiText(color, message, box_color, row, col, fontsize, box));
            }

        }
        public void Add(HRegion defect_region, string color_code, string Name, string type)
        {
            regions.Add(new RegionInfo(defect_region, Name, type));
            ColorCodes.Add(color_code);
        }
        public void AddCharacterResult(CharacterResult characterResult, string color, string box_color, TextPosition textPosition = TextPosition.Top, double fontsize = 12)
        {
            var position = Functions.GetDisplayPosition(textPosition, characterResult.Box.row, characterResult.Box.col, characterResult.Box.phi, characterResult.Box.length1, characterResult.Box.length2);
            AddText(characterResult.ClassName, color, box_color, position.row, position.col, fontsize);
            AddRect2(box_color, characterResult.Box.row, characterResult.Box.col, characterResult.Box.phi, characterResult.Box.length1, characterResult.Box.length2);
        }
        public void Dispose()
        {
            image.Dispose();
            foreach (RegionInfo region in regions)
            {
                region.region.Dispose();
            }
            //foreach (IDisplayable display in lst_display)
            //{
            //    display.Dispose();
            //}
        }
    }
}
