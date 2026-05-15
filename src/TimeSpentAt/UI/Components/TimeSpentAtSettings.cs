using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Xml;

using LiveSplit.Model;
using LiveSplit.TimeFormatters;
using LiveSplit.UI;

namespace TimeSpentAt.UI.Components;

public class TimeSpentAtSettings : UserControl
{
    public const string CurrentComparisonChoice = "Current Comparison";

    private readonly LiveSplitState _state;

    private TextBox _instanceNameBox = null!;
    private TextBox _searchTextBox = null!;
    private CheckBox _matchInsideWordsCheck = null!;
    private CheckBox _showLabelCheck = null!;
    private CheckBox _showCounterCheck = null!;
    private CheckBox _labelBoldCheck = null!;
    private TextBox _labelTextBox = null!;
    private CheckBox _overrideLabelColorCheck = null!;
    private Button _labelColorButton = null!;
    private CheckBox _showComparisonsCheck = null!;
    private ComboBox _comparison1Combo = null!;
    private ComboBox _comparison2Combo = null!;
    private ComboBox _comparisonCountCombo = null!;
    private CheckBox _comparisonLabelBoldCheck = null!;
    private CheckBox _comparisonTimeBoldCheck = null!;
    private CheckBox _overrideComparisonLabelColorCheck = null!;
    private Button _comparisonLabelColorButton = null!;
    private CheckBox _overrideComparisonTimeColorCheck = null!;
    private Button _comparisonTimeColorButton = null!;
    private CheckBox _sumBoldCheck = null!;
    private CheckBox _overrideSumColorCheck = null!;
    private Button _sumColorButton = null!;
    private RadioButton _secondsRadio = null!;
    private RadioButton _tenthsRadio = null!;
    private RadioButton _hundredthsRadio = null!;
    private RadioButton _millisecondsRadio = null!;

    public TimeSpentAtSettings(LiveSplitState state)
    {
        _state = state;

        InstanceName = string.Empty;
        SearchText = string.Empty;
        MatchInsideWords = false;

        ShowLabel = true;
        LabelText = string.Empty;
        ShowCounter = true;
        LabelBold = false;
        OverrideLabelColor = false;
        LabelColor = Color.White;

        ShowComparisons = false;
        Comparison1 = "Personal Best";
        Comparison2 = "Best Segments";
        ComparisonCount = 2;
        ComparisonLabelBold = false;
        ComparisonTimeBold = false;
        OverrideComparisonLabelColor = false;
        ComparisonLabelColor = Color.White;
        OverrideComparisonTimeColor = false;
        ComparisonTimeColor = Color.White;

        SumBold = false;
        OverrideSumColor = false;
        SumColor = Color.White;
        Accuracy = TimeAccuracy.Hundredths;

        BuildUI();
        UpdateControlsFromSettings();
    }

    public string InstanceName { get; private set; }

    public string SearchText { get; private set; }

    public bool MatchInsideWords { get; private set; }

    public bool ShowLabel { get; private set; }

    public string LabelText { get; private set; }

    public bool ShowCounter { get; private set; }

    public bool LabelBold { get; private set; }

    public bool OverrideLabelColor { get; private set; }

    public Color LabelColor { get; private set; }

    public bool ShowComparisons { get; private set; }

    public string Comparison1 { get; private set; }

    public string Comparison2 { get; private set; }

    public int ComparisonCount { get; private set; }

    public bool ComparisonLabelBold { get; private set; }

    public bool ComparisonTimeBold { get; private set; }

    public bool OverrideComparisonLabelColor { get; private set; }

    public Color ComparisonLabelColor { get; private set; }

    public bool OverrideComparisonTimeColor { get; private set; }

    public Color ComparisonTimeColor { get; private set; }

    public bool SumBold { get; private set; }

    public bool OverrideSumColor { get; private set; }

    public Color SumColor { get; private set; }

