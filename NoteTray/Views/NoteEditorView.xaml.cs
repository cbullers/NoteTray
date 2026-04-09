using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using NoteTray.ViewModels;

namespace NoteTray.Views;

public partial class NoteEditorView : UserControl
{
    private bool _isPreviewMode;

    public bool IsPreviewMode => _isPreviewMode;

    private static readonly SolidColorBrush TextBrush = new(Color.FromRgb(0xE2, 0xE8, 0xF0));
    private static readonly SolidColorBrush HeadingBrush = new(Color.FromRgb(0xFF, 0xFF, 0xFF));
    private static readonly SolidColorBrush CodeBgBrush = new(Color.FromRgb(0x24, 0x30, 0x44));
    private static readonly new SolidColorBrush BorderBrush = new(Color.FromRgb(0x2E, 0x3D, 0x55));
    private static readonly SolidColorBrush LinkBrush = new(Color.FromRgb(0x5B, 0x9C, 0xF6));
    private static readonly SolidColorBrush BlockquoteBrush = new(Color.FromRgb(0x5B, 0x9C, 0xF6));
    private static readonly FontFamily DefaultFont = new("Segoe UI");
    private static readonly FontFamily CodeFont = new("Cascadia Code, Consolas");

    public NoteEditorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_isPreviewMode)
        {
            _isPreviewMode = false;
            ShowEditMode();
        }

        if (e.NewValue != null)
        {
            EditorTextBox.Focus();
            EditorTextBox.CaretIndex = EditorTextBox.Text.Length;
        }
    }

    public void TogglePreview()
    {
        _isPreviewMode = !_isPreviewMode;

        if (_isPreviewMode)
            ShowPreviewMode();
        else
            ShowEditMode();
    }

    private void ShowPreviewMode()
    {
        var content = (DataContext as NoteViewModel)?.Content ?? string.Empty;
        var doc = ConvertMarkdownToFlowDocument(content);

        EditorTextBox.Visibility = Visibility.Collapsed;
        PreviewViewer.Document = doc;
        PreviewViewer.Visibility = Visibility.Visible;
    }

    private void ShowEditMode()
    {
        PreviewViewer.Visibility = Visibility.Collapsed;
        PreviewViewer.Document = null;
        EditorTextBox.Visibility = Visibility.Visible;
        EditorTextBox.Focus();
    }

    private FlowDocument ConvertMarkdownToFlowDocument(string markdown)
    {
        var doc = new FlowDocument
        {
            FontFamily = DefaultFont,
            FontSize = 14,
            Foreground = TextBrush,
            PagePadding = new Thickness(12),
            LineHeight = 1.6 * 14
        };

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];

            // Blank line
            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            // Fenced code block
            if (line.TrimStart().StartsWith("```"))
            {
                i++;
                var codeLines = new List<string>();
                while (i < lines.Length && !lines[i].TrimStart().StartsWith("```"))
                {
                    codeLines.Add(lines[i]);
                    i++;
                }
                if (i < lines.Length) i++; // skip closing ```

                var codePara = new Paragraph
                {
                    FontFamily = CodeFont,
                    FontSize = 13,
                    Background = CodeBgBrush,
                    Padding = new Thickness(12),
                    BorderBrush = BorderBrush,
                    BorderThickness = new Thickness(1),
                    Foreground = TextBrush,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                codePara.Inlines.Add(new Run(string.Join(Environment.NewLine, codeLines)));
                doc.Blocks.Add(codePara);
                continue;
            }

            // Heading
            var headingMatch = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
            if (headingMatch.Success)
            {
                var level = headingMatch.Groups[1].Value.Length;
                var text = headingMatch.Groups[2].Value;
                var fontSize = level switch
                {
                    1 => 22.4,  // 1.6em
                    2 => 19.6,  // 1.4em
                    3 => 16.8,  // 1.2em
                    4 => 15.4,
                    5 => 14.0,
                    _ => 13.0
                };

                var para = new Paragraph
                {
                    FontSize = fontSize,
                    FontWeight = FontWeights.Bold,
                    Foreground = HeadingBrush,
                    Margin = new Thickness(0, 14, 0, 7)
                };

                if (level <= 2)
                {
                    para.BorderBrush = BorderBrush;
                    para.BorderThickness = new Thickness(0, 0, 0, 1);
                    para.Padding = new Thickness(0, 0, 0, 4);
                }

                AddInlineContent(para.Inlines, text);
                doc.Blocks.Add(para);
                i++;
                continue;
            }

            // Horizontal rule
            if (Regex.IsMatch(line, @"^(\*{3,}|-{3,}|_{3,})\s*$"))
            {
                var rule = new Paragraph
                {
                    BorderBrush = BorderBrush,
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    Margin = new Thickness(0, 14, 0, 14),
                    FontSize = 1
                };
                doc.Blocks.Add(rule);
                i++;
                continue;
            }

            // Blockquote
            if (line.TrimStart().StartsWith("> ") || line.TrimStart() == ">")
            {
                var quoteLines = new List<string>();
                while (i < lines.Length && (lines[i].TrimStart().StartsWith("> ") || lines[i].TrimStart() == ">"))
                {
                    quoteLines.Add(Regex.Replace(lines[i], @"^>\s?", ""));
                    i++;
                }

                var para = new Paragraph
                {
                    BorderBrush = BlockquoteBrush,
                    BorderThickness = new Thickness(3, 0, 0, 0),
                    Padding = new Thickness(12, 8, 8, 8),
                    Background = CodeBgBrush,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                AddInlineContent(para.Inlines, string.Join(Environment.NewLine, quoteLines));
                doc.Blocks.Add(para);
                continue;
            }

            // Unordered list
            if (Regex.IsMatch(line, @"^\s*[-*+]\s+"))
            {
                var list = new List { MarkerStyle = TextMarkerStyle.Disc, Foreground = TextBrush };
                while (i < lines.Length && Regex.IsMatch(lines[i], @"^\s*[-*+]\s+"))
                {
                    var itemText = Regex.Replace(lines[i], @"^\s*[-*+]\s+", "");
                    var listItem = new ListItem();
                    var itemPara = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
                    AddInlineContent(itemPara.Inlines, itemText);
                    listItem.Blocks.Add(itemPara);
                    list.ListItems.Add(listItem);
                    i++;
                }
                doc.Blocks.Add(list);
                continue;
            }

            // Ordered list
            if (Regex.IsMatch(line, @"^\s*\d+\.\s+"))
            {
                var list = new List { MarkerStyle = TextMarkerStyle.Decimal, Foreground = TextBrush };
                while (i < lines.Length && Regex.IsMatch(lines[i], @"^\s*\d+\.\s+"))
                {
                    var itemText = Regex.Replace(lines[i], @"^\s*\d+\.\s+", "");
                    var listItem = new ListItem();
                    var itemPara = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
                    AddInlineContent(itemPara.Inlines, itemText);
                    listItem.Blocks.Add(itemPara);
                    list.ListItems.Add(listItem);
                    i++;
                }
                doc.Blocks.Add(list);
                continue;
            }

            // Regular paragraph
            {
                var paraLines = new List<string>();
                while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i])
                       && !lines[i].TrimStart().StartsWith("#")
                       && !lines[i].TrimStart().StartsWith("```")
                       && !lines[i].TrimStart().StartsWith("> ")
                       && !Regex.IsMatch(lines[i], @"^\s*[-*+]\s+")
                       && !Regex.IsMatch(lines[i], @"^\s*\d+\.\s+")
                       && !Regex.IsMatch(lines[i], @"^(\*{3,}|-{3,}|_{3,})\s*$"))
                {
                    paraLines.Add(lines[i]);
                    i++;
                }

                var para = new Paragraph { Margin = new Thickness(0, 4, 0, 4) };
                AddInlineContent(para.Inlines, string.Join(" ", paraLines));
                doc.Blocks.Add(para);
            }
        }

        return doc;
    }

    private void AddInlineContent(InlineCollection inlines, string text)
    {
        // Process inline markdown: bold, italic, code, links, strikethrough
        var pattern = @"(`[^`]+`)" +                          // inline code
                      @"|(\*\*\*.+?\*\*\*|___[^_]+___)" +    // bold+italic
                      @"|(\*\*.+?\*\*|__[^_]+__)" +          // bold
                      @"|(\*[^*]+\*|_[^_]+_)" +              // italic
                      @"|(~~.+?~~)" +                         // strikethrough
                      @"|(\[([^\]]+)\]\(([^)]+)\))";          // link

        var parts = Regex.Split(text, pattern);

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
                continue;

            // Inline code
            if (part.StartsWith('`') && part.EndsWith('`') && part.Length > 1)
            {
                var code = part[1..^1];
                inlines.Add(new Run(code)
                {
                    FontFamily = CodeFont,
                    Background = CodeBgBrush,
                    FontSize = 13
                });
                continue;
            }

            // Bold + Italic
            if ((part.StartsWith("***") && part.EndsWith("***")) ||
                (part.StartsWith("___") && part.EndsWith("___")))
            {
                var inner = part[3..^3];
                inlines.Add(new Run(inner)
                {
                    FontWeight = FontWeights.Bold,
                    FontStyle = FontStyles.Italic
                });
                continue;
            }

            // Bold
            if ((part.StartsWith("**") && part.EndsWith("**")) ||
                (part.StartsWith("__") && part.EndsWith("__")))
            {
                var inner = part[2..^2];
                inlines.Add(new Run(inner) { FontWeight = FontWeights.Bold });
                continue;
            }

            // Italic
            if ((part.StartsWith('*') && part.EndsWith('*') && part.Length > 1) ||
                (part.StartsWith('_') && part.EndsWith('_') && part.Length > 1))
            {
                var inner = part[1..^1];
                inlines.Add(new Run(inner) { FontStyle = FontStyles.Italic });
                continue;
            }

            // Strikethrough
            if (part.StartsWith("~~") && part.EndsWith("~~"))
            {
                var inner = part[2..^2];
                inlines.Add(new Run(inner)
                {
                    TextDecorations = TextDecorations.Strikethrough
                });
                continue;
            }

            // Link
            var linkMatch = Regex.Match(part, @"^\[([^\]]+)\]\(([^)]+)\)$");
            if (linkMatch.Success)
            {
                var linkText = linkMatch.Groups[1].Value;
                var hyperlink = new Run(linkText) { Foreground = LinkBrush };
                inlines.Add(hyperlink);
                continue;
            }

            // Plain text
            inlines.Add(new Run(part));
        }
    }
}
