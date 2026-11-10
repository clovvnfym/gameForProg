using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AdvancedSnake.Model;

namespace AdvancedSnake.View
{
    public class GameForm : Form
    {
        private const int CellSize = 25;
        private const int HudHeight = 44;
        private const int BoardPx = GameModel.GridSize * CellSize;

        private GameModel model;
        private readonly System.Windows.Forms.Timer gameTimer;

        private readonly Font titleFont = new("Arial", 28, FontStyle.Bold);
        private readonly Font bigFont = new("Arial", 22, FontStyle.Bold);
        private readonly Font midFont = new("Arial", 13, FontStyle.Bold);
        private readonly Font textFont = new("Arial", 11);
        private readonly Font smallFont = new("Arial", 9);

        private bool inMenu = true;
        private bool recordSaved;
        private Difficulty selectedDifficulty = Difficulty.Normal;
        private bool wrapWalls;
        private int menuRecord;

        public GameForm()
        {
            Text = "Advanced Snake MVC";
            ClientSize = new Size(BoardPx, BoardPx + HudHeight);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            DoubleBuffered = true;
            KeyPreview = true;
            StartPosition = FormStartPosition.CenterScreen;

            model = CreateModel();
            menuRecord = model.HighScore;

            gameTimer = new System.Windows.Forms.Timer { Interval = model.CurrentInterval };
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();

            KeyDown += GameForm_KeyDown;
            FormClosing += (s, e) => SaveRecord();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                gameTimer.Dispose();
                titleFont.Dispose(); bigFont.Dispose(); midFont.Dispose();
                textFont.Dispose(); smallFont.Dispose();
            }
            base.Dispose(disposing);
        }

        // ---------- логика формы ----------

        private GameModel CreateModel() =>
            new GameModel(selectedDifficulty, wrapWalls) { HighScore = HighScoreStorage.Load(selectedDifficulty) };

        private void StartGame()
        {
            model = CreateModel();
            recordSaved = false;
            inMenu = false;
            gameTimer.Interval = model.CurrentInterval;
            Invalidate();
        }

        private void ReturnToMenu()
        {
            SaveRecord();
            inMenu = true;
            menuRecord = HighScoreStorage.Load(selectedDifficulty);
            Invalidate();
        }

        private void SaveRecord()
        {
            if (model.NewRecord) HighScoreStorage.Save(model.Difficulty, model.HighScore);
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            if (inMenu) return;

            model.UpdateStep();

            if (model.IsGameOver && !recordSaved)
            {
                recordSaved = true;
                SaveRecord();
            }
            if (gameTimer.Interval != model.CurrentInterval)
                gameTimer.Interval = model.CurrentInterval;
            Invalidate();
        }