    public TimeAccuracy Accuracy { get; private set; }

    public string LabelForDisplay()
    {
        if (!string.IsNullOrWhiteSpace(LabelText))
            return CollapseWhitespace(LabelText);

        if (!string.IsNullOrWhiteSpace(SearchText))
            return CollapseWhitespace(SearchText);

        return "Time Spent At";
    }

    public void RefreshComparisons()
    {
        PopulateComparisonCombo(_comparison1Combo, Comparison1);
        PopulateComparisonCombo(_comparison2Combo, Comparison2);
    }

    private void BuildUI()
    {
        SuspendLayout();
        Controls.Clear();

        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        Dock = DockStyle.Fill;
        Padding = new Padding(7);
        Size = new Size(476, 520);

        FlowLayoutPanel flow = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Dock = DockStyle.Top,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        flow.Controls.Add(MakeSection("Search", BuildSearchSection()));
        flow.Controls.Add(MakeSection("Label", BuildLabelSection()));
        flow.Controls.Add(MakeSection("Comparisons", BuildComparisonSection()));
        flow.Controls.Add(MakeSection("Sum", BuildSumSection()));
        flow.Controls.Add(MakeSection("Accuracy", BuildAccuracySection()));

        Controls.Add(flow);
        ResumeLayout(false);
        PerformLayout();
    }

    private Control BuildSearchSection()
    {
        TableLayoutPanel table = MakeGrid(3);

        table.Controls.Add(MakeLabel("Instance:"), 0, 0);
        _instanceNameBox = MakeTextBox(InstanceName, 80);
        _instanceNameBox.TextChanged += (_, _) => InstanceName = _instanceNameBox.Text;
        table.SetColumnSpan(_instanceNameBox, 3);
        table.Controls.Add(_instanceNameBox, 1, 0);

        table.Controls.Add(MakeLabel("Search:"), 0, 1);
        _searchTextBox = MakeTextBox(SearchText, 120);
        _searchTextBox.TextChanged += (_, _) =>
        {
            SearchText = _searchTextBox.Text;
            UpdateLabelPlaceholder();
        };
        table.SetColumnSpan(_searchTextBox, 3);
        table.Controls.Add(_searchTextBox, 1, 1);

        _matchInsideWordsCheck = MakeCheck("Match inside longer words", MatchInsideWords);
        _matchInsideWordsCheck.CheckedChanged += (_, _) =>
            MatchInsideWords = _matchInsideWordsCheck.Checked;
        table.SetColumnSpan(_matchInsideWordsCheck, 4);
        table.Controls.Add(_matchInsideWordsCheck, 0, 2);

        return table;
    }

    private Control BuildLabelSection()
    {
        TableLayoutPanel table = MakeGrid(3);

        _showLabelCheck = MakeCheck("Show label", ShowLabel);
        _showLabelCheck.CheckedChanged += (_, _) =>
        {
            ShowLabel = _showLabelCheck.Checked;
            UpdateLabelControlStates();
        };
        table.Controls.Add(_showLabelCheck, 0, 0);

        _showCounterCheck = MakeCheck("Show count", ShowCounter);
        _showCounterCheck.CheckedChanged += (_, _) => ShowCounter = _showCounterCheck.Checked;
        table.Controls.Add(_showCounterCheck, 1, 0);

        _labelBoldCheck = MakeCheck("Bold", LabelBold);
        _labelBoldCheck.CheckedChanged += (_, _) => LabelBold = _labelBoldCheck.Checked;
        table.Controls.Add(_labelBoldCheck, 2, 0);

        table.Controls.Add(MakeLabel("Text:"), 0, 1);
        _labelTextBox = MakeTextBox(LabelText, 120);
        _labelTextBox.TextChanged += (_, _) => LabelText = _labelTextBox.Text;
        table.SetColumnSpan(_labelTextBox, 3);
        table.Controls.Add(_labelTextBox, 1, 1);

        _overrideLabelColorCheck = MakeCheck("Custom color", OverrideLabelColor);
        _overrideLabelColorCheck.CheckedChanged += (_, _) =>
        {
            OverrideLabelColor = _overrideLabelColorCheck.Checked;
            UpdateLabelControlStates();
        };
        table.Controls.Add(_overrideLabelColorCheck, 0, 2);

        _labelColorButton = MakeColorButton(LabelColor, color => LabelColor = color);
        table.Controls.Add(_labelColorButton, 1, 2);

        return table;
    }

