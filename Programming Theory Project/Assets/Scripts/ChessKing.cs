using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Assets.Scripts
{
    public class ChessKing: ChessPiece, IChessPiece
    {
        /// <summary>
        /// Generates the basic one-square moves a king can make in any direction
        /// </summary>
        /// <returns>List of possible basic king moves</returns> 
        private List<ChessMove> GenerateSimplePotentialMoves()
        {
            var moves = new List<ChessMove>();
            int[] deltas = { -1, 0, 1 };// Possible movement offsets in any direction

            // Iterate through all possible movement directions for the king
            foreach (int dx in deltas) {
                foreach (int dy in deltas) {
                    // Skip the case where the king doesn't move (dx == 0 and dy == 0)
                    if (dx != 0 || dy != 0) {
                        BoardCoords step = new BoardCoords(dx, dy);
                        BoardCoords nextSquare = coords + step;

                        // Check if the new position is inside the board and either empty or occupied by an opponent's piece
                        if (nextSquare.IsInsideBoard(chessBoard)) {                            
                            ChessPiece pieceAtDestination = chessBoard.GetPiece(nextSquare.i, nextSquare.j);
                            if (chessBoard.IsSquareEmpty(nextSquare) || pieceAtDestination.pieceColor != pieceColor) {
                                moves.Add(new ChessMove(this, nextSquare));
                            }
                        }
                    }
                }
            }
            return moves;
        }

        /// <summary>
        /// Generates all potential moves for the king, including both simple moves and castling
        /// </summary>
        /// <returns>Complete list of potential king moves</returns>
        public override List<ChessMove> GenerateAllPotentialMoves()
        {
            // Get the basic single-square moves first
            var moves = GenerateSimplePotentialMoves();

            // Add castling moves if conditions are met
            var kingStartPosition = new BoardCoords(4, pawnStartRow - pawnDirection);

            if (prevCoords.Equals(kingStartPosition)) // King has not moved from its starting position
            {
                // Check for kingside castling (to the right)
                if (CanCastleKingside()) {
                    BoardCoords kingsideCastleSquare = new BoardCoords(coords.i + 2, coords.j);
                    moves.Add(new ChessMove(this, kingsideCastleSquare));
                }

                // Check for queenside castling (to the left)
                if (CanCastleQueenside()) {
                    BoardCoords queensideCastleSquare = new BoardCoords(coords.i - 2, coords.j);
                    moves.Add(new ChessMove(this, queensideCastleSquare));
                }
            }

            return moves;
        }

        /// <summary>
        /// Verifies if kingside castling is possible
        /// </summary>
        /// <returns>True if kingside castling is allowed, false otherwise</returns>
        private bool CanCastleKingside()
        {
            // Determine opponent's color to check for attacks
            PieceColor opponentColor = pieceColor == PieceColor.White ? PieceColor.Black : PieceColor.White;

            // Check if the squares between king and rook are empty and not under attack
            for (int i = coords.i + 1; i <= coords.i + 2; i++) {
                if (chessBoard.GetPiece(i, coords.j) != null || chessBoard.IsSquareUnderAttack(new BoardCoords(i, coords.j), opponentColor)) {
                    return false; // Square is occupied or under attack
                }
            }

            // Check if the rook is in its initial position and has not moved
            var rookStartCoords = new BoardCoords(coords.i + 3, coords.j);
            ChessPiece rook = chessBoard.GetPiece(rookStartCoords);
            return rook is ChessRook && rook.prevCoords.Equals(rookStartCoords) && rook.pieceColor == pieceColor;
        }

        /// <summary>
        /// Verifies if queenside castling is possible
        /// </summary>
        /// <returns>True if queenside castling is allowed, false otherwise</returns>
        private bool CanCastleQueenside()
        {
            // Determine opponent's color to check for attacks
            PieceColor opponentColor = pieceColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
            
            // Check if the squares between king and rook are empty and not under attack
            for (int i = coords.i - 1; i >= coords.i - 3; i--) {
                if (chessBoard.GetPiece(i, coords.j) != null || chessBoard.IsSquareUnderAttack(new BoardCoords(i, coords.j), opponentColor)) {
                    return false; // Square is occupied or under attack
                }
            }


            // Check if the rook is in its initial position and has not moved
            var rookStartCoords = new BoardCoords(coords.i - 4, coords.j);
            ChessPiece rook = chessBoard.GetPiece(rookStartCoords);
            return rook is ChessRook && rook.prevCoords.Equals(rookStartCoords) && rook.pieceColor == pieceColor;
        }

        /// <summary>
        /// Generates a list of moves where the king can capture an opponent's piece
        /// </summary>
        /// <returns>List of possible capture moves for the king</returns>
        public override List<ChessMove> GenerateCaptureOpportunities()
        {
            // The movement and capture logic for the king is the same (generates simple moves)
            var moves = GenerateSimplePotentialMoves();
            return moves;
        }
    }
}
