using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using Snake.Model;
using Snake;

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
    }
}
