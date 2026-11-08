using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PWQuestOverlay
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (var instance = new Mutex(true, @"Local\PWQuestOverlay", out createdNew))
            {
                if (!createdNew) return;

                try { NativeMethods.SetProcessDpiAwarenessContext(new IntPtr(-4)); }
                catch { }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                var store = new StateStore();
                try
                {
                    string routePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "quest_routes.json");
                    string workbookPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rota_leveling_pw_144.xlsx");
                    RouteRepository repository = RouteRepository.Load(routePath);
                    AppState state = store.Load();
                    store.Log("Overlay iniciado. Rotas: " + routePath);
                    Application.Run(new OverlayForm(repository, store, state, routePath, workbookPath));
                    store.Log("Overlay encerrado.");
                }
                catch (Exception error)
                {
                    store.Log("Falha fatal: " + error);
                    MessageBox.Show(
                        "Não foi possível iniciar o PW Quest Overlay.\n\n" + error.Message +
                        "\n\nLog: " + store.LogPath,
                        "PW Quest Overlay",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
    }
}