    private Control BuildComparisonSection()
    {
        TableLayoutPanel table = MakeGrid(5);

        _showComparisonsCheck = MakeCheck("Show comparisons", ShowComparisons);
        _showComparisonsCheck.CheckedChanged += (_, _) =>
        {
            ShowComparisons = _showComparisonsCheck.Checked;
            UpdateComparisonControlStates();
        };
        table.SetColumnSpan(_showComparisonsCheck, 2);
        table.Controls.Add(_showComparisonsCheck, 0, 0);

        table.Controls.Add(MakeLabel("Lines:"), 2, 0);
        _comparisonCountCombo = MakeCombo("1", "2");
        _comparisonCountCombo.SelectedIndexChanged += (_, _) =>
            ComparisonCount = _comparisonCountCombo.SelectedIndex == 0 ? 1 : 2;
        table.Controls.Add(_comparisonCountCombo, 3, 0);

        table.Controls.Add(MakeLabel("Comp 1:"), 0, 1);
        _comparison1Combo = MakeCombo();
        _comparison1Combo.SelectedIndexChanged += (_, _) =>
        {
            if (_comparison1Combo.SelectedItem != null)
                Comparison1 = _comparison1Combo.SelectedItem.ToString();
        };
        table.Controls.Add(_comparison1Combo, 1, 1);

        table.Controls.Add(MakeLabel("Comp 2:"), 2, 1);
        _comparison2Combo = MakeCombo();
        _comparison2Combo.SelectedIndexChanged += (_, _) =>
        {
            if (_comparison2Combo.SelectedItem != null)
                Comparison2 = _comparison2Combo.SelectedItem.ToString();
        };
        table.Controls.Add(_comparison2Combo, 3, 1);

        _comparisonLabelBoldCheck = MakeCheck("Bold labels", ComparisonLabelBold);
        _comparisonLabelBoldCheck.CheckedChanged += (_, _) =>
            ComparisonLabelBold = _comparisonLabelBoldCheck.Checked;
        table.Controls.Add(_comparisonLabelBoldCheck, 0, 2);

        _comparisonTimeBoldCheck = MakeCheck("Bold times", ComparisonTimeBold);
        _comparisonTimeBoldCheck.CheckedChanged += (_, _) =>
            ComparisonTimeBold = _comparisonTimeBoldCheck.Checked;
        table.Controls.Add(_comparisonTimeBoldCheck, 1, 2);

        _overrideComparisonLabelColorCheck = MakeCheck("Label color", OverrideComparisonLabelColor);
        _overrideComparisonLabelColorCheck.CheckedChanged += (_, _) =>
        {
            OverrideComparisonLabelColor = _overrideComparisonLabelColorCheck.Checked;
            UpdateComparisonControlStates();
        };
        table.Controls.Add(_overrideComparisonLabelColorCheck, 0, 3);

        _comparisonLabelColorButton = MakeColorButton(ComparisonLabelColor, color => ComparisonLabelColor = color);
        table.Controls.Add(_comparisonLabelColorButton, 1, 3);

        _overrideComparisonTimeColorCheck = MakeCheck("Time color", OverrideComparisonTimeColor);
        _overrideComparisonTimeColorCheck.CheckedChanged += (_, _) =>
        {
            OverrideComparisonTimeColor = _overrideComparisonTimeColorCheck.Checked;
            UpdateComparisonControlStates();
        };
        table.Controls.Add(_overrideComparisonTimeColorCheck, 2, 3);

        _comparisonTimeColorButton = MakeColorButton(ComparisonTimeColor, color => ComparisonTimeColor = color);
        table.Controls.Add(_comparisonTimeColorButton, 3, 3);

        Label hint = MakeLabel("Comp 1 and Comp 2 are summed over every matching segment.");
        table.SetColumnSpan(hint, 4);
        table.Controls.Add(hint, 0, 4);

        return table;
    }

