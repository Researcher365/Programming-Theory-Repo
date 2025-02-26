using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum PieceColor { Black, White }
public enum PieceType { Pawn, Knight, Bishop, Rook, Queen, King }

public interface IChessPiece
{
    List<ChessMove> GenerateAllPotentialMoves();
    List<ChessMove> GenerateCaptureOpportunities();

}

public class ChessPiece: MonoBehaviour, IChessPiece
{
    public BoardCoords coords;
    public int prefabIndex;
    public float price, movementSpeed = 5f;
    public bool isSelected;
    public int lastMoveNumber;
    public BoardCoords prevCoords;

    public GameController gameController;
    public ChessBoard chessBoard;

    public float selectedHeight = 2f;

    private GameObject tempList;

    private Vector3 _targetPos;
    public Vector3 targetPos
    {
        get { return _targetPos; }
        set { _targetPos = value; }
    }
    public PieceColor pieceColor
    {
        get { return (prefabIndex < 6) ? PieceColor.White : PieceColor.Black; }
    }
    public PieceType pieceType
    {
        get { return (pieceColor == PieceColor.White) ? (PieceType)prefabIndex : (PieceType)(11 - prefabIndex); }
    }

    public bool isPawn
    {
        get { return pieceType == PieceType.Pawn; }
    }

    public int pawnDirection
    {
        get { return (pieceColor == PieceColor.White) ? 1 : -1; }
    }

    public bool isKing
    {
        get { return pieceType == PieceType.King; }
    }


    public static char PieceTypeSymbol(PieceType pieceType)
    {
        return pieceType switch {
            PieceType.Pawn => 'p',
            PieceType.Knight => 'n',
            PieceType.Bishop => 'b',
            PieceType.Rook => 'r',
            PieceType.Queen => 'q',
            PieceType.King => 'k',
            _ => throw new ArgumentException("Unknown piece type")
        };
    }
    public static PieceType ParsePieceType(char symbol)
    {
        return symbol switch {
            'q' => PieceType.Queen,
            'r' => PieceType.Rook,
            'b' => PieceType.Bishop,
            'n' => PieceType.Knight,
            _ => throw new ArgumentException("Invalid promotion piece type in UCI move")
        };
    }

    //public bool MovedInGame(ChessGame chessGame)
    //{
    //    return chessGame.FindPieceMoves(this).size > 0;
    //}

    public bool MovedInGame()
    {
        return lastMoveNumber != 0;
    }

    public int pawnStartLine
    {
        get {
            int lineOffset = 2;
            if (pieceColor == PieceColor.White)
                return lineOffset - 1;
            else
                return (int)chessBoard.jSize - lineOffset;
        }
    }
    public int pawnTransformLine
    {
        get {
            if (pieceColor == PieceColor.White)
                return (int)chessBoard.jSize - 1;
            else
                return 0;
        }
    }

    //----------------------------------------------------

    public ChessPiece CloneToBoard(ChessBoard toChessBoard)
    {
        GameObject newObject = Instantiate(gameObject, tempList.transform);
        ChessPiece newPiece = newObject.GetComponent<ChessPiece>();
        newPiece.CopyFrom(this);
        newPiece.chessBoard = toChessBoard;

        newObject.SetActive(false);
        return newPiece;
    }

    public void CopyFrom(ChessPiece originalPiece)
    {
        prefabIndex = originalPiece.prefabIndex;
        coords = new BoardCoords(originalPiece.coords);
        prevCoords = new BoardCoords(originalPiece.prevCoords);
        chessBoard = originalPiece.chessBoard;
        gameController = originalPiece.gameController;
        price = originalPiece.price;
        movementSpeed = originalPiece.movementSpeed;
        lastMoveNumber = originalPiece.lastMoveNumber;
        selectedHeight = originalPiece.selectedHeight;
        targetPos = originalPiece.targetPos;

    }

    public void SetSquareToSlideTo(BoardCoords toCoords)
    {
        targetPos = gameController.GetSquareCenterPosition(toCoords.i, toCoords.j);
    }

    public bool MustBeTransformed()
    {
        return coords.j == pawnTransformLine && isPawn;
    }

    public void Select()
    {
        targetPos = transform.position + Vector3.up * (selectedHeight - transform.position.y);

        isSelected = true;
    }

    public void Deselect()
    {

        targetPos = transform.position + Vector3.up * (0 - transform.position.y);

        isSelected = false;
    }

    public void SwitchSelection()
    {
        if (isSelected)
            Deselect();
        else
            Select();
    }

    public bool IsAvailableForMoving()
    {
        return chessBoard.whiteToMove && pieceColor == PieceColor.White
            || chessBoard.blackToMove && pieceColor == PieceColor.Black;
    }

    public bool isOnTargetPosition()
    {
        float delta = 0.1f;
        return (targetPos - transform.position).magnitude <= delta;
    }

    public bool IsTakeableEnPassant()
    {
        return isPawn
            && prevCoords.j == pawnStartLine
            && coords.j == pawnStartLine + 2 * pawnDirection
            && !IsAvailableForMoving()
            && lastMoveNumber == chessBoard.moveNumber - 1;
    }

    // check for legal move regardless of the check(shah)
    public bool CanBeMovedTo(BoardCoords toSquare)
    {
        ChessMove chessMove = new ChessMove(this, toSquare);
        chessMove.disabledCheck = true; // disabled Check Condition
        return chessMove.IsLegal();
    }

    public void MakeMove(BoardCoords toCoord)
    {
        ChessMove move = new ChessMove(this, toCoord);
        move.MakeOnBoard();

    }

    void Start()
    {
        gameController = GameObject.FindWithTag("GameController").GetComponent<GameController>();
        tempList = GameObject.Find("TemporaryPiecesList");
        if (chessBoard == null)
            chessBoard = gameController.chessBoard;
        lastMoveNumber = 0;
    }

    public void TranslateToTarget()
    {
        transform.position = Vector3.Lerp(transform.position, targetPos, movementSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        TranslateToTarget();
    }

    public virtual List<ChessMove> GenerateAllPotentialMoves()
    {
        return new List<ChessMove>();
    }

    public virtual List<ChessMove> GenerateCaptureOpportunities()
    {
        return new List<ChessMove>();
    }

    public virtual List<ChessMove> GeneratePossibleMoves()
    {
        var moves = GenerateAllPotentialMoves();

        // Фильтруем ходы, чтобы исключить ходы на клетки, занятые фигурами того же цвета
        // Используем LINQ для фильтрации ходов
        var result = moves
            .Where(move => move.to.IsInsideBoard(chessBoard) && 
                           (chessBoard.IsSquareEmpty(move.to) || move.attackedPiece.pieceColor != pieceColor))
            .ToList();


        return result;
    }
}
