using System;
using System.Windows.Forms;
using AdvancedSnake.View;

namespace AdvancedSnake
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GameForm());
        }
    }
}
