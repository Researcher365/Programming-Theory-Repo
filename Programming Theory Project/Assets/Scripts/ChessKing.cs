using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Assets.Scripts
{
    public class ChessKing: ChessPiece, IChessPiece
    {
        private List<ChessMove> GenerateSimplePotentialMoves()
        {
            var moves = new List<ChessMove>();
            int[] deltas = { -1, 0, 1 };

            // Перебираем все возможные направления движения для короля
            foreach (int dx in deltas) {
                foreach (int dy in deltas) {
                    // Исключаем случай, когда король не двигается (dx == 0 и dy == 0)
                    if (dx != 0 || dy != 0) {
                        BoardCoords step = new BoardCoords(dx, dy);
                        BoardCoords nextSquare = coords + step;

                        // Проверяем, находится ли новая позиция внутри доски и свободна ли она или занята фигурой противника
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
        public override List<ChessMove> GenerateAllPotentialMoves()
        {
            
            var moves = GenerateSimplePotentialMoves();

            // Castling logic
            var kingStartPosition = new BoardCoords(4, pawnStartLine - pawnDirection);

            if (prevCoords.Equals(kingStartPosition)) // King has not moved
            {
                // Check for kingside castling
                if (CanCastleKingside()) {
                    BoardCoords kingsideCastleSquare = new BoardCoords(coords.i + 2, coords.j);
                    moves.Add(new ChessMove(this, kingsideCastleSquare));
                }

                // Check for queenside castling
                if (CanCastleQueenside()) {
                    BoardCoords queensideCastleSquare = new BoardCoords(coords.i - 2, coords.j);
                    moves.Add(new ChessMove(this, queensideCastleSquare));
                }
            }

            return moves;
        }

        private bool CanCastleKingside()
        {
            PieceColor opponentColor = pieceColor == PieceColor.White ? PieceColor.Black : PieceColor.White;

            // Check if the squares between king and rook are empty and not under attack
            for (int i = coords.i + 1; i <= coords.i + 2; i++) {
                if (chessBoard.GetPiece(i, coords.j) != null || chessBoard.IsSquareUnderAttack(new BoardCoords(i, coords.j), opponentColor)) {
                    return false;
                }
            }

            // Check if the rook is in its initial position and has not moved
            var rookStartCoords = new BoardCoords(coords.i + 3, coords.j);
            ChessPiece rook = chessBoard.GetPiece(rookStartCoords);
            return rook is ChessRook && rook.prevCoords.Equals(rookStartCoords) && rook.pieceColor == pieceColor;
        }

        private bool CanCastleQueenside()
        {
            PieceColor opponentColor = pieceColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
            // Check if the squares between king and rook are empty and not under attack
            for (int i = coords.i - 1; i >= coords.i - 3; i--) {
                if (chessBoard.GetPiece(i, coords.j) != null || chessBoard.IsSquareUnderAttack(new BoardCoords(i, coords.j), opponentColor)) {
                    return false;
                }
            }


            // Check if the rook is in its initial position and has not moved
            var rookStartCoords = new BoardCoords(coords.i - 4, coords.j);
            ChessPiece rook = chessBoard.GetPiece(rookStartCoords);
            return rook is ChessRook && rook.prevCoords.Equals(rookStartCoords) && rook.pieceColor == pieceColor;
        }


        public override List<ChessMove> GenerateCaptureOpportunities()
        {
            // Логика движения и взятия для короля одинакова
            var moves = GenerateSimplePotentialMoves();

            // Исключаем рокировку из списка возможных взятий
            var captureMoves = moves
                .Where(move => !IsCastlingMove(move))
                .ToList();

            return captureMoves;
        }

        private bool IsCastlingMove(ChessMove move)
        {
            // Проверяем, является ли ход рокировкой
            return Math.Abs(move.to.i - move.from.i) == 2 && move.from.j == pawnStartLine - pawnDirection;
        }

    }
}
