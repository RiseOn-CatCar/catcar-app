#!/usr/bin/env dotnet
#:property PublishAot=false
using System.Globalization;
using System.Xml.Linq;

if (args.Length != 6 || args[0] != "--root" || args[2] != "--reports" || args[4] != "--minimum" ||
    !double.TryParse(args[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var minimum) || !double.IsFinite(minimum) || minimum < 0 || minimum > 100 ||
    !Directory.Exists(args[1]))
{
    Console.Error.WriteLine("Usage: --root <catcar-app> --reports <OpenCover directory> --minimum <0-100>");
    return 2;
}

if (!Directory.Exists(args[3]))
{
    Console.Error.WriteLine("Coverage report directory is missing.");
    return 1;
}

var root = Path.GetFullPath(args[1]);
var scopes = new[]
{
    "src/SharedKernel/",
    "src/Contexts/ServiceOperations/Domain/",
    "src/Contexts/CatalogInventory/Domain/",
};
var lines = new Dictionary<(string Path, int Line), bool>();
var reports = Directory.GetFiles(args[3], "*.xml", SearchOption.AllDirectories);
try
{
    foreach (var report in reports)
    {
        var document = XDocument.Load(report);
        if (document.Root?.Name.LocalName != "CoverageSession") continue;
        foreach (var module in document.Descendants("Module"))
        {
            var files = module.Descendants("File")
                .Where(f => f.Attribute("uid") is not null && f.Attribute("fullPath") is not null)
                .ToDictionary(f => (string)f.Attribute("uid")!, f => Path.GetFullPath((string)f.Attribute("fullPath")!, root));
            foreach (var point in module.Descendants("SequencePoint"))
            {
                var fileId = (string?)point.Attribute("fileid") ?? (string?)point.Parent?.Parent?.Element("FileRef")?.Attribute("uid");
                if (fileId is null || !files.TryGetValue(fileId, out var path) ||
                    !int.TryParse((string?)point.Attribute("sl"), out var line) || line <= 0 ||
                    !int.TryParse((string?)point.Attribute("vc"), out var visits)) continue;
                var key = (path, line);
                lines[key] = lines.GetValueOrDefault(key) || visits > 0;
            }
        }
    }
}
catch (Exception exception) when (exception is System.Xml.XmlException or IOException or UnauthorizedAccessException or ArgumentException)
{
    Console.Error.WriteLine($"Cannot read coverage: {exception.Message}");
    return 1;
}

var failed = false;
foreach (var scope in scopes)
{
    var prefix = Path.GetFullPath(scope, root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    var selected = lines.Where(entry => entry.Key.Path.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
    var covered = selected.Count(entry => entry.Value);
    var percent = selected.Length == 0 ? 0 : 100d * covered / selected.Length;
    Console.WriteLine($"{scope}: {covered}/{selected.Length} ({percent:F2}%)");
    failed |= selected.Length == 0 || percent < minimum;
}
var identityPrefix = Path.GetFullPath("src/Contexts/IdentityAccess/Domain/", root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
var identityLines = lines.Where(entry => entry.Key.Path.StartsWith(identityPrefix, StringComparison.Ordinal)).ToArray();
var identityCovered = identityLines.Count(entry => entry.Value);
Console.WriteLine(identityLines.Length == 0
    ? "IdentityAccess domain: no instrumented lines (informational)"
    : $"IdentityAccess domain: {identityCovered}/{identityLines.Length} ({100d * identityCovered / identityLines.Length:F2}%, informational)");
return failed ? 1 : 0;
