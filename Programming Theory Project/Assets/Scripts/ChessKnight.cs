using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts
{
    /// <summary>
    /// Represents a knight chess piece with its specific L-shaped movement pattern
    /// </summary>
    public class ChessKnight: ChessPiece, IChessPiece
    {
        /// <summary>
        /// Generates all potential moves for a knight
        /// </summary>
        /// <returns>List of all potential knight moves in L-shape pattern</returns>
        public override List<ChessMove> GenerateAllPotentialMoves()
        {
            var moves = new List<ChessMove>();
            int[] moveLength = new int[] { -2, -1, 1, 2 }; // Possible offsets for knight's L-shaped movement

            // Iterate through all possible L-shaped knight movements
            foreach (int i in moveLength) {
                foreach (int j in moveLength) {
                    // Knight moves in L-shape pattern, so i and j must have different absolute values
                    if (Mathf.Abs(i) != Mathf.Abs(j)) {
                        BoardCoords newCoords = coords + new BoardCoords(i, j);

                        // Check if the new position is inside the board and either empty or occupied by an opponent's piece
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

        /// <summary>
        /// Generates capture opportunities for a knight
        /// For knights, capture moves are identical to regular moves since they can jump over pieces
        /// </summary>
        /// <returns>List of potential capture moves</returns>
        public override List<ChessMove> GenerateCaptureOpportunities()
        {
            // Knights can capture any piece they can move to, so the logic is the same as for regular moves
            return GenerateAllPotentialMoves();
        }

        /// <summary>
        /// Generates all legal moves for this knight
        /// </summary>
        /// <returns>List of all legal knight moves</returns>
        public override List<ChessMove> GeneratePossibleMoves()
        {
            var moves = GenerateAllPotentialMoves();

            // Use LINQ to filter moves - keep only those where destination is inside the board
            // and either empty or contains an opponent's piece
            var result = moves
                .Where(move => move.to.IsInsideBoard(chessBoard) &&
                               (chessBoard.IsSquareEmpty(move.to) || move.attackedPiece.pieceColor != pieceColor))
                .ToList();

            return result;
        }

    }
}