    private Control BuildSumSection()
    {
        TableLayoutPanel table = MakeGrid(2);

        _sumBoldCheck = MakeCheck("Bold sum", SumBold);
        _sumBoldCheck.CheckedChanged += (_, _) => SumBold = _sumBoldCheck.Checked;
        table.Controls.Add(_sumBoldCheck, 0, 0);

        _overrideSumColorCheck = MakeCheck("Custom color", OverrideSumColor);
        _overrideSumColorCheck.CheckedChanged += (_, _) =>
        {
            OverrideSumColor = _overrideSumColorCheck.Checked;
            UpdateSumControlStates();
        };
        table.Controls.Add(_overrideSumColorCheck, 0, 1);

        _sumColorButton = MakeColorButton(SumColor, color => SumColor = color);
        table.Controls.Add(_sumColorButton, 1, 1);

        return table;
    }

    private Control BuildAccuracySection()
    {
        FlowLayoutPanel flow = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        _secondsRadio = MakeRadio("Seconds");
        _tenthsRadio = MakeRadio("Tenths");
        _hundredthsRadio = MakeRadio("Hundredths");
        _millisecondsRadio = MakeRadio("Milliseconds");

        _secondsRadio.CheckedChanged += (_, _) => { if (_secondsRadio.Checked) Accuracy = TimeAccuracy.Seconds; };
        _tenthsRadio.CheckedChanged += (_, _) => { if (_tenthsRadio.Checked) Accuracy = TimeAccuracy.Tenths; };
        _hundredthsRadio.CheckedChanged += (_, _) => { if (_hundredthsRadio.Checked) Accuracy = TimeAccuracy.Hundredths; };
        _millisecondsRadio.CheckedChanged += (_, _) => { if (_millisecondsRadio.Checked) Accuracy = TimeAccuracy.Milliseconds; };

        flow.Controls.Add(_secondsRadio);
        flow.Controls.Add(_tenthsRadio);
        flow.Controls.Add(_hundredthsRadio);
        flow.Controls.Add(_millisecondsRadio);

        return flow;
    }

    private static GroupBox MakeSection(string title, Control content)
    {
        const int SectionWidth = 440;
        int contentWidth = SectionWidth - 18;
        Size preferred = content.GetPreferredSize(new Size(contentWidth, 0));
        content.Location = new Point(8, 19);
        content.Size = new Size(contentWidth, preferred.Height);

        GroupBox group = new()
        {
            Text = title,
            Margin = new Padding(0, 0, 0, 6),
            Padding = new Padding(6),
            Size = new Size(SectionWidth, Math.Max(48, preferred.Height + 30))
        };
        group.Controls.Add(content);
        return group;
    }

