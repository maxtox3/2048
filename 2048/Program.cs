using System;
using System.Collections.Generic;

namespace _2048 {
    internal class Program {
        private static void Main(string[] args) {
            var game = new Game();
            game.Run();
        }
    }

    internal class Game {
        private readonly int nCols;

        private readonly int nRows;
        private readonly Random random = new Random();

        public Game() {
            Board = new ulong[4, 4];
            nRows = Board.GetLength(0);
            nCols = Board.GetLength(1);
            Score = 0;
        }

        public ulong Score { get; private set; }
        public ulong[,] Board { get; }

        public void Run() {
            var hasUpdated = true;
            do {
                if (hasUpdated) PutNewValue();

                Display();

                if (IsDead())
                    using (new ColorOutput(ConsoleColor.Red)) {
                        Console.WriteLine("YOU ARE DEAD!!!");
                        break;
                    }

                Console.WriteLine("Use arrow keys to move the tiles. Press Ctrl-C to exit.");
                var input = Console.ReadKey(true); // BLOCKING TO WAIT FOR INPUT
                Console.WriteLine(input.Key.ToString());

                switch (input.Key) {
                    case ConsoleKey.UpArrow:
                        hasUpdated = Update(Direction.Up);
                        break;

                    case ConsoleKey.DownArrow:
                        hasUpdated = Update(Direction.Down);
                        break;

                    case ConsoleKey.LeftArrow:
                        hasUpdated = Update(Direction.Left);
                        break;

                    case ConsoleKey.RightArrow:
                        hasUpdated = Update(Direction.Right);
                        break;

                    default:
                        hasUpdated = false;
                        break;
                }
            } while (true); // use CTRL-C to break out of loop

            Console.WriteLine("Press any key to quit...");
            Console.Read();
        }

        private static ConsoleColor GetNumberColor(ulong num) {
            switch (num) {
                case 0:
                    return ConsoleColor.DarkGray;
                case 2:
                    return ConsoleColor.Cyan;
                case 4:
                    return ConsoleColor.Magenta;
                case 8:
                    return ConsoleColor.Red;
                case 16:
                    return ConsoleColor.Green;
                case 32:
                    return ConsoleColor.Yellow;
                case 64:
                    return ConsoleColor.Yellow;
                case 128:
                    return ConsoleColor.DarkCyan;
                case 256:
                    return ConsoleColor.Cyan;
                case 512:
                    return ConsoleColor.DarkMagenta;
                case 1024:
                    return ConsoleColor.Magenta;
                default:
                    return ConsoleColor.Red;
            }
        }

        private static bool Update(ulong[,] board, Direction direction, out ulong score) {
            var nRows = board.GetLength(0);
            var nCols = board.GetLength(1);

            score = 0;
            var hasUpdated = false;

            // You shouldn't be dead at this point. We always check if you're dead at the end of the Update()

            // Drop along row or column? true: process inner along row; false: process inner along column
            var isAlongRow = direction == Direction.Left || direction == Direction.Right;

            // Should we process inner dimension in increasing index order?
            var isIncreasing = direction == Direction.Left || direction == Direction.Up;

            var outterCount = isAlongRow ? nRows : nCols;
            var innerCount = isAlongRow ? nCols : nRows;
            var innerStart = isIncreasing ? 0 : innerCount - 1;
            var innerEnd = isIncreasing ? innerCount - 1 : 0;

            var drop = isIncreasing
                ? innerIndex => innerIndex - 1
                : new Func<int, int>(innerIndex => innerIndex + 1);

            var reverseDrop = isIncreasing
                ? innerIndex => innerIndex + 1
                : new Func<int, int>(innerIndex => innerIndex - 1);

            var getValue = isAlongRow
                ? (x, i, j) => x[i, j]
                : new Func<ulong[,], int, int, ulong>((x, i, j) => x[j, i]);

            var setValue = isAlongRow
                ? (x, i, j, v) => x[i, j] = v
                : new Action<ulong[,], int, int, ulong>((x, i, j, v) => x[j, i] = v);

            Func<int, bool> innerCondition = index =>
                Math.Min(innerStart, innerEnd) <= index && index <= Math.Max(innerStart, innerEnd);

            for (var i = 0; i < outterCount; i++)
            for (var j = innerStart; innerCondition(j); j = reverseDrop(j)) {
                if (getValue(board, i, j) == 0) continue;

                var newJ = j;
                do {
                    newJ = drop(newJ);
                }
                // Continue probing along as long as we haven't hit the boundary and the new position isn't occupied
                while (innerCondition(newJ) && getValue(board, i, newJ) == 0);

                if (innerCondition(newJ) && getValue(board, i, newJ) == getValue(board, i, j)) {
                    // We did not hit the canvas boundary (we hit a node) AND no previous merge occurred AND the nodes' values are the same
                    // Let's merge
                    var newValue = getValue(board, i, newJ) * 2;
                    setValue(board, i, newJ, newValue);
                    setValue(board, i, j, 0);

                    hasUpdated = true;
                    score += newValue;
                }
                else {
                    // Reached the boundary OR...
                    // we hit a node with different value OR...
                    // we hit a node with same value BUT a prevous merge had occurred
                    // 
                    // Simply stack along
                    newJ = reverseDrop(newJ); // reverse back to its valid position
                    if (newJ != j) hasUpdated = true;

                    var value = getValue(board, i, j);
                    setValue(board, i, j, 0);
                    setValue(board, i, newJ, value);
                }
            }

            return hasUpdated;
        }

        private bool Update(Direction dir) {
            ulong score;
            var isUpdated = Update(Board, dir, out score);
            Score += score;
            return isUpdated;
        }

        private bool IsDead() {
            ulong score;
            foreach (var dir in new[] {Direction.Down, Direction.Up, Direction.Left, Direction.Right}) {
                var clone = (ulong[,]) Board.Clone();
                if (Update(clone, dir, out score)) return false;
            }

            // tried all directions. none worked.
            return true;
        }

        private void Display() {
            Console.Clear();
            Console.WriteLine();
            for (var i = 0; i < nRows; i++) {
                for (var j = 0; j < nCols; j++)
                    using (new ColorOutput(GetNumberColor(Board[i, j]))) {
                        Console.Write($"{Board[i, j],6}");
                    }

                Console.WriteLine();
                Console.WriteLine();
            }

            Console.WriteLine("Score: {0}", Score);
            Console.WriteLine();
        }

        private void PutNewValue() {
            // Find all empty slots
            var emptySlots = new List<Tuple<int, int>>();
            for (var iRow = 0; iRow < nRows; iRow++)
            for (var iCol = 0; iCol < nCols; iCol++)
                if (Board[iRow, iCol] == 0)
                    emptySlots.Add(new Tuple<int, int>(iRow, iCol));

            // We should have at least 1 empty slot. Since we know the user is not dead
            var iSlot = random.Next(0, emptySlots.Count); // randomly pick an empty slot
            var value = random.Next(0, 100) < 95
                ? 2
                : (ulong) 4; // randomly pick 2 (with 95% chance) or 4 (rest of the chance)
            Board[emptySlots[iSlot].Item1, emptySlots[iSlot].Item2] = value;
        }

        #region Utility Classes

        private enum Direction {
            Up,
            Down,
            Right,
            Left
        }

        // disposable 
        private class ColorOutput : IDisposable {
            public ColorOutput(ConsoleColor fg, ConsoleColor bg = ConsoleColor.Black) {
                Console.ForegroundColor = fg;
                Console.BackgroundColor = bg;
            }

            public void Dispose() {
                Console.ResetColor();
            }
        }

        #endregion Utility Classes
    }
}