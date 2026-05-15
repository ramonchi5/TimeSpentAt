using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

using LiveSplit.Model;
using LiveSplit.TimeFormatters;
using LiveSplit.UI;
using LiveSplit.UI.Components;

namespace TimeSpentAt.UI.Components;

public class TimeSpentAt : IComponent
{
    private const string Dash = "-";
    private const float HorizontalPadding = 5f;
    private const float SmallFontScale = 0.72f;
    private const float MinSmallFontPt = 7f;
    private static readonly PointF[] FixedBlurOffsets =
    {
        new(-1f, 0f),
        new(1f, 0f),
        new(0f, -1f),
        new(0f, 1f),
        new(0f, 0f)
    };
    private static readonly float[] FixedBlurWeights = { 0.26f, 0.26f, 0.26f, 0.26f, 1f };

    private readonly LiveSplitState _state;
    private readonly TimeSpentAtSettings _settings;

    private float _rowHeight = 30f;
    private string _labelText = "";
    private string _sumText = Dash;
    private string _comparison1Label = "";
    private string _comparison1Time = Dash;
    private string _comparison2Label = "";
    private string _comparison2Time = Dash;
    private int _matchCount;

    public TimeSpentAt(LiveSplitState state)
    {
        _state = state;
        _settings = new TimeSpentAtSettings(state);
        CalculateDisplayValues(state);
    }

    public float PaddingTop => 0f;

    public float PaddingLeft => 0f;

    public float PaddingBottom => 0f;

    public float PaddingRight => 0f;

    public IDictionary<string, Action> ContextMenuControls => null!;

    public float VerticalHeight => _rowHeight;

    public float MinimumWidth => 120f;

    public float HorizontalWidth => 300f;

    public float MinimumHeight => 20f;

    public string ComponentName
    {
        get
        {
            string name = _settings.InstanceName;
            return string.IsNullOrWhiteSpace(name)
                ? "Time Spent At"
                : "Time Spent At - " + name;
        }
    }

    public Control GetSettingsControl(LayoutMode mode)
    {
        _settings.RefreshComparisons();
        return _settings;
    }

    public void SetSettings(XmlNode settings)
    {
        _settings.SetSettings(settings);
        CalculateDisplayValues(_state);
    }

    public XmlNode GetSettings(XmlDocument document)
    {
        return _settings.GetSettings(document);
    }

    public int GetSettingsHashCode()
    {
        return _settings.GetSettingsHashCode();
    }

    public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
    {
        CalculateDisplayValues(state);
        invalidator?.Invalidate(0, 0, width, height);
    }

    public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
    {
        DrawRow(g, state, width, _rowHeight);
    }

    public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
    {
        DrawRow(g, state, HorizontalWidth, height);
    }

    public void Dispose()
    {
    }

    private void CalculateDisplayValues(LiveSplitState state)
    {
        IRun run = state.Run;
        TimingMethod method = state.CurrentTimingMethod;
        string searchText = NormalizeSearchText(_settings.SearchText);

        _matchCount = 0;
        _sumText = Dash;
        _comparison1Time = Dash;
        _comparison2Time = Dash;

        string label = _settings.LabelForDisplay();
        _labelText = FormatLabelWithCount(label, _matchCount);

        string comparison1 = string.Empty;
        string comparison2 = string.Empty;
        _comparison1Label = string.Empty;
        _comparison2Label = string.Empty;

        if (_settings.ShowComparisons)
        {
            comparison1 = ResolveComparisonChoice(state, run, _settings.Comparison1, "Personal Best");
            comparison2 = ResolveComparisonChoice(state, run, _settings.Comparison2, "Best Segments");
            _comparison1Label = AbbreviateComparison(comparison1);
            _comparison2Label = AbbreviateComparison(comparison2);
        }

        if (run == null || run.Count == 0 || string.IsNullOrEmpty(searchText))
            return;

        TimeSpentAtSum actual = CalculateActualSum(state, run, method, searchText);
        _matchCount = actual.Count;
        _sumText = FormatTime(actual.Sum, _settings.Accuracy);

        _labelText = FormatLabelWithCount(label, _matchCount);

        if (_settings.ShowComparisons)
        {
            _comparison1Time = FormatTime(CalculateComparisonSum(run, method, searchText, comparison1), _settings.Accuracy);
            _comparison2Time = FormatTime(CalculateComparisonSum(run, method, searchText, comparison2), _settings.Accuracy);
        }
    }

