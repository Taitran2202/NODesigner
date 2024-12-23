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

namespace NOVisionDesigner.Designer.Accquisition.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class AcqScriptWindow : Window,INotifyPropertyChanged
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private RoslynHost _host;
        GigEVision2Script node;
        private static PrintOptions PrintOptions { get; } =
                new PrintOptions { MemberDisplayFormat = MemberDisplayFormat.SeparateLines };
        private static string FormatObject(object o)
        {
            return CSharpObjectFormatter.Instance.FormatObject(o, PrintOptions);
        }
        ScriptHost ScriptHost;
        public AcqScriptWindow(GigEVision2Script node,ScriptHost scriptHost)
        {
            InitializeComponent();
            this.node = node;
            this.ScriptHost = scriptHost;
            this.DataContext = this;
            
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
            
            
            var result =  CSharpScript.Create(editor1.Text, options: ScriptOptions.Default
                .WithReferences(_host.DefaultReferences)
                .WithImports(_host.DefaultImports)               
                .WithImports("HalconDotNet", "NodeNetwork.Toolkit.ValueNode","System")
                .AddReferences(Assembly.GetAssembly(typeof(HalconDotNet.HalconAPI)), Assembly.GetAssembly(typeof(System.AttributeTargets)))
                .AddReferences(Assembly.GetAssembly(typeof(NodeNetwork.Toolkit.NodeTemplate))),typeof(AcquisitionCodeContext));
            var diagnostics = result.Compile();
            if (diagnostics.Any(t => t.Severity == DiagnosticSeverity.Error))
            {
                CompilationError = string.Join(Environment.NewLine, diagnostics.Select(FormatObject));
                return true;
            }
            CompilationError = "Compilation complete at " +DateTime.Now.ToString();
            var runMethod = result.CreateDelegate();
            ScriptHost.Script = runMethod;
            ScriptHost.ScriptText = editor1.Text;
            //var output = await runMethod.Invoke(globals: context);

            //Console.WriteLine(output);
            return true;
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
                _host = new CustomRoslynHost(additionalAssemblies: new[]
            {
                Assembly.Load("RoslynPad.Roslyn.Windows"),
                Assembly.Load("RoslynPad.Editor.Windows"),
            }, RoslynHostReferences.NamespaceDefault.With(assemblyReferences: new[]
            {
                typeof(object).Assembly,
                typeof(System.Text.RegularExpressions.Regex).Assembly,
                 Assembly.GetAssembly(typeof(System.Net.Http.HttpClient)),
                typeof(System.Linq.Enumerable).Assembly,
                Assembly.GetAssembly(typeof(HalconDotNet.HalconAPI)),
                Assembly.GetAssembly(typeof(NodeNetwork.Toolkit.NodeTemplate)),
                Assembly.GetAssembly(typeof(Newtonsoft.Json.JsonConvert)),
                Assembly.GetAssembly(typeof(NumSharp.NDArray))


            }, typeNamespaceImports: new[] { typeof(MyContext) })
            .With(imports: new List<string>() { "HalconDotNet", "NodeNetwork.Toolkit.ValueNode", "System" }));
            });

            var id=editor1.Initialize(_host, new ClassificationHighlightColors(),
                MainWindow.AppPath, string.Empty);
            if(ScriptHost.ScriptText==null | ScriptHost.ScriptText == string.Empty)
            {
                editor1.Text = "";
            }
            else
            {
                editor1.Text = ScriptHost.ScriptText;
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
            
        }

        private void btn_cancel_message_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void btn_ok_message_Click(object sender, RoutedEventArgs e)
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
                hostObjectType: typeof(AcquisitionCodeContext),
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
    
}
