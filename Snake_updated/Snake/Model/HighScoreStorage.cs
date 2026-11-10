using System;
using System.IO;

namespace AdvancedSnake.Model
{
    public static class HighScoreStorage
    {
        private static string PathFor(Difficulty d)
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AdvancedSnake");
            return Path.Combine(dir, $"highscore_{d}.txt");
        }

        public static int Load(Difficulty d)
        {
            try
            {
                string path = PathFor(d);
                if (File.Exists(path) && int.TryParse(File.ReadAllText(path).Trim(), out int value))
                    return Math.Max(0, value);
            }
            catch (Exception) { /* нет доступа к файлу — просто играем без рекорда */ }
            return 0;
        }

        public static void Save(Difficulty d, int score)
        {
            try
            {
                string path = PathFor(d);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, score.ToString());
            }
            catch (Exception) { }
        }
    }
}
