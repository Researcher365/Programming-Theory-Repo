
using Assets.Scripts;
using Cinemachine;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using TMPro;
using UnityEngine.UIElements;
using System.Collections.Generic;

public enum ChessSideControllerType { human, engine };

public struct ScreenStatus
{
    public GameObject screen;
    public bool isActive;
    public ScreenStatus(GameObject screen, bool isActive)
    {
        this.screen = screen;
        this.isActive = isActive;
    }
}

public struct ScreenStatuses
{
    public List<ScreenStatus> Statuses;

    public ScreenStatuses(GameController gController)
    {
        Statuses = new List<ScreenStatus>();
        Statuses.Add(new ScreenStatus(gController.gameControlScreen, gController.gameControlScreen.activeInHierarchy));
        Statuses.Add(new ScreenStatus(gController.mainMenuScreen, gController.mainMenuScreen.activeInHierarchy));
        Statuses.Add(new ScreenStatus(gController.pieceChoiceScreen, gController.pieceChoiceScreen.activeInHierarchy));
        Statuses.Add(new ScreenStatus(gController.settingsScreen, gController.settingsScreen.activeInHierarchy));
    }

    public void RecoverToGameController(GameController gController)
    {
        foreach (var Status in Statuses)
            Status.screen.SetActive(Status.isActive);

        gController.UpdateCameraPosition();
    }

}

public class GameController: MonoBehaviour
{
    public GameObject boardSurface;
    Vector3 bounds;

    public Camera mainCamera;
    public GameObject[] piecesPrefabs;
    public ChessBoard chessBoard;
    public ChessSideControllerType whiteController, blackController;

    private const uint boardHorizontalSquaresCount = 8;
    private const uint boardVerticalSquaresCount = 8;
    private bool cancelEngineCalculationRequested = false;

    [SerializeField] private ChessPiece selectedPiece;
    [SerializeField] ChessEngineInterface engineInterface;
    [SerializeField] public GameObject pieceChoiceScreen;
    [SerializeField] public GameObject mainMenuScreen;
    [SerializeField] public GameObject settingsScreen;
    [SerializeField] public GameObject gameControlScreen;
    [SerializeField] TMP_Dropdown levelDropdown;
    [SerializeField] TMP_Text counterText;

    public PiecesCounter piecesCounter;

    [SerializeField] public CinemachineVirtualCamera playerCamera;

    Stack<ScreenStatuses> screenStatusStack;

    private BoardCoords pawnTransformCoord;

    //==== GETTERS

    public bool isEngineTurn
    {
        get {
            return
            chessBoard.whiteToMove && whiteController == ChessSideControllerType.engine ||
            chessBoard.blackToMove && blackController == ChessSideControllerType.engine;
        }
    }

    //==== METHODS

    public void FlipBoard()
    {
        GameObject target = GameObject.Find("TargetPoint");

        target.transform.rotation = Quaternion.LookRotation(Vector3.Project(target.transform.forward, Vector3.forward).normalized);
        target.transform.Rotate(Vector3.up, 180);
    }

    public void InitGame()
    {
        if (chessBoard != null)
            chessBoard.Clear();
        selectedPiece = null;
        chessBoard = new ChessBoard(boardHorizontalSquaresCount, boardVerticalSquaresCount);
        chessBoard.gameController = this;
        chessBoard.isActive = false;

        GoToScreen(mainMenuScreen);
    }

    // Starts new game after menu-button clicked.
    public void StartNewGame(int gameType)
    {
        screenStatusStack.Clear();
        screenStatusStack.Push(new ScreenStatuses(this));
        


        chessBoard.SetStartChessPosition();

        piecesCounter = new PiecesCounter();
        piecesCounter.Count(chessBoard);
        counterText.text = piecesCounter.ToString();


        switch (gameType) {
            case 0:
                whiteController = ChessSideControllerType.human;
                blackController = ChessSideControllerType.human;
                break;
            case 1:
                whiteController = ChessSideControllerType.human;
                blackController = ChessSideControllerType.engine;
                break;
            case 2:
                whiteController = ChessSideControllerType.engine;
                blackController = ChessSideControllerType.human;
                break;
            case 3:
                whiteController = ChessSideControllerType.engine;
                blackController = ChessSideControllerType.engine;
                break;
        }
        chessBoard.isActive = true;

        GoToScreen(gameControlScreen);
    }

