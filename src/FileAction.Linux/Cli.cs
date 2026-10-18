using FileAction.Core;
using static FileAction.Core.Loc;

namespace FileAction.Linux;

/// <summary>Command-line use: list and run operations without the wheel (scripts, keyboard shortcuts).</summary>
public static class Cli
{
    public static readonly string[] Commands = ["list", "run", "integrate", "help", "--help", "-h", "--version"];

    public static string Version => UpdateInfo.Normalize(typeof(Cli).Assembly.GetName().Version ?? new Version(0, 0, 0)).ToString();

    public static int Run(string[] args)
    {
        var settings = LinuxSettings.Load();
        Loc.Current = settings.Language ?? Loc.Detect();
        try
        {
            return args[0] switch
            {
                "list" => List(args[1..], settings),
                "run" => RunOperation(args[1..], settings),
                "integrate" => Integrate(args[1..]),
                "--version" => Print($"fileaction {Version}"),
                _ => Print(Help),
            };
        }
        catch (ConversionException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int Print(string text)
    {
        Console.WriteLine(text);
        return 0;
    }

    private static string Help => $"""
        fileaction {Version} – {T("convert files by dropping them on a wheel", "حوّل الملفات بإفلاتها على عجلة")}

          fileaction                       {T("a wheel you can drop files on", "عجلة يمكن إفلات الملفات عليها")}
          fileaction FILE...               {T("the wheel for these files", "عجلة هذه الملفات")}
          fileaction list FILE...          {T("what can be done with them", "ما يمكن فعله بها")}
          fileaction run ID FILE... [--param KEY=VALUE]... [--out DIR]
                                           {T("runs a conversion or tool (ID from list)", "ينفّذ تحويلًا أو أداة (المعرّف من الأمر list)")}
          fileaction integrate [--remove]  {T("FileAction item in Nautilus, Dolphin and Nemo", "عنصر FileAction في Nautilus وDolphin وNemo")}
          fileaction --version
        """;

    private static List<FileItem> Items(IEnumerable<string> paths)
    {
        var items = new List<FileItem>();
        foreach (var p in paths)
        {
            var full = Path.GetFullPath(p);
            if (!File.Exists(full) && !Directory.Exists(full))
                throw new ConversionException(T($"No such file: {p}", $"لا يوجد ملف بهذا الاسم: {p}"));
            items.Add(new FileItem(full));
        }
        if (items.Count == 0)
            throw new ConversionException(T("Give at least one file.", "مرّر ملفًا واحدًا على الأقل."));
        return items;
    }

    private static int List(string[] args, LinuxSettings settings)
    {
        var items = Items(args);
        var tools = new ToolLocator();
        foreach (var wheel in new[] { WheelKind.Convert, WheelKind.Tools })
        {
            var ops = Core.Operations.Catalog.For(LinuxOperations.Conversions(settings), wheel, items, tools);
            if (ops.Count == 0)
                continue;
            Console.WriteLine(wheel == WheelKind.Convert ? T("Convert:", "التحويل:") : T("Tools:", "الأدوات:"));
            foreach (var op in ops)
                Console.WriteLine($"  {op.Id,-22} {op.Label}{(op.Subtitle is null ? "" : $" ({op.Subtitle})")}");
        }
        return 0;
    }

    private static int RunOperation(string[] args, LinuxSettings settings)
    {
        if (args.Length < 2)
            throw new ConversionException(Help);
        var parameters = new Dictionary<string, string>();
        string? output = null;
        var paths = new List<string>();
        for (var i = 1; i < args.Length; i++)
        {
            if (args[i] == "--param" && i + 1 < args.Length && args[i + 1].Split('=', 2) is [var k, var v])
            {
                parameters[k] = v;
                i++;
            }
            else if (args[i] == "--out" && i + 1 < args.Length)
            {
                output = Path.GetFullPath(args[++i]);
            }
            else
            {
                paths.Add(args[i]);
            }
        }
        var items = Items(paths);
        var tools = new ToolLocator();
        var op = LinuxOperations.Conversions(settings).FirstOrDefault(o => o.Id == args[0])
            ?? throw new ConversionException(T($"Unknown ID: {args[0]} (see fileaction list)", $"معرّف غير معروف: {args[0]} (راجع fileaction list)"));
        if (!op.AppliesTo(items))
            throw new ConversionException(T($"{op.Id} doesn't apply to these files.", $"\u200F{op.Id} لا ينطبق على هذه الملفات."));
        if (!op.IsAvailable(tools, items))
            throw new ConversionException(T($"{op.Id} needs a tool that isn't installed.", $"\u200F{op.Id} يحتاج أداة غير مثبّتة.")
                + (op.Requires.Count > 0 ? " " + string.Join(", ", op.Requires.Select(ToolLocator.InstallHint)) : ""));

        var context = new OperationContext
        {
            Tools = tools,
            Parameters = parameters,
            OutputDirectory = output ?? (string.IsNullOrWhiteSpace(settings.OutputDirectory) ? null : settings.OutputDirectory),
            Settings = settings.Quality,
            Progress = Console.IsErrorRedirected ? new Progress<double>() : new ConsoleProgress(),
        };
        var result = op.ExecuteAsync(items, context).GetAwaiter().GetResult();
        if (!Console.IsErrorRedirected)
            Console.Error.Write("\r       \r");
        foreach (var path in result.Outputs)
            Console.WriteLine(path);
        if (result.Message is not null)
            Console.Error.WriteLine(result.Message);
        return 0;
    }

    /// <summary>Percentage on stderr, written as it is reported (Progress&lt;T&gt; would post it later).</summary>
    private sealed class ConsoleProgress : IProgress<double>
    {
        public void Report(double value)
        {
            lock (this)
                Console.Error.Write($"\r{value,6:P0}");
        }
    }

    private static int Integrate(string[] args)
    {
        if (args.Length == 2 && args[0] == "--export")
        {
            foreach (var file in Shell.Integration.Export(args[1]))
                Console.WriteLine(file);
            return 0;
        }
        var remove = args.Contains("--remove");
        foreach (var line in Shell.Integration.Apply(!remove))
            Console.WriteLine(line);
        return 0;
    }
}
