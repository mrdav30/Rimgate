using UnityEngine;
using Verse;

namespace Rimgate;

public static class DrawUtil
{
    public const float SliderHeight = 22f;

    public const float SliderGap = 8f;

    public const float SliderInputGap = 2f;

    public const float CheckboxGap = 4f;

    public const float SectionHeaderTopGap = 8f;

    public const float SectionHeaderBottomGap = 4f;

    public const float ListingVerticalSpacing = 2f;

    public const float SettingsLabelWidth = 240f;

    public static void DrawSectionHeader(Listing_Standard listing, string label, bool addTopGap = true)
    {
        if (addTopGap)
            listing.Gap(SectionHeaderTopGap);

        GameFont previousFont = Text.Font;
        Text.Font = GameFont.Medium;
        listing.Label(label);
        Text.Font = previousFont;

        listing.Gap(2f);
        listing.GapLine();
        listing.Gap(SectionHeaderBottomGap);
    }

    public static void DrawCheckbox(Listing_Standard listing, string label, ref bool value)
    {
        listing.CheckboxLabeled(label, ref value);
        listing.Gap(CheckboxGap);
    }

    public static float GetSliderLineHeight()
    {
        return SliderHeight + SliderGap;
    }

    public static float GetSliderWithInputHeight()
    {
        return (SliderHeight * 2f) + SliderInputGap + SliderGap;
    }

    public static float GetSectionHeaderHeight(string label, float width, bool addTopGap = true)
    {
        float topGap = addTopGap ? SectionHeaderTopGap : 0f;
        return topGap
            + GetTextHeight(label, width, GameFont.Medium)
            + ListingVerticalSpacing
            + 2f
            + Listing.DefaultGap
            + SectionHeaderBottomGap;
    }

    public static float GetCheckboxHeight(string label, float width)
    {
        return GetTextHeight(label, width, GameFont.Small) + ListingVerticalSpacing + CheckboxGap;
    }

    public static float GetTextHeight(string text, float width, GameFont font)
    {
        GameFont previousFont = Text.Font;
        Text.Font = font;
        float height = Text.CalcHeight(text, Mathf.Max(1f, width));
        Text.Font = previousFont;
        return height;
    }

    public static int DrawIntInputRow(
        Rect rowRect,
        int value,
        ref string valueBuffer,
        int min,
        int max,
        string label = null,
        float labelPct = 0.35f,
        float fieldPct = 0.60f)
    {
        valueBuffer ??= value.ToString();

        Rect labelRect = rowRect.LeftPart(labelPct);
        Rect fieldRect = rowRect.RightPart(fieldPct);

        Widgets.Label(labelRect, label ?? "RG_SetTo".Translate());
        string newBuffer = Widgets.TextField(fieldRect, valueBuffer);

        if (newBuffer != valueBuffer)
        {
            valueBuffer = newBuffer;
            if (int.TryParse(newBuffer, out int parsed))
            {
                value = Mathf.Clamp(parsed, min, max);
                valueBuffer = value.ToString();
            }
        }

        return value;
    }

    public static void DrawIntSliderWithInput(
        Listing_Standard listing,
        string label,
        ref int value,
        ref string valueBuffer,
        int min,
        int max,
        string valueSuffix = "")
    {
        Rect sliderRect = listing.GetRect(SliderHeight);
        int sliderValue = Mathf.RoundToInt(Widgets.HorizontalSlider(
            sliderRect,
            value,
            min,
            max,
            true,
            $"{label}: {value}{valueSuffix}",
            min.ToString(),
            max.ToString(),
            1f));

        if (sliderValue != value)
        {
            value = sliderValue;
            valueBuffer = value.ToString();
        }

        listing.Gap(SliderInputGap);
        Rect inputRect = listing.GetRect(SliderHeight);
        value = DrawIntInputRow(
            inputRect,
            value,
            ref valueBuffer,
            min,
            max,
            labelPct: 0.45f,
            fieldPct: 0.50f);

        listing.Gap(SliderGap);
    }

    public static void DrawIntSlider(
        Listing_Standard listing,
        string label,
        ref int value,
        int min,
        int max,
        string valueSuffix = "")
    {
        Rect rect = listing.GetRect(SliderHeight);
        value = Mathf.RoundToInt(Widgets.HorizontalSlider(
            rect,
            value,
            min,
            max,
            true,
            $"{label}: {value}{valueSuffix}",
            min.ToString(),
            max.ToString(),
            1f));

        listing.Gap(SliderGap);
    }

    public static void DrawFloatSlider(
        Listing_Standard listing,
        string label,
        ref float value,
        float min,
        float max,
        float roundTo,
        string format,
        string valueSuffix = "")
    {
        Rect rect = listing.GetRect(SliderHeight);
        value = Widgets.HorizontalSlider(
            rect,
            value,
            min,
            max,
            true,
            $"{label}: {value.ToString(format)}{valueSuffix}",
            min.ToString(format),
            max.ToString(format),
            roundTo);

        listing.Gap(SliderGap);
    }

    public static void DrawPercentSlider(
        Listing_Standard listing,
        string label,
        ref float value,
        float min,
        float max,
        float roundTo)
    {
        int percent = Mathf.RoundToInt(value * 100f);
        int minPercent = Mathf.RoundToInt(min * 100f);
        int maxPercent = Mathf.RoundToInt(max * 100f);

        Rect rect = listing.GetRect(SliderHeight);
        value = Widgets.HorizontalSlider(
            rect,
            value,
            min,
            max,
            true,
            $"{label}: {percent}%",
            $"{minPercent}%",
            $"{maxPercent}%",
            roundTo);

        listing.Gap(SliderGap);
    }
}
