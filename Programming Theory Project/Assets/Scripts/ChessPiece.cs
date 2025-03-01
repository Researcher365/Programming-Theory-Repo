using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

// Enum to represent the color of chess pieces
public enum PieceColor { Black, White }

// Enum to represent different types of chess pieces
public enum PieceType { Pawn, Knight, Bishop, Rook, Queen, King }

// Interface defining the required functionality for all chess pieces
public interface IChessPiece
{
    // Generates all potential moves for a piece, regardless of their legality
    List<ChessMove> GenerateAllPotentialMoves();

    // Generates all capture opportunities a piece has, regardless of opponent's pieces
    List<ChessMove> GenerateCaptureOpportunities();

}

public class ChessPiece: MonoBehaviour, IChessPiece
{
    // Current position of the piece on the board
    public BoardCoords coords;

    // Index of the prefab used to render this chess piece
    public int prefabIndex;

    // The piece's value and its movement speed for animations
    public float price, movementSpeed = 5f;

    // Whether this piece is currently selected by the player
    public bool isSelected;

    // The last move number when this piece was moved
    public int lastMoveNumber;

    // Previous coordinates of the piece, used to track initial positions
    public BoardCoords prevCoords;

    // Reference to the game controller
    public GameController gameController;

    // Reference to the chess board this piece belongs to
    public ChessBoard chessBoard;

    // Height offset when the piece is selected
    public float selectedHeight = 2f;

    // Reference to a temporary GameObject for cloning pieces
    private GameObject tempList;

    // The target position for piece movement animations
    private Vector3 _targetPos;
    public Vector3 targetPos
    {
        get { return _targetPos; }
        set { _targetPos = value; }
    }

    // Determines the piece color based on prefab index
    public PieceColor pieceColor
    {
        get { return (prefabIndex < 6) ? PieceColor.White : PieceColor.Black; }
    }

    // Determines the piece type based on prefab index and color
    public PieceType pieceType
    {
        get { return (pieceColor == PieceColor.White) ? (PieceType)prefabIndex : (PieceType)(11 - prefabIndex); }
    }

    // Checks if this piece is a pawn
    public bool isPawn
    {
        get { return pieceType == PieceType.Pawn; }
    }

    // Gets the direction of pawn movement (up for white, down for black)
    public int pawnDirection
    {
        get { return (pieceColor == PieceColor.White) ? 1 : -1; }
    }

    // Checks if this piece is a king
    public bool isKing
    {
        get { return pieceType == PieceType.King; }
    }

    /// <summary>
    /// Converts a piece type to its character representation
    /// </summary>
    /// <param name="pieceType">The type of chess piece</param>
    /// <returns>Character representing the piece type</returns>
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

    /// <summary>
    /// Parses a character into a piece type for promotions
    /// </summary>
    /// <param name="symbol">Character representing a piece</param>
    /// <returns>The corresponding piece type</returns>
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

    /// <summary>
    /// Checks if the piece has moved during the game
    /// </summary>
    /// <returns>True if the piece has moved, false otherwise</returns>
    public bool MovedInGame()
    {
        return lastMoveNumber != 0;
    }

    // Gets the starting row for pawns based on their color
    public int pawnStartRow
    {
        get {
            int lineOffset = 2;
            if (pieceColor == PieceColor.White)
                return lineOffset - 1;
            else
                return (int)chessBoard.jSize - lineOffset;
        }
    }

    // Gets the promotion row for pawns based on their color
    public int pawnPromotionRow
    {
        get {
            if (pieceColor == PieceColor.White)
                return (int)chessBoard.jSize - 1;
            else
                return 0;
        }
    }

    /// <summary>
    /// Creates a clone of this piece on another chess board
    /// </summary>
    /// <param name="toChessBoard">The target chess board</param>
    /// <returns>The cloned chess piece</returns>
    public ChessPiece CloneToBoard(ChessBoard toChessBoard)
    {
        GameObject newObject = Instantiate(gameObject, tempList.transform);
        ChessPiece newPiece = newObject.GetComponent<ChessPiece>();
        newPiece.CopyFrom(this);
        newPiece.chessBoard = toChessBoard;

        newObject.SetActive(false);
        return newPiece;
    }

    /// <summary>
    /// Copies all properties from another chess piece
    /// </summary>
    /// <param name="originalPiece">The piece to copy from</param>
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

    /// <summary>
    /// Sets the target position for the piece to slide to
    /// </summary>
    /// <param name="toCoords">The destination coordinates</param>
    public void SetSquareToSlideTo(BoardCoords toCoords)
    {
        targetPos = gameController.GetSquareCenterPosition(toCoords.i, toCoords.j);
    }

