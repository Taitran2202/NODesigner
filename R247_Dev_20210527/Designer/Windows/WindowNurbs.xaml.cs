using DevExpress.Xpf.Core;
using HalconDotNet;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WindowsInput.Native;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for WindowNurbs.xaml
    /// </summary>
    public partial class WindowNurbs : ThemedWindow,INotifyPropertyChanged
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);


        private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const int KEYEVENTF_KEYUP = 0x0002;

        public static void SendKeyPress(byte keyCode)
        {
            keybd_event(keyCode, 0, 0, 0); // Key down
            Thread.Sleep(100);
            keybd_event(keyCode, 0, KEYEVENTF_KEYUP, 0); // Key up
        }
        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;
        public const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const int MOUSEEVENTF_RIGHTUP = 0x0010;
        //This simulates a left mouse click
        public static void LeftMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }
        public static void RightMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, xpos, ypos, 0, 0);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_RIGHTUP, xpos, ypos, 0, 0);
        }
        Nurbs srcRegion;
        private double zoomFactor = 1.0;
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        bool _is_draw;
        public bool IsDraw
        {
            get
            {
                return _is_draw;
            }
            set
            {
                if (_is_draw != value)
                {
                    _is_draw = value;
                    RaisePropertyChanged("IsDraw");
                }
            }
        }
        bool _can_accept;
        public bool CanAccept
        {
            get
            {
                return _can_accept;
            }
            set
            {
                if (_can_accept != value)
                {
                    _can_accept = value;
                    RaisePropertyChanged("CanAccept");
                }
            }
        }
       
        private bool isDragging;
        private double offsetX;
        private double offsetY;
        double originalX;
        double originalY;
        void AddDragBehavior(HWindowControlWPF window)
        {
            window.HMouseMove += (s, e) =>
            {
                if (_is_draw)
                {
                    return;
                }
                if (isDragging && e.Button == MouseButton.Left)
                {
                    
                    double deltaX = e.X- offsetX;
                    double deltaY = e.Y - offsetY;
                    
                    window.HalconWindow.SetPart(originalY - deltaY, originalX - deltaX,
                        originalY - deltaY + window.ImagePart.Height - 1, originalX - deltaX + window.ImagePart.Width - 1);
                    DispData();
                    //offsetX = e.Column ;
                    //offsetY = e.Row;
                }
            };
            window.HMouseDown += (s, e) =>
            {
                offsetX = e.X;
                offsetY = e.Y;
                originalX = window.ImagePart.X;
                originalY = window.ImagePart.Y;
                originalY = window.ImagePart.Y;
                isDragging = true;
            };
            window.HMouseUp += (s, e) =>
            {
                isDragging = false;
            };
        }
        public WindowNurbs(HImage image, Nurbs region, HHomMat2D transform, Window owner, bool is_edit)
        {
            this.srcRegion = region;
            this.image = image;
            this.transform = transform;
            CloneRegion(region);
            this.is_edit = is_edit;
            InitializeComponent();
            AddDragBehavior(window_display);
            this.Owner = owner;
            this.Closing += (o, e) =>
            {
                try
                {
                    if (IsDraw)
                    {
                        
                        var pos = window_display.PointToScreen(new Point(10, 10));
                        Task.Run(() =>
                        {
                            RightMouseClick((int)pos.X, (int)pos.Y);
                        });
                        
                        if (IsDraw == false)
                        {
                            return;
                        }
                        e.Cancel = true;
                       // DXMessageBox.Show("Press right mouse on image before closing window");
                        
                        //window_display.HalconWindow.GetPart(out int r1, out int c1, out int r2, out int c2);
                        //window_display.HalconWindow.SendMouseDownEvent((r1 + r2) / 2, (c1 + c2) / 2, 4);
                    }
                }catch(Exception ex)
                {
                    e.Cancel = true;
                }
                
            };
            this.SourceInitialized += MainWindow_SourceInitialized;
            this.DataContext = this;
        }

        private void CloneRegion(Nurbs region)
        {
            this.region = new Nurbs(region.Minus);
            this.region.rows = region.rows;
            this.region.cols = region.cols;
            this.region.weights = region.weights;
            this.region.CreateRegion();
        }

        bool is_edit = false;
        Nurbs region;
        HHomMat2D transform;
        HWindow display;
        HImage image;
        public bool complete = false;
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            try
            {
                
                display = window_display.HalconWindow;
                display.SetDraw("margin");
                display.SetLineWidth(2);
                display.SetColor(new HTuple("blue","green"));
                if (image != null)
                {
                    display.AttachBackgroundToWindow(image);
                    
                    //window_display.SetFullImagePart(image);
                   
                }

            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                this.Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
           
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            Task.Run((Action)(() =>
            {
                try
                {
                    FitImageToWindow(display, image);
                    this.IsDraw = true;
                    if (is_edit)
                    {
                        try
                        {
                            

                                region.Edit(display, transform);
                                is_edit = false;
                            
                        }
                        catch (Exception ex)
                        {
                            is_edit = false;
                        }
                    }
                    else
                        region.Draw(display, transform);
                    this.IsDraw = false;
                    if (transform != null)
                    {
                        region.region.AffineTransRegion(transform, "constant").DispObj(display);
                    }
                    else
                    {
                        region.region.DispObj(display);
                    }
                    //is_edit = true;
                    complete = true;
                }
                catch (Exception ex)
                {
                    complete = true;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.Close();
                    });
                   
                }
            }));
           
        }
        static void FitImageToWindow(HWindow display,HImage image )
        {
            image.GetImageSize(out int originalWidth, out int originalHeight);
            int windowWidth, windowHeight;
            display.GetWindowExtents(out _,out _,out windowWidth,out windowHeight );

            double widthRatio = (double)windowWidth / originalWidth;
            double heightRatio = (double)windowHeight / originalHeight;

            // Choose the smaller ratio to ensure the entire image fits inside the window
            double zoomFactor = Math.Min(widthRatio, heightRatio);

            double newWidth = ((double)originalWidth) * zoomFactor;
            double newHeight = ((double)originalHeight) * zoomFactor;

            int offsetX = (int)((windowWidth - newWidth) / 2);
            int offsetY = (int)((windowHeight - newHeight) / 2);

            display.SetPart(-offsetY, -offsetX, originalHeight - 1 + offsetY, originalWidth - 1 + offsetX);
            //display.SetWindowExtents(0, 0, (int)(newHeight - 1), (int)(newWidth - 1));
        }
        static void ZoomInAndKeepRatio(HWindow display, int originalWidth, int originalHeight, double zoomFactor)
        {
            HTuple newWidth, newHeight;

            if (zoomFactor <= 0)
                return;

            // Calculate new dimensions
            newWidth = originalWidth * zoomFactor;
            newHeight = originalHeight * zoomFactor;

            // Get the aspect ratio of the original and new dimensions
            double aspectRatioOriginal = (double)originalWidth / originalHeight;
            double aspectRatioNew = (double)newWidth / newHeight;

            // Adjust the new dimensions to maintain the original aspect ratio
            if (aspectRatioOriginal > aspectRatioNew)
            {
                newHeight = originalWidth / aspectRatioOriginal;
            }
            else
            {
                newWidth = originalHeight * aspectRatioOriginal;
            }

            display.SetPart(0, 0, originalHeight - 1, originalWidth - 1);
            display.SetWindowExtents(0, 0, newHeight - 1, newWidth - 1);
        }
        private void btn_zoom_in_Click(object sender, RoutedEventArgs e)
        {
            // Zoom in by increasing the zoom factor
            zoomFactor = 0.9;
            try
            {
                UpdateZoomedPart();

            }
            catch (Exception ex)
            {

            }
        }
        private void UpdateZoomedPart(double centerX, double centerY)
        {
            //double centerX = window_display.ImagePart.Width / 2.0 + window_display.ImagePart.X;
            //double centerY = window_display.ImagePart.Height / 2.0 + window_display.ImagePart.Y;

            double newWidth = window_display.ImagePart.Width * zoomFactor;
            double newHeight = window_display.ImagePart.Height * zoomFactor;
            
            double newTop = centerY-(centerY-window_display.ImagePart.Top)*zoomFactor;
            double newLeft = centerX - (centerX - window_display.ImagePart.Left) * zoomFactor;

            window_display.HalconWindow.SetPart(newTop, newLeft, newTop + newHeight - 1, newLeft + newWidth - 1);
            if (transform != null)
            {

                region.region.AffineTransRegion(transform, "constant").DispObj(display);
            }
            else
            {
                region.region.DispObj(display);
            }
        }
        private void UpdateZoomedPart()
        {
            double centerX = window_display.ImagePart.Width / 2.0+ window_display.ImagePart.X;
            double centerY = window_display.ImagePart.Height / 2.0+ window_display.ImagePart.Y;

            double newWidth = window_display.ImagePart.Width * zoomFactor;
            double newHeight = window_display.ImagePart.Height * zoomFactor;

            double newTop = centerY - (newHeight / 2.0);
            double newLeft = centerX - (newWidth / 2.0);

            window_display.HalconWindow.SetPart(newTop, newLeft, newTop + newHeight - 1, newLeft + newWidth - 1);
            if (transform != null)
            {

                region.region.AffineTransRegion(transform, "constant").DispObj(display);
            }
            else
            {
                region.region.DispObj(display);
            }
        }
        private void btn_zoom_out_Click(object sender, RoutedEventArgs e)
        {
            // Zoom out by decreasing the zoom factor
            zoomFactor = 1.1;
            //if (zoomFactor < 0.1) zoomFactor = 0.1;
            try
            {
                UpdateZoomedPart();
            }
            catch (Exception ex)
            {

            }

        }

        private void btn_accept_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HXLDCont cont = new HXLDCont();
                cont.GenContourNurbsXld(region.rows, region.cols, new HTuple("auto"), region.weights, 3, new HTuple(1), new HTuple(5));
                cont.GenRegionContourXld("filled");
                this.srcRegion.rows = region.rows;
                this.srcRegion.cols = region.cols;
                this.srcRegion.weights = region.weights;
                this.srcRegion.CreateRegion();
            }
            catch(Exception ex)
            {
                DXMessageBox.Show(this,"Current region is abnormal!!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            this.DialogResult = true;
            this.Close();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        
        private void btn_edit_Click(object sender, RoutedEventArgs e)
        {
            if (!is_edit& !IsDraw)
            {
                window_display.HalconWindow.ClearWindow();
                IsDraw = true;
                is_edit = true;
                try
                {
                    region.Edit(display, transform);
                }catch(Exception ex)
                {

                }
                
                is_edit = false;
                IsDraw = false;
                if (transform != null)
                {
                    
                    region.region.AffineTransRegion(transform, "constant").DispObj(display);
                }
                else
                {
                    region.region.DispObj(display);
                }
            }
        }

        private void btn_reset_Click(object sender, RoutedEventArgs e)
        {
            if (!is_edit & !IsDraw)
            {
                is_edit = false;
                window_display.HalconWindow.ClearWindow();
                IsDraw = true;
                region.Draw(display, transform);
                IsDraw = false;
                DispData();
                is_edit = false;
                complete = true;
            }
        }

        private void DispData()
        {
            if (transform != null)
            {
                region.region.AffineTransRegion(transform, "constant").DispObj(display);
            }
            else
            {
                region.region.DispObj(display);
            }
        }
        #region   
        public const int WM_GETTEXT = 0x0D;
        public const int WM_SETTEXT = 0x0C;
        public const int WM_SIZEING = 0x0214;
        public const int WM_COPYDATA = 0x004A;
        public const int WM_LBUTTONDBLCLK = 0x0203;

        public const int WM_LBUTTONDOWN = 0x201;
        public const int WM_LBUTTONUP = 0x202;
        #endregion

        #region   
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpData;
        }
        #endregion
        void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            //IntPtr hwnd = window_display.HalconID;
            //HwndSource.FromHwnd(hwnd).AddHook(new HwndSourceHook(WndProc));
        }
        //public const int WM_GETTEXT = 0x0D;  
        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            switch (msg)
            {

                case WM_LBUTTONDOWN:
                    //txtUserName.Text = "Left button down";  
                    MessageBox.Show("Left button down");
                    break;

                case WM_LBUTTONUP:
                    //txtUserName.Text = "Left button up";  
                    MessageBox.Show("Left button up");
                    break;

                default:
                    {
                        break;
                    }


            }
            return IntPtr.Zero;
        }

        private void btn_change_edit_mode_Click(object sender, RoutedEventArgs e)
        {
            var pos = window_display.PointToScreen(new Point(window_display.ActualWidth/2, window_display.ActualHeight / 2));
            Point currentPosition = Mouse.GetPosition(null);
            Task.Run(() =>
            {
                LeftMouseClick((int)pos.X, (int)pos.Y);
                Thread.Sleep(10);
                SendKeyPress(0x10); // Sends the left shift key press
                Thread.Sleep(10);
                SetCursorPos((int)currentPosition.X, (int)currentPosition.Y);
            });
            


        }

        private void btn_finish_drawing_Click(object sender, RoutedEventArgs e)
        {
            var pos = window_display.PointToScreen(new Point(window_display.ActualWidth / 2, window_display.ActualHeight / 2));
            Point currentPosition = Mouse.GetPosition(null);
            Task.Run(() =>
            {
                RightMouseClick((int)pos.X, (int)pos.Y);
                Thread.Sleep(10);
                SetCursorPos((int)currentPosition.X, (int)currentPosition.Y);
            });

        }

        private void window_display_HMouseWheel(object sender, HMouseEventArgsWPF e)
        {
            if (e.Delta > 0)
            {
                zoomFactor = 0.9;
            }
            else
            {
                zoomFactor = 1.1;
            }
            UpdateZoomedPart(e.Column, e.Row);
        }
    }
    
}
