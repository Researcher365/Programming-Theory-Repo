using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;

public class PiecesCounter
{
    public struct PieceKey
    {
        public PieceColor color;
        public PieceType type;

        public PieceKey(ChessPiece piece)
        {
            color = piece.pieceColor;
            type = piece.pieceType;
        }
        public PieceKey(PieceColor pColor, PieceType pType)
        {
            this.color = pColor;
            this.type = pType;
        }
    }


    private Dictionary<PieceKey, int> counter;
    public PiecesCounter()
    {
        counter = new Dictionary<PieceKey, int>();
    }

    public void AddPiece(ChessPiece piece)
    {
        PieceKey key = new PieceKey(piece);
        if (counter.ContainsKey(key)) {
            counter[key]++;
        }
        else {
            counter.Add(key, 1);
        }
    }

    public void Count(ChessBoard board)
    {
        counter.Clear();

        for (int i = 0; i < board.iSize; i++)
            for (int j = 0; j < board.iSize; j++) {
                ChessPiece piece = board.GetPiece(i, j);
                if (piece != null)
                    AddPiece(piece);
            }
    }

    public string ToString()
    {
        Dictionary<PieceType, string> typeNames = new Dictionary<PieceType, string>();
        typeNames.Add(PieceType.Pawn, "pawns");
        typeNames.Add(PieceType.Knight, "knights");
        typeNames.Add(PieceType.Bishop, "bishops");
        typeNames.Add(PieceType.Rook, "rooks");
        typeNames.Add(PieceType.Queen, "queens");

        string whiteCounterStr = "White has:\n";
        foreach (PieceType pieceType in typeNames.Keys) {
            whiteCounterStr += "  " + typeNames[pieceType] + " = " + counter[new PieceKey(PieceColor.White, pieceType)] + "\n";
        }

        string blackCounterStr = "Black has:\n";
        foreach (PieceType pieceType in typeNames.Keys) {
            blackCounterStr += "  " + typeNames[pieceType] + " = " + counter[new PieceKey(PieceColor.Black, pieceType)] + "\n";
        }

        return whiteCounterStr + blackCounterStr;
    }

}
