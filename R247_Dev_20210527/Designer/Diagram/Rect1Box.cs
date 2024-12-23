using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Diagram
{
    public class Rect1Box:ContentControl
    {
        public static readonly DependencyProperty IsDragableProperty = DependencyProperty.Register(
                "IsDragable", typeof(bool),
                typeof(Rect1Box)
                );

        public bool IsDragable
        {
            get => (bool)GetValue(IsDragableProperty);
            set => SetValue(IsDragableProperty, value);
        }

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
               "IsSelected", typeof(bool),
               typeof(Rect1Box)
               );

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }
        public int Index = -1;
    }
}
