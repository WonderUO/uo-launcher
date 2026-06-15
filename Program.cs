using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace UOLauncher
{
    static class Program
    {
        private static Mutex mutex;

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--generate-manifest")
            {
                string dir = args.Length > 1 ? args[1] : Directory.GetCurrentDirectory();
                ManifestGenerator.Run(new[] { dir });
                return;
            }

            bool createdNew;
            mutex = new Mutex(true, "UOLauncher_Instance", out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("El launcher ya está en ejecución.", "UO Launcher",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fatal: " + ex.Message, "UO Launcher",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }
}
