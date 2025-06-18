using System;
using System.IO;
using System.Linq;
using System.Text;

namespace HomeSite.Helpers
{
    class EnumGenerator
    {
        public static void GenerateEnums(string versionsPath, string outputFile)
        {
            if (!Directory.Exists(versionsPath))
            {
                Console.WriteLine($"Папка не найдена: {versionsPath}");
                return;
            }

            var sb = new StringBuilder();
            var allVersionNames = new HashSet<string>();

            var coreDirs = Directory.GetDirectories(versionsPath)
                                    .Select(Path.GetFileName)
                                    .Where(n => !string.IsNullOrWhiteSpace(n))
                                    .ToList();

            // 1. Enum Core
            sb.AppendLine("// Auto generated enums from existing versions on host");
            sb.AppendLine("namespace HomeSite.Generated");
            sb.AppendLine("{");
            sb.AppendLine("\tpublic enum ServerCore");
            sb.AppendLine("\t{");
            foreach (var core in coreDirs)
            {
                sb.AppendLine($"\t\t{ToEnumName(core)},");
            }
            sb.AppendLine("\t}");
            sb.AppendLine();

            // 2. Enum для каждой платформы (Forge, Paper и т.д.)
            foreach (var core in coreDirs)
            {
                string corePath = Path.Combine(versionsPath, core);
                var versions = Directory.GetDirectories(corePath)
                                        .Select(Path.GetFileName)
                                        .Where(n => !string.IsNullOrWhiteSpace(n))
                                        .ToList();

                if (versions.Count == 0)
                    continue;

                sb.AppendLine($"\tpublic enum {ToEnumName(core)}");
                sb.AppendLine("\t{");
                foreach (var version in versions)
                {
                    string enumName = ToEnumName(version);
                    sb.AppendLine($"\t\t{enumName},");
                    allVersionNames.Add(enumName);
                }
                sb.AppendLine("\t}");
                sb.AppendLine();
            }

            // 3. Enum MinecraftVersion (все уникальные версии, отсортированные)
            var sortedVersions = allVersionNames
                .Select(v => new
                {
                    Original = v,
                    Parts = v.TrimStart('_').Split('_').Select(p => int.TryParse(p, out var num) ? num : 0).ToArray()
                })
                .OrderBy(v => v.Parts.ElementAtOrDefault(0))
                .ThenBy(v => v.Parts.ElementAtOrDefault(1))
                .ThenBy(v => v.Parts.ElementAtOrDefault(2))
                .Select(v => v.Original)
                .ToList();

            sb.AppendLine("\tpublic enum MinecraftVersion");
            sb.AppendLine("\t{");
            foreach (var version in sortedVersions)
            {
                sb.AppendLine($"\t\t{version},");
            }
            sb.AppendLine("\t}");

            sb.AppendLine("}"); // end namespace

            File.WriteAllText(outputFile, sb.ToString());
            GenerateVersionHelper("Generated/VersionHelperGenerated.cs", sortedVersions);
            Console.WriteLine($"Файл с enum'ами создан: {outputFile}");
        }

        // Корректное имя enum-члена
        private static string ToEnumName(string name)
        {
            var clean = name.Replace(" ", "_")
                            .Replace("-", "_")
                            .Replace(".", "_");
                            //.Replace("__", "_");

            clean = new string(clean.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

            //if (!char.IsLetter(clean.FirstOrDefault()))
            //    clean = "_" + clean;

            return clean;
        }

        private static void GenerateVersionHelper(string filePath, List<string> sortedVersions)
        {
            var sb = new StringBuilder();

            sb.AppendLine("// Auto-generated helper for MinecraftVersion");
            sb.AppendLine("namespace HomeSite.Generated");
            sb.AppendLine("{");
            sb.AppendLine("\tpublic static class VersionHelperGenerated");
            sb.AppendLine("\t{");

            // GetVersion
            sb.AppendLine("\t\tpublic static string GetVersion(MinecraftVersion version)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\treturn version switch");
            sb.AppendLine("\t\t{");
            foreach (var v in sortedVersions)
                sb.AppendLine($"\t\t\t\tMinecraftVersion.{v} => \"{v}\",");
            sb.AppendLine("\t\t\t\t\t_ => \"\"");
            sb.AppendLine("\t\t\t\t};");
            sb.AppendLine("\t\t\t}");

            // GetVersionDBO
            sb.AppendLine();
            sb.AppendLine("\t\tpublic static string GetVersionDBO(MinecraftVersion version)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\treturn version switch");
            sb.AppendLine("\t\t\t{");
            foreach (var v in sortedVersions)
            {
                string readable = v.TrimStart('_').Replace('_', '.');
                sb.AppendLine($"\t\t\tMinecraftVersion.{v} => \"{readable}\",");
            }
            sb.AppendLine("\t\t_ => \"\"");
            sb.AppendLine("\t\t\t};");
            sb.AppendLine("\t\t}");

            sb.AppendLine("\t}");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"Файл с VersionHelper создан: {filePath}");
        }
    }
}
