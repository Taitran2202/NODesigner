﻿using ReactiveUI;
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
    /// Interaction logic for IntegerValueEditorView.xaml
    /// </summary>
    public partial class DefaultOutputValueEditorView<T> : DefaultOutputValueEditorView, IViewFor<DefaultOutputValueEditorViewModel<T>>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(DefaultOutputValueEditorViewModel<T>), typeof(DefaultOutputValueEditorView<T>), new PropertyMetadata(null));

        public DefaultOutputValueEditorViewModel<T> ViewModel
        {
            get => (DefaultOutputValueEditorViewModel<T>)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (DefaultOutputValueEditorViewModel<T>)value;
        }
        #endregion

        public DefaultOutputValueEditorView()
        {
            this.WhenActivated(d => d(
                this.Bind(ViewModel, vm => vm.Value, v => v.text.Text)
            ));

        }


        
    }
    public partial class DefaultOutputValueEditorView : UserControl
    {
        public DefaultOutputValueEditorView()
        {
            InitializeComponent();
        }
    }
}
