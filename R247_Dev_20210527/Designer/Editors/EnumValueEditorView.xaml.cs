using Microsoft.Win32;
using ReactiveUI;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Editors
{
    /// <summary>
    /// Interaction logic for StringValueEditorView.xaml
    /// </summary>
    public class EnumValueEditorViewGeneric<T> : EnumValueEditorView, IViewFor<EnumValueEditorViewModel<T>>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(EnumValueEditorViewModel<T>), typeof(EnumValueEditorViewGeneric<T>), new PropertyMetadata(null));

        public EnumValueEditorViewModel<T> ViewModel
        {
            get => (EnumValueEditorViewModel<T>)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (EnumValueEditorViewModel<T>)value;
        }
        #endregion

        public EnumValueEditorViewGeneric():base()
        {
            //InitializeComponent();


            this.WhenActivated(new Action<IDisposable>((d) =>
            {
                if (ViewModel != null)
                {

                    this.valueUpDown.ItemsSource = ViewModel.EnumList;
                    this.valueUpDown.DataContext = ViewModel;
                }
            }));

        }
        
    }

    public partial class EnumValueEditorView:UserControl
    {
        public EnumValueEditorView()
        {
            InitializeComponent();
            
        }
        
    }

}
