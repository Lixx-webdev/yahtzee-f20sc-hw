using Yahtzee.Core.Game;
using Yahtzee.Core.Model;

namespace Yahtzee.WinForms.Forms;

/// <summary>
/// Dialog for setting up a new game: which seats are in play, each player's name, and whether
/// each player is a human or a computer opponent.
/// </summary>
internal sealed class NewGameDialog : Form
{
    /// <summary>The controls for one seat at the table.</summary>
    private sealed record SeatRow(CheckBox Include, TextBox Name, ComboBox Type);

    private readonly List<SeatRow> _seats = [];
    private readonly ErrorProvider _errors = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };

    public NewGameDialog()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "New Game";
        Font = UiTheme.Body;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 3,
            Dock = DockStyle.Fill,
        };

        var intro = new Label
        {
            Text = $"Choose up to {GameEngine.MaxPlayers} players. Players take turns in the order listed.",
            AutoSize = true,
            Margin = new Padding(3, 0, 3, 12),
        };
        layout.Controls.Add(intro, 0, 0);
        layout.SetColumnSpan(intro, 3);

        layout.Controls.Add(new Label { Text = "Player", Font = UiTheme.Bold, AutoSize = true }, 0, 1);
        layout.Controls.Add(new Label { Text = "Name", Font = UiTheme.Bold, AutoSize = true }, 1, 1);
        layout.Controls.Add(new Label { Text = "Type", Font = UiTheme.Bold, AutoSize = true }, 2, 1);

        for (int seat = 0; seat < GameEngine.MaxPlayers; seat++)
        {
            var row = CreateSeatRow(seat);
            _seats.Add(row);
            layout.Controls.Add(row.Include, 0, seat + 2);
            layout.Controls.Add(row.Name, 1, seat + 2);
            layout.Controls.Add(row.Type, 2, seat + 2);
        }

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 16, 0, 0),
        };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(90, 32) };
        var start = new Button { Text = "Start Game", AutoSize = true, MinimumSize = new Size(110, 32) };
        start.Click += (_, _) => TryAccept();
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(start);
        layout.Controls.Add(buttons, 0, GameEngine.MaxPlayers + 2);
        layout.SetColumnSpan(buttons, 3);

        Controls.Add(layout);
        AcceptButton = start;
        CancelButton = cancel;
        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>The players chosen, in turn order. Only valid after the dialog returns <see cref="DialogResult.OK"/>.</summary>
    public IReadOnlyList<Player> Players { get; private set; } = [];

    private SeatRow CreateSeatRow(int seat)
    {
        bool firstSeat = seat == 0;
        var include = new CheckBox
        {
            Text = $"Player {seat + 1}",
            Checked = firstSeat,
            Enabled = !firstSeat, // a game always has at least one player
            AutoSize = true,
            Anchor = AnchorStyles.Left,
        };
        var name = new TextBox
        {
            Text = $"Player {seat + 1}",
            MaxLength = Player.MaxNameLength,
            Width = 200,
            Enabled = firstSeat,
            Margin = new Padding(3, 3, 24, 3),
        };
        var type = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 250,
            Enabled = firstSeat,
        };
        type.Items.AddRange([.. PlayerTypeOption.All]);
        type.SelectedIndex = 0;

        // Keep default names meaningful: "Player 2" becomes "Computer 2" when switched to a computer, and back.
        string humanName = $"Player {seat + 1}", computerName = $"Computer {seat + 1}";
        type.SelectedIndexChanged += (_, _) =>
        {
            if (name.Text == humanName || name.Text == computerName)
                name.Text = ((PlayerTypeOption)type.SelectedItem!).IsComputer ? computerName : humanName;
        };

        include.CheckedChanged += (_, _) =>
        {
            name.Enabled = type.Enabled = include.Checked;
            _errors.SetError(name, "");
        };
        name.TextChanged += (_, _) => _errors.SetError(name, "");

        return new SeatRow(include, name, type);
    }

    /// <summary>Validates the names and, if they are all valid, builds the players and closes the dialog.</summary>
    private void TryAccept()
    {
        var included = _seats.Where(seat => seat.Include.Checked).ToList();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool valid = true;

        foreach (var seat in included)
        {
            string name = seat.Name.Text.Trim();
            string error = name.Length == 0 ? "Please enter a name."
                : !seenNames.Add(name) ? "Each player needs a different name."
                : "";
            _errors.SetError(seat.Name, error);
            valid &= error.Length == 0;
        }

        if (!valid)
            return;

        Players = included
            .Select(seat => new Player(seat.Name.Text, ((PlayerTypeOption)seat.Type.SelectedItem!).CreateStrategy()))
            .ToList();
        DialogResult = DialogResult.OK;
    }
}
