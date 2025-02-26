using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts
{
    public class ChessPawn: ChessPiece, IChessPiece
    {
        public bool AtStartPosition { get { return coords.j == pawnStartLine; } }
        public override List<ChessMove> GenerateCaptureOpportunities()
        {
            var moves = new List<ChessMove>();

            moves.Add(new ChessMove(this, coords + new BoardCoords(+1, pawnDirection)));
            moves.Add(new ChessMove(this, coords + new BoardCoords(-1, pawnDirection)));
            return moves;
        }

        public override List<ChessMove> GenerateAllPotentialMoves()
        {
            var moves = new List<ChessMove>();
            moves.Add(new ChessMove(this, coords + new BoardCoords(0, pawnDirection)));
            if (AtStartPosition) {
                moves.Add(new ChessMove(this, coords + new BoardCoords(0, 2 * pawnDirection)));
            }
            return moves;
        }

        public override List<ChessMove> GeneratePossibleMoves()
        {
            // Формируем списки ходов для пешки: простые ходы вперёд и потенциальные взятия
            var moves = GenerateAllPotentialMoves();
            var captures = GenerateCaptureOpportunities();

            // Фильтруем обычные ходы пешки: они должны быть внутри доски, а клетка назначения должна быть пуста
            var validMoves = moves
                .Where(move =>
                    move.to.IsInsideBoard(chessBoard) &&
                    move.attackedPiece == null
                )
                .ToList();

            // Фильтруем ходы на взятие: клетка назначения должна быть внутри доски 
            // и либо там стоит фигура противника, либо это взятие на проходе
            var validCaptures = captures
                .Where(move =>
                    move.to.IsInsideBoard(chessBoard) &&
                    (move.isEnPassant()
                        || (move.attackedPiece != null
                            && move.attackedPiece.pieceColor != pieceColor))
                )
                .ToList();

            // Объединяем два списка в итоговый
            var result = new List<ChessMove>();
            result.AddRange(validMoves);
            result.AddRange(validCaptures);

            return result;
        }


    }
}