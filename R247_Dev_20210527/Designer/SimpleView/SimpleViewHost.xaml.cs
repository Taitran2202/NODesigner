using NodeNetwork.ViewModels;
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
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Grid;
using System.Collections.ObjectModel;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.Views;
using NOVisionDesigner.Windows;
using System.ComponentModel;
using NOVisionDesigner.Designer.Misc;
using System.Threading;
using System.Diagnostics;

namespace NOVisionDesigner.Designer.SimpleView
{
    /// <summary>
    /// Interaction logic for SimpleViewHost.xaml
    /// </summary>
    public partial class SimpleViewHost : UserControl, INotifyPropertyChanged
    {
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
       
        
        public DesignerHost _designer_host;
        public DesignerHost DesignerHost
        {
            get
            {
                return _designer_host;
            }
            set
            {
                if (_designer_host != value)
                {
                    _designer_host = value;
                    RaisePropertyChanged("DesignerHost");
                }
            }
        }
        public SimpleViewHost()
        {
            InitializeComponent();
            this.DataContextChanged += SimpleViewHost_DataContextChanged;


            
           
        }

        private void SimpleViewHost_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(this.DesignerHost == null)
            {
                if(DataContext is HMI)
                {
                    this.DesignerHost = (this.DataContext as HMI).host;
                    if (this.DesignerHost != null)
                    {
                        ViewModel.UserViewModel.Instance.WhenAnyValue(x => x.CurrentUser).Subscribe(x =>
                        {
                            foreach (var item in DesignerHost.HMI.BindingList)
                            {
                                if (item.Role.Contains(x.Role.Name))
                                {
                                    item.IsEditable = true;
                                }
                                else
                                {
                                    if (x.Role.ID == 0 || Debugger.IsAttached)
                                    {
                                        item.IsEditable = true;
                                    }
                                    else
                                    {
                                        item.IsEditable = false;
                                    }
                                    
                                }
                            }
                        });
                    }
                    
                }

                
            }
        }