    public void UpdateCameraPosition()
    {
        playerCamera.Priority = 
            (mainMenuScreen.activeInHierarchy 
            || settingsScreen.activeInHierarchy) ? 1 : 10;
    }

    void LookToMainMenuScreen(bool isActive)
    {
        mainMenuScreen.SetActive(isActive);
        UpdateCameraPosition();
    }

    void GoToScreen(GameObject screen)
    {
        ScreenStatuses statuses = new ScreenStatuses(this);

        screen.SetActive(true);

        foreach (ScreenStatus otherStatus in statuses.Statuses)
            if (otherStatus.screen != screen)
                otherStatus.screen.SetActive(false);

        UpdateCameraPosition();
    }

    public float squareSizeX
    {
        get { return 2.0f * bounds.x / chessBoard.iSize; }
    }
    public float squareSizeZ
    {
        get { return 2.0f * bounds.x / chessBoard.iSize; }
    }

    public Vector3 GetSquareCenterPosition(BoardCoords coords)
    {
        return GetSquareCenterPosition(coords.i, coords.j);
    }

    public Vector3 GetSquareCenterPosition(int i, int j)
    {
        Vector3 pos = new Vector3(0, 0, 0);
        pos.x = -bounds.x + i * squareSizeX + squareSizeX * 0.5f;
        pos.z = -bounds.z + j * squareSizeZ + squareSizeZ * 0.5f;
        return pos;
    }

    public BoardCoords GetBoardCoordinates(Vector3 mouseClickPosition)
    {
        BoardCoords coords = new BoardCoords(-1, -1);

        Ray ClickRay = mainCamera.ScreenPointToRay(mouseClickPosition);

        if (Physics.Raycast(ClickRay, out var hit))  // Если луч пересекает объект
        {
            // Получаем мировую позицию объекта
            Vector3 worldPosition = hit.point;
            float squareXSize = 2.0f * bounds.x / chessBoard.iSize;
            float squareZSize = 2.0f * bounds.z / chessBoard.jSize;
            coords.i = Mathf.FloorToInt((bounds.x + worldPosition.x) / squareXSize);
            coords.j = Mathf.FloorToInt((bounds.z + worldPosition.z) / squareZSize);
        }
        return coords;
    }

    public ChessPiece CreateNewPiece3D(int prefabIndex, BoardCoords coords)
    {
        GameObject prefab = piecesPrefabs[prefabIndex];
        Vector3 startPos = GetSquareCenterPosition(coords);
        GameObject newPiece = Instantiate(prefab, startPos, prefab.transform.rotation, GameObject.Find("ActivePieces").transform);
        ChessPiece chessPiece = newPiece.GetComponent<ChessPiece>();
        chessPiece.targetPos = startPos;

        return chessPiece;
    }

    void OnBoardSquareOrPieceDown(BoardCoords clickedCoords)
    {
        Debug.Log("board coords: " + clickedCoords.ToString());
        ChessPiece pieceOnClickedSquare = chessBoard.GetPiece(clickedCoords);


        if (selectedPiece != null && selectedPiece != pieceOnClickedSquare)
            selectedPiece.Deselect();

        // if there is a piece on clicked position and it could be selected
        if (!chessBoard.IsSquareEmpty(clickedCoords) && pieceOnClickedSquare.IsAvailableForMoving()) {

            pieceOnClickedSquare.SwitchSelection();
            if (pieceOnClickedSquare.isSelected)
                selectedPiece = pieceOnClickedSquare;
            else
                selectedPiece = null;
        }
        // if there is a selected and no the same color piece on clicked square
        else if (selectedPiece != null) {

            ChessMove move = new ChessMove(selectedPiece, clickedCoords);
            ChessBoard virtualBoard = chessBoard.VirtualBoardAfterFreeMove(move);

            if (move.IsLegal() && !(virtualBoard.IsCheckOnTheBoard(selectedPiece.pieceColor)))
                // moving selected piece if possible and no check
                MakeInteractiveMove(move);

            virtualBoard.Clear(); // destroying temp pieces
        }

        piecesCounter.Count(chessBoard);
        counterText.text = piecesCounter.ToString();
    }

