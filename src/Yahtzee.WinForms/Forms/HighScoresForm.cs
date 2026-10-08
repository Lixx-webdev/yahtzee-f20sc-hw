using System.Data.Common;
using Yahtzee.Core.Data;

namespace Yahtzee.WinForms.Forms;

/// <summary>
/// High-score tables from the games history: the five best scores overall, and the five best scores of
/// a chosen player, each with the date it was achieved.
/// </summary>
internal sealed class HighScoresForm : Form
{
    private const int TableSize = 5;

    private readonly IGameHistoryRepository _history;
    private readonly ListView _overallList = CreateScoreList();
    private readonly ListView _playerList = CreateScoreList();
    private readonly ComboBox _playerBox = new()
    {
        Width = 220,
        DropDownStyle = ComboBoxStyle.DropDown, // pick a known name or type one
        AutoCompleteMode = AutoCompleteMode.SuggestAppend,
        AutoCompleteSource = AutoCompleteSource.ListItems,
    };
    private readonly Label _messageLabel = new() { AutoSize = true, ForeColor = Color.DimGray };

    public HighScoresForm(IGameHistoryRepository history)
    {
        _history = history;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "High Scores";
        Font = UiTheme.Body;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };
        layout.Controls.Add(Heading("Top 5 scores (all players)"));
        layout.Controls.Add(_overallList);
        layout.Controls.Add(Heading("Top 5 scores of a player"));

        var showButton = new Button { Text = "Show", AutoSize = true, MinimumSize = new Size(80, 30) };
        showButton.Click += async (_, _) => await LoadPlayerScoresAsync();
        _playerBox.SelectedIndexChanged += async (_, _) => await LoadPlayerScoresAsync();
        var playerRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 6) };
        playerRow.Controls.AddRange([new Label { Text = "Player:", AutoSize = true, Margin = new Padding(3, 6, 3, 3) },
            _playerBox, showButton]);
        layout.Controls.Add(playerRow);
        layout.Controls.Add(_playerList);
        layout.Controls.Add(_messageLabel);

        var close = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            MinimumSize = new Size(90, 32),
            Anchor = AnchorStyles.Right,
            Margin = new Padding(3, 12, 3, 3),
        };
        layout.Controls.Add(close);

        Controls.Add(layout);
        AcceptButton = showButton; // Enter in the name box shows that player's scores
        CancelButton = close;
        ResumeLayout(false);
        PerformLayout();

        Load += async (_, _) =>
        {
            SizeColumns(_overallList);
            SizeColumns(_playerList);
            await LoadOverallAsync();
        };
    }

    private static Label Heading(string text) =>
        new() { Text = text, Font = UiTheme.Bold, AutoSize = true, Margin = new Padding(3, 10, 3, 4) };

    private static ListView CreateScoreList()
    {
        var list = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            Size = new Size(520, 40 + 26 * TableSize),
            MultiSelect = false,
        };
        list.Columns.Add("#");
        list.Columns.Add("Player");
        list.Columns.Add("Score", -2, HorizontalAlignment.Right);
        list.Columns.Add("Date", -2);
        return list;
    }

    /// <summary>Pixel column widths are not DPI-scaled, so share out the list's real width once it is known.</summary>
    private static void SizeColumns(ListView list)
    {
        int width = list.ClientSize.Width;
        int[] percent = [8, 42, 16];
        for (int i = 0; i < percent.Length; i++)
            list.Columns[i].Width = width * percent[i] / 100;
        list.Columns[3].Width = width - list.Columns.Cast<ColumnHeader>().Take(3).Sum(column => column.Width);
    }

    private async Task LoadOverallAsync()
    {
        try
        {
            var names = await _history.GetPlayerNamesAsync();
            _playerBox.Items.Clear();
            _playerBox.Items.AddRange([.. names]);

            var scores = await _history.GetTopScoresAsync(TableSize);
            Fill(_overallList, scores);
            _messageLabel.Text = scores.Count == 0
                ? "No completed games yet. Finish a game and its scores will appear here."
                : "";
        }
        catch (Exception ex) when (ex is DbException or IOException or UnauthorizedAccessException)
        {
            _messageLabel.Text = $"The games history could not be read: {ex.Message}";
        }
    }

    private async Task LoadPlayerScoresAsync()
    {
        string name = _playerBox.Text.Trim();
        if (name.Length == 0)
            return;
        try
        {
            var scores = await _history.GetTopScoresForPlayerAsync(name, TableSize);
            Fill(_playerList, scores);
            _messageLabel.Text = scores.Count == 0 ? $"No completed games found for \"{name}\"." : "";
        }
        catch (Exception ex) when (ex is DbException or IOException or UnauthorizedAccessException)
        {
            _messageLabel.Text = $"The games history could not be read: {ex.Message}";
        }
    }

    private static void Fill(ListView list, IReadOnlyList<HighScore> scores)
    {
        list.BeginUpdate();
        list.Items.Clear();
        for (int i = 0; i < scores.Count; i++)
        {
            var score = scores[i];
            list.Items.Add(new ListViewItem(
            [
                (i + 1).ToString(),
                score.IsAi ? $"{score.PlayerName} (AI)" : score.PlayerName,
                score.Score.ToString(),
                score.PlayedAt.ToLocalTime().ToString("d MMM yyyy, HH:mm"),
            ]));
        }
        list.EndUpdate();
    }
}
