using Yahtzee.Core.Game;

namespace Yahtzee.WinForms.Forms;

/// <summary>End-of-game summary: every player's name and score, highest first.</summary>
internal sealed class ResultsForm : Form
{
    /// <param name="standings">Final standings, already sorted by score (see <see cref="GameEngine.GetStandings"/>).</param>
    public ResultsForm(IReadOnlyList<PlayerStanding> standings)
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Game Over";
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

        layout.Controls.Add(new Label
        {
            Text = DescribeWinner(standings),
            Font = UiTheme.Heading,
            ForeColor = UiTheme.Felt,
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            Margin = new Padding(3, 0, 3, 12),
        });

        var table = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            Size = new Size(420, 40 + 28 * standings.Count),
            MultiSelect = false,
        };
        table.Columns.Add("Rank");
        table.Columns.Add("Player");
        table.Columns.Add("Score", -2, HorizontalAlignment.Right);
        // Column widths are pixels and are not DPI-scaled, so share out the list's real width once it is known.
        Load += (_, _) =>
        {
            int width = table.ClientSize.Width;
            table.Columns[0].Width = width * 18 / 100;
            table.Columns[1].Width = width * 57 / 100;
            table.Columns[2].Width = width - table.Columns[0].Width - table.Columns[1].Width;
        };
        foreach (var standing in standings)
        {
            string name = standing.IsAi ? $"{standing.Name} (AI)" : standing.Name;
            var item = new ListViewItem([Ordinal(standing.Rank), name, standing.Score.ToString()]);
            if (standing.Rank == 1)
                item.Font = UiTheme.Bold;
            table.Items.Add(item);
        }
        layout.Controls.Add(table);

        var close = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            MinimumSize = new Size(90, 32),
            Anchor = AnchorStyles.Right,
            Margin = new Padding(3, 16, 3, 3),
        };
        layout.Controls.Add(close);

        Controls.Add(layout);
        AcceptButton = close;
        CancelButton = close;
        ResumeLayout(false);
        PerformLayout();
    }

    private static string DescribeWinner(IReadOnlyList<PlayerStanding> standings)
    {
        var winners = standings.Where(standing => standing.Rank == 1).ToList();
        return winners switch
        {
            [var only] when standings.Count == 1 => $"Final score: {only.Score} points",
            [var only] => $"{only.Name} wins with {only.Score} points!",
            _ => $"It's a tie! {string.Join(" and ", winners.Select(w => w.Name))} share first place with {winners[0].Score} points.",
        };
    }

    private static string Ordinal(int rank) => (rank % 100, rank % 10) switch
    {
        (11 or 12 or 13, _) => $"{rank}th",
        (_, 1) => $"{rank}st",
        (_, 2) => $"{rank}nd",
        (_, 3) => $"{rank}rd",
        _ => $"{rank}th",
    };
}
