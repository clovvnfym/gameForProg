using System;
using System.Drawing;
using System.Windows.Forms;
using AdvancedSnake.Model;

namespace AdvancedSnake.View
{
    public class GameForm : Form
    {
        private GameModel model;
        private System.Windows.Forms.Timer gameTimer;
        private const int CellSize = 25;

        public GameForm()
        {
            this.Text = "Advanced Snake MVC";
            this.ClientSize = new Size(GameModel.GridSize * CellSize, GameModel.GridSize * CellSize + 40);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.StartPosition = FormStartPosition.CenterScreen;

            model = new GameModel();

            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = model.CurrentInterval;
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();

            this.KeyDown += GameForm_KeyDown;
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            model.UpdateStep();
            if (gameTimer.Interval != model.CurrentInterval)
            {
                gameTimer.Interval = model.CurrentInterval;
            }
            this.Invalidate();
        }

        private void GameForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                model.IsPaused = !model.IsPaused;
                this.Invalidate();
                return;
            }
            if (e.KeyCode == Keys.R && model.IsGameOver)
            {
                model.Reset();
                gameTimer.Interval = model.CurrentInterval;
                this.Invalidate();
                return;
            }

            if (model.IsPaused || model.IsGameOver) return;

            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.W:
                    if (model.CurrentDirection != Direction.Down) model.CurrentDirection = Direction.Up;
                    break;
                case Keys.Down:
                case Keys.S:
                    if (model.CurrentDirection != Direction.Up) model.CurrentDirection = Direction.Down;
                    break;
                case Keys.Left:
                case Keys.A:
                    if (model.CurrentDirection != Direction.Right) model.CurrentDirection = Direction.Left;
                    break;
                case Keys.Right:
                case Keys.D:
                    if (model.CurrentDirection != Direction.Left) model.CurrentDirection = Direction.Right;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.Black);

            foreach (var obstacle in model.Obstacles)
            {
                g.FillRectangle(Brushes.Gray, obstacle.X * CellSize, obstacle.Y * CellSize, CellSize, CellSize);
                g.DrawRectangle(Pens.DarkGray, obstacle.X * CellSize, obstacle.Y * CellSize, CellSize, CellSize);
            }

            Brush foodBrush = Brushes.Red;
            if (model.CurrentFoodType == FoodType.Bonus) foodBrush = Brushes.Gold;
            if (model.CurrentFoodType == FoodType.SpeedUp) foodBrush = Brushes.Magenta;
            g.FillEllipse(foodBrush, model.Food.X * CellSize, model.Food.Y * CellSize, CellSize, CellSize);

            for (int i = 0; i < model.Snake.Count; i++)
            {
                Brush snakeBrush = (i == 0) ? Brushes.LimeGreen : Brushes.Green;
                g.FillRectangle(snakeBrush, model.Snake[i].X * CellSize, model.Snake[i].Y * CellSize, CellSize, CellSize);
                g.DrawRectangle(Pens.Black, model.Snake[i].X * CellSize, model.Snake[i].Y * CellSize, CellSize, CellSize);
            }

            g.FillRectangle(Brushes.DarkSlateGray, 0, GameModel.GridSize * CellSize, this.Width, 40);
            string infoText = $"Очки: {model.Score}   Скорость: {1000 / gameTimer.Interval} шаг/сек";
            g.DrawString(infoText, new Font("Arial", 11, FontStyle.Bold), Brushes.White, 10, GameModel.GridSize * CellSize + 10);

            if (model.IsGameOver)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, Color.Black)), 0, 0, this.Width, this.Height);
                g.DrawString("КОНЕЦ ИГРЫ", new Font("Arial", 22, FontStyle.Bold), Brushes.Red, 140, 180);
                g.DrawString("Нажмите 'R' для перезапуска", new Font("Arial", 12), Brushes.White, 125, 240);
            }
            else if (model.IsPaused)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(150, Color.Black)), 0, 0, this.Width, this.Height);
                g.DrawString("ПАУЗА", new Font("Arial", 22, FontStyle.Bold), Brushes.Yellow, 190, 200);
            }
        }
    }
}