    private TimeSpentAtSum CalculateActualSum(LiveSplitState state, IRun run, TimingMethod method, string searchText)
    {
        TimeSpan sum = TimeSpan.Zero;
        int count = 0;
        int completedLimit = GetCompletedSegmentLimit(state, run);
        int activeIndex = GetActiveSegmentIndex(state, run);

        for (int i = 0; i < run.Count; i++)
        {
            if (!SegmentMatches(run[i].Name, searchText, _settings.MatchInsideWords))
                continue;

            TimeSpan? segmentTime = null;
            if (i < completedLimit)
                segmentTime = GetCompletedSegmentTime(run, i, method);
            else if (i == activeIndex)
                segmentTime = GetActiveSegmentTime(run, state, i, method);

            if (!segmentTime.HasValue)
                continue;

            sum += segmentTime.Value;
            count++;
        }

        return new TimeSpentAtSum(sum, count);
    }

    private TimeSpan? CalculateComparisonSum(IRun run, TimingMethod method, string searchText, string comparison)
    {
        if (!HasComparison(run, comparison))
            return null;

        TimeSpan sum = TimeSpan.Zero;
        bool hasAny = false;

        for (int i = 0; i < run.Count; i++)
        {
            if (!SegmentMatches(run[i].Name, searchText, _settings.MatchInsideWords))
                continue;

            TimeSpan? segmentTime = GetComparisonSegmentTime(run, i, comparison, method);
            if (!segmentTime.HasValue)
                continue;

            sum += segmentTime.Value;
            hasAny = true;
        }

        return hasAny ? sum : null;
    }

    private static int GetCompletedSegmentLimit(LiveSplitState state, IRun run)
    {
        return state.CurrentPhase switch
        {
            TimerPhase.Ended => run.Count,
            TimerPhase.Running or TimerPhase.Paused => Clamp(state.CurrentSplitIndex, 0, run.Count),
            _ => 0
        };
    }

    private static int GetActiveSegmentIndex(LiveSplitState state, IRun run)
    {
        if (state.CurrentPhase is not (TimerPhase.Running or TimerPhase.Paused))
            return -1;

        int index = state.CurrentSplitIndex;
        return index >= 0 && index < run.Count ? index : -1;
    }

    private static TimeSpan? GetCompletedSegmentTime(IRun run, int index, TimingMethod method)
    {
        if (index < 0 || index >= run.Count)
            return null;

        TimeSpan? endTime = run[index].SplitTime[method];
        if (!endTime.HasValue)
            return null;

        if (index == 0)
            return endTime;

        TimeSpan? startTime = run[index - 1].SplitTime[method];
        return startTime.HasValue ? endTime.Value - startTime.Value : null;
    }

    private static TimeSpan? GetActiveSegmentTime(IRun run, LiveSplitState state, int index, TimingMethod method)
    {
        TimeSpan? currentTime = state.CurrentTime[method];
        if (!currentTime.HasValue)
            return null;

        if (index == 0)
            return currentTime;

        TimeSpan? previousSplitTime = run[index - 1].SplitTime[method];
        return previousSplitTime.HasValue ? currentTime.Value - previousSplitTime.Value : null;
    }

    private static TimeSpan? GetComparisonSegmentTime(IRun run, int index, string comparison, TimingMethod method)
    {
        if (index < 0 || index >= run.Count)
            return null;

        TimeSpan? endTime = run[index].Comparisons[comparison][method];
        if (!endTime.HasValue)
            return null;

        if (index == 0)
            return endTime;

        TimeSpan? startTime = run[index - 1].Comparisons[comparison][method];
        return startTime.HasValue ? endTime.Value - startTime.Value : null;
    }

