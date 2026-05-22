using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using ConfigurableReader.Core;
using ConfigurableReader.Parsers.Txt;
using ConfigurableReader.Parsers.Epub;
using ConfigurableReader.Parsers.Pdf;
using ConfigurableReader.Parsers.Docx;
using ConfigurableReader.Parsers.Markdown;
using ConfigurableReader.Views;

namespace ConfigurableReader;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var registry = new DocumentRegistry();
            registry.RegisterParser(new TxtBookParser());
            registry.RegisterParser(new EpubBookParser());
            registry.RegisterParser(new PdfBookParser());
            registry.RegisterParser(new DocxBookParser());
            registry.RegisterParser(new MarkdownBookParser());

            desktop.MainWindow = new MainWindow(registry);
        }

        base.OnFrameworkInitializationCompleted();
    }
}