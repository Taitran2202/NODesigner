using DevExpress.Xpf.Core;
using HalconDotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using RoslynPad.Editor;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.CodeActions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CSharpScriptingWindow : ThemedWindow, INotifyPropertyChanged
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private RoslynHost _host;
        CSharpScriptingNode node;
        private static PrintOptions PrintOptions { get; } =
                new PrintOptions { MemberDisplayFormat = MemberDisplayFormat.SeparateLines };
        private static string FormatObject(object o)
        {
            return CSharpObjectFormatter.Instance.FormatObject(o, PrintOptions);
        }
        private object _syncLock = new Object();
        public CSharpScriptingWindow(CSharpScriptingNode node)
        {
            InitializeComponent();
            this.node = node;
            this.DataContext = this;
            var Collection = CollectionViewSource.GetDefaultView(node.LastError);
            BindingOperations.EnableCollectionSynchronization(node.LastError, _syncLock);
            grid_error.ItemsSource = Collection;
            //BindingOperations.EnableCollectionSynchronization(node.LastError, node.bindingLock);

        }
        string _CompilationError;
        public string CompilationError
        {
            get
            {
                return _CompilationError;
            }
            set
            {
                if (_CompilationError != value)
                {
                    _CompilationError = value;
                    RaisePropertyChanged("CompilationError");
                }
            }
        }

        public string Result { get; set; }
        public Script<object> Script { get; private set; }
        public async Task<bool> TrySubmit()
        {
            var context = new CodeContext() { designerHost = node.designer , InspectionContext = node.LastContext as InspectionContext};
            foreach (var item in node.Inputs.Items)
            {
                var type = item.GetType().GetGenericArguments()[0];
                context.Parameters.Add(item.Name, item);
            }
            var result =  CSharpScript.Create(editor1.Text, options: ScriptOptions.Default
                .WithReferences(_host.DefaultReferences)
                .WithImports(_host.DefaultImports)               
                .WithImports("HalconDotNet", "NodeNetwork.Toolkit.ValueNode","System")
                .AddReferences(Assembly.GetAssembly(typeof(HalconDotNet.HalconAPI)), Assembly.GetAssembly(typeof(System.AttributeTargets)))
                .AddReferences(Assembly.GetAssembly(typeof(NodeNetwork.Toolkit.NodeTemplate))),typeof(CodeContext));
            var diagnostics = result.Compile();
            if (diagnostics.Any(t => t.Severity == DiagnosticSeverity.Error))
            {
                CompilationError = string.Join(Environment.NewLine, diagnostics.Select(FormatObject));
                return true;
            }
            CompilationError = "Compilation complete at " +DateTime.Now.ToString();
            var runMethod = result.CreateDelegate();
            node.Script = runMethod;
            node.ScriptText = editor1.Text;
            //var output = await runMethod.Invoke(globals: context);

            //Console.WriteLine(output);
            return true;
        }
        public string GetInputOutput(BaseNode node)
        {
            List<string> result = new List<string>();
            result.Add("using HalconDotNet;");
            result.Add("using NodeNetwork.Toolkit.ValueNode;");
            result.Add("using System;");
            result.Add("using System.Linq;");
            result.Add("//input variables");
            foreach (var item in node.Inputs.Items)
            {
                var type = item.GetType().GetGenericArguments()[0];
                var name = item.Name;
                result.Add(type.Name + " " + name + "=(Parameters[\""+name + "\"] \n\t\t\t\tas ValueNodeInputViewModel<"+ type.Name + ">).Value;");
                var use_namespace = $"using {type.Namespace};";
                if (!result.Any(x => x == use_namespace))
                    result.Insert(4, use_namespace);
            }
            result.Add("//define output variables");
            foreach (var item in node.Outputs.Items)
            {
                var type = item.GetType().GetGenericArguments()[0];
                var name = item.Name;
                result.Add(type.ToString() + " " + name + ";");
            }
            result.Add("//define processing function here\n\n");
            result.Add("//set result to output");
            foreach (var item in node.Outputs.Items)
            {
                var type = item.GetType().GetGenericArguments()[0];
                var name = item.Name;
                result.Add("Parameters.Add(\"" + name + "\","+name+");");
            }
            result.Add("//return Parameters");
            result.Add("return Parameters;");
            var inputString =String.Join(Environment.NewLine, result);
            return inputString;
        }

        class DocumentViewModel : INotifyPropertyChanged
        {
            private bool _isReadOnly;
            private readonly RoslynHost _host;
            private string _result;

            public DocumentViewModel(RoslynHost host)
            {
                _host = host;
            }

            internal void Initialize(DocumentId id)
            {
                Id = id;
            }


            public DocumentId Id { get; private set; }

            public bool IsReadOnly
            {
                get { return _isReadOnly; }
                private set { SetProperty(ref _isReadOnly, value); }
            }

            
            public Script<object> Script { get; private set; }

            public string Text { get; set; }

            public bool HasError { get; private set; }

            public string Result
            {
                get { return _result; }
                private set { SetProperty(ref _result, value); }
            }

            private static MethodInfo HasSubmissionResult { get; } =
                typeof(Compilation).GetMethod(nameof(HasSubmissionResult), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            private static PrintOptions PrintOptions { get; } =
                new PrintOptions { MemberDisplayFormat = MemberDisplayFormat.SeparateLines };

            public async Task<bool> TrySubmit()
            {
                Result = null;

                Script = 
                    CSharpScript.Create(Text, ScriptOptions.Default
                        .WithReferences(_host.DefaultReferences)
                        .WithImports(_host.DefaultImports));

                var compilation = Script.GetCompilation();
                var hasResult = (bool)HasSubmissionResult.Invoke(compilation, null);
                var diagnostics = Script.Compile();
                if (diagnostics.Any(t => t.Severity == DiagnosticSeverity.Error))
                {
                    Result = string.Join(Environment.NewLine, diagnostics.Select(FormatObject));
                    return false;
                }

                IsReadOnly = true;

                await Execute(hasResult);

                return true;
            }

            private async Task Execute(bool hasResult)
            {
                try
                {
                    var result = await Script.RunAsync();

                    if (result.Exception != null)
                    {
                        HasError = true;
                        Result = FormatException(result.Exception);
                    }
                    else
                    {
                        Result = hasResult ? FormatObject(result.ReturnValue) : null;
                    }
                }
                catch (Exception ex)
                {
                    HasError = true;
                    Result = FormatException(ex);
                }
            }

            private static string FormatException(Exception ex)
            {
                return CSharpObjectFormatter.Instance.FormatException(ex);
            }

            private static string FormatObject(object o)
            {
                return CSharpObjectFormatter.Instance.FormatObject(o, PrintOptions);
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
            {
                if (!EqualityComparer<T>.Default.Equals(field, value))
                {
                    field = value;
                    OnPropertyChanged(propertyName);
                    return true;
                }
                return false;
            }

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void OnItemLoaded(object sender, RoutedEventArgs e)
        {

        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
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
                .With(imports: new List<string>() { "HalconDotNet", "NodeNetwork.Toolkit.ValueNode", "System","System.Linq" }));
            });

            var id=editor1.Initialize(_host, new ClassificationHighlightColors(),
                MainWindow.AppPath, string.Empty);
            if(node.ScriptText==null | node.ScriptText == string.Empty)
            {
                editor1.Text =
                GetInputOutput(this.node);
            }
            else
            {
                editor1.Text = node.ScriptText;
            }
            
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await TrySubmit();
            }catch(Exception ex)
            {
                CompilationError = ex.Message;
            }
           
        }

        private void btn_edit_io_Click(object sender, RoutedEventArgs e)
        {
            IOScriptingNodeEditor wd = new IOScriptingNodeEditor(node);
            wd.ShowDialog();
            if (wd.isChanged)
            {
                bd_message.Visibility = Visibility.Visible;
            }
        }

        private void btn_cancel_message_Click(object sender, RoutedEventArgs e)
        {
            bd_message.Visibility = Visibility.Hidden;
            //insert inpput and output changes
            

        }
        string ReplaceChanges(BaseNode node)
        {
            var code = editor1.Text;
            var OriginalCode = code.Replace("\r","").Split('\n').ToList();
            var resultCode = new List<string>();
            var stop1 = OriginalCode.IndexOf("//define output variables");
            var stop2 = OriginalCode.IndexOf("//define processing function here");
            var stop3 = OriginalCode.IndexOf("//set result to output");
            var stop4 = OriginalCode.IndexOf("//return Parameters");
            
            var start = OriginalCode.IndexOf("//input variables");
            resultCode.AddRange(OriginalCode.GetRange(0, start));
            resultCode.Add("//input variables");
            foreach (var item in node.Inputs.Items)
            {
                var type = item.GetType().GetGenericArguments()[0];
                var name = item.Name;
                resultCode.Add(type.Name + " " + name + "=(Parameters[\"" + name + "\"] \n\t\t\t\tas ValueNodeInputViewModel<" + type.Name + ">).Value;");
                var use_namespace = $"using {type.Namespace};";
                if (!resultCode.Any(x => x == use_namespace))
                    resultCode.Insert(start, use_namespace);
            }
            resultCode.Add("//define output variables");
            foreach (var item in node.Outputs.Items)
            {
                var type = item.GetType().GetGenericArguments()[0];
                var name = item.Name;
                resultCode.Add(type.Name + " " + name + ";");
            }
            resultCode.AddRange(OriginalCode.GetRange(stop2, stop3-stop2));

            resultCode.Add("//set result to output");
            foreach (var item in node.Outputs.Items)
            {
                var type = item.GetType().GetGenericArguments()[0];
                var name = item.Name;
                resultCode.Add("Parameters.Add(\"" + name + "\"," + name + ");");
            }
            resultCode.Add("//return Parameters");
            resultCode.Add("return Parameters;");
            //find input insert block
            var inputString = String.Join(Environment.NewLine, resultCode);
            return inputString;
        }

        private void btn_ok_message_Click(object sender, RoutedEventArgs e)
        {
            var text = GetInputOutput(node);
            node.ScriptText = text;
            editor1.Text = text;
            bd_message.Visibility = Visibility.Hidden;
        }

        private void btn_replace_changes_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                var text = ReplaceChanges(node);
                node.ScriptText = text;
                editor1.Text = text;
            }
            catch(Exception ex)
            {
                DXMessageBox.Show(this,"Cannot replace changes with error: "+ex.Message,"Error",
                    MessageBoxButton.OK,MessageBoxImage.Error);
                return;
            }
            bd_message.Visibility = Visibility.Hidden;


        }

        private void btn_preview_changes_Click(object sender, RoutedEventArgs e)
        {
            var current = editor1.Text;
            var ahead = ReplaceChanges(node);
            bd_message.Visibility = Visibility.Hidden;
            var wd = new PreviewCodeChangesWindow(node, current, ahead);
            wd.ShowDialog();
            editor1.Text = wd.Result;
            node.ScriptText = wd.Result;
        }

        private void btn_undo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_redo_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    internal sealed class CodeActionsConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((CodeAction)value).GetCodeActions();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
    public class CustomRoslynHost : RoslynHost
    {
        public CustomRoslynHost(IEnumerable<Assembly> additionalAssemblies = null, RoslynHostReferences references = null, ImmutableArray<string>? disabledDiagnostics = null): base(additionalAssemblies, references, disabledDiagnostics)
        {
            
        }
        protected override Project CreateProject(Solution solution, DocumentCreationArgs args, CompilationOptions compilationOptions, Project previousProject = null)
        {
            var name = args.Name ?? "Program";
            var id = ProjectId.CreateNewId(name);

            var parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Script, languageVersion: LanguageVersion.Latest);

            compilationOptions = compilationOptions.WithScriptClassName(name);

            solution = solution.AddProject(ProjectInfo.Create(
                id,
                VersionStamp.Create(),
                name,
                name,
                LanguageNames.CSharp,
                isSubmission: true,
                parseOptions: parseOptions,
                hostObjectType: typeof(CodeContext),
                compilationOptions: compilationOptions,
                metadataReferences: previousProject != null ? ImmutableArray<MetadataReference>.Empty : DefaultReferences,
                projectReferences: previousProject != null ? new[] { new ProjectReference(previousProject.Id) } : null));

            var project = solution.GetProject(id);

            return project;
        }
    }
    public class MyContext
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
    public class CodeContext
    {
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public DesignerHost designerHost { get; set; }
        public InspectionContext InspectionContext { get; set; }

    }
}
