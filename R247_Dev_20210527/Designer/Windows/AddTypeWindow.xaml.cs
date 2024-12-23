using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using NOVisionDesigner.Designer.Accquisition;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
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
    /// Interaction logic for AddTypeWindow.xaml
    /// </summary>
    public partial class AddTypeWindow : ThemedWindow
    {
        CSharpScriptingNode node;
        IOScriptingNodeEditor window;
        List<Type> ListSearchedType;
        ICollectionView current_list, all_list;
        ObservableCollection<TypeItem> _listAllType;
        public ObservableCollection<TypeItem> ListAllType
        {
            get
            {
                return _listAllType;
            }
            set
            {
                if (_listAllType != value)
                {
                    _listAllType = value;
                    RaisePropertyChanged("ListAllType");
                }
            }
        }
        //ObservableCollection<Type> _listTypeDisplay;
        //public ObservableCollection<Type> ListTypeDisplay
        //{
        //    get
        //    {
        //        return _listTypeDisplay;
        //    }
        //    set
        //    {
        //        if (_listTypeDisplay != value)
        //        {
        //            _listTypeDisplay = value;
        //            RaisePropertyChanged("ListTypeDisplay");
        //        }
        //    }
        //}
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public AddTypeWindow(IOScriptingNodeEditor window)
        {
            this.window = window;
            this.node = window.node;
            InitializeComponent();
            this.DataContext = this;
            
            ListAllType = new ObservableCollection<TypeItem>();
            string[] assemblies = { "NOVISION Designer.exe", "halcondotnet.dll", "System.dll", "mscorlib.dll","MySql.Data.dll", "System.Net.Http.dll" };
            //Assembly.Load("System.Net.Http.dll");
            ListSearchedType = new List<Type>();
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assemblies.Contains(a.ManifestModule.Name))
                {
                    foreach (Type t in DesignerHost.GetLoadableTypes(a))
                    {
                        ListSearchedType.Add(t);
                    }
                }
            }
            foreach (var nodetype in DesignerHost.nodeTypes)
            {
                Type t = Type.GetType("NOVisionDesigner.Designer.Nodes." + nodetype);
                var prop = t.GetProperties().Where(x => x.PropertyType.BaseType?.Name == "NodeOutputViewModel");
                foreach (var p in prop)
                {
                    var name = p?.PropertyType.GetGenericArguments()[0];
                    if (!ListSearchedType.Contains(name))
                    {
                        ListSearchedType.Add(name);
                    }
                }
            }
            ListAllType.Add(ConvertToNestedList(ListSearchedType));
            lst_current.ItemsSource = node.TypesCollection;
            tv_search.ItemsSource = ListAllType;
            current_list = CollectionViewSource.GetDefaultView(lst_current.ItemsSource);
            current_list.Filter = CurrentFilter;
            //tb_search_searched_TextChanged(null, null);
            //tb_search_current_TextChanged(null, null);
            txt_search_searched_EditValueChanged(null, null);
            //txt_search_current_EditValueChanged(null, null);
        }
        private bool CurrentFilter(object item)
        {
            if (txt_search_current.Text == "") 
                return true;
            return (item as Type).FullName.ToLower().Contains(txt_search_current.Text.ToLower());
        }
        public TypeItem ConvertToNestedList(List<Type> list)
        {
            TypeItem items = new TypeItem { Name="All common types"};
            try
            {
                foreach (Type t in list)
                {
                    var namespaces = t.Namespace?.Split('.');
                    AddChildren(items, t, namespaces, 0);
                }
            }
            catch { return null; }
            return items;
        }
        private void AddChildren(TypeItem item, Type t, string[] namespaces, int index)
        {
            if (namespaces == null)
            {
                //item.Children.Add(new TypeItem { Name = t.FullName, ItemType = t });
                return;
            }
            //if (index == 0)
            //{
            //    item.Name = namespaces[index];
            //    AddChildren(item, t, namespaces, index + 1);
            //}
            else if (index >= 0 && index < namespaces.Length)
            {
                if (item.Children.FirstOrDefault(x => x.Name == namespaces[index]) == null)
                {
                    item.Children.Add(new TypeItem { Name = namespaces[index] });
                }
                AddChildren(item.Children.FirstOrDefault(x=>x.Name==namespaces[index]), t, namespaces, index + 1);
            }
            else item.Children.Add(new TypeItem { Name = t.FullName, ItemType = t });
        }
        private void btn_add_Click_1(object sender, RoutedEventArgs e)
        {
            var selected = (sender as SimpleButton).DataContext as TypeItem;
            if (selected != null)
            {
                var type = selected.ItemType;
                if (type != null)
                {
                    if (!node.TypesCollection.Contains(type))
                    {
                        node.TypesCollection.Add(type);
                        //ListTypeDisplay.Add(type);
                        window.ListType.Insert(window.ListType.Count - 1, type.Name);
                        lst_current.SelectedIndex = node.TypesCollection.IndexOf(type);
                    }
                }
            }
        }

        private void btn_remove_Click_1(object sender, RoutedEventArgs e)
        {
            var selected = (sender as SimpleButton).DataContext as Type;
            if (selected != null)
            {
                var index = node.TypesCollection.IndexOf(selected as Type);
                window.ListType.Remove(selected.Name);
                node.TypesCollection.Remove(selected as Type);
                //ListTypeDisplay.Remove(lst_current.SelectedItem as Type);
                
                lst_current.SelectedIndex = index > 0 ? index - 1 : 0;
            }
        }

        //private void tb_search_current_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    ListTypeDisplay.Clear();
        //    var searches = node.ListType.Where(x => x.FullName.ToLower().Contains(tb_search_current.Text.ToLower()));
        //    foreach(var search in searches)
        //    {
        //        ListTypeDisplay.Add(search);
        //    }
        //}

        //private void tb_search_searched_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    ListSearchedTypeDisplay.Clear();
        //    var searches = ListSearchedType.Where(x => x.FullName.ToLower().Contains(tb_search_searched.Text.ToLower()));
        //    var nestedItems = ConvertToNestedList(searches.ToList());
        //    ListSearchedTypeDisplay.Add(nestedItems);
        //}


        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void tv_search_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            
        }

        private void txt_search_searched_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            ListAllType.Clear();
            var searches = ListSearchedType.Where(x => x.FullName.ToLower().Contains((sender as TextEdit) != null ? (sender as TextEdit).EditText.ToLower() : ""));
            var nestedItems = ConvertToNestedList(searches.ToList());
            ListAllType.Add(nestedItems);
        }

        private void txt_search_current_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            current_list.Refresh();
            //ListTypeDisplay.Clear();
            //var searches = node.ListType.Where(x => x.FullName.ToLower().Contains((sender as TextEdit) != null ? (sender as TextEdit).EditText.ToLower() : ""));
            //foreach (var search in searches)
            //{
            //    ListTypeDisplay.Add(search);
            //}
        }
    }
    //public interface IType
    //{
    //    string Name { get; }
    //    Type ItemType { get; }
    //    ObservableCollection<IType> Children { get; }
    //} 
    public class TypeItem
    {
        public string Name { get; set; }
        public Type ItemType { get; set; }
        public ObservableCollection<TypeItem> Children { get; set; } = new ObservableCollection<TypeItem>();
    }
}
