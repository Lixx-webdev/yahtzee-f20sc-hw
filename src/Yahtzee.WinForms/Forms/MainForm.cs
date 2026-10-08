using System.Data.Common;
using Yahtzee.Core.AI;
using Yahtzee.Core.Data;
using Yahtzee.Core.Game;
using Yahtzee.Core.Model;
using Yahtzee.Core.Persistence;
using Yahtzee.Core.Rules;
using Yahtzee.WinForms.Controls;

namespace Yahtzee.WinForms.Forms;

/// <summary>
/// The main game window. The left side is the dice table (whose turn, rolls left, dice, roll button);
/// the right side is the scorecard grid with a "This roll" score-preview column for the current player.
/// </summary>
/// <remarks>
/// The form holds no game rules of its own: it calls <see cref="GameEngine"/> actions and redraws itself
/// from the engine's state whenever <see cref="GameEngine.StateChanged"/> fires. It also handles the
/// "outside the game" features: saving and loading, the unsaved-changes prompt, and the games history.
/// </remarks>
internal sealed class MainForm : Form
{
    /// <summary>Delay between visible steps of a computer player's turn.</summary>
    private const int AiStepDelayMs = 800;

    private const int CategoryColumn = 0;
    private const int PreviewColumn = 1;
    private const int FirstPlayerColumn = 2;

    private const string SaveFileFilter = "Yahtzee saved games (*.yahtzee)|*.yahtzee|All files (*.*)|*.*";

    private readonly DieControl[] _dice = new DieControl[Scorer.DiceCount];
    private readonly Label _turnLabel = new();
    private readonly Label _roundLabel = new();
    private readonly Label _rollsLabel = new();
    private readonly Label _hintLabel = new();
    private readonly Button _rollButton = new();
    private readonly Button _newGameButton = new();
    private readonly Button _openButton = new();
    private readonly DataGridView _scoreGrid = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly System.Windows.Forms.Timer _aiTimer = new() { Interval = AiStepDelayMs };
    private readonly ToolStripMenuItem _saveItem = new("&Save", null, null, Keys.Control | Keys.S);
    private readonly ToolStripMenuItem _saveAsItem = new("Save &As...");
    private readonly IGameHistoryRepository _history;

    private GameEngine? _game;

    /// <summary>File the current game was last saved to or loaded from, or null if it has never been saved.</summary>
    private string? _savePath;

    /// <summary>
    /// Unsaved-changes tracking: <see cref="_changeCount"/> goes up on every engine change, and
    /// <see cref="_savedChangeCount"/> remembers its value at the last save. A counter (rather than a
    /// true/false flag) stays correct even if an AI move happens while a save is being written.
    /// </summary>
    private int _changeCount;
    private int _savedChangeCount;

    /// <summary>Number of dialogs currently open; computer players wait while any are showing.</summary>
    private int _modalDepth;

    /// <summary>Set once the user has dealt with unsaved changes, so the next close really closes.</summary>
    private bool _closeConfirmed;

    /// <param name="history">Where completed games are recorded and high scores are read from.</param>
    public MainForm(IGameHistoryRepository history)
    {
        _history = history;
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Yahtzee";
        Font = UiTheme.Body;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1180, 760);
        MinimumSize = new Size(1000, 700);

        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.Controls.Add(BuildDiceTable(), 0, 0);
        mainLayout.Controls.Add(BuildScoreGrid(), 1, 0);

        // Dock order: the fill control must be added before the edge-docked menu and status bar.
        Controls.Add(mainLayout);
        Controls.Add(BuildStatusBar());
        Controls.Add(BuildMenu());

        _aiTimer.Tick += (_, _) => PlayAiStep();

