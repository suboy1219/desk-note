using System.Threading;
using System.Windows.Forms;

namespace DeskNote;

internal static class Program
{
    private const string MutexName = @"Local\DeskNote.SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var startHidden = args.Any(a =>
            string.Equals(a, "--startup", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a, "--silent", StringComparison.OrdinalIgnoreCase));

        Application.Run(new NoteForm(startHidden));
    }
}
