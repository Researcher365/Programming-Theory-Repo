using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts
{

    public struct BoardCoords
    {
        public int i, j;
        public bool isActive;
        public bool Equals(BoardCoords other)
        {
            return (i == other.i) && (j == other.j);
        }
        public static BoardCoords operator +(BoardCoords a, BoardCoords b)
        {
            return new BoardCoords(a.i + b.i, a.j + b.j);
        }
        public static BoardCoords operator -(BoardCoords a, BoardCoords b)
        {
            return new BoardCoords(a.i - b.i, a.j - b.j);
        }
        public static BoardCoords operator *(int a, BoardCoords b)
        {
            return new BoardCoords(a * b.i, a * b.j);
        }
        public static BoardCoords operator *(BoardCoords b, int a)
        {
            return new BoardCoords(a * b.i, a * b.j);
        }

        public BoardCoords normalized
        {
            get { return new BoardCoords(AdditionalMath.Sgn(i), AdditionalMath.Sgn(j)); }
        }

        public int magnitude
        {
            get {
                return System.Math.Abs(i) + System.Math.Abs(j);
            }
        }

        public BoardCoords(int newI, int newJ)
        {
            i = newI;
            j = newJ;
            isActive = false;
        }
        public BoardCoords(BoardCoords coords)
        {
            i = coords.i;
            j = coords.j;
            isActive = false;
        }
        public bool IsInsideBoard(uint szI, uint szJ)
        {
            return (i >= 0 && i < szI && j >= 0 && j < szJ);
        }
        public bool IsInsideBoard(ChessPiece[,] board)
        {
            return IsInsideBoard((uint)board.GetLength(0), (uint)board.GetLength(1));
        }
        public bool IsInsideBoard(ChessBoard board)
        {
            return IsInsideBoard(board.iSize, board.jSize);
        }
        public readonly string ToString()
        {
            char firstCoordLetter = 'a';
            return ((char)((int)firstCoordLetter + i)).ToString() + (j + 1).ToString();
        }

        public string notation
        {
            get { return ToString(); }
        }
    };
}