    private static bool SegmentMatches(string name, string searchText, bool matchInsideWords)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(searchText))
            return false;

        string normalizedName = NormalizeSegmentName(name);
        if (matchInsideWords)
            return normalizedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;

        int index = 0;
        while (index <= normalizedName.Length - searchText.Length)
        {
            int found = normalizedName.IndexOf(searchText, index, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
                return false;

            int before = found - 1;
            int after = found + searchText.Length;
            bool startsOnBoundary = before < 0 || !IsWordCharacter(normalizedName[before]);
            bool endsOnBoundary = after >= normalizedName.Length || !IsWordCharacter(normalizedName[after]);
            if (startsOnBoundary && endsOnBoundary)
                return true;

            index = found + 1;
        }

        return false;
    }

    private static string NormalizeSegmentName(string name)
    {
        string trimmed = name.Trim();
        int start = 0;
        while (start < trimmed.Length && trimmed[start] == '-')
        {
            start++;
            while (start < trimmed.Length && char.IsWhiteSpace(trimmed[start]))
                start++;
        }

        if (start > 0)
            trimmed = trimmed.Substring(start);

        return NormalizeSearchText(trimmed);
    }

    private static string NormalizeSearchText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        string[] parts = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }

    private static bool IsWordCharacter(char value)
    {
        return char.IsLetterOrDigit(value);
    }

    private static bool HasComparison(IRun run, string comparison)
    {
        if (run == null || string.IsNullOrEmpty(comparison))
            return false;

        try
        {
            foreach (string runComparison in run.Comparisons)
            {
                if (string.Equals(runComparison, comparison, StringComparison.Ordinal))
                    return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static string ResolveComparisonChoice(LiveSplitState state, IRun run, string comparison, string fallback)
    {
        string? resolved = comparison;
        if (string.Equals(comparison, TimeSpentAtSettings.CurrentComparisonChoice, StringComparison.Ordinal))
            resolved = state.CurrentComparison;

        if (HasComparison(run, resolved ?? string.Empty))
            return resolved ?? string.Empty;
        if (HasComparison(run, fallback))
            return fallback;

        return FirstComparisonName(run);
    }

    private static string FirstComparisonName(IRun run)
    {
        if (run == null)
            return string.Empty;

        try
        {
            foreach (string comparison in run.Comparisons)
                return comparison;
        }
        catch
        {
        }

        return string.Empty;
    }

    private static string AbbreviateComparison(string comparison)
    {
        return comparison switch
        {
            "Best Segments" => "Best",
            "Best Pace" => "Pace",
            "Personal Best" => "PB",
            "Average Segments" => "Avg",
            "Balanced PB" => "Bal",
            "Latest Run" => "Last",
            _ => comparison.Length > 8 ? comparison.Substring(0, 7) + "..." : comparison
        };
    }

    private static string FormatTime(TimeSpan? time, TimeAccuracy accuracy)
    {
        if (!time.HasValue || time.Value == TimeSpan.Zero)
            return Dash;

        TimeSpan value = time.Value;
        TimeSpan abs = value.Duration();
        string decimals = accuracy switch
        {
            TimeAccuracy.Hundredths => $".{abs.Milliseconds / 10:D2}",
            TimeAccuracy.Tenths => $".{abs.Milliseconds / 100:D1}",
            TimeAccuracy.Milliseconds => $".{abs.Milliseconds:D3}",
            _ => ""
        };

        string sign = value < TimeSpan.Zero ? "-" : "";
        int totalHours = (int)abs.TotalHours;
        int totalMinutes = (int)abs.TotalMinutes;

        if (totalHours >= 1)
            return $"{sign}{totalHours}:{abs.Minutes:D2}:{abs.Seconds:D2}{decimals}";
        if (totalMinutes >= 1)
            return $"{sign}{totalMinutes}:{abs.Seconds:D2}{decimals}";

        return $"{sign}{abs.Seconds}{decimals}";
    }

    private string FormatLabelWithCount(string label, int count)
    {
        return _settings.ShowCounter && count > 0
            ? $"{label} ({count})"
            : label;
    }

    private void DrawRow(Graphics g, LiveSplitState state, float width, float height)
    {
        CalculateDisplayValues(state);

        LiveSplit.Options.LayoutSettings layout = state.LayoutSettings;
        using Font labelFont = CreateColumnFont(layout.TextFont, _settings.LabelBold);
        using Font sumFont = CreateColumnFont(layout.TimesFont, _settings.SumBold);
        using Font comparisonLabelFont = CreateSmallFont(layout.TextFont, _settings.ComparisonLabelBold);
        using Font comparisonTimeFont = CreateSmallFont(layout.TimesFont, _settings.ComparisonTimeBold);

        float labelHeight = g.MeasureString("Ay", labelFont).Height;
        float sumHeight = g.MeasureString("88:88.88", sumFont).Height;
        float comparisonLineHeight = Math.Max(
            g.MeasureString("Ay", comparisonLabelFont).Height,
            g.MeasureString("88:88.88", comparisonTimeFont).Height);
        float comparisonHeight = _settings.ShowComparisons && _settings.ComparisonCount == 2
            ? comparisonLineHeight * 1.68f
            : comparisonLineHeight;

        _rowHeight = Math.Max(20f, Math.Max(Math.Max(labelHeight, sumHeight), comparisonHeight));
        float rowHeight = Math.Max(height, _rowHeight);

        Color layoutTextColor = layout.TextColor;
        Color labelColor = _settings.OverrideLabelColor ? _settings.LabelColor : layoutTextColor;
        Color sumColor = _settings.OverrideSumColor ? _settings.SumColor : layoutTextColor;
        Color comparisonLabelColor = _settings.OverrideComparisonLabelColor
            ? _settings.ComparisonLabelColor
            : layoutTextColor;
        Color comparisonTimeColor = _settings.OverrideComparisonTimeColor
            ? _settings.ComparisonTimeColor
            : layoutTextColor;

        float labelColumnWidth = _settings.ShowComparisons ? width * 0.42f : width * 0.58f;
        float comparisonColumnWidth = _settings.ShowComparisons ? width * 0.30f : 0f;
        float sumColumnWidth = Math.Max(0f, width - labelColumnWidth - comparisonColumnWidth);

        RectangleF labelRect = new(
            HorizontalPadding,
            Math.Max(0f, (rowHeight - labelHeight) / 2f),
            Math.Max(0f, labelColumnWidth - (HorizontalPadding * 2f)),
            labelHeight);

        RectangleF comparisonRect = new(
            labelColumnWidth,
            0f,
            Math.Max(0f, comparisonColumnWidth - HorizontalPadding),
            rowHeight);

        RectangleF sumRect = new(
            labelColumnWidth + comparisonColumnWidth,
            Math.Max(0f, (rowHeight - sumHeight) / 2f),
            Math.Max(0f, sumColumnWidth - HorizontalPadding),
            sumHeight);

        using StringFormat leftFormat = new()
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near,
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.EllipsisCharacter
        };
        using StringFormat rightFormat = new()
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Near,
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.EllipsisCharacter
        };

        if (_settings.ShowLabel)
            DrawTextWithEffectsClipped(g, _labelText, labelFont, labelColor, labelRect, leftFormat, layout);

        if (_settings.ShowComparisons)
        {
            DrawComparisonColumn(
                g,
                comparisonLabelFont,
                comparisonTimeFont,
                comparisonLabelColor,
                comparisonTimeColor,
                comparisonRect,
                comparisonLineHeight,
                leftFormat,
                rightFormat,
                layout);
        }

        DrawTextWithEffectsClipped(g, _sumText, sumFont, sumColor, sumRect, rightFormat, layout);
    }

    private void DrawComparisonColumn(
        Graphics g,
        Font labelFont,
        Font timeFont,
        Color labelColor,
        Color timeColor,
        RectangleF rect,
        float lineHeight,
        StringFormat leftFormat,
        StringFormat rightFormat,
        LiveSplit.Options.LayoutSettings layout)
    {
        if (rect.Width <= 0f || rect.Height <= 0f)
            return;

        bool twoLines = _settings.ComparisonCount == 2;
        float lineStep = lineHeight * 0.68f;
        float totalHeight = twoLines ? lineHeight + lineStep : lineHeight;
        float y1 = rect.Top + Math.Max(0f, (rect.Height - totalHeight) / 2f);
        float y2 = y1 + lineStep;

        DrawComparisonLine(
            g,
            _comparison1Label + ":",
            _comparison1Time,
            labelFont,
            timeFont,
            labelColor,
            timeColor,
            rect,
            y1,
            lineHeight,
            leftFormat,
            rightFormat,
            layout);

        if (twoLines)
        {
            DrawComparisonLine(
                g,
                _comparison2Label + ":",
                _comparison2Time,
                labelFont,
                timeFont,
                labelColor,
                timeColor,
                rect,
                y2,
                lineHeight,
                leftFormat,
                rightFormat,
                layout);
        }
    }

    private static void DrawComparisonLine(
        Graphics g,
        string label,
        string time,
        Font labelFont,
        Font timeFont,
        Color labelColor,
        Color timeColor,
        RectangleF rect,
        float y,
        float height,
        StringFormat leftFormat,
        StringFormat rightFormat,
        LiveSplit.Options.LayoutSettings layout)
    {
        float labelWidth = Math.Min(rect.Width * 0.48f, g.MeasureString(label, labelFont).Width + 4f);
        RectangleF labelRect = new(rect.Left, y, labelWidth, height);
        RectangleF timeRect = new(rect.Left + labelWidth, y, Math.Max(0f, rect.Width - labelWidth), height);

        DrawTextWithEffectsClipped(g, label, labelFont, labelColor, labelRect, leftFormat, layout);
        DrawTextWithEffectsClipped(g, time, timeFont, timeColor, timeRect, rightFormat, layout);
    }

    private static Font CreateColumnFont(Font baseFont, bool bold)
    {
        FontStyle style = bold
            ? baseFont.Style | FontStyle.Bold
            : baseFont.Style;

        try
        {
            return new Font(baseFont, style);
        }
        catch (ArgumentException)
        {
            return (Font)baseFont.Clone();
        }
    }

    private static Font CreateSmallFont(Font baseFont, bool bold)
    {
        FontStyle style = bold
            ? baseFont.Style | FontStyle.Bold
            : baseFont.Style;
        float size = Math.Max(MinSmallFontPt, baseFont.Size * SmallFontScale);

        try
        {
            return new Font(baseFont.FontFamily, size, style, baseFont.Unit);
        }
        catch (ArgumentException)
        {
            return CreateColumnFont(baseFont, bold);
        }
    }

    private static void DrawTextWithEffectsClipped(
        Graphics g,
        string text,
        Font font,
        Color textColor,
        RectangleF rect,
        StringFormat format,
        LiveSplit.Options.LayoutSettings settings)
    {
        if (string.IsNullOrEmpty(text) || rect.Width <= 0 || rect.Height <= 0)
            return;

        GraphicsState saved = g.Save();
        try
        {
            g.SetClip(rect);
            DrawTextWithEffects(g, text, font, textColor, rect, format, settings);
        }
        finally
        {
            g.Restore(saved);
        }
    }

    private static void DrawTextWithEffects(
        Graphics g,
        string text,
        Font font,
        Color textColor,
        RectangleF rect,
        StringFormat format,
        LiveSplit.Options.LayoutSettings settings)
    {
        if (string.IsNullOrEmpty(text) || font == null || rect.Width <= 0 || rect.Height <= 0)
            return;

        FancyTextEffects? fancyText = GetFancyTextEffects();
        bool overrideShadow = fancyText?.OverrideShadow == true;
        bool overrideOutline = fancyText?.OverrideOutline == true;
        bool hasGradient = fancyText?.HasGradient == true;

        bool hasShadow = overrideShadow
            ? fancyText!.ShadowEnabled
            : GetLayoutSetting(settings, "DropShadows", false);
        Color shadowColor = overrideShadow
            ? fancyText!.ShadowColor
            : GetLayoutSetting(settings, "ShadowsColor", Color.Transparent);
        Color outlineColor = overrideOutline
            ? fancyText!.OutlineColor
            : GetLayoutSetting(settings, "TextOutlineColor", Color.Transparent);

        SizeF measured = g.MeasureString(text, font);
        float x = rect.X;
        if (format.Alignment == StringAlignment.Far)
            x = rect.Right - measured.Width;
        else if (format.Alignment == StringAlignment.Center)
            x = rect.X + (rect.Width - measured.Width) / 2f;
        float y = rect.Y;

        using StringFormat nearFormat = new(format)
        {
            Alignment = StringAlignment.Near,
            Trimming = StringTrimming.None
        };
        nearFormat.FormatFlags |= StringFormatFlags.NoWrap;

        bool usePath = overrideShadow ||
                       overrideOutline ||
                       hasGradient ||
                       (g.TextRenderingHint == TextRenderingHint.AntiAlias && outlineColor.A > 0);

        if (usePath)
        {
            float fontSize = GetFontSize(g, font);
            float outlineSize = overrideOutline
                ? Math.Max(0f, fancyText!.OutlineSize)
                : GetOutlineSize(fontSize);
            RectangleF textBox = MakeTextBox(rect, measured, x, y);
            using GraphicsPath path = new();

            if (hasShadow && shadowColor.A > 0)
            {
                if (overrideShadow)
                {
                    DrawFancyShadow(g, text, font, fontSize, textBox, nearFormat, fancyText!);
                }
                else
                {
                    using SolidBrush shadowBrush = new(shadowColor);
                    path.AddString(text, font.FontFamily, (int)font.Style, fontSize, new RectangleF(x + 1f, y + 1f, 9999, 9999), nearFormat);
                    g.FillPath(shadowBrush, path);
                    path.Reset();
                    path.AddString(text, font.FontFamily, (int)font.Style, fontSize, new RectangleF(x + 2f, y + 2f, 9999, 9999), nearFormat);
                    g.FillPath(shadowBrush, path);
                    path.Reset();
                }
            }

            path.AddString(text, font.FontFamily, (int)font.Style, fontSize, new RectangleF(x, y, 9999, 9999), nearFormat);
            if (outlineColor.A > 0 && outlineSize > 0f)
            {
                using Pen outline = new(outlineColor, outlineSize) { LineJoin = LineJoin.Round };
                g.DrawPath(outline, path);
            }

            using Brush textBrush = CreateTextBrush(fancyText, textColor, textBox);
            g.FillPath(textBrush, path);
        }
        else
        {
            if (hasShadow && shadowColor.A > 0)
            {
                float shadowOffset = overrideShadow ? Math.Max(0f, fancyText!.ShadowSize) : 1f;
                using SolidBrush shadowBrush = new(shadowColor);
                g.DrawString(text, font, shadowBrush, x + shadowOffset, y + shadowOffset, nearFormat);
                if (!overrideShadow)
                    g.DrawString(text, font, shadowBrush, x + 2f, y + 2f, nearFormat);
            }

            using Brush textBrush = CreateTextBrush(fancyText, textColor, MakeTextBox(rect, measured, x, y));
            g.DrawString(text, font, textBrush, x, y, nearFormat);
        }
    }

    private static RectangleF MakeTextBox(RectangleF rect, SizeF measured, float x, float y)
    {
        float width = rect.Width > 0f && rect.Width < 4096f
            ? rect.Width
            : Math.Max(1f, measured.Width);
        float height = rect.Height > 0f && rect.Height < 4096f
            ? rect.Height
            : Math.Max(1f, measured.Height);
        return new RectangleF(x, y, width, height);
    }

    private static Brush CreateTextBrush(FancyTextEffects? effects, Color baseColor, RectangleF rect)
    {
        if (effects?.HasGradient != true)
            return new SolidBrush(baseColor);

        if (rect.Width <= 0f || rect.Height <= 0f)
            rect = new RectangleF(rect.X, rect.Y, Math.Max(1f, rect.Width), Math.Max(1f, rect.Height));

        PointF start;
        PointF end;
        switch (effects.GradientDirection)
        {
            case "Horizontal":
                start = new PointF(rect.Left, rect.Top);
                end = new PointF(rect.Right, rect.Top);
                break;
            case "DiagonalDown":
                start = new PointF(rect.Left, rect.Top);
                end = new PointF(rect.Right, rect.Bottom);
                break;
            case "DiagonalUp":
                start = new PointF(rect.Left, rect.Bottom);
                end = new PointF(rect.Right, rect.Top);
                break;
            default:
                start = new PointF(rect.Left, rect.Top);
                end = new PointF(rect.Left, rect.Bottom);
                break;
        }

        Color middle = effects.UseExistingColorMiddle ? baseColor : effects.GradientColor2;
        LinearGradientBrush brush = new(start, end, effects.GradientColor1, effects.GradientColor3)
        {
            InterpolationColors = new ColorBlend
            {
                Positions = new[] { 0f, 0.5f, 1f },
                Colors = new[] { effects.GradientColor1, middle, effects.GradientColor3 }
            }
        };
        return brush;
    }

    private static void DrawFancyShadow(
        Graphics g,
        string text,
        Font font,
        float fontSize,
        RectangleF textBox,
        StringFormat format,
        FancyTextEffects effects)
    {
        if (!effects.ShadowNormalEnabled && !effects.ShadowOutsideEnabled)
            DrawSimpleFancyShadow(g, text, font, textBox, format, effects);
        else
            DrawPathFancyShadow(g, text, font, fontSize, textBox, format, effects);
    }

    private static void DrawSimpleFancyShadow(
        Graphics g,
        string text,
        Font font,
        RectangleF textBox,
        StringFormat format,
        FancyTextEffects effects)
    {
        float offset = Math.Max(0f, effects.ShadowSize);
        using SolidBrush brush = new(effects.ShadowColor);
        g.DrawString(text, font, brush, textBox.X + offset, textBox.Y + offset, format);
    }

    private static void DrawPathFancyShadow(
        Graphics g,
        string text,
        Font font,
        float fontSize,
        RectangleF textBox,
        StringFormat format,
        FancyTextEffects effects)
    {
        GraphicsState saved = g.Save();
        try
        {
            if (effects.ShadowClipToRow)
            {
                g.ResetClip();
                RectangleF rowClip = new(-100000f, textBox.Y, 200000f, Math.Max(1f, textBox.Height));
                g.SetClip(rowClip, CombineMode.Replace);
            }

            using GraphicsPath textPath = new();
            textPath.AddString(text, font.FontFamily, (int)font.Style, fontSize, new RectangleF(textBox.X, textBox.Y, 9999f, 9999f), format);

            using GraphicsPath shadowPath = new();
            float expansion = GetShadowExpansion(effects, fontSize);
            float offset = Math.Max(0f, effects.ShadowSize);
            BuildShadowPath(textPath, shadowPath, expansion, offset);

            bool outsideOnly = !effects.ShadowNormalEnabled && effects.ShadowOutsideEnabled;
            if (effects.ShadowBlur > 0f)
                DrawFixedBlurShadow(g, shadowPath, textPath, effects.ShadowColor, outsideOnly, GetShadowMultiply(effects));
            else
            {
                using SolidBrush brush = new(GetStackedShadowColor(effects.ShadowColor, 1f, GetShadowMultiply(effects)));
                FillShadowPath(g, shadowPath, textPath, brush, outsideOnly);
            }
        }
        finally
        {
            g.Restore(saved);
        }
    }

    private static void DrawFixedBlurShadow(
        Graphics g,
        GraphicsPath shadowPath,
        GraphicsPath textPath,
        Color color,
        bool outsideOnly,
        int multiply)
    {
        for (int i = 0; i < FixedBlurOffsets.Length; i++)
        {
            Color sampleColor = GetStackedShadowColor(color, FixedBlurWeights[i], multiply);
            if (sampleColor.A <= 0)
                continue;

            using SolidBrush brush = new(sampleColor);
            PointF offset = FixedBlurOffsets[i];
            using GraphicsPath shiftedPath = (GraphicsPath)shadowPath.Clone();
            using Matrix matrix = new();
            matrix.Translate(offset.X, offset.Y);
            shiftedPath.Transform(matrix);
            FillShadowPath(g, shiftedPath, textPath, brush, outsideOnly);
        }
    }

    private static void BuildShadowPath(GraphicsPath textPath, GraphicsPath shadowPath, float expansion, float offset)
    {
        shadowPath.Reset();
        shadowPath.FillMode = FillMode.Winding;
        shadowPath.AddPath(textPath, false);

        if (expansion > 0.001f)
        {
            try
            {
                using GraphicsPath expandedPath = (GraphicsPath)textPath.Clone();
                using Pen expansionPen = new(Color.Black, expansion * 2f)
                {
                    LineJoin = LineJoin.Round,
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };

                expandedPath.FillMode = FillMode.Winding;
                expandedPath.Widen(expansionPen);
                shadowPath.AddPath(expandedPath, false);
                shadowPath.AddPath(textPath, false);
            }
            catch
            {
            }
        }

        if (Math.Abs(offset) > 0.001f)
        {
            using Matrix matrix = new();
            matrix.Translate(offset, offset);
            shadowPath.Transform(matrix);
        }
    }

    private static void FillShadowPath(
        Graphics g,
        GraphicsPath shadowPath,
        GraphicsPath textPath,
        Brush brush,
        bool outsideOnly)
    {
        if (!outsideOnly)
        {
            g.FillPath(brush, shadowPath);
            return;
        }

        using Region region = new(shadowPath);
        region.Exclude(textPath);
        g.FillRegion(brush, region);
    }

    private static int GetShadowMultiply(FancyTextEffects effects)
    {
        return Math.Max(1, Math.Min(1000, effects.ShadowMultiply));
    }

    private static float GetShadowExpansion(FancyTextEffects effects, float fontSize)
    {
        float percent = Math.Max(100f, Math.Min(500f, effects.ShadowSizePercent));
        return fontSize * ((percent - 100f) / 100f) * 0.3f;
    }

    private static Color GetStackedShadowColor(Color color, float weight, int multiply)
    {
        double alpha = Math.Max(0d, Math.Min(1d, (color.A / 255d) * weight));
        if (multiply > 1)
            alpha = 1d - Math.Pow(1d - alpha, multiply);

        int a = Math.Max(0, Math.Min(255, (int)Math.Round(alpha * 255d)));
        return Color.FromArgb(a, color.R, color.G, color.B);
    }

    private static float GetFontSize(Graphics g, Font font)
    {
        if (font.Unit == GraphicsUnit.Point)
            return font.Size * g.DpiY / 72;

        return font.Size;
    }

    private static float GetOutlineSize(float fontSize)
    {
        return 2.1f + (fontSize * 0.055f);
    }

    private static T GetLayoutSetting<T>(LiveSplit.Options.LayoutSettings settings, string propertyName, T fallback)
    {
        try
        {
            PropertyInfo? prop = settings.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
                return fallback;

            object? value = prop.GetValue(settings, null);
            if (value is T typedValue)
                return typedValue;
        }
        catch
        {
        }

        return fallback;
    }

    private static FancyTextEffects? GetFancyTextEffects()
    {
        try
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? runtime = assembly.GetType("LiveSplit.UI.Components.FancyTextRuntime", false);
                if (runtime == null)
                    continue;

                MethodInfo? method = runtime.GetMethod("GetCurrentEffects", BindingFlags.Public | BindingFlags.Static);
                object? effects = method?.Invoke(null, null);
                return effects == null ? null : FancyTextEffects.From(effects);
            }
        }
        catch
        {
        }

        return null;
    }

    private sealed class FancyTextEffects
    {
        public bool OverrideOutline { get; private set; }

        public Color OutlineColor { get; private set; }

        public float OutlineSize { get; private set; }

        public bool OverrideShadow { get; private set; }

        public bool ShadowEnabled { get; private set; }

        public bool ShadowNormalEnabled { get; private set; }

        public bool ShadowOutsideEnabled { get; private set; }

        public Color ShadowColor { get; private set; }

        public float ShadowSize { get; private set; }

        public int ShadowSizePercent { get; private set; }

        public float ShadowBlur { get; private set; }

        public int ShadowMultiply { get; private set; }

        public bool ShadowClipToRow { get; private set; }

        public bool HasGradient { get; private set; }

        public bool UseExistingColorMiddle { get; private set; }

        public Color GradientColor1 { get; private set; }

        public Color GradientColor2 { get; private set; }

        public Color GradientColor3 { get; private set; }

        public string GradientDirection { get; private set; } = "";

        public static FancyTextEffects From(object source)
        {
            return new FancyTextEffects
            {
                OverrideOutline = Read(source, "OverrideOutline", false),
                OutlineColor = Read(source, "OutlineColor", Color.Transparent),
                OutlineSize = Read(source, "OutlineSize", 0f),
                OverrideShadow = Read(source, "OverrideShadow", false),
                ShadowEnabled = Read(source, "ShadowEnabled", false),
                ShadowNormalEnabled = Read(source, "ShadowNormalEnabled", false),
                ShadowOutsideEnabled = Read(source, "ShadowOutsideEnabled", false),
                ShadowColor = Read(source, "ShadowColor", Color.Transparent),
                ShadowSize = Read(source, "ShadowSize", 1f),
                ShadowSizePercent = Read(source, "ShadowSizePercent", 100),
                ShadowBlur = Read(source, "ShadowBlur", 0f),
                ShadowMultiply = Read(source, "ShadowMultiply", 1),
                ShadowClipToRow = Read(source, "ShadowClipToRow", false),
                HasGradient = Read(source, "HasGradient", false),
                UseExistingColorMiddle = Read(source, "UseExistingColorMiddle", false),
                GradientColor1 = Read(source, "GradientColor1", Color.White),
                GradientColor2 = Read(source, "GradientColor2", Color.White),
                GradientColor3 = Read(source, "GradientColor3", Color.White),
                GradientDirection = ReadEnumName(source, "GradientDirection")
            };
        }

        private static T Read<T>(object source, string propertyName, T fallback)
        {
            try
            {
                PropertyInfo? property = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                object? value = property?.GetValue(source, null);
                return value is T typed ? typed : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static string ReadEnumName(object source, string propertyName)
        {
            try
            {
                PropertyInfo? property = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                return property?.GetValue(source, null)?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;

        return value;
    }

    private readonly struct TimeSpentAtSum
    {
        public TimeSpentAtSum(TimeSpan sum, int count)
        {
            Sum = sum;
            Count = count;
        }

        public TimeSpan Sum { get; }

        public int Count { get; }
    }
}
