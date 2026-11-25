using System.Collections;
using System.Reflection;
using RimBridgeServer.Sdk;
using RimWorld;
using Verse;

namespace Rimgate.AssetProbe;

// Inspect KCSG's actual resolved grids; never resolve or generate a layout here.
public sealed class ContentProbe
{
    [Tool("rimgate/layout_content_report", Description = "Read loaded Rimgate KCSG layout grids and report missing symbols or resolved symbols without a ThingDef/PawnKindDef. Does not resolve symbols, generate maps or change content.")]
    public static Task<object> Report(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var layoutType = Type.GetType("KCSG.StructureLayoutDef, KCSG", true)!;
        var database = typeof(DefDatabase<>).MakeGenericType(layoutType);
        var definitions = ((IEnumerable)database.GetProperty("AllDefsListForReading")!.GetValue(null))
            .Cast<Def>().Where(d => d.modContentPack?.PackageId == "mrdav30.rimgate")
            .OrderBy(d => d.defName).ToArray();
        var rawField = layoutType.GetField("layouts")!;
        var gridsField = layoutType.GetField("_layouts", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var symbolType = Type.GetType("KCSG.SymbolDef, KCSG", true)!;
        var thingField = symbolType.GetField("thingDef", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        var pawnField = symbolType.GetField("pawnKindDefNS", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        var findings = new Dictionary<(string Token, string Reason), Finding>();
        int unresolvedLayouts = 0, checkedCells = 0, ignoredCells = 0;
        foreach (var layout in definitions)
        {
            var raw = (List<List<string>>)rawField.GetValue(layout);
            var grids = ((IEnumerable)gridsField.GetValue(layout))?.Cast<Array>().ToArray();
            if (grids == null || grids.Length != raw.Count) unresolvedLayouts++;
            for (int layer = 0; layer < raw.Count; layer++)
            {
                var grid = grids != null && layer < grids.Length ? grids[layer] : null;
                for (int row = 0; row < raw[layer].Count; row++)
                {
                    var tokens = raw[layer][row].Split(',');
                    for (int column = 0; column < tokens.Length; column++)
                    {
                        var token = tokens[column];
                        if (token == ".") continue;
                        if (grid != null && (row >= grid.GetLength(0) || column >= grid.GetLength(1)))
                        {
                            ignoredCells++;
                            continue;
                        }
                        checkedCells++;
                        var symbol = grid?.GetValue(row, column);
                        string reason = grid == null ? "layoutNotResolved" : symbol == null ? "missingSymbol"
                            : thingField.GetValue(symbol) == null && pawnField.GetValue(symbol) == null
                                ? "symbolWithoutThingOrPawn" : null;
                        if (reason == null) continue;
                        var key = (token, reason);
                        if (!findings.TryGetValue(key, out var finding))
                            findings.Add(key, finding = new Finding(token, reason));
                        finding.Count++;
                        finding.Layouts.Add(layout.defName);
                    }
                }
            }
        }
        return new
        {
            success = definitions.Length > 0 && unresolvedLayouts == 0 && findings.Count == 0,
            gameVersion = VersionControl.CurrentVersionStringWithRev,
            layoutCount = definitions.Length, unresolvedLayoutCount = unresolvedLayouts,
            checkedNonemptyCells = checkedCells, ignoredOutOfBoundsCells = ignoredCells,
            unresolvedTokenCount = findings.Select(f => f.Key.Token).Distinct().Count(),
            unresolvedCellCount = findings.Values.Sum(f => f.Count),
            unresolvedTokens = findings.Values.OrderBy(f => f.Token).ThenBy(f => f.Reason)
                .Select(f => new { token = f.Token, reason = f.Reason, cells = f.Count,
                    layouts = f.Layouts.OrderBy(x => x).ToArray() }).ToArray(),
            limits = "Loaded KCSG resolved symbol references only. Missing target detection also covers non-null rotation fallbacks without a target definition. No row contents, map generation or rendered-world acceptance is implied."
        };
    });

    private sealed class Finding(string token, string reason)
    {
        public readonly string Token = token;
        public readonly string Reason = reason;
        public readonly HashSet<string> Layouts = new(StringComparer.Ordinal);
        public int Count;
    }

    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !Prefs.DevMode || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected private 1.7 profile and mod deployment.");
    }
}
