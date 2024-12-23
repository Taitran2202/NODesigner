using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.GroupNodes
{
    public enum PortType
    {
        Execution, Integer, String
    }
    public class GroupPortViewModel : PortViewModel
    {
        static GroupPortViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new MyPortView(), typeof(IViewFor<GroupPortViewModel>));
        }

        #region PortType
        public PortType PortType
        {
            get => _portType;
            set => this.RaiseAndSetIfChanged(ref _portType, value);
        }
        private PortType _portType;
        #endregion
    }
}
