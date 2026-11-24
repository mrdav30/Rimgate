namespace Rimgate.HarmonyPatches;

/// <summary>
/// Contains the state for the various harmony patches related to mobile containers. 
/// This is used to avoid having to use reflection to check for the state in multiple places, 
/// and to avoid having to pass the state through multiple layers of method calls.
/// </summary>
internal static class MobileContainerHarmonyState
{
    [System.ThreadStatic]
    private static int _constructionResourceSearchDepth;

    [System.ThreadStatic]
    private static int _buildGhostResourceReadoutDepth;

    internal static bool InConstructionResourceSearch => _constructionResourceSearchDepth > 0;
    internal static bool InBuildGhostResourceReadout => _buildGhostResourceReadoutDepth > 0;

    internal static void EnterConstructionResourceSearch()
    {
        _constructionResourceSearchDepth++;
    }

    internal static void ExitConstructionResourceSearch()
    {
        if (_constructionResourceSearchDepth > 0)
            _constructionResourceSearchDepth--;
    }

    internal static void EnterBuildGhostResourceReadout()
    {
        _buildGhostResourceReadoutDepth++;
    }

    internal static void ExitBuildGhostResourceReadout()
    {
        if (_buildGhostResourceReadoutDepth > 0)
            _buildGhostResourceReadoutDepth--;
    }
}
