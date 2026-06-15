using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UOLauncher
{
    public class Patcher
    {
        private readonly string localPath;
        private readonly Action<string, int> onStatus;

        private string ghOwner;
        private string ghRepo;
        private string ghBranch;
        private string[] filesToPatch;

        public Patcher(string localPath, Action<string, int> onStatus)
        {
            this.localPath = localPath;
            this.onStatus = onStatus;

            LoadConfig();
        }

        private void LoadConfig()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
            {
                filesToPatch = new[] {
                    "art.mul", "artidx.mul", "map0.mul", "map1.mul", "map2.mul", "map3.mul",
                    "statics0.mul", "staidx0.mul", "tiledata.mul", "hues.mul"
                };
                return;
            }

            string json = File.ReadAllText(path);
            ghOwner = JsonGet(json, "GitHubOwner") ?? "";
            ghRepo = JsonGet(json, "GitHubRepo") ?? "";
            ghBranch = JsonGet(json, "GitHubBranch") ?? "main";

            string raw = JsonGet(json, "Files");
            if (!string.IsNullOrEmpty(raw))
            {
                filesToPatch = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < filesToPatch.Length; i++)
                    filesToPatch[i] = filesToPatch[i].Trim().Trim('"', ' ');
            }
            else
            {
                filesToPatch = new[] {
                    "art.mul", "artidx.mul", "map0.mul", "statics0.mul",
                    "staidx0.mul", "tiledata.mul", "hues.mul"
                };
            }
        }

        public async void CheckAsync()
        {
            await Task.Run(() => Check());
        }

        private void Check()
        {
            if (string.IsNullOrEmpty(ghOwner) || string.IsNullOrEmpty(ghRepo))
            {
                onStatus("GitHub repo no configurado", -1);
                return;
            }

            string manifestUrl = string.Format("https://raw.githubusercontent.com/{0}/{1}/{2}/manifest.json", ghOwner, ghRepo, ghBranch);

            try
            {
                onStatus("Descargando manifiesto...", 0);

                string manifestJson;
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "UOLauncher/1.0");
                    manifestJson = wc.DownloadString(manifestUrl);
                }

                var remoteFiles = ParseManifest(manifestJson);
                if (remoteFiles.Count == 0)
                {
                    onStatus("Manifiesto vacio o invalido", -1);
                    return;
                }

                Directory.CreateDirectory(localPath);

                int done = 0;
                int total = remoteFiles.Count;

                foreach (var kv in remoteFiles)
                {
                    string fileName = kv.Key;
                    string remoteHash = kv.Value;

                    string localFile = Path.Combine(localPath, fileName);
                    bool needsDownload = true;

                    if (File.Exists(localFile))
                    {
                        string localHash = HashFile(localFile);
                        if (string.Equals(localHash, remoteHash, StringComparison.OrdinalIgnoreCase))
                            needsDownload = false;
                    }

                    if (needsDownload)
                    {
                        onStatus(string.Format("Descargando {0}...", fileName), (done * 100) / total);

                        string fileUrl = string.Format("https://raw.githubusercontent.com/{0}/{1}/{2}/{3}", ghOwner, ghRepo, ghBranch, fileName);
                        string tmpFile = localFile + ".tmp";

                        using (var wc = new WebClient())
                        {
                            wc.Headers.Add("User-Agent", "UOLauncher/1.0");
                            try
                            {
                                wc.DownloadFile(fileUrl, tmpFile);
                            }
                            catch (Exception ex)
                            {
                                onStatus(string.Format("Error descargando {0}: {1}", fileName, ex.Message), -1);
                                if (File.Exists(tmpFile)) File.Delete(tmpFile);
                                done++;
                                continue;
                            }
                        }

                        if (File.Exists(tmpFile))
                        {
                            if (File.Exists(localFile))
                                File.Delete(localFile);
                            File.Move(tmpFile, localFile);
                        }
                    }

                    done++;
                }

                onStatus("Todos los archivos actualizados!", 100);
                System.Threading.Thread.Sleep(1500);
                onStatus("Listo", -1);
            }
            catch (WebException)
            {
                onStatus("No se pudo conectar con GitHub (sin internet?)", -1);
            }
            catch (Exception ex)
            {
                onStatus("Error: " + ex.Message, -1);
            }
        }

        private Dictionary<string, string> ParseManifest(string json)
        {
            var result = new Dictionary<string, string>();

            foreach (string file in filesToPatch)
            {
                int fi = json.IndexOf("\"" + file + "\"", StringComparison.OrdinalIgnoreCase);
                if (fi < 0) continue;

                int hi = json.IndexOf("\"hash\": \"", fi, StringComparison.OrdinalIgnoreCase);
                if (hi < 0) continue;
                hi += 9;

                int he = json.IndexOf("\"", hi);
                if (he < 0) continue;

                string hash = json.Substring(hi, he - hi);
                if (hash.Length == 64)
                    result[file] = hash.ToLowerInvariant();
            }

            return result;
        }

        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(fs);
                var sb = new StringBuilder(64);
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static string JsonGet(string json, string key)
        {
            string search = "\"" + key + "\": \"";
            int s = json.IndexOf(search);
            if (s < 0) return null;
            s += search.Length;
            int e = json.IndexOf("\"", s);
            if (e < 0) return null;
            return json.Substring(s, e - s);
        }
    }
}