    public void OnLevelChanged()
    {
        //OnDropdownValueChanged onDropdownValueChanged;
        //GameObject.Find("Dropdown").GetComponent<DropdownTextMeshPro>();

        string elo = levelDropdown.value switch {
            0 => "2100",
            1 => "2250",
            2 => "2350",
            3 => "2450",
            4 => "2550",
            5 => "2650",
            6 => "2750",
            _ => "3190"
        };
        engineInterface.SetEngineOption("UCI_Elo", elo);

        Debug.Log($"New engine's ELO: {elo}");
    }

    public void OnSettingsClick()
    {
        screenStatusStack.Push(new ScreenStatuses(this));
        GoToScreen(settingsScreen);
    }

    void SwitchEngineEnabled()
    {
        if (chessBoard.whiteToMove) {
            if (whiteController == ChessSideControllerType.human)
                whiteController = ChessSideControllerType.engine;
            else
                whiteController = ChessSideControllerType.human;
        }
        else if (chessBoard.blackToMove) {
            if (blackController == ChessSideControllerType.human)
                blackController = ChessSideControllerType.engine;
            else
                blackController = ChessSideControllerType.human;
        }

    }

    void OnPlayerInput()
    {
        // Independent input
        if (Input.GetKeyDown(KeyCode.Escape)) {
            ScreenStatuses status = new ScreenStatuses(this);

            if (gameControlScreen.activeInHierarchy) {
                screenStatusStack.Push(status);
                GoToScreen(mainMenuScreen);
            }
            else if (screenStatusStack.Count > 0) {
                screenStatusStack.Pop().RecoverToGameController(this);
            }
            else {
                screenStatusStack.Push(status);
                GoToScreen(mainMenuScreen);

                cancelEngineCalculationRequested = true;
                engineInterface.StopCalculation();
            }

        }

        if (Input.GetKeyDown(KeyCode.E)) {
            cancelEngineCalculationRequested = true;
            engineInterface.StopCalculation();
            SwitchEngineEnabled();
        }

        if (Input.GetMouseButtonDown(1)) // Right mouse click
            FlipBoard();
        float horInput = Input.GetAxis("Horizontal");
        GameObject.Find("TargetPoint").transform.Rotate(Vector3.up, -horInput * 40 * Time.deltaTime);

        // Conditional input
        if (chessBoard.isActive && !isEngineTurn) {
            if (Input.GetMouseButtonDown(0)) // Left mouse click
            {
                Vector3 mousePos = Input.mousePosition;
                BoardCoords coords = GetBoardCoordinates(mousePos);
                if (coords.IsInsideBoard(chessBoard))
                    OnBoardSquareOrPieceDown(coords);
                else
                    Debug.Log("out of board!");

            }
        }


    }

    IEnumerator OnFinishPieceMovement()
    {
        while (!selectedPiece.isOnTargetPosition()) {
            yield return new YieldInstruction();
        }


        if (whiteController == blackController) // ' human vs human ' OR ' engine vs engine '
            FlipBoard();

        if (!pieceChoiceScreen.activeSelf)
            chessBoard.isActive = true;
    }

