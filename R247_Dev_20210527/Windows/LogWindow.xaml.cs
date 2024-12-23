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
using System.Windows.Shapes;
using DevExpress.Data.Linq;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.ServerMode;
using Microsoft.EntityFrameworkCore;
using NOVisionDesigner.Data;

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : ThemedWindow
    {
        EntityInstantFeedbackSource _ItemsSource;
        public EntityInstantFeedbackSource ItemsSource
        {
            get
            {
                if (_ItemsSource == null)
                {
                    _ItemsSource = new EntityInstantFeedbackSource
                    {
                        KeyExpression = nameof(LogData.Id)
                    };
                    _ItemsSource.GetQueryable += (sender, e) => {
                        var context = new LogContext();
                        e.QueryableSource = context.Log.AsNoTracking();
                    };
                }
                return _ItemsSource;
            }
        }
        public LogWindow()
        {
            
            
            InitializeComponent();
            gridcontrol.ItemsSource = ItemsSource;
        }
    }
}
