using System.Collections;
using System.Reflection;
using RimBridgeServer.Sdk;

namespace Rimgate.AssetProbe;

// Inspect the loaded Harmony instance without adding another Harmony binary to the companion.
public sealed class PatchProbe
{
    [Tool("rimgate/patch_report", Description = "Read the loaded Rimgate Harmony owner's actual patched methods and patch functions. Does not install patches or prove every patched feature's behavior.")]
    public static Task<object> Report(IRimBridgeContext context) => context.MainThread.InvokeAsync<object>(() =>
    {
        const string owner = "com.Rimgate.patches";
        var instance = typeof(RimgateMod).GetField("harmony")!.GetValue(null)
            ?? throw new InvalidOperationException("Rimgate's Harmony initialization failed.");
        var type = instance.GetType();
        var binary = System.Diagnostics.FileVersionInfo.GetVersionInfo(type.Assembly.Location);
        var patched = (IEnumerable<MethodBase>)type.GetMethod("GetPatchedMethods")!.Invoke(instance, null)!;
        var getInfo = type.GetMethod("GetPatchInfo", BindingFlags.Public | BindingFlags.Static)!;
        var targets = patched.Select(method =>
        {
            var info = getInfo.Invoke(null, [method])!;
            var patches = new[] { "Prefixes", "Postfixes", "Transpilers", "Finalizers" }.SelectMany(kind =>
                ((IEnumerable)info.GetType().GetField(kind)!.GetValue(info)!).Cast<object>()
                .Where(p => (string)p.GetType().GetField("owner")!.GetValue(p)! == owner)
                .Select(p => new {
                    kind, method = ((MethodInfo)p.GetType().GetProperty("PatchMethod")!.GetValue(p)!).ToString(),
                    declaringType = ((MethodInfo)p.GetType().GetProperty("PatchMethod")!.GetValue(p)!).DeclaringType?.FullName
                })).ToArray();
            return new { target = method.ToString(), declaringType = method.DeclaringType?.FullName, patches };
        }).OrderBy(t => t.declaringType).ThenBy(t => t.target).ToArray();
        var inventory = targets.SingleOrDefault(t => t.declaringType?.Contains("PawnInventoryOption+<GenerateThings>") == true);
        return new { success = targets.Length > 0 && inventory?.patches.Any(p => p.kind == "Transpilers"
                && p.declaringType == "Rimgate.HarmonyPatches.Harmony_PawnInventoryOption_GenerateThings") == true,
            owner, harmonyVersion = type.Assembly.GetName().Version?.ToString(),
            harmonyPath = type.Assembly.Location, binary.FileVersion, binary.ProductVersion, targets,
            limits = "Actual registration in this process, not acceptance of every patched gameplay feature." };
    });
}