    private static TableLayoutPanel MakeGrid(int rows)
    {
        TableLayoutPanel table = new()
        {
            AutoSize = true,
            ColumnCount = 4,
            RowCount = rows,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112f));
        for (int i = 0; i < rows; i++)
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));

        return table;
    }

    private static Label MakeLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static TextBox MakeTextBox(string text, int maxLength)
    {
        return new TextBox
        {
            Text = text,
            MaxLength = maxLength,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 6, 0)
        };
    }

    private static CheckBox MakeCheck(string text, bool isChecked)
    {
        return new CheckBox
        {
            Text = text,
            Checked = isChecked,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 4, 8, 0)
        };
    }

    private static RadioButton MakeRadio(string text)
    {
        return new RadioButton
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(0, 0, 8, 0)
        };
    }

    private static ComboBox MakeCombo(params string[] items)
    {
        ComboBox combo = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 106,
            Margin = new Padding(0, 2, 6, 0)
        };

        if (items.Length > 0)
            combo.Items.AddRange(items);

        return combo;
    }

    private Button MakeColorButton(Color initial, Action<Color> setter)
    {
        Button button = new()
        {
            BackColor = initial,
            FlatStyle = FlatStyle.Popup,
            UseVisualStyleBackColor = false,
            Width = 23,
            Height = 23,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 1, 8, 0)
        };
        button.Click += (_, _) =>
        {
            SettingsHelper.ColorButtonClick(button, this);
            setter(button.BackColor);
        };
        return button;
    }

    private void UpdateControlsFromSettings()
    {
        _instanceNameBox.Text = InstanceName;
        _searchTextBox.Text = SearchText;
        _matchInsideWordsCheck.Checked = MatchInsideWords;

        _showLabelCheck.Checked = ShowLabel;
        _showCounterCheck.Checked = ShowCounter;
        _labelBoldCheck.Checked = LabelBold;
        _labelTextBox.Text = LabelText;
        _overrideLabelColorCheck.Checked = OverrideLabelColor;
        _labelColorButton.BackColor = LabelColor;

        _showComparisonsCheck.Checked = ShowComparisons;
        _comparisonCountCombo.SelectedIndex = ComparisonCount == 1 ? 0 : 1;
        RefreshComparisons();
        _comparisonLabelBoldCheck.Checked = ComparisonLabelBold;
        _comparisonTimeBoldCheck.Checked = ComparisonTimeBold;
        _overrideComparisonLabelColorCheck.Checked = OverrideComparisonLabelColor;
        _comparisonLabelColorButton.BackColor = ComparisonLabelColor;
        _overrideComparisonTimeColorCheck.Checked = OverrideComparisonTimeColor;
        _comparisonTimeColorButton.BackColor = ComparisonTimeColor;

        _sumBoldCheck.Checked = SumBold;
        _overrideSumColorCheck.Checked = OverrideSumColor;
        _sumColorButton.BackColor = SumColor;

        _secondsRadio.Checked = Accuracy == TimeAccuracy.Seconds;
        _tenthsRadio.Checked = Accuracy == TimeAccuracy.Tenths;
        _hundredthsRadio.Checked = Accuracy == TimeAccuracy.Hundredths;
        _millisecondsRadio.Checked = Accuracy == TimeAccuracy.Milliseconds;

        UpdateLabelPlaceholder();
        UpdateLabelControlStates();
        UpdateComparisonControlStates();
        UpdateSumControlStates();
    }

    private void UpdateLabelPlaceholder()
    {
        // .NET Framework WinForms does not support TextBox placeholder text.
        // The display fallback is still handled by LabelForDisplay().
    }

    private void UpdateLabelControlStates()
    {
        if (_showCounterCheck != null)
            _showCounterCheck.Enabled = ShowLabel;
        if (_labelBoldCheck != null)
            _labelBoldCheck.Enabled = ShowLabel;
        if (_labelTextBox != null)
            _labelTextBox.Enabled = ShowLabel;
        if (_overrideLabelColorCheck != null)
            _overrideLabelColorCheck.Enabled = ShowLabel;
        if (_labelColorButton != null)
            _labelColorButton.Enabled = ShowLabel && OverrideLabelColor;
    }

    private void UpdateComparisonControlStates()
    {
        bool enabled = ShowComparisons;

        if (_comparisonCountCombo != null)
            _comparisonCountCombo.Enabled = enabled;
        if (_comparison1Combo != null)
            _comparison1Combo.Enabled = enabled;
        if (_comparison2Combo != null)
            _comparison2Combo.Enabled = enabled && ComparisonCount == 2;
        if (_comparisonLabelBoldCheck != null)
            _comparisonLabelBoldCheck.Enabled = enabled;
        if (_comparisonTimeBoldCheck != null)
            _comparisonTimeBoldCheck.Enabled = enabled;
        if (_overrideComparisonLabelColorCheck != null)
            _overrideComparisonLabelColorCheck.Enabled = enabled;
        if (_comparisonLabelColorButton != null)
            _comparisonLabelColorButton.Enabled = enabled && OverrideComparisonLabelColor;
        if (_overrideComparisonTimeColorCheck != null)
            _overrideComparisonTimeColorCheck.Enabled = enabled;
        if (_comparisonTimeColorButton != null)
            _comparisonTimeColorButton.Enabled = enabled && OverrideComparisonTimeColor;
    }

    private void UpdateSumControlStates()
    {
        if (_sumColorButton != null)
            _sumColorButton.Enabled = OverrideSumColor;
    }

    private void PopulateComparisonCombo(ComboBox combo, string selected)
    {
        combo.Items.Clear();
        combo.Items.Add(CurrentComparisonChoice);

        if (_state?.Run != null)
        {
            foreach (string comparison in _state.Run.Comparisons)
            {
                if (!string.Equals(comparison, CurrentComparisonChoice, StringComparison.Ordinal))
                    combo.Items.Add(comparison);
            }
        }

        if (!string.IsNullOrEmpty(selected) && combo.Items.Contains(selected))
            combo.SelectedItem = selected;
        else if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
    }

    public void SetSettings(XmlNode node)
    {
        if (node is not XmlElement element)
            return;

        InstanceName = ReadString(element, "InstanceName", InstanceName);
        SearchText = ReadString(element, "SearchText", SearchText);
        MatchInsideWords = ReadBool(element, "MatchInsideWords", MatchInsideWords);

        ShowLabel = ReadBool(element, "ShowLabel", ShowLabel);
        LabelText = ReadString(element, "LabelText", LabelText);
        ShowCounter = ReadBool(element, "ShowCounter", ShowCounter);
        LabelBold = ReadBool(element, "LabelBold", LabelBold);
        OverrideLabelColor = ReadBool(element, "OverrideLabelColor", OverrideLabelColor);
        LabelColor = ReadColor(element, "LabelColor", LabelColor);

        ShowComparisons = ReadBool(element, "ShowComparisons", ShowComparisons);
        Comparison1 = ReadString(element, "Comparison1", Comparison1);
        Comparison2 = ReadString(element, "Comparison2", Comparison2);
        ComparisonCount = Clamp(ReadInt(element, "ComparisonCount", ComparisonCount), 1, 2);
        ComparisonLabelBold = ReadBool(element, "ComparisonLabelBold", ComparisonLabelBold);
        ComparisonTimeBold = ReadBool(element, "ComparisonTimeBold", ComparisonTimeBold);
        OverrideComparisonLabelColor = ReadBool(element, "OverrideComparisonLabelColor", OverrideComparisonLabelColor);
        ComparisonLabelColor = ReadColor(element, "ComparisonLabelColor", ComparisonLabelColor);
        OverrideComparisonTimeColor = ReadBool(element, "OverrideComparisonTimeColor", OverrideComparisonTimeColor);
        ComparisonTimeColor = ReadColor(element, "ComparisonTimeColor", ComparisonTimeColor);

        SumBold = ReadBool(element, "SumBold", SumBold);
        OverrideSumColor = ReadBool(element, "OverrideSumColor", OverrideSumColor);
        SumColor = ReadColor(element, "SumColor", SumColor);
        Accuracy = ReadEnum(element, "Accuracy", Accuracy);

        UpdateControlsFromSettings();
    }

    public XmlNode GetSettings(XmlDocument document)
    {
        XmlElement parent = document.CreateElement("Settings");
        CreateSettingsNode(document, parent);
        return parent;
    }

    public int GetSettingsHashCode()
    {
        return CreateSettingsNode(null!, null!);
    }

    private int CreateSettingsNode(XmlDocument document, XmlElement parent)
    {
        return SettingsHelper.CreateSetting(document, parent, "Version", "1.0") ^
               SettingsHelper.CreateSetting(document, parent, "InstanceName", InstanceName) ^
               SettingsHelper.CreateSetting(document, parent, "SearchText", SearchText) ^
               SettingsHelper.CreateSetting(document, parent, "MatchInsideWords", MatchInsideWords) ^
               SettingsHelper.CreateSetting(document, parent, "ShowLabel", ShowLabel) ^
               SettingsHelper.CreateSetting(document, parent, "LabelText", LabelText) ^
               SettingsHelper.CreateSetting(document, parent, "ShowCounter", ShowCounter) ^
               SettingsHelper.CreateSetting(document, parent, "LabelBold", LabelBold) ^
               SettingsHelper.CreateSetting(document, parent, "OverrideLabelColor", OverrideLabelColor) ^
               SettingsHelper.CreateSetting(document, parent, "LabelColor", LabelColor) ^
               SettingsHelper.CreateSetting(document, parent, "ShowComparisons", ShowComparisons) ^
               SettingsHelper.CreateSetting(document, parent, "Comparison1", Comparison1) ^
               SettingsHelper.CreateSetting(document, parent, "Comparison2", Comparison2) ^
               SettingsHelper.CreateSetting(document, parent, "ComparisonCount", ComparisonCount) ^
               SettingsHelper.CreateSetting(document, parent, "ComparisonLabelBold", ComparisonLabelBold) ^
               SettingsHelper.CreateSetting(document, parent, "ComparisonTimeBold", ComparisonTimeBold) ^
               SettingsHelper.CreateSetting(document, parent, "OverrideComparisonLabelColor", OverrideComparisonLabelColor) ^
               SettingsHelper.CreateSetting(document, parent, "ComparisonLabelColor", ComparisonLabelColor) ^
               SettingsHelper.CreateSetting(document, parent, "OverrideComparisonTimeColor", OverrideComparisonTimeColor) ^
               SettingsHelper.CreateSetting(document, parent, "ComparisonTimeColor", ComparisonTimeColor) ^
               SettingsHelper.CreateSetting(document, parent, "SumBold", SumBold) ^
               SettingsHelper.CreateSetting(document, parent, "OverrideSumColor", OverrideSumColor) ^
               SettingsHelper.CreateSetting(document, parent, "SumColor", SumColor) ^
               SettingsHelper.CreateSetting(document, parent, "Accuracy", Accuracy);
    }

    private static string CollapseWhitespace(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        string[] parts = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }

    private static string ReadString(XmlElement element, string name, string fallback)
    {
        XmlElement? node = element[name];
        return node == null ? fallback : node.InnerText;
    }

    private static bool ReadBool(XmlElement element, string name, bool fallback)
    {
        XmlElement? node = element[name];
        return node == null ? fallback : SettingsHelper.ParseBool(node, fallback);
    }

    private static int ReadInt(XmlElement element, string name, int fallback)
    {
        string? text = element[name]?.InnerText;
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : fallback;
    }

    private static Color ReadColor(XmlElement element, string name, Color fallback)
    {
        XmlElement? node = element[name];
        return node == null ? fallback : SettingsHelper.ParseColor(node);
    }

    private static T ReadEnum<T>(XmlElement element, string name, T fallback) where T : struct
    {
        string? text = element[name]?.InnerText;
        return Enum.TryParse(text, out T value) ? value : fallback;
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        if (value < minimum)
            return minimum;
        if (value > maximum)
            return maximum;

        return value;
    }
}