    /// <summary>
    /// Checks if a pawn must be promoted
    /// </summary>
    /// <returns>True if the pawn has reached the promotion rank</returns>
    public bool MustBePromoted()
    {
        return coords.j == pawnPromotionRow && isPawn;
    }

    /// <summary>
    /// Selects this piece, raising it above the board
    /// </summary>
    public void Select()
    {
        targetPos = transform.position + Vector3.up * (selectedHeight - transform.position.y);

        isSelected = true;
    }

    /// <summary>
    /// Deselects this piece, returning it to the board surface
    /// </summary>
    public void Deselect()
    {

        targetPos = transform.position + Vector3.up * (0 - transform.position.y);

        isSelected = false;
    }

    /// <summary>
    /// Toggles the selection state of this piece
    /// </summary>
    public void SwitchSelection()
    {
        if (isSelected)
            Deselect();
        else
            Select();
    }

    /// <summary>
    /// Checks if this piece can be moved based on the current turn
    /// </summary>
    /// <returns>True if the piece can be moved, false otherwise</returns>
    public bool IsAvailableForMoving()
    {
        return chessBoard.whiteToMove && pieceColor == PieceColor.White
            || chessBoard.blackToMove && pieceColor == PieceColor.Black;
    }

    /// <summary>
    /// Checks if the piece has reached its target position
    /// </summary>
    /// <returns>True if the piece is at its target position</returns>
    public bool isOnTargetPosition()
    {
        float delta = 0.1f;
        return (targetPos - transform.position).magnitude <= delta;
    }

    /// <summary>
    /// Checks if this pawn can be captured en passant
    /// </summary>
    /// <returns>True if the pawn can be captured en passant</returns>
    public bool IsTakeableEnPassant()
    {
        return isPawn
            && prevCoords.j == pawnStartRow
            && coords.j == pawnStartRow + 2 * pawnDirection
            && !IsAvailableForMoving()
            && lastMoveNumber == chessBoard.moveNumber - 1;
    }

    /// <summary>
    /// Checks if this piece can legally move to the specified square, ignoring check
    /// </summary>
    /// <param name="toSquare">The destination square</param>
    /// <returns>True if the move is legal, ignoring check</returns>
    public bool CanBeMovedTo(BoardCoords toSquare)
    {
        ChessMove chessMove = new ChessMove(this, toSquare);
        chessMove.disabledCheck = true; // disabled Check Condition
        return chessMove.IsLegal();
    }

    /// <summary>
    /// Makes a move to the specified coordinates
    /// </summary>
    /// <param name="toCoord">The destination coordinates</param>
    public void MakeMove(BoardCoords toCoord)
    {
        ChessMove move = new ChessMove(this, toCoord);
        move.MakeOnBoard();

    }

    /// <summary>
    /// Called when the piece is instantiated
    /// </summary>
    void Start()
    {
        gameController = GameObject.FindWithTag("GameController").GetComponent<GameController>();
        tempList = GameObject.Find("TemporaryPiecesList");
        if (chessBoard == null)
            chessBoard = gameController.chessBoard;
        lastMoveNumber = 0;
    }

    /// <summary>
    /// Moves the piece toward its target position
    /// </summary>
    public void TranslateToTarget()
    {
        transform.position = Vector3.Lerp(transform.position, targetPos, movementSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Updates the piece position every frame
    /// </summary>
    void LateUpdate()
    {
        TranslateToTarget();
    }

    /// <summary>
    /// Base implementation for generating all potential moves
    /// Overridden by derived piece classes
    /// </summary>
    /// <returns>List of potential moves</returns>
    public virtual List<ChessMove> GenerateAllPotentialMoves()
    {
        return new List<ChessMove>();
    }

    /// <summary>
    /// Base implementation for generating capture opportunities
    /// Overridden by derived piece classes
    /// </summary>
    /// <returns>List of potential capture moves</returns>
    public virtual List<ChessMove> GenerateCaptureOpportunities()
    {
        return new List<ChessMove>();
    }

    /// <summary>
    /// Generates all legal moves for this piece
    /// </summary>
    /// <returns>List of legal moves</returns>
    public virtual List<ChessMove> GeneratePossibleMoves()
    {
        var moves = GenerateAllPotentialMoves(); // Uses polymorphism to call the appropriate derived class implementation

        // Filter moves to exclude those targeting squares with friendly pieces
        // Using LINQ for filtering
        var result = moves
            .Where(move => move.to.IsInsideBoard(chessBoard) && 
                           (chessBoard.IsSquareEmpty(move.to) || move.attackedPiece.pieceColor != pieceColor))
            .ToList();


        return result;
    }
}
