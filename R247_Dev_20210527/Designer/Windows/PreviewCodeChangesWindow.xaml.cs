using DevExpress.Xpf.Core;
using NOVisionDesigner.Designer.Nodes;
using RoslynPad.Editor;
using RoslynPad.Roslyn;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.IO;
using Microsoft.CodeAnalysis;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for PreviewCodeChangesWindow.xaml
    /// </summary>
    public partial class PreviewCodeChangesWindow : ThemedWindow
    {
        private RoslynHost _host;
        CSharpScriptingNode node;
        string current, ahead;
        public string Result { get; set; }
        public PreviewCodeChangesWindow(CSharpScriptingNode node, string current, string ahead)
        {
            this.node = node;
            this.current = current;
            this.ahead = ahead;
            InitializeComponent();
        }

        private void btn_discard_Click(object sender, RoutedEventArgs e)
        {
            Result = editor.Text;
            this.Close();
        }

        private void btn_accept_Click(object sender, RoutedEventArgs e)
        {
            Result = editor1.Text;
            this.Close();
        }

        private async void ThemedWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                var assemblyPath = Path.GetDirectoryName(typeof(Object).Assembly.Location);
                string sysRuntimePath = Path.Combine(assemblyPath, "System.Runtime.dll");
                var additionalReference = MetadataReference.CreateFromFile(sysRuntimePath);
                _host = new CustomRoslynHost(additionalAssemblies: new[]
                {
                    Assembly.Load("RoslynPad.Roslyn.Windows"),
                    Assembly.Load("RoslynPad.Editor.Windows"),
                }, RoslynHostReferences.NamespaceDefault.With(
                    references: new[] { additionalReference },
                    assemblyReferences: new[]
                {
                    typeof(object).Assembly,
                    typeof(System.Text.RegularExpressions.Regex).Assembly,
                     Assembly.GetAssembly(typeof(System.Net.Http.HttpClient)),
                    typeof(System.Linq.Enumerable).Assembly,
                    Assembly.GetAssembly(typeof(HalconDotNet.HalconAPI)),
                    Assembly.GetAssembly(typeof(NodeNetwork.Toolkit.NodeTemplate)),
                    Assembly.GetAssembly(typeof(Newtonsoft.Json.JsonConvert)),
                    Assembly.GetAssembly(typeof(NumSharp.NDArray)),

                }, typeNamespaceImports: new[] { typeof(MyContext) })
                .With(imports: new List<string>() { "HalconDotNet", "NodeNetwork.Toolkit.ValueNode", "System", "System.Linq" }));
            });

            var id = editor.Initialize(_host, new ClassificationHighlightColors(),
                MainWindow.AppPath, string.Empty);
            var id1 = editor1.Initialize(_host, new ClassificationHighlightColors(),
                MainWindow.AppPath, string.Empty);
            editor.Text = current;
            editor1.Text = ahead;
        }
    }
}
