using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using AdvancedSnake.Model;

namespace SnakeTests
{
    [TestClass]
    public class SnakeModelTests
    {
        [TestMethod]
        public void Test_SnakeMovesForwardOnUpdate()
        {
            GameModel model = new GameModel();
            model.IsPaused = false;
            Point expectedHead = new Point(10, 9);

            model.UpdateStep();

            Assert.AreEqual(expectedHead, model.Snake[0], "Змейка должна переместиться вверх на 1 ячейку.");
        }

        [TestMethod]
        public void Test_Pause_BlocksMovement()
        {
            GameModel model = new GameModel();
            model.IsPaused = true;
            Point headBefore = model.Snake[0];

            model.UpdateStep();

            Assert.AreEqual(headBefore, model.Snake[0], "Змейка не должна двигаться во время паузы.");
        }

        [TestMethod]
        public void Test_ReverseTurn_IsIgnored()
        {
            GameModel model = new GameModel(); // едет вверх
            model.TryTurn(Direction.Down);

            model.UpdateStep();

            Assert.AreEqual(new Point(10, 9), model.Snake[0], "Разворот на 180° запрещён.");
        }

        [TestMethod]
        public void Test_FastDoubleTurn_DoesNotReverseIntoBody()
        {
            GameModel model = new GameModel(); // едет вверх
            model.TryTurn(Direction.Left);
            model.TryTurn(Direction.Down); // раньше такая комбинация убивала змейку

            model.UpdateStep();
            Assert.AreEqual(new Point(9, 10), model.Snake[0]);

            model.UpdateStep();
            Assert.AreEqual(new Point(9, 11), model.Snake[0]);
            Assert.IsFalse(model.IsGameOver);
        }

        [TestMethod]
        public void Test_Wall_KillsSnake_WhenWrapDisabled()
        {
            GameModel model = new GameModel(Difficulty.Normal, wrapWalls: false);
            for (int i = 0; i < 10; i++) model.UpdateStep(); // голова дошла до y = 0
            Assert.IsFalse(model.IsGameOver);

            model.UpdateStep();

            Assert.IsTrue(model.IsGameOver);
        }

        [TestMethod]
        public void Test_Wall_Wraps_WhenWrapEnabled()
        {
            GameModel model = new GameModel(Difficulty.Normal, wrapWalls: true);
            for (int i = 0; i < 11; i++) model.UpdateStep();

            Assert.IsFalse(model.IsGameOver);
            Assert.AreEqual(new Point(10, GameModel.GridSize - 1), model.Snake[0]);
        }

        [TestMethod]
        public void Test_HardIsFasterThanEasy()
        {
            Assert.IsTrue(new GameModel(Difficulty.Hard).BaseInterval < new GameModel(Difficulty.Easy).BaseInterval);
        }

        [TestMethod]
        public void Test_Reset_KeepsHighScoreAndClearsState()
        {
            GameModel model = new GameModel { HighScore = 120 };
            for (int i = 0; i < 11; i++) model.UpdateStep(); // врезались в стену
            Assert.IsTrue(model.IsGameOver);

            model.Reset();

            Assert.IsFalse(model.IsGameOver);
            Assert.AreEqual(0, model.Score);
            Assert.AreEqual(120, model.HighScore);
        }
    }
}