        private void GameForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (inMenu)
            {
                HandleMenuKey(e.KeyCode);
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Space:
                    if (!model.IsGameOver) model.IsPaused = !model.IsPaused;
                    Invalidate();
                    break;
                case Keys.Escape:
                    ReturnToMenu();
                    break;
                case Keys.R:
                    if (model.IsGameOver) StartGame();
                    break;
                case Keys.Up:
                case Keys.W:
                    model.TryTurn(Direction.Up);
                    break;
                case Keys.Down:
                case Keys.S:
                    model.TryTurn(Direction.Down);
                    break;
                case Keys.Left:
                case Keys.A:
                    model.TryTurn(Direction.Left);
                    break;
                case Keys.Right:
                case Keys.D:
                    model.TryTurn(Direction.Right);
                    break;
            }
        }

        private void HandleMenuKey(Keys key)
        {
            switch (key)
            {
                case Keys.Up:
                case Keys.W:
                    SelectDifficulty((Difficulty)(((int)selectedDifficulty + 2) % 3));
                    break;
                case Keys.Down:
                case Keys.S:
                    SelectDifficulty((Difficulty)(((int)selectedDifficulty + 1) % 3));
                    break;
                case Keys.D1: case Keys.NumPad1: SelectDifficulty(Difficulty.Easy); break;
                case Keys.D2: case Keys.NumPad2: SelectDifficulty(Difficulty.Normal); break;
                case Keys.D3: case Keys.NumPad3: SelectDifficulty(Difficulty.Hard); break;
                case Keys.T:
                    wrapWalls = !wrapWalls;
                    Invalidate();
                    break;
                case Keys.Enter:
                case Keys.Space:
                    StartGame();
                    break;
                case Keys.Escape:
                    Close();
                    break;
            }
        }

        private void SelectDifficulty(Difficulty d)
        {
            selectedDifficulty = d;
            menuRecord = HighScoreStorage.Load(d);
            Invalidate();
        }

        private static string DifficultyName(Difficulty d) => d switch
        {
            Difficulty.Easy => "Лёгкий",
            Difficulty.Hard => "Сложный",
            _ => "Нормальный"
        };

        // ---------- отрисовка ----------

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Black);

            DrawBoard(g);
            DrawHud(g);

            if (inMenu) DrawMenu(g);
            else if (model.IsGameOver) DrawGameOver(g);
            else if (model.IsPaused)
            {
                using var overlay = new SolidBrush(Color.FromArgb(150, Color.Black));
                g.FillRectangle(overlay, 0, 0, ClientSize.Width, ClientSize.Height);
                DrawCentered(g, "ПАУЗА", bigFont, Brushes.Yellow, 200);
            }
        }

        private void DrawBoard(Graphics g)
        {
            using var dark = new SolidBrush(Color.FromArgb(20, 28, 20));
            using var light = new SolidBrush(Color.FromArgb(26, 36, 26));
            for (int x = 0; x < GameModel.GridSize; x++)
                for (int y = 0; y < GameModel.GridSize; y++)
                    g.FillRectangle((x + y) % 2 == 0 ? dark : light, x * CellSize, y * CellSize, CellSize, CellSize);

            foreach (var o in model.Obstacles)
            {
                g.FillRectangle(Brushes.Gray, o.X * CellSize, o.Y * CellSize, CellSize, CellSize);
                g.DrawRectangle(Pens.DarkGray, o.X * CellSize, o.Y * CellSize, CellSize, CellSize);
            }

            DrawFood(g);
            DrawSnake(g);
        }

        private void DrawFood(Graphics g)
        {
            Brush brush = model.CurrentFoodType switch
            {
                FoodType.Bonus => Brushes.Gold,
                FoodType.SpeedUp => Brushes.Magenta,
                _ => Brushes.Red
            };
            int fx = model.Food.X * CellSize, fy = model.Food.Y * CellSize;
            g.FillEllipse(brush, fx + 3, fy + 3, CellSize - 6, CellSize - 6);

            // кольцо-таймер у временной еды
            if (model.FoodLifetime > 0)
            {
                float sweep = 360f * model.FoodTicksLeft / model.FoodLifetime;
                using var pen = new Pen(Color.White, 2);
                g.DrawArc(pen, fx + 1, fy + 1, CellSize - 2, CellSize - 2, -90, sweep);
            }
        }

        private void DrawSnake(Graphics g)
        {
            using var headBrush = new SolidBrush(Color.LimeGreen);
            using var bodyA = new SolidBrush(Color.FromArgb(0, 150, 0));
            using var bodyB = new SolidBrush(Color.FromArgb(0, 120, 0));

            for (int i = model.Snake.Count - 1; i >= 0; i--)
            {
                Brush b = i == 0 ? headBrush : (i % 2 == 0 ? bodyA : bodyB);
                g.FillRectangle(b, model.Snake[i].X * CellSize + 1, model.Snake[i].Y * CellSize + 1, CellSize - 2, CellSize - 2);
            }

            // глаза
            Point head = model.Snake[0];
            float cx = head.X * CellSize + CellSize / 2f, cy = head.Y * CellSize + CellSize / 2f;
            float dx = model.CurrentDirection == Direction.Left ? -1 : model.CurrentDirection == Direction.Right ? 1 : 0;
            float dy = model.CurrentDirection == Direction.Up ? -1 : model.CurrentDirection == Direction.Down ? 1 : 0;
            foreach (int side in new[] { -1, 1 })
            {
                float ex = cx + dx * 5 + (-dy) * side * 5;
                float ey = cy + dy * 5 + dx * side * 5;
                g.FillEllipse(Brushes.White, ex - 3, ey - 3, 6, 6);
                g.FillEllipse(Brushes.Black, ex - 1.5f, ey - 1.5f, 3, 3);
            }
        }

        private void DrawHud(Graphics g)
        {
            g.FillRectangle(Brushes.DarkSlateGray, 0, BoardPx, ClientSize.Width, HudHeight);

            g.DrawString($"Очки: {model.Score}   Рекорд: {model.HighScore}", midFont, Brushes.White, 8, BoardPx + 3);

            using var right = new StringFormat { Alignment = StringAlignment.Far };
            int speed = 1000 / Math.Max(1, gameTimer.Interval);
            g.DrawString($"Ур. {model.Level}  {speed} шаг/с", midFont, Brushes.White,
                new RectangleF(0, BoardPx + 3, ClientSize.Width - 8, 24), right);

            string walls = model.WrapWalls ? "без стен" : "стены";
            g.DrawString($"{DifficultyName(model.Difficulty)}, {walls}   |   Пробел — пауза, Esc — меню",
                smallFont, Brushes.LightGray, 8, BoardPx + 26);
        }

        private void DrawMenu(Graphics g)
        {
            using var overlay = new SolidBrush(Color.FromArgb(225, Color.Black));
            g.FillRectangle(overlay, 0, 0, ClientSize.Width, ClientSize.Height);

            DrawCentered(g, "ЗМЕЙКА", titleFont, Brushes.LimeGreen, 25);
            DrawCentered(g, "Сложность (↑/↓ или 1–3):", textFont, Brushes.White, 95);

            for (int i = 0; i < 3; i++)
            {
                var d = (Difficulty)i;
                bool sel = d == selectedDifficulty;
                DrawCentered(g, (sel ? "> " : "") + $"{i + 1}. {DifficultyName(d)}", midFont,
                    sel ? Brushes.Yellow : Brushes.Gray, 125 + i * 30);
            }

            DrawCentered(g, $"Рекорд: {menuRecord}", textFont, Brushes.White, 225);
            DrawCentered(g, $"Стены: {(wrapWalls ? "проходимые" : "смертельные")}  (T — переключить)",
                textFont, Brushes.White, 255);

            DrawLegend(g, 305, Brushes.Red, "Обычная еда: +10 очков");
            DrawLegend(g, 331, Brushes.Gold, "Золотая: +30 и рост, быстро исчезает");
            DrawLegend(g, 357, Brushes.Magenta, "Фиолетовая: ускорение, быстро исчезает");
            DrawLegend(g, 383, Brushes.Gray, "Серые блоки — препятствия, их всё больше");

            DrawCentered(g, "Enter / Пробел — начать игру", midFont, Brushes.Yellow, 435);
            DrawCentered(g, "Управление: стрелки или WASD", smallFont, Brushes.LightGray, 470);
        }

        private void DrawLegend(Graphics g, int y, Brush color, string text)
        {
            g.FillEllipse(color, 70, y, 16, 16);
            g.DrawString(text, textFont, Brushes.White, 95, y - 2);
        }

        private void DrawGameOver(Graphics g)
        {
            using var overlay = new SolidBrush(Color.FromArgb(180, Color.Black));
            g.FillRectangle(overlay, 0, 0, ClientSize.Width, ClientSize.Height);

            if (model.IsWin) DrawCentered(g, "ПОБЕДА!", bigFont, Brushes.Gold, 160);
            else DrawCentered(g, "КОНЕЦ ИГРЫ", bigFont, Brushes.Red, 160);

            DrawCentered(g, $"Очки: {model.Score}", midFont, Brushes.White, 210);
            if (model.NewRecord) DrawCentered(g, "Новый рекорд!", midFont, Brushes.Gold, 240);
            DrawCentered(g, "R — играть снова,  Esc — меню", textFont, Brushes.White, 285);
        }

        private void DrawCentered(Graphics g, string text, Font font, Brush brush, float y)
        {
            using var sf = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString(text, font, brush, new RectangleF(0, y, ClientSize.Width, font.Height + 6), sf);
        }
    }
}
