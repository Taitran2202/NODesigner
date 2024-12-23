﻿using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
    /// Interaction logic for GroupEndpointEditorView.xaml
    /// </summary>
    public partial class GroupEndpointEditorView : IViewFor<IGroupEndpointEditorViewModel>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(IGroupEndpointEditorViewModel), typeof(GroupEndpointEditorView), new PropertyMetadata(null));

        public IGroupEndpointEditorViewModel ViewModel
        {
            get => (IGroupEndpointEditorViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (IGroupEndpointEditorViewModel)value;
        }
        #endregion
        public GroupEndpointEditorView()
        {
            InitializeComponent();
            this.WhenAnyValue(v => v.editNameButton.IsChecked)
                .Select(isChecked => (isChecked ?? false) ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, v => v.nameTextBox.Visibility);

            this.WhenActivated(d =>
            {
                this.Bind(ViewModel, vm => vm.Endpoint.Name, v => v.nameTextBox.Text).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.MoveUp, v => v.upButton).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.MoveDown, v => v.downButton).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.Delete, v => v.deleteButton).DisposeWith(d);
            });
        }
    }
}
