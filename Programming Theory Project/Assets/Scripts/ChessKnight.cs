using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts
{
    public class ChessKnight: ChessPiece, IChessPiece
    {
        public override List<ChessMove> GenerateAllPotentialMoves()
        {
            var moves = new List<ChessMove>();
            int[] moveLength = new int[] { -2, -1, 1, 2 };

            // Перебираем все возможные ходы коня
            foreach (int i in moveLength) {
                foreach (int j in moveLength) {
                    // Исключаем ходы, где i и j равны по модулю, так как это не соответствует движению коня
                    if (Mathf.Abs(i) != Mathf.Abs(j)) {
                        BoardCoords newCoords = coords + new BoardCoords(i, j);

                        // Проверяем, находится ли новая позиция внутри доски и свободна ли она или занята фигурой противника
                        if (newCoords.IsInsideBoard(chessBoard)) {
                            ChessPiece pieceAtDestination = chessBoard.GetPiece(newCoords.i, newCoords.j);
                            if (chessBoard.IsSquareEmpty(newCoords) || pieceAtDestination.pieceColor != pieceColor) {
                                moves.Add(new ChessMove(this, newCoords));
                            }
                        }
                    }
                }
            }

            return moves;
        }

        public override List<ChessMove> GenerateCaptureOpportunities()
        {
            // Логика движения и взятия для коня одинакова
            return GenerateAllPotentialMoves();
        }

        public override List<ChessMove> GeneratePossibleMoves()
        {
            var moves = GenerateAllPotentialMoves();

            // Use LINQ to filter moves
            var result = moves
                .Where(move => move.to.IsInsideBoard(chessBoard) &&
                               (move.attackedPiece == null || move.attackedPiece.pieceColor != pieceColor))
                .ToList();

            return result;
        }

    }
}
