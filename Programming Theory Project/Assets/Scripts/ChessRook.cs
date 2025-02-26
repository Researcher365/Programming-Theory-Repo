using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts
{
    public class ChessRook: ChessPiece, IChessPiece
    {
        public override List<ChessMove> GenerateAllPotentialMoves()
        {
            var moves = new List<ChessMove>();
            int[] deltas = { -1, 0, 1 };

            foreach (int dx in deltas) {
                foreach (int dy in deltas) {
                    if ((dx == 0 || dy == 0) && !(dx == 0 && dy == 0)) {
                        BoardCoords step = new BoardCoords(dx, dy);
                        BoardCoords nextSquare = coords + step;

                        while (nextSquare.IsInsideBoard(chessBoard) && chessBoard.GetPiece(nextSquare.i, nextSquare.j) == null) {
                            moves.Add(new ChessMove(this, nextSquare));
                            nextSquare = nextSquare + step;
                        }

                        if (nextSquare.IsInsideBoard(chessBoard)) {
                            ChessPiece pieceAtDestination = chessBoard.GetPiece(nextSquare.i, nextSquare.j);
                            if (pieceAtDestination != null && pieceAtDestination.pieceColor != pieceColor) {
                                moves.Add(new ChessMove(this, nextSquare));
                            }
                        }
                    }
                }
            }

            return moves;
        }

        public override List<ChessMove> GenerateCaptureOpportunities()
        {
            // Rook's move and beat logic is the same
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
