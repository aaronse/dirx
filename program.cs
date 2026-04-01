using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DirX
{
    internal static class Program
    {
        private enum TimeField
        {
            Creation,
            Write
        }

        private enum SortField
        {
            Name,
            Extension,
            Size,
            Date
        }

        private sealed class SortTerm
        {
            public SortField Field { get; init; }
            public bool Descending { get; init; }
        }

        private sealed class Options
        {
            public bool Recurse { get; set; }
            public bool Bare { get; set; }
            public bool IncludeHidden { get; set; }
            public bool IncludeSystem { get; set; }
            public bool IncludeDirectories { get; set; }
            public bool IncludeFiles { get; set; } = true;
            public string PatternArgument { get; set; } = "";
            public string RootDirectory { get; set; } = "";
            public string SearchPattern { get; set; } = "*";
            public DateTime? MinDate { get; set; }
            public DateTime? MaxDate { get; set; }
            public TimeField TimeField { get; set; } = TimeField.Write;
            public List<SortTerm> SortTerms { get; } = new();
        }

        private static int Main(string[] args)
        {
            try
            {
                var options = ParseArguments(args);

                if (string.IsNullOrWhiteSpace(options.PatternArgument))
                {
                    PrintUsage();
                    return 1;
                }

                ResolvePattern(options);

                if (!Directory.Exists(options.RootDirectory))
                {
                    Console.Error.WriteLine($"Path not found: {options.RootDirectory}");
                    return 2;
                }

                if (options.MinDate.HasValue && options.MaxDate.HasValue && options.MinDate > options.MaxDate)
                {
                    Console.Error.WriteLine("/min must be less than or equal to /max.");
                    return 3;
                }

                IEnumerable<FileSystemInfo> entries = EnumerateEntries(options);
                entries = ApplyFilters(entries, options);
                entries = ApplySorting(entries, options);

                WriteOutput(entries, options);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 10;
            }
        }

        private static Options ParseArguments(string[] args)
        {
            var options = new Options();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (IsHelp(arg))
                {
                    PrintUsage();
                    Environment.Exit(0);
                }

                if (IsSwitch(arg))
                {
                    string normalized = NormalizeSwitch(arg);

                    if (string.Equals(normalized, "s", StringComparison.OrdinalIgnoreCase))
                    {
                        options.Recurse = true;
                        continue;
                    }

                    if (string.Equals(normalized, "b", StringComparison.OrdinalIgnoreCase))
                    {
                        options.Bare = true;
                        continue;
                    }

                    if (string.Equals(normalized, "od", StringComparison.OrdinalIgnoreCase))
                    {
                        options.SortTerms.Clear();
                        options.SortTerms.Add(new SortTerm { Field = SortField.Date, Descending = false });
                        continue;
                    }

                    if (string.Equals(normalized, "on", StringComparison.OrdinalIgnoreCase))
                    {
                        options.SortTerms.Clear();
                        options.SortTerms.Add(new SortTerm { Field = SortField.Name, Descending = false });
                        continue;
                    }

                    if (string.Equals(normalized, "oe", StringComparison.OrdinalIgnoreCase))
                    {
                        options.SortTerms.Clear();
                        options.SortTerms.Add(new SortTerm { Field = SortField.Extension, Descending = false });
                        continue;
                    }

                    if (string.Equals(normalized, "os", StringComparison.OrdinalIgnoreCase))
                    {
                        options.SortTerms.Clear();
                        options.SortTerms.Add(new SortTerm { Field = SortField.Size, Descending = false });
                        continue;
                    }

                    if (normalized.StartsWith("o:", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseSortList(normalized.Substring(2), options);
                        continue;
                    }

                    if (string.Equals(normalized, "a", StringComparison.OrdinalIgnoreCase))
                    {
                        options.IncludeHidden = true;
                        options.IncludeSystem = true;
                        options.IncludeDirectories = true;
                        options.IncludeFiles = true;
                        continue;
                    }

                    if (normalized.StartsWith("a:", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseAttributes(normalized.Substring(2), options);
                        continue;
                    }

                    if (normalized.StartsWith("t:", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseTimeField(normalized.Substring(2), options);
                        continue;
                    }

                    if (string.Equals(normalized, "min", StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 >= args.Length)
                            throw new ArgumentException("Missing value after /min.");
                        options.MinDate = ParseDate(args[++i], "/min");
                        continue;
                    }

                    if (string.Equals(normalized, "max", StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 >= args.Length)
                            throw new ArgumentException("Missing value after /max.");
                        options.MaxDate = ParseDate(args[++i], "/max");
                        continue;
                    }

                    throw new ArgumentException($"Unsupported switch: {arg}");
                }

                if (!string.IsNullOrWhiteSpace(options.PatternArgument))
                    throw new ArgumentException($"Unexpected extra argument: {arg}");

                options.PatternArgument = arg;
            }

            return options;
        }

        private static IEnumerable<FileSystemInfo> EnumerateEntries(Options options)
        {
            var root = new DirectoryInfo(options.RootDirectory);
            var searchOption = options.Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var results = new List<FileSystemInfo>();

            if (options.IncludeFiles)
            {
                try
                {
                    results.AddRange(root.EnumerateFiles(options.SearchPattern, searchOption));
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            if (options.IncludeDirectories)
            {
                try
                {
                    results.AddRange(root.EnumerateDirectories(options.SearchPattern, searchOption));
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return results;
        }

        private static IEnumerable<FileSystemInfo> ApplyFilters(IEnumerable<FileSystemInfo> entries, Options options)
        {
            foreach (var entry in entries)
            {
                var attr = entry.Attributes;

                bool isHidden = attr.HasFlag(FileAttributes.Hidden);
                bool isSystem = attr.HasFlag(FileAttributes.System);
                bool isDirectory = attr.HasFlag(FileAttributes.Directory);

                if (!options.IncludeHidden && isHidden)
                    continue;

                if (!options.IncludeSystem && isSystem)
                    continue;

                if (isDirectory && !options.IncludeDirectories)
                    continue;

                if (!isDirectory && !options.IncludeFiles)
                    continue;

                DateTime relevantTime = GetRelevantTime(entry, options.TimeField);

                if (options.MinDate.HasValue && relevantTime < options.MinDate.Value)
                    continue;

                if (options.MaxDate.HasValue && relevantTime > options.MaxDate.Value)
                    continue;

                yield return entry;
            }
        }

        private static IEnumerable<FileSystemInfo> ApplySorting(IEnumerable<FileSystemInfo> entries, Options options)
        {
            var list = entries.ToList();

            if (options.SortTerms.Count == 0)
            {
                return list.OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase);
            }

            IOrderedEnumerable<FileSystemInfo>? ordered = null;

            foreach (var term in options.SortTerms)
            {
                Func<FileSystemInfo, object> keySelector = term.Field switch
                {
                    SortField.Name => e => e.Name,
                    SortField.Extension => e => Path.GetExtension(e.Name),
                    SortField.Size => e => GetSize(e),
                    SortField.Date => e => GetRelevantTime(e, options.TimeField),
                    _ => e => e.Name
                };

                if (ordered == null)
                {
                    ordered = term.Descending
                        ? list.OrderByDescending(keySelector)
                        : list.OrderBy(keySelector);
                }
                else
                {
                    ordered = term.Descending
                        ? ordered.ThenByDescending(keySelector)
                        : ordered.ThenBy(keySelector);
                }
            }

            return ordered!
                .ThenBy(e => e.FullName, StringComparer.OrdinalIgnoreCase);
        }

        private static void WriteOutput(IEnumerable<FileSystemInfo> entries, Options options)
        {
            foreach (var entry in entries)
            {
                if (options.Bare)
                {
                    Console.WriteLine(entry.FullName);
                    continue;
                }

                bool isDirectory = entry.Attributes.HasFlag(FileAttributes.Directory);
                DateTime time = GetRelevantTime(entry, options.TimeField);
                long size = isDirectory ? 0 : GetSize(entry);

                string attr = FormatAttributes(entry.Attributes);
                string sizeText = isDirectory ? "<DIR>" : size.ToString("N0", CultureInfo.InvariantCulture);

                Console.WriteLine(
                    $"{time:yyyy-MM-dd HH:mm:ss}  {sizeText,12}  {attr,-6}  {entry.FullName}");
            }
        }

        private static DateTime GetRelevantTime(FileSystemInfo entry, TimeField timeField)
        {
            return timeField switch
            {
                TimeField.Creation => entry.CreationTime,
                _ => entry.LastWriteTime
            };
        }

        private static long GetSize(FileSystemInfo entry)
        {
            return entry is FileInfo file ? file.Length : 0L;
        }

        private static string FormatAttributes(FileAttributes attr)
        {
            var chars = new List<char>();

            if (attr.HasFlag(FileAttributes.Directory)) chars.Add('D');
            if (attr.HasFlag(FileAttributes.Hidden)) chars.Add('H');
            if (attr.HasFlag(FileAttributes.System)) chars.Add('S');
            if (attr.HasFlag(FileAttributes.ReadOnly)) chars.Add('R');
            if (attr.HasFlag(FileAttributes.Archive)) chars.Add('A');

            return chars.Count == 0 ? "-" : new string(chars.ToArray());
        }

        private static void ResolvePattern(Options options)
        {
            string full = Path.GetFullPath(options.PatternArgument);

            bool hasWildcard = full.IndexOfAny(new[] { '*', '?' }) >= 0;

            if (!hasWildcard)
            {
                if (Directory.Exists(full))
                {
                    options.RootDirectory = full;
                    options.SearchPattern = "*";
                    return;
                }

                options.RootDirectory = Path.GetDirectoryName(full) ?? Directory.GetCurrentDirectory();
                options.SearchPattern = Path.GetFileName(full);
                return;
            }

            string? dir = Path.GetDirectoryName(full);
            string file = Path.GetFileName(full);

            options.RootDirectory = string.IsNullOrWhiteSpace(dir)
                ? Directory.GetCurrentDirectory()
                : dir;

            options.SearchPattern = string.IsNullOrWhiteSpace(file) ? "*" : file;
        }

        private static void ParseSortList(string value, Options options)
        {
            options.SortTerms.Clear();

            foreach (char raw in value)
            {
                if (char.IsWhiteSpace(raw) || raw == ',')
                    continue;

                bool descending = false;
                char c = raw;

                if (c == '-')
                    continue;

                int idx = value.IndexOf(raw);
                if (idx > 0 && value[idx - 1] == '-')
                    descending = true;

                SortField field = char.ToLowerInvariant(c) switch
                {
                    'n' => SortField.Name,
                    'e' => SortField.Extension,
                    's' => SortField.Size,
                    'd' => SortField.Date,
                    _ => throw new ArgumentException($"Unsupported sort field in /o:{value}. Supported: n,e,s,d")
                };

                bool alreadySameField = options.SortTerms.Any(t => t.Field == field);
                if (!alreadySameField)
                {
                    options.SortTerms.Add(new SortTerm
                    {
                        Field = field,
                        Descending = descending
                    });
                }
            }

            if (options.SortTerms.Count == 0)
                throw new ArgumentException("No valid sort fields found in /o:...");
        }

        private static void ParseAttributes(string value, Options options)
        {
            options.IncludeHidden = false;
            options.IncludeSystem = false;
            options.IncludeDirectories = false;
            options.IncludeFiles = false;

            foreach (char c in value)
            {
                switch (char.ToLowerInvariant(c))
                {
                    case 'h':
                        options.IncludeHidden = true;
                        break;
                    case 's':
                        options.IncludeSystem = true;
                        break;
                    case 'd':
                        options.IncludeDirectories = true;
                        break;
                    case '-':
                        break;
                    default:
                        throw new ArgumentException($"Unsupported attribute filter in /a:{value}. Supported: h,s,d");
                }
            }

            if (!options.IncludeDirectories)
                options.IncludeFiles = true;
        }

        private static void ParseTimeField(string value, Options options)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Missing value for /t:");

            options.TimeField = char.ToLowerInvariant(value[0]) switch
            {
                'c' => TimeField.Creation,
                'w' => TimeField.Write,
                _ => throw new ArgumentException("Unsupported /t value. Supported: c,w")
            };
        }

        private static DateTime ParseDate(string text, string label)
        {
            if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var dt))
                return dt;

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt))
                return dt;

            throw new ArgumentException($"Invalid {label} date/time value: {text}");
        }

        private static bool IsSwitch(string arg)
        {
            return arg.StartsWith("/") || arg.StartsWith("-");
        }

        private static string NormalizeSwitch(string arg)
        {
            return arg.TrimStart('/', '-');
        }

        private static bool IsHelp(string arg)
        {
            return arg.Equals("/?") ||
                   arg.Equals("/h", StringComparison.OrdinalIgnoreCase) ||
                   arg.Equals("/help", StringComparison.OrdinalIgnoreCase) ||
                   arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                   arg.Equals("--help", StringComparison.OrdinalIgnoreCase);
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@"
dirx - dir-like file listing with date filtering

Usage:
  dirx [switches] <path-pattern> [/min <date>] [/max <date>]

Examples:
  dirx /s /b C:\Users\aaron\Videos\*.mp4 /min 2025-1-1 /max 2025-2-1
  dirx /s /o:-d C:\Temp\*.log
  dirx /b /on C:\Work\*.cs
  dirx /a:hd /s C:\Data\*

Supported switches:
  /s            Recurse into subdirectories
  /b            Bare output (full path only)
  /min <date>   Inclusive minimum date/time
  /max <date>   Inclusive maximum date/time

Sort:
  /o:n          Name ascending
  /o:e          Extension ascending
  /o:s          Size ascending
  /o:d          Date ascending
  /o:-d         Date descending
  /o:ne         Name, then extension
  /od /on /oe /os   Convenience forms

Time field:
  /t:w          Use LastWriteTime (default)
  /t:c          Use CreationTime

Attributes:
  /a            Include hidden, system, dirs, files
  /a:h          Include hidden
  /a:s          Include system
  /a:d          Include directories too

Notes:
  - This is a useful subset of classic DIR behavior, not a full clone.
  - Date filtering uses the selected /t field.
");
        }
    }
}