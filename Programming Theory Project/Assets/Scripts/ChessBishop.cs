using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts
{
    public class ChessBishop: ChessPiece, IChessPiece
    {
        public override List<ChessMove> GenerateAllPotentialMoves()
        {
            var moves = new List<ChessMove>();
            int[] deltas = { -1, 1 };

            // Перебираем все возможные диагональные направления движения
            foreach (int dx in deltas) {
                foreach (int dy in deltas) {
                    BoardCoords step = new BoardCoords(dx, dy);
                    BoardCoords nextSquare = coords + step;

                    // Продолжаем двигаться в выбранном направлении, пока не выйдем за пределы доски или не встретим другую фигуру
                    while (nextSquare.IsInsideBoard(chessBoard) && chessBoard.GetPiece(nextSquare.i, nextSquare.j) == null) {
                        moves.Add(new ChessMove(this, nextSquare));
                        nextSquare = nextSquare + step;
                    }

                    // Если на следующей клетке находится фигура противника, добавляем ход с взятием
                    if (nextSquare.IsInsideBoard(chessBoard)) {
                        
                        ChessPiece pieceAtDestination = chessBoard.GetPiece(nextSquare.i, nextSquare.j);
                        if (pieceAtDestination != null && pieceAtDestination.pieceColor != pieceColor) {
                            moves.Add(new ChessMove(this, nextSquare));
                        }
                    }
                }
            }

            return moves;
        }

        public override List<ChessMove> GenerateCaptureOpportunities()
        {
            // Логика движения и взятия для слона одинакова
            return GenerateAllPotentialMoves();
        }

        public override List<ChessMove> GeneratePossibleMoves()
        {
            var moves = GenerateAllPotentialMoves();

            // Фильтруем ходы, чтобы исключить ходы на клетки, занятые фигурами того же цвета
            // Use LINQ to filter moves
            var result = moves
                .Where(move => move.to.IsInsideBoard(chessBoard) &&
                               (move.attackedPiece == null || move.attackedPiece.pieceColor != pieceColor))
                .ToList();

            return result;
        }


    }
}
