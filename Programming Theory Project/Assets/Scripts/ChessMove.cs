using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{

    public class ChessMove
    {
        public ChessPiece piece;
        public BoardCoords from;
        public BoardCoords to;
        public int number;
        public ChessBoard chessBoard;
        public PieceType requestedTransformPiece;

        public bool disabledCheck;

        public bool isPawnMoving { get { return piece.isPawn; } }

        public string notation
        {
            get {
                char requestedPieceSymbol = ' ';
                if (piece.isPawn && to.j == piece.pawnTransformLine)
                    requestedPieceSymbol = ChessPiece.PieceTypeSymbol(requestedTransformPiece);

                if (requestedPieceSymbol == ' ')
                    return from.notation + to.notation;
                else
                    return from.notation + to.notation + requestedPieceSymbol;
            }
        }

        public ChessMove(ChessPiece chessPiece, BoardCoords to)
        {
            this.piece = chessPiece;
            this.chessBoard = chessPiece.chessBoard;
            this.from = chessPiece.coords;
            this.to = to;
            number = chessBoard.moveNumber;
        }
        public ChessMove(ChessMove move, ChessBoard chessBoard)
        {
            this.from = move.from;
            this.to = move.to;
            this.chessBoard = chessBoard;
            piece = chessBoard.GetPiece(from);
            number = chessBoard.moveNumber;
        }
        public ChessMove(BoardCoords from, BoardCoords to, ChessBoard chessBoard)
        {
            this.from = from;
            this.to = to;
            this.chessBoard = chessBoard;
            piece = chessBoard.GetPiece(from);
            number = chessBoard.moveNumber;
        }

        public ChessMove(BoardCoords from, BoardCoords to)
        {
            this.from = from;
            this.to = to;
            this.chessBoard = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>().chessBoard;
            piece = chessBoard.GetPiece(from);
            number = chessBoard.moveNumber;
        }
        public bool IsACastleMove()
        {
            BoardCoords step = (to - from).normalized;
            return piece.isKing && to.Equals(from + 2 * step);

        }

        public ChessPiece attackedPiece
        {
            get {
                if (chessBoard == null) return null;
                return chessBoard.GetPiece(to);
            }
        }

        // this method searching for pawn, which could be taken "en passant"
        public ChessPiece GetEnPassantPawn()
        {
            return chessBoard.GetPiece(to.i, from.j);
        }

        public bool isEnPassant()
        {
            return to.Equals(chessBoard.enPassant)
                   && chessBoard.enPassant.isActive
                   && isPawnMoving;
        }


        public void MakeOnBoard()
        {
            BoardCoords step = (to - from).normalized;
            chessBoard.enPassant.isActive = false;
            piece.prevCoords = new BoardCoords(from);

            chessBoard.SetPiece(piece, to);
            chessBoard.SetPiece(null, from);

            piece.coords = new BoardCoords(to);

            // if castle then also move the rook 
            if (piece.isKing && (to.Equals(from + 2 * step))) {

                ChessPiece rookForCastle = chessBoard.GetPiece(to + step);
                if (rookForCastle == null)
                    rookForCastle = chessBoard.GetPiece(to + 2 * step);
                if (rookForCastle != null) {
                    ChessMove additionalMove = new ChessMove(rookForCastle, to - step);
                    additionalMove.MakeOnBoard(); // and in the end it will switch move side!
                }
            }
            else
                chessBoard.SwitchMoveSide();

            chessBoard.moveNumber++;

            piece.lastMoveNumber = chessBoard.moveNumber;
            number = chessBoard.moveNumber;

            if (isPawnMoving && (to - from).magnitude == 2) {
                chessBoard.enPassant = from + step;
                chessBoard.enPassant.isActive = true;
            }

            piece.SetSquareToSlideTo(to);

        }

        public bool IsLegal()
        {
            if (piece == null || chessBoard == null) {
                return false;
            }

            // Получаем все возможные ходы для текущей фигуры
            List<ChessMove> possibleMoves = piece.GeneratePossibleMoves();

            // Проверяем, является ли текущий ход легальным
            foreach (var move in possibleMoves) {
                if (to.Equals(move.to)) {
                    if (!disabledCheck) {
                        // Проверяем, не находится ли король под шахом после хода
                        ChessBoard virtualBoard = chessBoard.VirtualBoardAfterFreeMove(move);
                        bool isCheckOnTheBoard = virtualBoard.IsCheckOnTheBoard(piece.pieceColor);
                        virtualBoard.Clear(); // очищаем временную доску
                        return !isCheckOnTheBoard;
                    }
                    else {
                        return true;
                    }
                }
            }

            return false;
        }

    }
}