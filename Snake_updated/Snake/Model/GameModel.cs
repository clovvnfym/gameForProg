using System;
using System.Collections.Generic;
using System.Drawing;

namespace AdvancedSnake.Model
{
    public class GameModel
    {
        public const int GridSize = 20;
        private const int BonusLifetime = 40;   // шагов, пока золотая еда лежит на поле
        private const int SpeedUpLifetime = 30; // шагов для фиолетовой еды

        public List<Point> Snake { get; private set; } = new();
        public Direction CurrentDirection { get; set; }
        public Point Food { get; private set; }
        public FoodType CurrentFoodType { get; private set; }
        public int FoodTicksLeft { get; private set; }
        public int FoodLifetime { get; private set; }
        public List<Point> Obstacles { get; private set; } = new();

        public int Score { get; private set; }
        public int HighScore { get; set; }
        public bool NewRecord { get; private set; }
        public int Level => Score / 50 + 1;

        public bool IsGameOver { get; private set; }
        public bool IsWin { get; private set; }
        public bool IsPaused { get; set; }

        public Difficulty Difficulty { get; }
        public bool WrapWalls { get; }
        public int BaseInterval { get; }
        public int MinInterval { get; }
        public int CurrentInterval { get; private set; }
        private int ObstacleStep { get; }

        private int speedUpTicksLeft;
        private int growthPending;
        private readonly List<Direction> pendingTurns = new();
        private readonly Random random = new();

        public GameModel(Difficulty difficulty = Difficulty.Normal, bool wrapWalls = false)
        {
            Difficulty = difficulty;
            WrapWalls = wrapWalls;
            switch (difficulty)
            {
                case Difficulty.Easy: BaseInterval = 220; MinInterval = 70; ObstacleStep = 150; break;
                case Difficulty.Hard: BaseInterval = 140; MinInterval = 45; ObstacleStep = 50; break;
                default:              BaseInterval = 200; MinInterval = 60; ObstacleStep = 100; break;
            }
            Reset();
        }

        public void Reset()
        {
            Snake = new List<Point> { new Point(10, 10), new Point(10, 11), new Point(10, 12) };
            CurrentDirection = Direction.Up;
            pendingTurns.Clear();
            Obstacles = new List<Point>();
            Score = 0;
            NewRecord = false;
            IsGameOver = false;
            IsWin = false;
            IsPaused = false;
            CurrentInterval = BaseInterval;
            speedUpTicksLeft = 0;
            growthPending = 0;
            SpawnFood();
        }

        public void TryTurn(Direction direction)
        {
            if (IsGameOver || IsPaused) return;
            Direction last = pendingTurns.Count > 0 ? pendingTurns[^1] : CurrentDirection;
            if (direction == last || IsOpposite(direction, last)) return;
            if (pendingTurns.Count < 2) pendingTurns.Add(direction);
        }

        private static bool IsOpposite(Direction a, Direction b) =>
            (a == Direction.Up && b == Direction.Down) || (a == Direction.Down && b == Direction.Up) ||
            (a == Direction.Left && b == Direction.Right) || (a == Direction.Right && b == Direction.Left);

        public void SpawnFood()
        {
            int free = GridSize * GridSize - Snake.Count - Obstacles.Count;
            if (free <= 0)
            {
                IsWin = true;
                IsGameOver = true;
                return;
            }

            while (true)
            {
                Point p = new Point(random.Next(0, GridSize), random.Next(0, GridSize));
                if (!Snake.Contains(p) && !Obstacles.Contains(p))
                {
                    Food = p;
                    break;
                }
            }

            int chance = random.Next(100);
            if (chance < 70) { CurrentFoodType = FoodType.Regular; FoodLifetime = 0; }
            else if (chance < 90) { CurrentFoodType = FoodType.Bonus; FoodLifetime = BonusLifetime; }
            else { CurrentFoodType = FoodType.SpeedUp; FoodLifetime = SpeedUpLifetime; }
            FoodTicksLeft = FoodLifetime;
        }

        public void SpawnObstacle()
        {
            Point head = Snake[0];
            for (int attempt = 0; attempt < 300; attempt++)
            {
                Point p = new Point(random.Next(1, GridSize - 1), random.Next(1, GridSize - 1));
                // не ставим препятствие вплотную к голове, чтобы оно не появлялось «из воздуха» перед носом
                bool tooClose = Math.Abs(p.X - head.X) + Math.Abs(p.Y - head.Y) <= 4;
                if (!tooClose && !Snake.Contains(p) && Food != p && !Obstacles.Contains(p))
                {
                    Obstacles.Add(p);
                    return;
                }
            }
        }

        public void UpdateStep()
        {
            if (IsGameOver || IsPaused) return;

            if (pendingTurns.Count > 0)
            {
                CurrentDirection = pendingTurns[0];
                pendingTurns.RemoveAt(0);
            }

            if (speedUpTicksLeft > 0)
            {
                speedUpTicksLeft--;
                if (speedUpTicksLeft == 0) RecalculateSpeed();
            }

            Point newHead = Snake[0];
            switch (CurrentDirection)
            {
                case Direction.Up: newHead.Y--; break;
                case Direction.Down: newHead.Y++; break;
                case Direction.Left: newHead.X--; break;
                case Direction.Right: newHead.X++; break;
            }

            if (WrapWalls)
            {
                newHead.X = (newHead.X + GridSize) % GridSize;
                newHead.Y = (newHead.Y + GridSize) % GridSize;
            }
            else if (newHead.X < 0 || newHead.X >= GridSize || newHead.Y < 0 || newHead.Y >= GridSize)
            {
                IsGameOver = true;
                return;
            }

            if (Snake.Contains(newHead) || Obstacles.Contains(newHead))
            {
                IsGameOver = true;
                return;
            }

            Snake.Insert(0, newHead);

            if (newHead == Food)
            {
                ApplyFoodEffect();
                SpawnFood();
            }
            else
            {
                if (growthPending > 0) growthPending--;
                else Snake.RemoveAt(Snake.Count - 1);

                // временная еда исчезает, если змейка не успела её съесть
                if (CurrentFoodType != FoodType.Regular && --FoodTicksLeft <= 0) SpawnFood();
            }
        }

        private void ApplyFoodEffect()
        {
            int oldScore = Score;

            switch (CurrentFoodType)
            {
                case FoodType.Regular: Score += 10; break;
                case FoodType.Bonus:
                    Score += 30;
                    growthPending += 2;
                    break;
                case FoodType.SpeedUp:
                    Score += 10;
                    speedUpTicksLeft = 120;
                    RecalculateSpeed();
                    break;
            }

            if (Score > HighScore)
            {
                HighScore = Score;
                NewRecord = true;
            }

            if (Score / ObstacleStep > oldScore / ObstacleStep) SpawnObstacle();
            if (Score / 50 > oldScore / 50 && speedUpTicksLeft == 0) RecalculateSpeed();
        }

        private void RecalculateSpeed()
        {
            int target = BaseInterval - (Score / 50) * 15;
            if (target < MinInterval) target = MinInterval;
            if (speedUpTicksLeft > 0) target /= 2;
            CurrentInterval = target;
        }
    }
}
