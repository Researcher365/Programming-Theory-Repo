using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


namespace Assets.Scripts
{
    public class ChessBoard
    {
        private ChessPiece[,] board; // 2D array representing the chessboard with chess pieces
        public GameController gameController; // Reference to the game controller managing the game state
        [SerializeField] public bool isActive; // Indicates if the chessboard is currently active
        public int moveNumber; // Tracks the number of moves made in the game
        public bool _whiteToMove; // Indicates if it's white's turn to move; false means it's black's turn


        public ChessPiece whiteKing; // Reference to the white king piece on the board
        public ChessPiece blackKing; // Reference to the black king piece on the board

        public BoardCoords enPassant; // Coordinates for en passant capture

        // Property to check if it's black's turn to move
        public bool blackToMove 
        {
            get { return !_whiteToMove; }
            set { _whiteToMove = !value; }
        }

        // Property to check if it's white's turn to move
        public bool whiteToMove
        {
            get { return _whiteToMove; }
            set { _whiteToMove = value; }
        }

        // Property to get the number of rows on the board
        public uint iSize { get { return (uint)board.GetLength(0); } }
        
        // Property to get the number of columns on the board
        public uint jSize { get { return (uint)board.GetLength(1); } }

        // Constructor to initialize the chessboard with specified dimensions
        public ChessBoard(uint iSize, uint jSize)
        {
            board = new ChessPiece[iSize, jSize];
            gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
        }

        // Retrieves the chess piece at the specified coordinates
        public ChessPiece GetPiece(int i, int j)
        {
            BoardCoords coords = new BoardCoords(i, j);
            if (coords.IsInsideBoard(board))
                return board[i, j];

            return null;
        }

        // Retrieves the chess piece at the specified board coordinates
        public ChessPiece GetPiece(BoardCoords coords)
        {
            if (coords.IsInsideBoard(board))
                return board[coords.i, coords.j];

            return null;
        }

        // Checks if the specified square is empty
        public bool IsSquareEmpty(BoardCoords coords)
        {
            if (coords.IsInsideBoard(board)) {
                return GetPiece(coords) == null;
            }
            return false;
        }

        // Switches the turn to the opposite side
        public void SwitchMoveSide()
        {
            _whiteToMove = !_whiteToMove;
        }

        // Places a chess piece at the specified coordinates
        public void SetPiece(ChessPiece piece, BoardCoords coords)
        {
            if (coords.IsInsideBoard(board)) {
                board[coords.i, coords.j] = piece;
            }
            else
                Debug.Log("trying to set piece out of bounds of chessboard!");
        }

        // Checks if the path for a piece's move is correct and free of obstacles
        public bool WayCorrectAndFree(ChessPiece selectedPiece, BoardCoords start, BoardCoords end)
        {
            PieceType pieceType = selectedPiece.pieceType;

            if (pieceType == PieceType.Knight) return true;

            BoardCoords step = (end - start).normalized;


            if (pieceType == PieceType.Bishop && (step.i == 0 || step.j == 0)) return false; // way is not correct
            if (pieceType == PieceType.Rook && !(step.i == 0 || step.j == 0)) return false; // way is not correct

            BoardCoords cursor = start + step;
            int wayLength = 0;
            while (cursor.IsInsideBoard(this) && !cursor.Equals(end)) {

                if (!IsSquareEmpty(cursor))
                    return false;
                cursor = cursor + step;
                wayLength++;
            }

            if (pieceType == PieceType.King) {
                // standart king's move
                if (wayLength == 0)
                    return cursor.IsInsideBoard(this);
                // castle condition
                if (wayLength == 1 && step.j == 0 && !selectedPiece.MovedInGame()) {

                    // checking for attacked fields during the way
                    bool wayBeaten = false;

                    ChessPiece rookForCastle = GetPiece(end + step);
                    if (rookForCastle == null) rookForCastle = GetPiece(end + 2 * step);

                    bool rookConditionCorrect =
                        rookForCastle != null
                        && rookForCastle.pieceType == PieceType.Rook
                        && rookForCastle.IsAvailableForMoving()
                        && !rookForCastle.MovedInGame();

                    if (!wayBeaten && IsSquareEmpty(end) && rookConditionCorrect) {

                        return true;
                    }
                }
                else
                    return false;
            }
            return cursor.IsInsideBoard(this);
        }

        // Clears the chessboard of all pieces
        public void Clear()
        {
            int iSize = board.GetLength(0);
            int jSize = board.GetLength(1);

            for (int i = 0; i < iSize; i++)
                for (int j = 0; j < jSize; j++)
                    if (board[i, j] != null) {
                        Object.Destroy(board[i, j].gameObject);
                        board[i, j] = null;
                    }
            board = null;
            board = new ChessPiece[iSize, jSize];
        }

