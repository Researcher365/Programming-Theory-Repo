using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts
{
    /// <summary>
    /// Represents a pawn chess piece with its specific movement and capture rules
    /// </summary>
    public class ChessPawn: ChessPiece, IChessPiece // INHERITANCE from ChessPiece base class
    {
        /// <summary>
        /// Determines if the pawn is at its starting position
        /// </summary>
        public bool AtStartPosition { get { return coords.j == pawnStartRow; } }

        /// <summary>
        /// Generates all possible capture opportunities for a pawn
        /// </summary>
        /// <returns>List of potential capturing moves</returns>
        public override List<ChessMove> GenerateCaptureOpportunities()
        {
            var moves = new List<ChessMove>();

            // Pawns capture diagonally forward
            moves.Add(new ChessMove(this, coords + new BoardCoords(+1, pawnDirection)));
            moves.Add(new ChessMove(this, coords + new BoardCoords(-1, pawnDirection)));
            return moves;
        }

        /// <summary>
        /// Generates all potential forward moves for a pawn (non-capturing)
        /// </summary>
        /// <returns>List of potential forward moves</returns>
        public override List<ChessMove> GenerateAllPotentialMoves()
        {
            var moves = new List<ChessMove>();

            // Standard one-square forward move
            moves.Add(new ChessMove(this, coords + new BoardCoords(0, pawnDirection)));

            // Two-square forward move from starting position
            if (AtStartPosition) {
                moves.Add(new ChessMove(this, coords + new BoardCoords(0, 2 * pawnDirection)));
            }
            return moves;
        }

        /// <summary>
        /// Generates all valid moves for this pawn, including both forward moves and captures
        /// </summary>
        /// <returns>List of all legal pawn moves</returns>
        public override List<ChessMove> GeneratePossibleMoves()
        {
            // Get standard forward moves and potential diagonal captures
            var moves = GenerateAllPotentialMoves();
            var captures = GenerateCaptureOpportunities();

            // Filter forward moves: they must be inside the board and target square must be empty
            var validMoves = moves
                .Where(move =>
                    move.to.IsInsideBoard(chessBoard) &&
                    chessBoard.IsSquareEmpty(move.to)
                )
                .ToList();

            // Filter capturing moves: they must be inside the board and
            // either capture an enemy piece or be an en passant capture
            var validCaptures = captures
                .Where(move =>
                    move.to.IsInsideBoard(chessBoard) &&
                    (move.isEnPassant()
                        || (move.attackedPiece != null
                            && move.attackedPiece.pieceColor != pieceColor))
                )
                .ToList();

            // Combine both move types into a single result list
            var result = new List<ChessMove>();
            result.AddRange(validMoves);
            result.AddRange(validCaptures);

            return result;
        }


    }
}