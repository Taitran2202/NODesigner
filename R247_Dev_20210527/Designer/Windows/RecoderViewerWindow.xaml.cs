using DevExpress.Xpf.Core;
using NOVisionDesigner.Windows;
using System;
using System.Collections.Generic;
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

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for RecoderViewerWindow.xaml
    /// </summary>
    public partial class RecoderViewerWindow : ThemedWindow
    {
        public static Point LastWindowState = new Point(0, 0);
        public static double LastWidth = 0;
        public static double LastHeight = 0;
        public RecoderViewerWindow(Recorder recorder)
        {
            InitializeComponent();
            view.Recorder = recorder;
            if(LastHeight>0 & LastWidth > 0)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Top = LastWindowState.Y;
                this.Left = LastWindowState.X;
                this.Width = LastWidth;
                this.Height = LastHeight;
            }
            ShiftWindowOntoScreenHelper.ShiftWindowOntoScreen(this);
            this.SizeChanged += RecoderViewerWindow_SizeChanged;
            this.LocationChanged += RecoderViewerWindow_LocationChanged;
        }

        private void RecoderViewerWindow_LocationChanged(object sender, EventArgs e)
        {
            if(this.WindowState == WindowState.Normal)
            {
                UpdateLastLocation();
            }
            
        }

        private void RecoderViewerWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                UpdateLastLocation();
            }
        }
        void UpdateLastLocation()
        {
            if(this.Left<0 | this.Top < 0)
            {
                return;
            }
            LastWindowState.X = this.Left;
            LastWindowState.Y = this.Top;
            LastWidth = this.ActualWidth;
            LastHeight = this.ActualHeight;
        }
    }
    public static class ShiftWindowOntoScreenHelper
    {
        /// <summary>
        ///     Intent:  
        ///     - Shift the window onto the visible screen.
        ///     - Shift the window away from overlapping the task bar.
        /// </summary>
        public static void ShiftWindowOntoScreen(Window window)
        {
            // Note that "window.BringIntoView()" does not work.                            
            if (window.Top < SystemParameters.VirtualScreenTop)
            {
                window.Top = SystemParameters.VirtualScreenTop;
            }

            if (window.Left < SystemParameters.VirtualScreenLeft)
            {
                window.Left = SystemParameters.VirtualScreenLeft;
            }

            if (window.Left + window.Width > SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth)
            {
                window.Left = SystemParameters.VirtualScreenWidth + SystemParameters.VirtualScreenLeft - window.Width;
            }

            if (window.Top + window.Height > SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
            {
                window.Top = SystemParameters.VirtualScreenHeight + SystemParameters.VirtualScreenTop - window.Height;
            }

           
        }

        
        
    }
}