    void MakeMoveWithoutTransformation(ChessMove move)
    {
        selectedPiece = move.piece;
        selectedPiece.SetSquareToSlideTo(move.to);

        // 1. destroy "captured" piece, if exist
        if (!chessBoard.IsSquareEmpty(move.to)) {
            Destroy(move.attackedPiece.gameObject);
            chessBoard.SetPiece(null, move.to);
        }
        else if (move.isEnPassant()) {
            // if there is a pawn 'En Passant'
            ChessPiece enPassantPawn = move.GetEnPassantPawn();
            if (enPassantPawn != null) {
                chessBoard.SetPiece(null, enPassantPawn.coords);
                Destroy(enPassantPawn.gameObject);
                enPassantPawn = null;
            }
        }

        // 2. make engine's move on board
        move.MakeOnBoard();

    }

    void MakeInteractiveMove(ChessMove move)
    {
        chessBoard.isActive = false;

        MakeMoveWithoutTransformation(move);
        // if piece must be transformed...
        if (move.piece.MustBeTransformed()) {
            // ask user, which type of piece he wanna put?
            AskPawnTransformation(move.to);
        }
        StartCoroutine(OnFinishPieceMovement());
    }

    void AskPawnTransformation(BoardCoords coords)
    {
        GoToScreen(pieceChoiceScreen);
        pawnTransformCoord = coords;
    }

    public void TransformPawn(ChessPiece selectedPiece, PieceType toPieceType)
    {
        // replace old piece with a new one
        chessBoard.PutNewPiece(selectedPiece.pieceColor, toPieceType, pawnTransformCoord);
        Destroy(selectedPiece.gameObject);
    }
    public void TransformPawn(ChessMove move)
    {
        // replace old piece with a new one
        chessBoard.PutNewPiece(move.piece.pieceColor, move.requestedTransformPiece, move.to);
        Destroy(move.piece.gameObject);
    }

    public void PawnTransformationOnClick(int pieceType)
    {
        // replace old piece with a new one
        TransformPawn(selectedPiece, (PieceType)pieceType);
        chessBoard.isActive = true;
        GoToScreen(gameControlScreen);
    }

    void MakeEngineMove(ChessMove move)
    {
        // 1. make move without transformation
        MakeMoveWithoutTransformation(move);
        // 2. if piece must be transformed...
        if (move.piece.MustBeTransformed()) {
            // transform pawn without asking
            TransformPawn(move);
        }

        // 3. Deselect piece
        selectedPiece = null;
    }

    private Task<ChessMove> EngineCalculatingMove()
    {
        Task<ChessMove> task = Task.Run(() => {

            ChessMove move = engineInterface.GetRecommendedMove(chessBoard, 5 * 60 * 1000);

            return move; // 60 sec maximun            
        });

        return task;
    }

    async void OnEngineInput()
    {
        chessBoard.isActive = false;

        // 1. request for move from engine
        ChessMove move = await EngineCalculatingMove();

        if (!cancelEngineCalculationRequested) {
            // 2. make engine's move
            
            MakeEngineMove(move);
        }
        cancelEngineCalculationRequested = false;
        chessBoard.isActive = true;
    }

    bool SelectedPieceOnTargetPosition()
    {
        return (selectedPiece != null && selectedPiece.isOnTargetPosition());
    }

    // Start is called before the first frame update
    void Start()
    {
        bounds = boardSurface.GetComponent<MeshRenderer>().bounds.max;
        engineInterface = GameObject.Find("ChessEngineController").GetComponent<ChessEngineInterface>();
        playerCamera = GameObject.Find("PlayerCamera").GetComponent<CinemachineVirtualCamera>();

        screenStatusStack = new Stack<ScreenStatuses>();

        InitGame();
        cancelEngineCalculationRequested = false;
    }

    // Update is called once per frame
    void Update()
    {
        OnPlayerInput();

        if (chessBoard.isActive && isEngineTurn)
            if (selectedPiece == null || SelectedPieceOnTargetPosition())
                OnEngineInput();
    }
}
