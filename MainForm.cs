using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace UOLauncher
{
    public class MainForm : Form
    {
        private Panel topBar;
        private Panel bottomBar;
        private Button btnPlay;
        private Button btnPatch;
        private Label lblStatus;
        private Label lblTitle;
        private ProgressBar progressBar;
        private WebBrowser webBrowser;
        private Patcher patcher;

        private string remoteUrl;
        private string localPath;
        private string classicUoExe;
        private string serverName;

        public MainForm()
        {
            LoadConfig();

            this.Text = serverName ?? "UO Launcher";
            this.Size = new Size(1024, 740);
            this.MinimumSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.Icon = SystemIcons.Application;

            CreateTopBar();
            CreateWebBrowser();
            CreateBottomBar();

            if (!string.IsNullOrEmpty(remoteUrl))
                webBrowser.Navigate(remoteUrl);

            StartPatching();
        }

        private void LoadConfig()
        {
            string path = Path.Combine(Application.StartupPath, "appsettings.json");
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    remoteUrl = JsonGet(json, "RemoteUrl");
                    localPath = JsonGet(json, "LocalPath");
                    classicUoExe = JsonGet(json, "ClassicUoExe");
                    serverName = JsonGet(json, "ServerName");
                }
                catch { }
            }

            if (string.IsNullOrEmpty(localPath))
                localPath = Application.StartupPath;
            if (string.IsNullOrEmpty(classicUoExe))
                classicUoExe = Path.Combine(Application.StartupPath, "ClassicUO.exe");
            if (string.IsNullOrEmpty(serverName))
                serverName = "UO Launcher";
        }

        private void CreateTopBar()
        {
            topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.FromArgb(24, 24, 24)
            };

            lblTitle = new Label
            {
                Text = "  " + serverName,
                ForeColor = Color.FromArgb(200, 170, 110),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = false,
                Width = 250,
                Height = 48,
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnPatch = new Button
            {
                Text = "Check Updates",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatAppearance = { BorderColor = Color.FromArgb(80, 80, 80) },
                Font = new Font("Segoe UI", 9),
                Size = new Size(110, 28),
                Cursor = Cursors.Hand
            };
            btnPatch.Location = new Point(260, 10);
            btnPatch.Click += BtnPatch_Click;

            btnPlay = new Button
            {
                Text = "  PLAY  ",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatAppearance = { BorderColor = Color.FromArgb(0, 90, 180) },
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(100, 32),
                Cursor = Cursors.Hand
            };
            btnPlay.Click += BtnPlay_Click;

            lblStatus = new Label
            {
                Text = "Listo",
                ForeColor = Color.FromArgb(140, 140, 140),
                Font = new Font("Segoe UI", 9),
                AutoSize = true
            };

            topBar.Controls.AddRange(new Control[] { lblTitle, btnPatch, btnPlay, lblStatus });
            this.Controls.Add(topBar);

            this.Resize += (s, e) =>
            {
                btnPlay.Left = this.ClientSize.Width - btnPlay.Width - 12;
                lblStatus.Left = btnPlay.Left - lblStatus.Width - 16;
            };

            Shown += (s, e) =>
            {
                btnPlay.Left = ClientSize.Width - btnPlay.Width - 12;
                lblStatus.Left = btnPlay.Left - lblStatus.Width - 16;
            };
        }

        private void CreateWebBrowser()
        {
            webBrowser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                ScriptErrorsSuppressed = true,
                AllowWebBrowserDrop = false,
                IsWebBrowserContextMenuEnabled = false,
                WebBrowserShortcutsEnabled = false
            };
            this.Controls.Add(webBrowser);
        }

        private void CreateBottomBar()
        {
            bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = Color.FromArgb(18, 18, 18)
            };

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 4,
                Style = ProgressBarStyle.Continuous,
                ForeColor = Color.FromArgb(0, 120, 215),
                BackColor = Color.FromArgb(30, 30, 30),
                Visible = false
            };

            this.Controls.Add(progressBar);
            this.Controls.Add(bottomBar);
        }

        private void StartPatching()
        {
            patcher = new Patcher(localPath, OnPatchStatus);
            patcher.CheckAsync();
        }

        private void OnPatchStatus(string text, int progress)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { Invoke(new Action(() => OnPatchStatus(text, progress))); }
                catch { }
                return;
            }

            lblStatus.Text = text;
            lblStatus.Left = btnPlay.Left - lblStatus.Width - 16;

            if (progress >= 0)
            {
                progressBar.Visible = true;
                progressBar.Value = Math.Min(progress, 100);
            }
            else
            {
                progressBar.Visible = false;
            }
        }

        private void BtnPatch_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Checking...";
            if (patcher != null) patcher.CheckAsync();
            try { webBrowser.Refresh(); } catch { }
        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            if (File.Exists(classicUoExe))
            {
                try
                {
                    System.Diagnostics.Process.Start(classicUoExe);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo lanzar ClassicUO:\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                var result = MessageBox.Show(
                    "ClassicUO no encontrado en:\n" + classicUoExe +
                    "\n\n¿Buscar ClassicUO.exe manualmente?",
                    "UO Launcher", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    using (var ofd = new OpenFileDialog())
                    {
                        ofd.Filter = "ClassicUO.exe|ClassicUO.exe";
                        ofd.Title = "Seleccionar ClassicUO.exe";
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            classicUoExe = ofd.FileName;
                            System.Diagnostics.Process.Start(classicUoExe);
                        }
                    }
                }
            }
        }

        private static string JsonGet(string json, string key)
        {
            string search = "\"" + key + "\": \"";
            int start = json.IndexOf(search);
            if (start < 0) return null;
            start += search.Length;
            int end = json.IndexOf("\"", start);
            if (end < 0) return null;
            return json.Substring(start, end - start);
        }
    }
}
