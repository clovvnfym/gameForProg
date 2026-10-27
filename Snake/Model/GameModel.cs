using System;
using System.Collections.Generic;
using System.Drawing;

namespace AdvancedSnake.Model
{
    public class GameModel
    {
        public const int GridSize = 20;
        public List<Point> Snake { get; private set; }
        public Direction CurrentDirection { get; set; }
        public Point Food { get; private set; }
        public FoodType CurrentFoodType { get; private set; }
        public List<Point> Obstacles { get; private set; }
        public int Score { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsPaused { get; set; }
        public int BaseInterval { get; private set; } = 200;
        public int CurrentInterval { get; private set; }
        private int speedUpTicksLeft = 0;
        private Random random = new Random();

        public GameModel() { Reset(); }

        public void Reset()
        {
            Snake = new List<Point> { new Point(10, 10), new Point(10, 11), new Point(10, 12) };
            CurrentDirection = Direction.Up;
            Obstacles = new List<Point>();
            Score = 0;
            IsGameOver = false;
            IsPaused = false;
            CurrentInterval = BaseInterval;
            speedUpTicksLeft = 0;
            SpawnFood();
        }

        public void SpawnFood()
        {
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
            if (chance < 70) CurrentFoodType = FoodType.Regular;
            else if (chance < 90) CurrentFoodType = FoodType.Bonus;
            else CurrentFoodType = FoodType.SpeedUp;
        }

        public void SpawnObstacle()
        {
            while (true)
            {
                Point p = new Point(random.Next(1, GridSize - 1), random.Next(1, GridSize - 1));
                if (!Snake.Contains(p) && Food != p && !Obstacles.Contains(p))
                {
                    Obstacles.Add(p);
                    break;
                }
            }
        }

        public void UpdateStep()
        {
            if (IsGameOver || IsPaused) return;

            if (speedUpTicksLeft > 0)
            {
                speedUpTicksLeft--;
                if (speedUpTicksLeft == 0) RecalculateSpeed();
            }

            Point head = Snake[0];
            Point newHead = head;

            switch (CurrentDirection)
            {
                case Direction.Up: newHead.Y--; break;
                case Direction.Down: newHead.Y++; break;
                case Direction.Left: newHead.X--; break;
                case Direction.Right: newHead.X++; break;
            }

            if (newHead.X < 0 || newHead.X >= GridSize || newHead.Y < 0 || newHead.Y >= GridSize)
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
                Snake.RemoveAt(Snake.Count - 1);
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
                    Snake.Add(Snake[Snake.Count - 1]);
                    Snake.Add(Snake[Snake.Count - 1]);
                    break;
                case FoodType.SpeedUp:
                    Score += 10;
                    speedUpTicksLeft = 120;
                    RecalculateSpeed();
                    break;
            }

            if (Score / 100 > oldScore / 100) SpawnObstacle();
            if (Score / 50 > oldScore / 50 && speedUpTicksLeft == 0) RecalculateSpeed();
        }

        private void RecalculateSpeed()
        {
            int speedDifficultyBonus = (Score / 50) * 15;
            int targetInterval = BaseInterval - speedDifficultyBonus;
            if (targetInterval < 60) targetInterval = 60;
            if (speedUpTicksLeft > 0) targetInterval /= 2;
            CurrentInterval = targetInterval;
        }
    }
}
