using RimWorld;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace Rimgate;

public enum RimgateLogLevel
{
    Off = 0,
    Error = 1,
    Warning = 2,
    Message = 3,
    Debug = 4
}

public static class LogUtil
{
    private const string Prefix = "Rimgate :: ";

    private static RimgateLogLevel CurrentLevel => RimgateMod.Settings?.LogLevel ?? RimgateLogLevel.Warning;

    public static bool IsDebugEnabled => CurrentLevel >= RimgateLogLevel.Debug;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Message(string message)
    {
        if (CurrentLevel < RimgateLogLevel.Message)
            return;

        Log.Message(PrefixMessage(message));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(string message)
    {
        if (!IsDebugEnabled)
            return;

        Log.Message(PrefixMessage(message));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warning(string message)
    {
        if (CurrentLevel < RimgateLogLevel.Warning)
            return;

        Log.Warning(PrefixMessage(message));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DebugWarning(string message)
    {
        if (!IsDebugEnabled)
            return;

        Log.Warning(PrefixMessage(message));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(string message)
    {
        if (CurrentLevel < RimgateLogLevel.Error)
            return;

        Log.Error(PrefixMessage(message));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(Exception exception, string context = null)
    {
        if (exception == null)
            return;

        if (string.IsNullOrWhiteSpace(context))
            Error(exception.ToString());
        else
            Error($"{context}:\n{exception}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowTextMote(string text, Vector3 drawPos, Map map)
    {
        MoteMaker.ThrowText(drawPos, map, text, -1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowTextMote(string text, IntVec3 c, Map map)
    {
        MoteMaker.ThrowText(c.ToVector3Shifted(), map, text, -1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string PrefixMessage(string message) => $"{Prefix}{message}";
}
