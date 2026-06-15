using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UOLauncher
{
    public static class ManifestGenerator
    {
        public static void Run(string[] args)
        {
            string targetDir = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
            string outputPath = Path.Combine(targetDir, "manifest.json");
            string[] patterns = { "*.mul", "*.idx", "*.uop" };

            Console.WriteLine("Generando manifest.json para: " + targetDir);
            Console.WriteLine();

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"version\": \"1.0.0\",");
            sb.AppendLine("  \"files\": {");

            bool first = true;
            foreach (string pattern in patterns)
            {
                foreach (string file in Directory.GetFiles(targetDir, pattern))
                {
                    string name = Path.GetFileName(file);
                    string hash = HashFile(file);
                    long size = new FileInfo(file).Length;

                    if (!first) sb.AppendLine(",");
                    first = false;

                    sb.Append("    \"").Append(name).Append("\": { ");
                    sb.Append("\"size\": ").Append(size).Append(", ");
                    sb.Append("\"hash\": \"").Append(hash).Append("\"");
                    sb.Append(" }");

                    Console.WriteLine("{0,-30} {1,10} bytes  {2}", name, size, hash.Substring(0, 16) + "...");
                }
            }

            sb.AppendLine();
            sb.AppendLine("  }");
            sb.AppendLine("}");

            File.WriteAllText(outputPath, sb.ToString());
            Console.WriteLine();
            Console.WriteLine("Manifest generado: " + outputPath);
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
    }
}
