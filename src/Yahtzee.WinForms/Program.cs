using Yahtzee.Core.Data;
using Yahtzee.WinForms.Forms;

namespace Yahtzee.WinForms;

internal static class Program
{
    /// <summary>Application entry point.</summary>
    /// <param name="args">
    /// Optional <c>--database &lt;file&gt;</c> to use a different games-history database
    /// (for example a fresh one for a demonstration); otherwise the default location is used.
    /// </param>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        string databasePath = args is ["--database", var path] ? path : SqliteGameHistoryRepository.DefaultDatabasePath;
        var history = new SqliteGameHistoryRepository(databasePath);
        Application.Run(new MainForm(history));
    }
}
