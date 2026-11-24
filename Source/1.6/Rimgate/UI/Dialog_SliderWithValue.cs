using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Dialog_SliderWithValue : Window
{
    public Func<int, string> TextGetter;

    public int From;
    public int To;

    public float RoundTo = 1f;
    public float ExtraBottomSpace;

    private readonly Action<int> _confirmAction;
    private int _curValue;

    // Optional: numeric input box
    private readonly bool _showNumericEntry;
    private string _curValueBuffer;

    // Optional: add suffix (e.g. "tiles")
    private readonly string _unitLabel;

    public override Vector2 InitialSize => new Vector2(360f, 170f + ExtraBottomSpace);
    protected override float Margin => 10f;

    public Dialog_SliderWithValue(
        Func<int, string> textGetter,
        int from,
        int to,
        Action<int> confirmAction,
        int startingValue = int.MinValue,
        float roundTo = 1f,
        bool showNumericEntry = true,
        string unitLabel = null)
    {
        TextGetter = textGetter;
        From = from;
        To = to;
        _confirmAction = confirmAction;
        RoundTo = roundTo;
        _showNumericEntry = showNumericEntry;
        _unitLabel = unitLabel;

        forcePause = true;
        closeOnClickedOutside = true;

        _curValue = (startingValue == int.MinValue) ? from : Mathf.Clamp(startingValue, from, to);
        _curValueBuffer = _curValue.ToString();
    }

    public Dialog_SliderWithValue(
        string text,
        int from,
        int to,
        Action<int> confirmAction,
        int startingValue = int.MinValue,
        float roundTo = 1f,
        bool showNumericEntry = true,
        string unitLabel = null)
        : this(val => string.Format(text, val), from, to, confirmAction, startingValue, roundTo, showNumericEntry, unitLabel) { }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;
        string header = TextGetter(_curValue);
        float headerH = DrawUtil.GetTextHeight(header, inRect.width, GameFont.Small);
        var headerRect = new Rect(inRect.x, inRect.y, inRect.width, headerH);
        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(headerRect, header);
        Text.Anchor = TextAnchor.UpperLeft;

        float y = headerRect.yMax + DrawUtil.SliderGap;

        // Slider row
        var sliderRect = new Rect(inRect.x, y, inRect.width, 40f);

        int newValue = (int)Widgets.HorizontalSlider(
            sliderRect,
            _curValue,
            From,
            To,
            middleAlignment: true,
            label: _unitLabel,
            leftAlignedLabel: null,
            rightAlignedLabel: null,
            RoundTo);

        // Min/Max labels under slider
        GUI.color = ColoredText.SubtleGrayColor;
        Text.Font = GameFont.Tiny;

        var minRect = new Rect(inRect.x, sliderRect.yMax - 10f, inRect.width / 2f, Text.LineHeight);
        Widgets.Label(minRect, From.ToString());

        Text.Anchor = TextAnchor.UpperRight;
        var maxRect = new Rect(inRect.x + inRect.width / 2f, sliderRect.yMax - 10f, inRect.width / 2f, Text.LineHeight);
        Widgets.Label(maxRect, To.ToString());
        Text.Anchor = TextAnchor.UpperLeft;

        GUI.color = Color.white;
        Text.Font = GameFont.Small;

        // Optional numeric entry box
        y = minRect.yMax + DrawUtil.SliderGap;
        if (_showNumericEntry)
        {
            var entryRow = new Rect(inRect.x, y, inRect.width, 28f);
            newValue = DrawUtil.DrawIntInputRow(entryRow, newValue, ref _curValueBuffer, From, To);
        }

        // Apply newValue + keep buffer in sync when slider moves
        newValue = Mathf.Clamp(newValue, From, To);
        if (newValue != _curValue)
        {
            _curValue = newValue;
            _curValueBuffer = _curValue.ToString();
        }

        // Bottom buttons
        float buttonH = 30f;
        float gap = 10f;
        float halfW = (inRect.width - gap) / 2f;
        var cancelRect = new Rect(inRect.x, inRect.yMax - buttonH, halfW, buttonH);
        var okRect = new Rect(inRect.x + halfW + gap, inRect.yMax - buttonH, halfW, buttonH);

        if (Widgets.ButtonText(cancelRect, "CancelButton".Translate()))
            Close();

        if (Widgets.ButtonText(okRect, "OK".Translate()))
        {
            Close();
            _confirmAction(_curValue);
        }
    }
}
