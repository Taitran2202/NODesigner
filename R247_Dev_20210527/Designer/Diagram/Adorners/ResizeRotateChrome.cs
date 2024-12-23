using System.Windows;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Diagram
{
    public class ResizeRotateChrome : Control
    {
        static ResizeRotateChrome()
        {
            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(ResizeRotateChrome), new FrameworkPropertyMetadata(typeof(ResizeRotateChrome)));
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
