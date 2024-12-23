using DynamicData;
using HalconDotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.Windows;
using NuGet;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Scripting", "C#", Icon: "Designer/icons/icons8-cs-100.png",sortIndex:5)]
    public class CSharpScriptingNode : BaseNode
    {
        static CSharpScriptingNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<CSharpScriptingNode>));
        }
        public override void Dispose()
        {
            
            base.Dispose();
        }
        [HMIProperty("Enable log error")]
        public bool EnableLogError { get; set; } = false;
        [HMIProperty("Script editor")]
        public IReactiveCommand OpenScriptEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor", sender)); }
        }
        public override void OnCommand(string type,Control sender)
        {
            if (type == "editor")
            {
                CSharpScriptingWindow wd = new CSharpScriptingWindow(this);
                wd.Owner = Window.GetWindow(sender);
                wd.ShowDialog();
            }
        }
        bool BuildScript()
        {
            var context = new CodeContext();
            foreach (var item in Inputs.Items)
            {
                var type = item.GetType().GetGenericArguments()[0];
                context.Parameters.Add(item.Name, item);
            }
            var result = CSharpScript.Create(ScriptText, options: ScriptOptions.Default
                .WithImports("HalconDotNet", "NodeNetwork.Toolkit.ValueNode", "System")
                .AddReferences(Assembly.GetAssembly(typeof(HalconDotNet.HalconAPI)), Assembly.GetAssembly(typeof(System.AttributeTargets)))
                .AddReferences(Assembly.GetAssembly(typeof(NodeNetwork.Toolkit.NodeTemplate)),
                Assembly.GetAssembly(typeof(System.Net.Http.HttpClient)),
                Assembly.GetAssembly(typeof(Newtonsoft.Json.JsonConvert)),
                Assembly.GetAssembly(typeof(NOVisionDesigner.Designer.Nodes.USBOutput)),
                Assembly.GetAssembly(typeof(NumSharp.NDArray))
                ), typeof(CodeContext));
            var diagnostics = result.Compile();
            if (diagnostics.Any(t => t.Severity == DiagnosticSeverity.Error))
            {
                
                return false;
            }
            Script = result.CreateDelegate();
            return true;
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            Inputs.Clear();
            Outputs.Clear();
            #region old property, use for old jobs, DO NOT DELETE
            if (ListType.Count==0)
            {
                foreach (var nodetype in DesignerHost.nodeTypes)
                {
                    Type t = Type.GetType("NOVisionDesigner.Designer.Nodes." + nodetype);
                    var prop = t.GetProperties().Where(x => x.PropertyType.BaseType?.Name == "NodeOutputViewModel");
                    foreach (var p in prop)
                    {
                        var name = p?.PropertyType.GetGenericArguments()[0];
                        if (!ListType.Contains(name))
                        {
                            ListType.Add(name);
                        }
                    }
                }
            }
            #endregion
            if (TypesCollection.Count == 0)
            {
                foreach(var t in ListType)
                {
                    if (!TypesCollection.Contains(t))
                    {
                        TypesCollection.Add(t);
                    }
                }
            }
            foreach (var inputItem in inputItems)
            {
                Type itemtype = TypesCollection.FirstOrDefault(x => x.Name == inputItem.ItemType);
                Type[] paramType = new Type[] { itemtype };
                Type classType = typeof(ValueNodeInputViewModel<>);
                Type consType = classType.MakeGenericType(paramType);
                NodeInputViewModel input = (NodeInputViewModel)Activator.CreateInstance(consType, new object[] { null, null });
                input.Name = inputItem.Name;
                input.PortType = inputItem.ItemType;
                Inputs.Add(input);
            }
            foreach (var ouputItem in outputItems)
            {
                Type itemtype = TypesCollection.FirstOrDefault(x => x.Name == ouputItem.ItemType);
                Type[] paramType = new Type[] { itemtype };
                Type classType = typeof(ValueNodeOutputViewModel<>);
                Type consType = classType.MakeGenericType(paramType);
                NodeOutputViewModel output = (NodeOutputViewModel)Activator.CreateInstance(consType);
                output.Name = ouputItem.Name;
                output.PortType = ouputItem.ItemType;
                Outputs.Add(output);
            }
        }
        public override void OnLoadComplete()
        {
            BuildScript();
        }
        public ObservableCollection<IOItem> inputItems { get; set; } =  new ObservableCollection<IOItem>();
        public ObservableCollection<IOItem> outputItems { get; set; } = new ObservableCollection<IOItem>();
        public List<Type> ListType { get; set; } = new List<Type>();
        public ObservableCollection<Type> TypesCollection { get; set; } = new ObservableCollection<Type>();
        public string ScriptText { get; set; } = "";
        public ScriptRunner<object> Script { get; set; }
        public ValueNodeInputViewModel<HRegion> RegionInput { get; set; }
        public ValueNodeInputViewModel<bool> BoolInput { get; set; }
        public ValueNodeInputViewModel<HImage> ImageInput { get; set; }
        public ValueNodeOutputViewModel<bool> Result { get; set; }
        public ObservableQueue<ExceptionData> LastError { get; set; } = new ObservableQueue<ExceptionData>(10);
        public object bindingLock= new object();
        public override void Run(object context)
        {
            if (Script != null)
            {
                var codeContext = new CodeContext() { designerHost = designer,InspectionContext = context as InspectionContext};
                foreach (var item in this.Inputs.Items)
                {
                    codeContext.Parameters.Add(item.Name, item);
                }
                Dictionary<string, object> output=null;
                try
                {
                    output = Script.Invoke(globals: codeContext).Result as Dictionary<string, object>;
                }
                catch (Exception ex)
                {
                    this.IsError = true;
                    if (EnableLogError)
                    {
                        LastError.Enqueue(new ExceptionData() { Ex = ex, Time = DateTime.Now });
                    }
                    
                    
                }

                if (output != null)
                {
                    foreach(var item in this.Outputs.Items)
                    {
                        if (output.ContainsKey(item.Name))
                        {
                            MethodInfo method = item.GetType().GetMethod("OnNext");

                            method.Invoke(item,new object[] { output[item.Name] });
                        }
                    }
                }
            }
        }

        public CSharpScriptingNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "C#";
            foreach (var nodetype in DesignerHost.nodeTypes)
            {
                Type t = Type.GetType("NOVisionDesigner.Designer.Nodes." + nodetype);
                var prop = t.GetProperties().Where(x => x.PropertyType.BaseType?.Name == "NodeOutputViewModel");
                foreach (var p in prop)
                {
                    var name = p?.PropertyType.GetGenericArguments()[0];
                    if (!TypesCollection.Contains(name))
                    {
                        TypesCollection.Add(name);
                    }

                }
            }
            
            this.Inputs.Connect().ActOnEveryObject((x) => {
                
                if(x is ValueNodeInputViewModel<bool>)
                {
                    (x as ValueNodeInputViewModel<bool>).ValueChanged.Subscribe(z =>
                    {
                        if (NodeType != NodeType.Event)
                        {
                            return;
                        }
                        Task.Run(new Action(() =>
                        {
                            this.Run(new InspectionContext(null, null, 1.0, 1, this.ID, 0));
                        }

                        ));
                    });
                }
                
            }, (y) => { 

            });
        }
    }
    public class ExceptionData
    {
        public DateTime Time { get; set; }
        public Exception Ex { get; set; }
    }
    public class ObservableQueue<T> : INotifyCollectionChanged, IEnumerable<T>
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private readonly Queue<T> queue = new Queue<T>();
        public int Max { get; set; }
        public int Count { 
            get
            {
                return queue.Count;
            }
        }
        public ObservableQueue(int max)
        {
            this.Max = max;
            queue = new Queue<T>(Max);

        }
        public T Enqueue(T item)
        {
            T dequeueItem = default(T);
            if (queue.Count > Max)
            {
                dequeueItem=Dequeue();
            }
            queue.Enqueue(item);
            if (CollectionChanged != null)
                CollectionChanged(this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add, item));
            return dequeueItem;
        }

        public T Dequeue()
        {
            if (queue.IsEmpty())
            {
                return default(T);
            }
            var item = queue.Dequeue();
            if (CollectionChanged != null)
                CollectionChanged(this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove, item,0));
            return item;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    public class IOItem
    {
        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }
        string _itemType;
        public string ItemType
        {
            get
            {
                return _itemType;
            }
            set
            {
                if (_itemType != value)
                {
                    _itemType = value;
                    RaisePropertyChanged("ItemType");
                }
            }
        }
    }
}
