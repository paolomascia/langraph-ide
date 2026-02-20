using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace LangraphIDE.Windows
{
    public partial class EditorDialog : Window
    {
        public string EditedText { get; private set; } = string.Empty;

        private static IHighlightingDefinition? _pythonHighlightingDark;
        private static IHighlightingDefinition? _pythonHighlightingLight;

        public EditorDialog(string title, string initialText, string editorMode = "text")
        {
            InitializeComponent();

            Title = title;
            TitleText.Text = title;

            CodeEditor.Text = initialText ?? string.Empty;
            EditedText = initialText ?? string.Empty;

            if (editorMode == "python")
            {
                ApplyPythonHighlighting();
                CodeEditor.WordWrap = false;
            }
            else
            {
                CodeEditor.WordWrap = true;
                CodeEditor.ShowLineNumbers = false;
            }

            ApplyEditorTheme();

            Loaded += (s, e) => CodeEditor.Focus();
        }

        private void ApplyPythonHighlighting()
        {
            bool isDark = LangraphIDE.ThemeManager.CurrentTheme == LangraphIDE.AppTheme.Dark;
            if (isDark)
            {
                _pythonHighlightingDark ??= CreatePythonHighlighting(true);
                CodeEditor.SyntaxHighlighting = _pythonHighlightingDark;
            }
            else
            {
                _pythonHighlightingLight ??= CreatePythonHighlighting(false);
                CodeEditor.SyntaxHighlighting = _pythonHighlightingLight;
            }
        }

        private void ApplyEditorTheme()
        {
            bool isDark = LangraphIDE.ThemeManager.CurrentTheme == LangraphIDE.AppTheme.Dark;
            CodeEditor.LineNumbersForeground = isDark
                ? new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E))
                : new SolidColorBrush(Color.FromRgb(0x65, 0x6D, 0x76));
        }

        private static IHighlightingDefinition? CreatePythonHighlighting(bool isDark)
        {
            string commentColor = isDark ? "#6A9955" : "#008000";
            string stringColor = isDark ? "#CE9178" : "#A31515";
            string keywordColor = isDark ? "#569CD6" : "#0000FF";
            string builtinColor = isDark ? "#DCDCAA" : "#795E26";
            string numberColor = isDark ? "#B5CEA8" : "#098658";
            string decoratorColor = isDark ? "#D7BA7D" : "#AF00DB";
            string classColor = isDark ? "#4EC9B0" : "#267F99";

            var xshd = $@"<?xml version=""1.0""?>
<SyntaxDefinition name=""Python"" extensions="".py"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
    <Color name=""Comment"" foreground=""{commentColor}"" />
    <Color name=""String"" foreground=""{stringColor}"" />
    <Color name=""Keyword"" foreground=""{keywordColor}"" fontWeight=""bold"" />
    <Color name=""BuiltinFunction"" foreground=""{builtinColor}"" />
    <Color name=""Number"" foreground=""{numberColor}"" />
    <Color name=""Decorator"" foreground=""{decoratorColor}"" />
    <Color name=""ClassName"" foreground=""{classColor}"" />

    <RuleSet>
        <Span color=""Comment"" begin=""#"" />
        <Span color=""String"" multiline=""true"">
            <Begin>'''</Begin>
            <End>'''</End>
        </Span>
        <Span color=""String"" multiline=""true"">
            <Begin>&quot;&quot;&quot;</Begin>
            <End>&quot;&quot;&quot;</End>
        </Span>
        <Span color=""String"">
            <Begin>'</Begin>
            <End>'</End>
            <RuleSet>
                <Span begin=""\\"" end=""."" />
            </RuleSet>
        </Span>
        <Span color=""String"">
            <Begin>&quot;</Begin>
            <End>&quot;</End>
            <RuleSet>
                <Span begin=""\\"" end=""."" />
            </RuleSet>
        </Span>
        <Span color=""Decorator"" begin=""@"" end=""$"" />

        <Keywords color=""Keyword"">
            <Word>and</Word>
            <Word>as</Word>
            <Word>assert</Word>
            <Word>async</Word>
            <Word>await</Word>
            <Word>break</Word>
            <Word>class</Word>
            <Word>continue</Word>
            <Word>def</Word>
            <Word>del</Word>
            <Word>elif</Word>
            <Word>else</Word>
            <Word>except</Word>
            <Word>finally</Word>
            <Word>for</Word>
            <Word>from</Word>
            <Word>global</Word>
            <Word>if</Word>
            <Word>import</Word>
            <Word>in</Word>
            <Word>is</Word>
            <Word>lambda</Word>
            <Word>nonlocal</Word>
            <Word>not</Word>
            <Word>or</Word>
            <Word>pass</Word>
            <Word>raise</Word>
            <Word>return</Word>
            <Word>try</Word>
            <Word>while</Word>
            <Word>with</Word>
            <Word>yield</Word>
            <Word>True</Word>
            <Word>False</Word>
            <Word>None</Word>
        </Keywords>

        <Keywords color=""BuiltinFunction"">
            <Word>print</Word>
            <Word>len</Word>
            <Word>range</Word>
            <Word>str</Word>
            <Word>int</Word>
            <Word>float</Word>
            <Word>list</Word>
            <Word>dict</Word>
            <Word>set</Word>
            <Word>tuple</Word>
            <Word>type</Word>
            <Word>isinstance</Word>
            <Word>hasattr</Word>
            <Word>getattr</Word>
            <Word>setattr</Word>
            <Word>open</Word>
            <Word>input</Word>
            <Word>enumerate</Word>
            <Word>zip</Word>
            <Word>map</Word>
            <Word>filter</Word>
            <Word>sorted</Word>
            <Word>reversed</Word>
            <Word>sum</Word>
            <Word>min</Word>
            <Word>max</Word>
            <Word>abs</Word>
            <Word>round</Word>
            <Word>any</Word>
            <Word>all</Word>
            <Word>super</Word>
            <Word>self</Word>
        </Keywords>

        <Rule color=""Number"">
            \b0[xX][0-9a-fA-F]+\b|\b0[oO][0-7]+\b|\b0[bB][01]+\b|\b[0-9]+\.?[0-9]*([eE][+-]?[0-9]+)?\b
        </Rule>
    </RuleSet>
</SyntaxDefinition>";

            try
            {
                using var reader = new XmlTextReader(new StringReader(xshd));
                return HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
            catch
            {
                return null;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            EditedText = CodeEditor.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
