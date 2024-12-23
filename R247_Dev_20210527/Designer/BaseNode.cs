using HalconDotNet;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.Extensions;
using NOVisionDesigner.Designer.Misc;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using static NOVisionDesigner.Designer.Misc.InspectionContext;

namespace NOVisionDesigner.Designer
{
    public enum NodeType
    {
        Function,Event,FlowControl, Literal, Group
    }
    public class BaseNode: NodeExpression, IDisposable,IHalconDeserializable
    {
        public virtual List<string> GetExtendTag()
        {
            return new List<string>();
        }
        List<ProcessStatistic> Profiler;

        public override void OnRemove()
        {
            if (System.IO.Directory.Exists(Dir))
            {
                try
                {
                    System.IO.Directory.Delete(Dir, true);
                }catch { 
                }
                
            }
        }
        public virtual void Dispose()
        {

        }
        public Calibration calib { get; set; }
        bool _is_enabled=true;
        public bool IsEnabled
        {
            get
            {
                return _is_enabled;
            }
            set
            {
                if (_is_enabled != value)
                {
                    _is_enabled = value;
                }
            }
        }
        public virtual void Save(HFile file) { }
        public virtual void Load(DeserializeFactory item) { }
        public DesignerHost designer;  
        [Serializeable]
        public bool ShowDisplay 
        {
            get
            {
                return _show_display;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _show_display, value);
            }
        }
        bool _show_display=true;
        [SerializeIgnore]
        public string Dir { get; set; }
        private NodeType _node_type;
        [Serializeable]
        public NodeType NodeType {
            get
            {
                return _node_type;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _node_type, value);
            }
        }
        public void RunOnce(object context)
        {
            //reset relative
            //foreach (var item in Outputs.Items)
            //{
            //    item.IsUpdated = false;
            //    foreach (var item2 in item.Connections.Items)
            //    {
            //        item2.Input.Parent.Reset();
            //    }
            //}
            this.IsUpdated = true;
            try
            {
                //LastContext = context;
                this.Run(LastContext);
            }
            catch (Exception ex)
            {
                IsError = true;
                return;
            }
            if (IsError)
            {
                IsError = false;
            }

            UpdateConnections(LastContext);
            //(LastContext as InspectionContext)
        }
        public virtual void OnLoadComplete()
        {

        }
        public BaseNode(DesignerHost designer, string dir,string id)
        {
            this.designer = designer;
            this.calib = designer.Calibration;
            this.ID = id;
            if (dir == null)
                return;
            
            //create path to save data
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            this.Dir = System.IO.Path.Combine(dir, id);
            System.IO.Directory.CreateDirectory(Dir);
        }

        public BaseNode(NodeType NodeType)
        {
            this.NodeType = NodeType;
           
        }
        public ICommand Command
        {
            get
            {
                return new CommandHandler((object type) => OnCommand(type.ToString()), () =>true);
            }
        }
        public virtual void OnCommand(string type, Control sender=null)
        {

        }

    }
   
    public class CommandHandler : ICommand
    {
        private Action<object> _action;
        private Func<bool> _canExecute;

        /// <summary>
        /// Creates instance of the command handler
        /// </summary>
        /// <param name="action">Action to be executed by the command</param>
        /// <param name="canExecute">A bolean property to containing current permissions to execute the command</param>
        public CommandHandler(Action<object> action, Func<bool> canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        /// <summary>
        /// Wires CanExecuteChanged event 
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Forcess checking if execute is allowed
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute.Invoke();
        }

        public void Execute(object parameter)
        {
            _action(parameter);
        }
        
    }


}
