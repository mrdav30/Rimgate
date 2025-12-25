using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Dialog_SliderWithValue : Window
{
    public Func<int, string> TextGetter;

    public int from;
    public int to;

    public float roundTo = 1f;
    public float extraBottomSpace;

    private readonly Action<int> _confirmAction;
    private int _curValue;

    // Optional: numeric input box
    private readonly bool _showNumericEntry;
    private string _curValueBuffer;

    // Optional: add suffix (e.g. "tiles")
    private readonly string _unitLabel;

    public override Vector2 InitialSize => new Vector2(360f, 170f + extraBottomSpace);
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
        this.TextGetter = textGetter;
        this.from = from;
        this.to = to;
        this._confirmAction = confirmAction;
        this.roundTo = roundTo;
        this._showNumericEntry = showNumericEntry;
        this._unitLabel = unitLabel;

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
        float headerH = Text.CalcHeight(header, inRect.width);
        var headerRect = new Rect(inRect.x, inRect.y, inRect.width, headerH);
        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(headerRect, header);
        Text.Anchor = TextAnchor.UpperLeft;

        float y = headerRect.yMax + 8f;

        // Slider row
        var sliderRect = new Rect(inRect.x, y, inRect.width, 30f);

        int newValue = (int)Widgets.HorizontalSlider(
            sliderRect,
            _curValue,
            from,
            to,
            middleAlignment: true,
            label: null,
            leftAlignedLabel: null,
            rightAlignedLabel: null,
            roundTo);

        // Min/Max labels under slider
        GUI.color = ColoredText.SubtleGrayColor;
        Text.Font = GameFont.Tiny;

        var minRect = new Rect(inRect.x, sliderRect.yMax - 10f, inRect.width / 2f, Text.LineHeight);
        Widgets.Label(minRect, from.ToString());

        Text.Anchor = TextAnchor.UpperRight;
        var maxRect = new Rect(inRect.x + inRect.width / 2f, sliderRect.yMax - 10f, inRect.width / 2f, Text.LineHeight);
        Widgets.Label(maxRect, to.ToString());
        Text.Anchor = TextAnchor.UpperLeft;

        GUI.color = Color.white;
        Text.Font = GameFont.Small;

        // Optional numeric entry box
        y = sliderRect.yMax + 10f;
        if (_showNumericEntry)
        {
            var entryRow = new Rect(inRect.x, y, inRect.width, 28f);

            // left label
            var labelRect = entryRow.LeftPart(0.35f);
            Widgets.Label(labelRect, "RG_SetTo".Translate());

            var fieldRect = entryRow.RightPart(0.60f);
            _curValueBuffer = Widgets.TextField(fieldRect, _curValueBuffer);

            if (int.TryParse(_curValueBuffer, out int parsed))
            {
                parsed = Mathf.Clamp(parsed, from, to);
                if (parsed != _curValue)
                {
                    newValue = parsed;
                }
            }

            y = entryRow.yMax + 8f;
        }

        // Apply newValue + keep buffer in sync when slider moves
        newValue = Mathf.Clamp(newValue, from, to);
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