        // Creates a copy of the current chessboard
        public ChessBoard GetCopy()
        {
            ChessBoard virtualBoard = new ChessBoard(iSize, jSize);
            virtualBoard.Clear();
            for (int i = 0; i < iSize; i++)
                for (int j = 0; j < jSize; j++) {
                    BoardCoords coords = new BoardCoords(i, j);

                    ChessPiece originalPiece = GetPiece(coords);
                    ChessPiece newPiece = null;
                    if (originalPiece != null) {
                        newPiece = originalPiece.CloneToBoard(virtualBoard);
                    }

                    if (newPiece != null && newPiece.isKing) {
                        if (newPiece.pieceColor == PieceColor.White)
                            virtualBoard.whiteKing = newPiece;
                        if (newPiece.pieceColor == PieceColor.Black)
                            virtualBoard.blackKing = newPiece;
                    }
                    virtualBoard.SetPiece(newPiece, coords);
                }
            virtualBoard.whiteToMove = whiteToMove;            
            virtualBoard.moveNumber = moveNumber;
            return virtualBoard;
        }

        // Simulates a move on a virtual board and returns the resulting board state
        public ChessBoard VirtualBoardAfterFreeMove(ChessMove chessMove)
        {
            ChessBoard virtualBoard = this.GetCopy();
            ChessMove virtualMove = new ChessMove(chessMove, virtualBoard);
            virtualMove.MakeOnBoard();
            return virtualBoard;
        }

        // Checks if a specific square is under attack by the opponent
        public bool IsSquareUnderAttack(BoardCoords square, PieceColor opponentColor)
        {
            for (int i = 0; i < iSize; i++) {
                for (int j = 0; j < jSize; j++) {
                    ChessPiece piece = GetPiece(i, j);

                    if (piece != null && piece.pieceColor == opponentColor) {
                        List<ChessMove> captureMoves = piece.GenerateCaptureOpportunities();

                        foreach (var move in captureMoves) {
                            if (move.to.Equals(square)) {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        // Checks if the king of the specified color is in check
        public bool IsCheckOnTheBoard(PieceColor kingColor)
        {
            BoardCoords kingCoords;
            PieceColor otherColor = 1 - kingColor;

            if (kingColor == PieceColor.White)
                kingCoords = whiteKing.coords;
            else
                kingCoords = blackKing.coords;

            for (int i = 0; i < iSize; i++)
                for (int j = 0; j < jSize; j++)
                    if (board[i, j] != null) {

                        // Check if a piece of the opposite color can move to the king's square
                        if (board[i, j].pieceColor == otherColor
                            && board[i, j].CanBeMovedTo(kingCoords))
                            return true;
                    }
            return false;
        }

        // Places a new chess piece of the specified color and type at the given coordinates
        public ChessPiece PutNewPiece(PieceColor pieceColor, PieceType pieceType, BoardCoords coords)
        {
            int prefabIndex = (pieceColor == PieceColor.White) ? (int)pieceType : 11 - (int)pieceType;
            return PutNewPiece(prefabIndex, coords);
        }

        // Places a new chess piece using a prefab index at the given coordinates
        public ChessPiece PutNewPiece(int prefabIndex, BoardCoords coords)
        {
            ChessPiece chessPiece = gameController.CreateNewPiece3D(prefabIndex, coords);
            chessPiece.gameController = gameController;
            chessPiece.chessBoard = this;
            chessPiece.coords = new BoardCoords(coords);
            chessPiece.prevCoords = new BoardCoords(coords);
            chessPiece.prefabIndex = prefabIndex;
            SetPiece(chessPiece, coords);
            return chessPiece;
        }
        // Sets up the chess
        public void SetStartChessPosition()
        {
            int[,] startPosition = new int[8, 8]
            {
            { 3, 0, -1, -1, -1, -1, 11, 8 },
            { 1, 0, -1, -1, -1, -1, 11, 10 },
            { 2, 0, -1, -1, -1, -1, 11, 9 },
            { 4, 0, -1, -1, -1, -1, 11, 7 },
            { 5, 0, -1, -1, -1, -1, 11, 6 },
            { 2, 0, -1, -1, -1, -1, 11, 9 },
            { 1, 0, -1, -1, -1, -1, 11, 10 },
            { 3, 0, -1, -1, -1, -1, 11, 8 }
            };

            Clear();

            int iSize = startPosition.GetLength(0);
            int jSize = startPosition.GetLength(1);

            for (int i = 0; i < iSize; i++)
                for (int j = 0; j < jSize; j++)
                    if (startPosition[i, j] >= 0) {

                        ChessPiece chessPiece = PutNewPiece(startPosition[i, j], new BoardCoords(i, j));
                        board[i, j] = chessPiece;

                        if (chessPiece.isKing) {
                            if (chessPiece.pieceColor == PieceColor.White)
                                whiteKing = chessPiece;
                            if (chessPiece.pieceColor == PieceColor.Black)
                                blackKing = chessPiece;
                        }

                    }
                    else
                        board[i, j] = null;

            whiteToMove = true;
            moveNumber = 1;
            
        }
    }
}