        public void SetDesigner(DesignerHost designerHost)
        {
            this.DesignerHost = designerHost;
            if (designerHost.HMI.BindingList == null)
            {
                designerHost.HMI.BindingList = new ObservableCollection<NodeBinding>();
            }
            
            //lst_binding.ItemsSource =  designerHost.HMI.BindingList;
            //grid_main.DataContext = DesignerHost.HMI;
            //this.DataContext = DesignerHost.HMI;
        }
        private void btn_add_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.PrivilegeViewModel.Instance.CanEditHMISetting)
            {
                if (DesignerHost == null)
                {
                    return;
                }
                var propselection_window = new PropertiesSelectionWindow(DesignerHost);
                propselection_window.ShowDialog();
            }
            else
            {
                MessageBox.Show("You dont have privilege to do this!","Warning");
            }
            
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            var selectedItem = b.DataContext as NodeBinding;
            if (selectedItem != null)
            {
                DesignerHost.HMI.BindingList.Remove(selectedItem);
            }
        }

        private void btn_up_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            var selectedItem = b.DataContext as NodeBinding;
            if (selectedItem != null)
            {
                var index = DesignerHost.HMI.BindingList.IndexOf(selectedItem);
                if (index > 0)
                {
                    DesignerHost.HMI.BindingList.Move(index, index - 1);
                }
                
            }
        }

        private void btn_down_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            var selectedItem = b.DataContext as NodeBinding;
            if (selectedItem != null)
            {
                var index = DesignerHost.HMI.BindingList.IndexOf(selectedItem);
                if (index < DesignerHost.HMI.BindingList.Count-1)
                {
                    DesignerHost.HMI.BindingList.Move(index, index + 1);
                    
                }

            }
        }

        private void simpleButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.PrivilegeViewModel.Instance.CanEditHMISetting)
            {
                if (DesignerHost != null)
                {
                    DesignerHost.HMI.IsEdit = !DesignerHost.HMI.IsEdit;
                }
            }
            else
            {
                if (DesignerHost.HMI.IsEdit)
                {
                    DesignerHost.HMI.IsEdit = false;
                }
                else
                {
                    MessageBox.Show("You dont have privilege to do this!", "Warning");
                }
                
            }
            
        }

        private void btn_edit_hmi_privilege_Click(object sender, RoutedEventArgs e)
        {
            EditHMIPrivilegeWindow wd = new EditHMIPrivilegeWindow((sender as Button).DataContext as NodeBinding);
            wd.Show();
        }

        private void btn_move_Click(object sender, RoutedEventArgs e)
        {

        }



        private void b_PreviewMouseMove(object sender, MouseEventArgs e)
        {

            
            
        }

        private void b_Drop(object sender, DragEventArgs e)
        {
            var source = e.Data.GetData("Source") as NodeBinding;
            if (source != null)
            {
                int newIndex = lst_binding.Items.IndexOf((sender as FrameworkElement).DataContext);
                var list = lst_binding.ItemsSource as ObservableCollection<NodeBinding>;
                list.RemoveAt(list.IndexOf(source));
                list.Insert(newIndex, source);
            }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var data = new DataObject();
                data.SetData("Source", (sender as FrameworkElement).DataContext);
                DragDrop.DoDragDrop(sender as DependencyObject, data, DragDropEffects.Move);
                e.Handled = true;
            }
        }
    }

    public class MyTemplateSelector : DataTemplateSelector
    {
        public static DataTemplate CreateTextboxTemplate(Binding bindingPath)
        {
            if(bindingPath.Mode == BindingMode.OneWay)
            {
                return CreateLabelTemplate(bindingPath);
            }
            var textboxFactory = new FrameworkElementFactory(typeof(TextBox));
            
            textboxFactory.SetValue(TextBox.HorizontalContentAlignmentProperty,HorizontalAlignment.Right);
            textboxFactory.SetValue(TextBox.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            textboxFactory.SetBinding(TextBox.TextProperty, bindingPath);
            return new DataTemplate
            {
                VisualTree = textboxFactory,
            };
        }
        public static DataTemplate CreateValueBoxTemplate(Binding bindingPath)
        {
            if (bindingPath.Mode == BindingMode.OneWay)
            {
                return CreateLabelTemplate(bindingPath);
            }
            var textboxFactory = new FrameworkElementFactory(typeof(NumericUpDownWithKeyboard));

            textboxFactory.SetValue(NumericUpDownWithKeyboard.HorizontalContentAlignmentProperty, HorizontalAlignment.Right);
            textboxFactory.SetValue(NumericUpDownWithKeyboard.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            textboxFactory.SetValue(NumericUpDownWithKeyboard.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            textboxFactory.SetBinding(NumericUpDownWithKeyboard.ValueProperty, bindingPath);
            return new DataTemplate
            {
                VisualTree = textboxFactory,
            };
        }
        public static DataTemplate CreateEnumTemplate(Binding bindingPath,Array items)
        {
            var textboxFactory = new FrameworkElementFactory(typeof(ComboBox));

            textboxFactory.SetValue(ComboBox.HorizontalContentAlignmentProperty, HorizontalAlignment.Right);
            textboxFactory.SetValue(ComboBox.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            textboxFactory.SetBinding(ComboBox.SelectedItemProperty, bindingPath);
            textboxFactory.SetValue(ComboBox.ItemsSourceProperty, items);
            return new DataTemplate
            {
                VisualTree = textboxFactory,
            };
        }
        public static DataTemplate CreateLabelTemplate(Binding bindingPath)
        {
            var textboxFactory = new FrameworkElementFactory(typeof(TextBox));
            bindingPath.Mode = BindingMode.OneWay;
            textboxFactory.SetBinding(TextBox.TextProperty, bindingPath);
            textboxFactory.SetValue(TextBox.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            textboxFactory.SetValue(TextBox.IsReadOnlyProperty,true);
            return new DataTemplate
            {
                VisualTree = textboxFactory,
            };
        }
        public static DataTemplate CreateButtonTemplate(Binding bindingPath,string description)
        {
            var textboxFactory = new FrameworkElementFactory(typeof(Button));
            textboxFactory.SetValue(Button.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            textboxFactory.SetValue(Button.ContentProperty, description);
            textboxFactory.SetValue(Button.HorizontalContentAlignmentProperty, HorizontalAlignment.Left);
            textboxFactory.SetBinding(Button.CommandProperty, bindingPath);
            Binding b = new Binding();
            b.RelativeSource = new RelativeSource(RelativeSourceMode.Self);
            textboxFactory.SetBinding(Button.CommandParameterProperty, b);
            return new DataTemplate
            {
                VisualTree = textboxFactory,
            };
        }
        public static DataTemplate CreateCheckboxtemplate(Binding bindingPath)
        {
            var textboxFactory = new FrameworkElementFactory(typeof(ToggleSwitch));
            //textboxFactory.SetValue(ToggleSwitch.ContentTemplateProperty, null);
            textboxFactory.SetValue(ToggleSwitch.CheckedStateContentProperty, "ON");
            textboxFactory.SetValue(ToggleSwitch.UncheckedStateContentProperty, "OFF");
            textboxFactory.SetValue(ToggleSwitch.ContentPlacementProperty, ToggleSwitchContentPlacement.Near);
            textboxFactory.SetValue(ToggleSwitch.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            textboxFactory.SetValue(ToggleSwitch.HorizontalContentAlignmentProperty, HorizontalAlignment.Right);
            textboxFactory.SetBinding(ToggleSwitch.IsCheckedProperty, bindingPath);
            
            return new DataTemplate
            {
                VisualTree = textboxFactory,
            };
        }
        public static DataTemplate CreatePropertyButtonTemplate(object target, string description)
        {
            var textboxFactory = new FrameworkElementFactory(typeof(Button));
            textboxFactory.SetValue(Button.ContentProperty, description);
            textboxFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(
                (o, e) =>
                {
                    PropertiesWindow wd = new PropertiesWindow(target);
                    wd.ShowDialog();
                }
                ));

            return new DataTemplate
            {
                VisualTree = textboxFactory,
            };
        }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement elem = container as FrameworkElement;
            if (elem == null)
            {
                return null;
            }
            if (item == null || !(item is NodeBinding))
            {
                throw new ApplicationException();
            }
            var nodebinding = item as NodeBinding;
            if (nodebinding.Node == null)
            {
                if (nodebinding.TagManager != null)
                {
                    if (nodebinding.TagManager.TagList.Any(x => x.Name == nodebinding.Label))
                    {
                        Binding myTagBinding = new Binding();
                        myTagBinding.Source = nodebinding.TagManager.TagList[nodebinding.TagIndex];
                        myTagBinding.Path = new PropertyPath("Value");
                        myTagBinding.Mode = BindingMode.OneWay;
                        return CreateLabelTemplate(myTagBinding);
                    }
                }
                
                return UnsupportedTemplate();
            }
            Binding myBinding = new Binding(nodebinding.PropName);
            myBinding.Source = nodebinding.Node;
            var propData = nodebinding.Node.GetType().GetProperty(nodebinding.PropName);
            if (propData.SetMethod != null)
            {
                if (!(propData.CanWrite & propData.SetMethod.IsPublic))
                {
                    myBinding.Mode = BindingMode.OneWay;
                }
            }
            
            var type = propData.PropertyType;
            
            if (type == typeof(int))
            {            
                
                return CreateValueBoxTemplate(myBinding);
            }
            if (type == typeof(string))
            {                              
                return CreateTextboxTemplate(myBinding);
            }
            if (type == typeof(double))
            {
                return CreateValueBoxTemplate(myBinding);
            }
            if (type == typeof(IReactiveCommand))
            {
                var description = (nodebinding.Node.GetType().GetProperty(nodebinding.PropName).GetCustomAttributes(typeof(HMIProperty), false)[0] as HMIProperty).Description;
                return CreateButtonTemplate(myBinding,description);
            }
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(ValueNodeInputViewModel<>))
                {
                    var generictype = type.GetGenericArguments()[0];
                    var prop = nodebinding.Node.GetType().GetProperty(nodebinding.PropName).GetValue(nodebinding.Node) as NodeInputViewModel;
                    
                    myBinding.Path = new PropertyPath("Editor");
                    myBinding.Source = prop;
                    var contentFactory = new FrameworkElementFactory(typeof(ViewModelViewHost));
                    contentFactory.SetValue(ViewModelViewHost.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                    contentFactory.SetValue(ViewModelViewHost.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
                    contentFactory.SetValue(ViewModelViewHost.VerticalContentAlignmentProperty, VerticalAlignment.Stretch);
                    contentFactory.SetBinding(ViewModelViewHost.ViewModelProperty, myBinding);
                    return new DataTemplate
                    {
                        VisualTree = contentFactory,
                    };

                    //if (generictype == typeof(int))
                    //{
                    //    myBinding.Path = new PropertyPath("Editor");
                    //    myBinding.Source = prop;
                    //    var contentFactory = new FrameworkElementFactory(typeof(ViewModelViewHost));
                    //    contentFactory.SetBinding(ViewModelViewHost.ViewModelProperty, myBinding);
                    //    contentFactory.SetValue(ViewModelViewHost.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                    //    contentFactory.SetValue(ViewModelViewHost.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
                    //    contentFactory.SetValue(ViewModelViewHost.VerticalContentAlignmentProperty, VerticalAlignment.Stretch);
                    //    return new DataTemplate
                    //    {
                    //        VisualTree = contentFactory,
                    //    };
                    //}
                    //if (generictype == typeof(string))
                    //{
                    //    myBinding.Path = new PropertyPath("Editor");
                    //    myBinding.Source = prop;
                    //    var contentFactory = new FrameworkElementFactory(typeof(ViewModelViewHost));
                    //    contentFactory.SetBinding(ViewModelViewHost.ViewModelProperty, myBinding);
                    //    contentFactory.SetValue(ViewModelViewHost.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                    //    contentFactory.SetValue(ViewModelViewHost.VerticalContentAlignmentProperty, VerticalAlignment.Stretch);
                    //    return new DataTemplate
                    //    {
                    //        VisualTree = contentFactory,
                    //    };
                    //}
                    //if (generictype == typeof(DateTime))
                    //{
                    //    myBinding.Path = new PropertyPath("Editor");
                    //    myBinding.Source = prop;
                    //    var contentFactory = new FrameworkElementFactory(typeof(ViewModelViewHost));
                    //    contentFactory.SetBinding(ViewModelViewHost.ViewModelProperty, myBinding);
                    //    contentFactory.SetValue(ViewModelViewHost.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                    //    contentFactory.SetValue(ViewModelViewHost.VerticalContentAlignmentProperty, VerticalAlignment.Stretch);
                    //    return new DataTemplate
                    //    {
                    //        VisualTree = contentFactory,
                    //    };
                    //}
                    //if (generictype == typeof(Boolean))
                    //{
                    //    myBinding.Path = new PropertyPath("Editor");
                    //    myBinding.Source = prop;
                    //    var contentFactory = new FrameworkElementFactory(typeof(ViewModelViewHost));
                    //    contentFactory.SetValue(ViewModelViewHost.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                    //    contentFactory.SetValue(ViewModelViewHost.VerticalContentAlignmentProperty, VerticalAlignment.Stretch);
                    //    contentFactory.SetBinding(ViewModelViewHost.ViewModelProperty, myBinding);
                    //    return new DataTemplate
                    //    {
                    //        VisualTree = contentFactory,
                    //    };
                    //}
                    //else
                    //{

                    //}
                }
                else
                {
                    if (type.GetGenericTypeDefinition() == typeof(ValueNodeOutputViewModel<>))
                    {
                        var generictype = type.GetGenericArguments()[0];
                        var prop = nodebinding.Node.GetType().GetProperty(nodebinding.PropName).GetValue(nodebinding.Node);
                        if (true)
                        {
                            myBinding.Path = new PropertyPath("CurrentValue");
                            myBinding.Source = prop;
                            return CreateLabelTemplate(myBinding);
                        }
                        if (generictype == typeof(string))
                        {
                            myBinding.Path = new PropertyPath("Editor");
                            myBinding.Source = prop;
                            var contentFactory = new FrameworkElementFactory(typeof(ViewModelViewHost));
                            contentFactory.SetBinding(ViewModelViewHost.ViewModelProperty, myBinding);
                            contentFactory.SetValue(ViewModelViewHost.HorizontalAlignmentProperty, HorizontalAlignment.Right);
                            return new DataTemplate
                            {
                                VisualTree = contentFactory,
                            };
                        }
                        if (generictype == typeof(DateTime))
                        {
                            myBinding.Path = new PropertyPath("Editor");
                            myBinding.Source = prop;
                            var contentFactory = new FrameworkElementFactory(typeof(ViewModelViewHost));
                            contentFactory.SetValue(ViewModelViewHost.HorizontalAlignmentProperty, HorizontalAlignment.Right);
                            contentFactory.SetBinding(ViewModelViewHost.ViewModelProperty, myBinding);
                            return new DataTemplate
                            {
                                VisualTree = contentFactory,
                            };
                        }
                        if (generictype == typeof(Boolean))
                        {
                            myBinding.Path = new PropertyPath("Editor");
                            myBinding.Source = prop;
                            var contentFactory = new FrameworkElementFactory(typeof(ViewModelViewHost));
                            contentFactory.SetValue(ViewModelViewHost.HorizontalAlignmentProperty, HorizontalAlignment.Right);
                            contentFactory.SetBinding(ViewModelViewHost.ViewModelProperty, myBinding);
                            return new DataTemplate
                            {
                                VisualTree = contentFactory,
                            };
                        }
                        else
                        {
                            myBinding.Path = new PropertyPath("Editor");
                            myBinding.Source = prop;
                            var contentFactory = new FrameworkElementFactory(typeof(ViewModelViewHost));
                            contentFactory.SetValue(ViewModelViewHost.HorizontalAlignmentProperty, HorizontalAlignment.Right);
                            contentFactory.SetBinding(ViewModelViewHost.ViewModelProperty, myBinding);
                            return new DataTemplate
                            {
                                VisualTree = contentFactory,
                            };
                        }

                    }
                }
               
            }

            if ((nodebinding.Node.GetType().GetProperty(nodebinding.PropName).GetCustomAttributes(typeof(HMIProperty), false)[0] as HMIProperty).IsCustomClass)
            {
                var description = (nodebinding.Node.GetType().GetProperty(nodebinding.PropName).GetCustomAttributes(typeof(HMIProperty), false)[0] as HMIProperty).Description;
                var value = nodebinding.Node.GetType().GetProperty(nodebinding.PropName).GetValue(nodebinding.Node);
                return CreatePropertyButtonTemplate(value, description);
            }
            if (type.IsEnum)
            {
                return CreateEnumTemplate(myBinding, Enum.GetValues(type));
            }
            if (type == typeof(bool))
            {

                return CreateCheckboxtemplate(myBinding);
            }
            return UnsupportedTemplate();
            throw new ApplicationException();
        }

        private static DataTemplate UnsupportedTemplate()
        {
            var defaultFactory = new FrameworkElementFactory(typeof(TextBlock));
            defaultFactory.SetValue(TextBlock.TextProperty, "Unsupported Data Type!");
            return new DataTemplate
            {

                VisualTree = defaultFactory
            };
        }
    }
}