        ResumeLayout(false);
        PerformLayout();
        RefreshUi();
    }

    // ------------------------------------------------------------------
    // Layout
    // ------------------------------------------------------------------

    private MenuStrip BuildMenu()
    {
        _saveItem.Click += async (_, _) => await SaveGameAsync(saveAs: false);
        _saveAsItem.Click += async (_, _) => await SaveGameAsync(saveAs: true);

        var gameMenu = new ToolStripMenuItem("&Game");
        gameMenu.DropDownItems.AddRange(
        [
            new ToolStripMenuItem("&New Game...", null, async (_, _) => await NewGameAsync(), Keys.Control | Keys.N),
            new ToolStripMenuItem("&Open Saved Game...", null, async (_, _) => await OpenGameAsync(), Keys.Control | Keys.O),
            _saveItem,
            _saveAsItem,
            new ToolStripSeparator(),
            new ToolStripMenuItem("&High Scores...", null, (_, _) => ShowHighScores(), Keys.Control | Keys.H),
            new ToolStripSeparator(),
            new ToolStripMenuItem("E&xit", null, (_, _) => Close(), Keys.Alt | Keys.F4),
        ]);

        var helpMenu = new ToolStripMenuItem("&Help");
        helpMenu.DropDownItems.Add(new ToolStripMenuItem("&How to Play", null, (_, _) => ShowHowToPlay(), Keys.F1));

        var menu = new MenuStrip();
        menu.Items.AddRange([gameMenu, helpMenu]);
        MainMenuStrip = menu;
        return menu;
    }

    private StatusStrip BuildStatusBar()
    {
        var status = new StatusStrip();
        _statusLabel.Spring = true;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        status.Items.Add(_statusLabel);
        return status;
    }

    private Control BuildDiceTable()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            BackColor = UiTheme.Felt,
            Padding = new Padding(24),
            Margin = new Padding(0),
        };

        StyleFeltLabel(_turnLabel, UiTheme.Heading);
        StyleFeltLabel(_roundLabel, UiTheme.Subheading);
        StyleFeltLabel(_rollsLabel, new Font(UiTheme.Subheading, FontStyle.Bold));
        _rollsLabel.ForeColor = UiTheme.HeldFace;
        _rollsLabel.Margin = new Padding(3, 3, 3, 16);

        var diceRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Color.Transparent };
        for (int i = 0; i < _dice.Length; i++)
        {
            int index = i; // captured by the click handler
            var die = new DieControl { AccessibleName = $"Die {i + 1}" };
            die.Click += (_, _) => ToggleDie(index);
            _dice[i] = die;
            diceRow.Controls.Add(die);
        }

        _rollButton.Size = new Size(220, 48);
        _rollButton.Font = UiTheme.Subheading;
        _rollButton.Margin = new Padding(3, 16, 3, 8);
        _rollButton.BackColor = Color.WhiteSmoke;
        _rollButton.Click += (_, _) => RollDice();

        StyleFeltLabel(_hintLabel, UiTheme.Body);
        _hintLabel.MaximumSize = new Size(440, 0);

        _newGameButton.Text = "Start a New Game";
        _newGameButton.Size = new Size(220, 48);
        _newGameButton.Font = UiTheme.Subheading;
        _newGameButton.BackColor = Color.WhiteSmoke;
        _newGameButton.Margin = new Padding(3, 16, 3, 3);
        _newGameButton.Click += async (_, _) => await NewGameAsync();

        _openButton.Text = "Open a Saved Game";
        _openButton.Size = new Size(220, 48);
        _openButton.Font = UiTheme.Subheading;
        _openButton.BackColor = Color.WhiteSmoke;
        _openButton.Margin = new Padding(3, 8, 3, 3);
        _openButton.Click += async (_, _) => await OpenGameAsync();

        panel.Controls.AddRange([_turnLabel, _roundLabel, _rollsLabel, diceRow, _rollButton, _hintLabel, _newGameButton, _openButton]);
        return panel;
    }

    private static void StyleFeltLabel(Label label, Font font)
    {
        label.AutoSize = true;
        label.Font = font;
        label.ForeColor = UiTheme.FeltText;
        label.BackColor = Color.Transparent;
    }

    private Control BuildScoreGrid()
    {
        var grid = _scoreGrid;
        grid.Dock = DockStyle.Fill;
        grid.Margin = new Padding(12);
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AllowUserToResizeColumns = false;
        grid.AllowUserToOrderColumns = false;
        grid.RowHeadersVisible = false;
        grid.MultiSelect = false;
        grid.TabStop = false;
        grid.BackgroundColor = SystemColors.Window;
        grid.BorderStyle = BorderStyle.None;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.EnableHeadersVisualStyles = false; // lets us colour the current player's header
        grid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.HeaderBack;
        grid.ColumnHeadersDefaultCellStyle.Font = UiTheme.Bold;
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.ShowCellToolTips = true;

        // The grid is a display, not a selectable table: hide the selection highlight.
        grid.SelectionChanged += (_, _) => grid.ClearSelection();
        grid.CellClick += OnScoreCellClick;
        grid.CellMouseEnter += (_, e) => grid.Cursor = IsScorableCell(e.RowIndex, e.ColumnIndex) ? Cursors.Hand : Cursors.Default;
        grid.CellMouseLeave += (_, _) => grid.Cursor = Cursors.Default;

        return grid;
    }

    /// <summary>Creates the grid's columns (one per player) and rows (one per category plus a total).</summary>
    private void BuildScoreGridFor(GameEngine game)
    {
        var grid = _scoreGrid;
        grid.Columns.Clear();
        grid.Rows.Clear();

        // Pixel heights are not scaled automatically, so convert them for the monitor's DPI.
        grid.ColumnHeadersHeight = LogicalToDeviceUnits(40);
        grid.RowTemplate.Height = LogicalToDeviceUnits(32);

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Category",
            FillWeight = 160,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft, Font = UiTheme.Bold, Padding = new Padding(8, 0, 0, 0) },
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "This roll",
            FillWeight = 90,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = { BackColor = UiTheme.PreviewBack },
        });
        foreach (var player in game.Players)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = player.IsAi ? $"{player.Name} (AI)" : player.Name,
                FillWeight = 100,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
        }

        foreach (var category in CategoryExtensions.All)
        {
            int row = grid.Rows.Add();
            grid.Rows[row].Tag = category;
            var nameCell = grid.Rows[row].Cells[CategoryColumn];
            nameCell.Value = category.DisplayName();
            nameCell.ToolTipText = DescribeCategory(category);
        }

        int totalRow = grid.Rows.Add();
        grid.Rows[totalRow].Cells[CategoryColumn].Value = "TOTAL";
        grid.Rows[totalRow].DefaultCellStyle.BackColor = UiTheme.TotalBack;
        grid.Rows[totalRow].DefaultCellStyle.Font = UiTheme.Bold;
        grid.Rows[totalRow].Cells[PreviewColumn].Style.BackColor = UiTheme.TotalBack;
    }

    // ------------------------------------------------------------------
    // Game lifecycle
    // ------------------------------------------------------------------

    private bool IsGameInProgress => _game is { IsGameOver: false };

    private bool IsHumanTurn => _game is { IsGameOver: false, CurrentPlayer.IsAi: false };

    /// <summary>A game is running and has changed since it was last saved (or was never saved).</summary>
    private bool HasUnsavedChanges => IsGameInProgress && _changeCount != _savedChangeCount;

    private async Task NewGameAsync()
    {
        if (!await ConfirmUnsavedChangesAsync("starting a new game"))
            return;

        using var dialog = new NewGameDialog();
        if (ShowModal(() => dialog.ShowDialog(this)) != DialogResult.OK)
            return;

        StartGame(new GameEngine(dialog.Players, new RandomDiceRoller()), savePath: null);
        SetStatus($"New game started with {dialog.Players.Count} player(s). {_game!.CurrentPlayer.Name} goes first.");
    }

    /// <summary>Makes <paramref name="game"/> the current game, replacing any previous one.</summary>
    /// <param name="game">The new or loaded game.</param>
    /// <param name="savePath">The file it was loaded from, or null for a new game.</param>
    private void StartGame(GameEngine game, string? savePath)
    {
        if (_game is not null)
        {
            _game.StateChanged -= OnGameStateChanged;
            _game.DiceRolled -= OnDiceRolled;
            _game.CategoryScored -= OnCategoryScored;
            _game.GameOver -= OnGameOver;
        }

        _game = game;
        game.StateChanged += OnGameStateChanged;
        game.DiceRolled += OnDiceRolled;
        game.CategoryScored += OnCategoryScored;
        game.GameOver += OnGameOver;
        _savePath = savePath;
        _changeCount = _savedChangeCount = 0; // a new or just-loaded game has nothing unsaved

        BuildScoreGridFor(game);
        RefreshUi();
        _rollButton.Focus();
    }

    // ------------------------------------------------------------------
    // Saving, loading and the unsaved-changes prompt
    // ------------------------------------------------------------------

    /// <summary>
    /// If the game has unsaved changes, asks whether to save before <paramref name="action"/>.
    /// Returns true when it is fine to continue: nothing to save, saved successfully, or "No" (don't save).
    /// </summary>
    private async Task<bool> ConfirmUnsavedChangesAsync(string action)
    {
        if (!HasUnsavedChanges)
            return true;

        var answer = ShowModal(() => MessageBox.Show(this,
            $"The current game has unsaved changes.\n\nDo you want to save it before {action}?",
            "Save Game?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question));
        return answer switch
        {
            DialogResult.Yes => await SaveGameAsync(saveAs: false),
            DialogResult.No => true,
            _ => false, // Cancel, or the message box was closed
        };
    }

    /// <summary>
    /// Saves the current game. Uses the file it was last saved to unless <paramref name="saveAs"/> is set
    /// or it has never been saved, in which case the user picks a file.
    /// </summary>
    /// <returns>True if the game was saved.</returns>
    private async Task<bool> SaveGameAsync(bool saveAs)
    {
        if (_game is not { CanSave: true } game)
            return false;

        string? path = saveAs ? null : _savePath;
        if (path is null)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Save Game",
                Filter = SaveFileFilter,
                DefaultExt = SaveGameSerializer.FileExtension,
                AddExtension = true,
                FileName = _savePath is null ? $"Yahtzee {DateTime.Now:yyyy-MM-dd HH-mm}" : Path.GetFileName(_savePath),
            };
            if (ShowModal(() => dialog.ShowDialog(this)) != DialogResult.OK)
                return false;
            path = dialog.FileName;
        }

        var snapshot = game.CreateSnapshot();
        int changesSaved = _changeCount; // remember which state was written, in case play continues meanwhile
        try
        {
            await SaveGameSerializer.SaveAsync(snapshot, path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError($"The game could not be saved.\n\n{ex.Message}");
            return false;
        }

        _savePath = path;
        _savedChangeCount = changesSaved;
        SetStatus($"Game saved to {path}.");
        UpdateTitleAndMenus();
        return true;
    }

    private async Task OpenGameAsync()
    {
        if (!await ConfirmUnsavedChangesAsync("opening another game"))
            return;

        using var dialog = new OpenFileDialog { Title = "Open Saved Game", Filter = SaveFileFilter };
        if (ShowModal(() => dialog.ShowDialog(this)) != DialogResult.OK)
            return;

        GameEngine game;
        try
        {
            var snapshot = await SaveGameSerializer.LoadAsync(dialog.FileName);
            game = GameEngine.Restore(snapshot, new RandomDiceRoller(), StrategyCatalog.Create);
        }
        catch (Exception ex) when (ex is InvalidSaveFileException or IOException or UnauthorizedAccessException)
        {
            ShowError($"The game could not be opened.\n\n{ex.Message}");
            return;
        }

        StartGame(game, dialog.FileName);
        SetStatus($"Loaded {Path.GetFileName(dialog.FileName)}. It is {game.CurrentPlayer.Name}'s turn.");
    }

    /// <summary>Offers to save unsaved changes before the window closes.</summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (_closeConfirmed || !HasUnsavedChanges || e.CloseReason == CloseReason.WindowsShutDown)
            return;

        // Saving is asynchronous but this event must decide immediately, so cancel this close,
        // ask (and maybe save) afterwards, and then close again unless the user cancelled.
        e.Cancel = true;
        BeginInvoke(async () =>
        {
            if (await ConfirmUnsavedChangesAsync("exiting"))
            {
                _closeConfirmed = true;
                Close();
            }
        });
    }

    private void ShowHighScores()
    {
        using var form = new HighScoresForm(_history);
        ShowModal(() => form.ShowDialog(this));
    }

    // ------------------------------------------------------------------
    // Player actions
    // ------------------------------------------------------------------

    private void RollDice()
    {
        if (IsHumanTurn && _game!.CanRoll)
            _game.Roll();
    }

    private void ToggleDie(int index)
    {
        if (IsHumanTurn && _game!.CanHold)
            _game.ToggleHeld(index);
    }

    private void OnScoreCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (!IsScorableCell(e.RowIndex, e.ColumnIndex))
            return;

        var category = (Category)_scoreGrid.Rows[e.RowIndex].Tag!;
        int points = Scorer.Score(category, _game!.Dice);
        if (points == 0)
        {
            var answer = ShowModal(() => MessageBox.Show(this,
                $"Your dice don't fit {category.DisplayName()}, so it will score 0.\n\nScore 0 in {category.DisplayName()}?",
                "Score zero?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2));
            if (answer != DialogResult.Yes)
                return;
        }

        _game.Score(category);
    }

    /// <summary>Whether clicking this grid cell would score a category for the current human player.</summary>
    private bool IsScorableCell(int row, int column) =>
        IsHumanTurn
        && _game!.CanScore
        && column == PreviewColumn
        && row >= 0
        && _scoreGrid.Rows[row].Tag is Category category
        && !_game.CurrentPlayer.ScoreCard.IsFilled(category);

    /// <summary>Keyboard shortcuts: 1–5 hold or release a die, R rolls.</summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        int? dieIndex = keyData switch
        {
            >= Keys.D1 and <= Keys.D5 => (int)(keyData - Keys.D1),
            >= Keys.NumPad1 and <= Keys.NumPad5 => (int)(keyData - Keys.NumPad1),
            _ => null,
        };

        if (dieIndex is int index && IsHumanTurn && _game!.CanHold)
        {
            ToggleDie(index);
            return true;
        }
        if (keyData == Keys.R && _rollButton.Enabled)
        {
            RollDice();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ------------------------------------------------------------------
    // Computer players
    // ------------------------------------------------------------------

    /// <summary>
    /// Starts or stops the AI timer depending on whether a computer player is on turn and no dialog is open.
    /// </summary>
    private void UpdateAiTimer()
    {
        bool aiTurn = _game is { IsGameOver: false, CurrentPlayer.IsAi: true } && _modalDepth == 0;
        if (aiTurn && !_aiTimer.Enabled)
            _aiTimer.Start();
        else if (!aiTurn)
            _aiTimer.Stop();
    }

    private void PlayAiStep()
    {
        _aiTimer.Stop();
        if (_game is not { IsGameOver: false, CurrentPlayer.IsAi: true })
            return;

        try
        {
            _game.PlayAiStep(); // raises StateChanged, which restarts the timer if the AI is still on turn
        }
        catch (InvalidOperationException ex)
        {
            ShowError($"The computer player made an invalid move:\n\n{ex.Message}");
        }
    }

    // ------------------------------------------------------------------
    // Engine events
    // ------------------------------------------------------------------

    private void OnGameStateChanged(object? sender, EventArgs e)
    {
        _changeCount++;
        RefreshUi();
    }

    private void OnDiceRolled(object? sender, DiceRolledEventArgs e) =>
        SetStatus($"{e.Player.Name} rolled {string.Join("  ", e.Dice)}.");

    private void OnCategoryScored(object? sender, CategoryScoredEventArgs e) =>
        SetStatus($"{e.Player.Name} scored {e.Score} in {e.Category.DisplayName()}.");

    /// <summary>Shows the results and records the game in the games history.</summary>
    /// <remarks><c>async void</c> is acceptable here because this is an event handler and its errors are caught.</remarks>
    private async void OnGameOver(object? sender, GameOverEventArgs e)
    {
        // Show the results after the engine finishes raising events and the board has been redrawn.
        BeginInvoke(() =>
        {
            using var results = new ResultsForm(e.Standings);
            ShowModal(() => results.ShowDialog(this));
        });

        try
        {
            await _history.RecordGameAsync(new CompletedGame(e.CompletedAt, e.Standings));
            SetStatus("Game over. The result has been added to the games history (Game > High Scores).");
        }
        catch (Exception ex) when (ex is DbException or IOException or UnauthorizedAccessException)
        {
            SetStatus($"Game over, but the result could not be saved to the games history: {ex.Message}");
        }
    }

    // ------------------------------------------------------------------
    // Rendering
    // ------------------------------------------------------------------

    /// <summary>Redraws everything from the current game state.</summary>
    private void RefreshUi()
    {
        if (_game is null)
        {
            ShowWelcome();
            return;
        }

        var game = _game;
        var player = game.CurrentPlayer;
        bool humanTurn = IsHumanTurn;

        _turnLabel.Text = game.IsGameOver ? "Game over"
            : player.IsAi ? $"{player.Name}'s turn (computer)"
            : $"{player.Name}'s turn";
        _roundLabel.Text = game.IsGameOver
            ? $"All {GameEngine.TotalRounds} rounds played"
            : $"Round {game.Round} of {GameEngine.TotalRounds}";
        _rollsLabel.Text = game.IsGameOver ? "" : $"Rolls left: {game.RollsRemaining} of {GameEngine.MaxRollsPerTurn}";

        for (int i = 0; i < _dice.Length; i++)
        {
            var die = _dice[i];
            die.Value = game.RollsUsed > 0 ? game.Dice[i] : 0;
            die.Held = game.RollsUsed > 0 && game.Held[i];
            die.ShowRollCaption = game.CanHold;
            die.Enabled = humanTurn && game.CanHold;
        }

        int diceToRoll = game.RollsUsed == 0 ? Scorer.DiceCount : game.Held.Count(held => !held);
        _rollButton.Visible = !game.IsGameOver;
        _rollButton.Enabled = humanTurn && game.CanRoll && diceToRoll > 0;
        _rollButton.Text = game.RollsUsed == 0 ? "Roll Dice (R)"
            : diceToRoll == 0 ? "All dice held"
            : $"Re-roll {diceToRoll} {(diceToRoll == 1 ? "die" : "dice")} (R)";

        _hintLabel.Text = game switch
        {
            { IsGameOver: true } => "The game is over. Start a new game from the Game menu.",
            _ when !humanTurn => $"{player.Name} is playing...",
            { RollsUsed: 0 } => "Roll the dice to start your turn.",
            { CanRoll: true } => "Click dice (or press 1-5) to hold them, then re-roll. " +
                                 "Or click a score in the \"This roll\" column to end your turn.",
            _ => "No rolls left. Click a score in the \"This roll\" column.",
        };
        _newGameButton.Visible = _openButton.Visible = game.IsGameOver;

        RefreshScoreGrid(game);
        UpdateTitleAndMenus();
        UpdateAiTimer();
    }

    private void ShowWelcome()
    {
        _turnLabel.Text = "Welcome to Yahtzee";
        _roundLabel.Text = "No game in progress";
        _rollsLabel.Text = "";
        foreach (var die in _dice)
        {
            die.Value = 0;
            die.Held = false;
            die.Enabled = false;
        }
        _rollButton.Visible = false;
        _hintLabel.Text = "Start a new game to choose the players. Up to " +
                          $"{GameEngine.MaxPlayers} players can take turns on this computer.";
        _newGameButton.Visible = _openButton.Visible = true;
        UpdateTitleAndMenus();
        SetStatus("Ready.");
    }

    /// <summary>Shows the file name and an unsaved-changes marker in the title, and enables Save when possible.</summary>
    private void UpdateTitleAndMenus()
    {
        string name = _savePath is null ? "new game" : Path.GetFileNameWithoutExtension(_savePath);
        Text = _game is null ? "Yahtzee" : $"Yahtzee - {name}{(HasUnsavedChanges ? " (unsaved changes)" : "")}";
        _saveItem.Enabled = _saveAsItem.Enabled = _game is { CanSave: true };
    }

    private void RefreshScoreGrid(GameEngine game)
    {
        var preview = game.Preview();
        int currentColumn = FirstPlayerColumn + game.CurrentPlayerIndex;

        foreach (DataGridViewRow row in _scoreGrid.Rows)
        {
            var previewCell = row.Cells[PreviewColumn];
            if (row.Tag is Category category)
            {
                bool hasPreview = preview.TryGetValue(category, out int points);
                previewCell.Value = hasPreview ? points.ToString() : "";
                previewCell.Style.ForeColor = points > 0 ? UiTheme.Accent : UiTheme.PreviewZero;
                previewCell.Style.Font = points > 0 ? UiTheme.Bold : UiTheme.Body;
                previewCell.ToolTipText = hasPreview && IsHumanTurn ? $"Click to score {points} in {category.DisplayName()}" : "";

                for (int p = 0; p < game.Players.Count; p++)
                    row.Cells[FirstPlayerColumn + p].Value = game.Players[p].ScoreCard.GetScore(category)?.ToString() ?? "";
            }
            else
            {
                for (int p = 0; p < game.Players.Count; p++)
                    row.Cells[FirstPlayerColumn + p].Value = game.Players[p].Total.ToString();
            }
        }

        for (int column = FirstPlayerColumn; column < _scoreGrid.Columns.Count; column++)
        {
            bool isCurrent = column == currentColumn && !game.IsGameOver;
            _scoreGrid.Columns[column].HeaderCell.Style.BackColor = isCurrent ? UiTheme.CurrentPlayerHeader : UiTheme.HeaderBack;
            _scoreGrid.Columns[column].DefaultCellStyle.BackColor = isCurrent ? Color.FromArgb(255, 250, 230) : SystemColors.Window;
        }
        _scoreGrid.Columns[PreviewColumn].HeaderText = game.IsGameOver ? "This roll" : $"This roll ({game.CurrentPlayer.Name})";
    }

    private void SetStatus(string message) => _statusLabel.Text = message;

    /// <summary>
    /// Runs <paramref name="show"/> (which opens a dialog) with computer players paused, so the game
    /// cannot move on while the user is reading a message or choosing a file.
    /// </summary>
    private T ShowModal<T>(Func<T> show)
    {
        _modalDepth++;
        _aiTimer.Stop();
        try
        {
            return show();
        }
        finally
        {
            _modalDepth--;
            UpdateAiTimer();
        }
    }

    private void ShowError(string message) =>
        ShowModal(() => MessageBox.Show(this, message, "Yahtzee", MessageBoxButtons.OK, MessageBoxIcon.Error));

    // ------------------------------------------------------------------
    // Help
    // ------------------------------------------------------------------

    private static string DescribeCategory(Category category) => category switch
    {
        >= Category.Aces and <= Category.Sixes => $"Sum of all dice showing {category.Face()}.",
        Category.Chance => "Sum of all dice, any combination.",
        Category.ThreeOfAKind => "At least three dice the same: sum of all dice.",
        Category.FourOfAKind => "At least four dice the same: sum of all dice.",
        Category.FullHouse => $"Three of one number and two of another: {Scorer.FullHouseScore} points. (Five of a kind does not count.)",
        Category.SmallStraight => $"Four sequential dice: {Scorer.SmallStraightScore} points.",
        Category.LargeStraight => $"Five sequential dice: {Scorer.LargeStraightScore} points.",
        Category.Yahtzee => $"All five dice the same: {Scorer.YahtzeeScore} points.",
        _ => "",
    };

    private void ShowHowToPlay()
    {
        string categories = string.Join("\n", CategoryExtensions.All.Select(c => $"  {c.DisplayName()}: {DescribeCategory(c)}"));
        ShowModal(() => MessageBox.Show(this,
            "Each turn, roll the dice up to three times. Between rolls, click dice (or press 1-5) to hold them; " +
            "held dice are gold and will not be re-rolled.\n\n" +
            "After any roll, click a score in the \"This roll\" column to record it. Each category can be used " +
            "once, and dice that don't fit a category score 0 in it.\n\n" +
            $"The game lasts {GameEngine.TotalRounds} rounds. The highest total wins.\n\n" + categories,
            "How to Play", MessageBoxButtons.OK, MessageBoxIcon.Information));
    }
}
