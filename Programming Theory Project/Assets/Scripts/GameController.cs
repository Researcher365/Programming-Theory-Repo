
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
    private const float CAMERA_ROTATION_SPEED = 40f;
    private const int ENGINE_MAX_CALCULATION_TIME_MS = 5 * 60 * 1000; // 5 минут

    public GameObject boardSurface;// Поверхность игровой доски
    Vector3 bounds;// Границы доски

    public Camera mainCamera; // Основная камера
    public GameObject[] piecesPrefabs; // Префабы шахматных фигур
    public ChessBoard chessBoard; // Объект, представляющий шахматную доску

    // Типы контроллеров для белых и черных (человек или движок)
    public ChessSideControllerType whiteController, blackController;

    private const uint boardHorizontalSquaresCount = 8; // Количество клеток по горизонтали
    private const uint boardVerticalSquaresCount = 8;// Количество клеток по вертикали
    private bool cancelEngineCalculationRequested = false;// Флаг запроса на отмену расчета хода движком

    [SerializeField] private ChessPiece selectedPiece;// Выбранная в данный момент фигура
    [SerializeField] ChessEngineInterface engineInterface;// Интерфейс взаимодействия с шахматным движком
    [SerializeField] public GameObject pieceChoiceScreen;// Экран выбора фигуры (при превращении пешки)
    [SerializeField] public GameObject mainMenuScreen;// Экран главного меню
    [SerializeField] public GameObject settingsScreen;// Экран настроек
    [SerializeField] public GameObject gameControlScreen;// Экран управления игрой
    [SerializeField] TMP_Dropdown levelDropdown;// Выпадающий список уровней сложности
    [SerializeField] TMP_Text counterText;// Текст для отображения счетчика фигур
    [SerializeField] public CinemachineVirtualCamera playerCamera;// Виртуальная камера для игрока
    [SerializeField] public PiecesCounter piecesCounter;// Счетчик фигур

    Stack<ScreenStatuses> screenStatusStack;// Стек для хранения состояний экранов
    private BoardCoords pawnTransformCoord;// Координаты пешки для трансформации

    //==== GETTERS
    
    // Свойство, определяющее, является ли текущий ход ходом движка
    public bool isEngineTurn
    {
        get {
            return
            chessBoard.whiteToMove && whiteController == ChessSideControllerType.engine ||
            chessBoard.blackToMove && blackController == ChessSideControllerType.engine;
        }
    }

    //==== METHODS

    // Переворачивает доску
    public void FlipBoard()
    {
        GameObject target = GameObject.Find("TargetPoint");

        target.transform.rotation = Quaternion.LookRotation(Vector3.Project(target.transform.forward, Vector3.forward).normalized);
        target.transform.Rotate(Vector3.up, 180);
    }

    // Инициализирует новую игру
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

    // Начинает новую игру после нажатия кнопки в меню
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

    // Обновляет позицию камеры в зависимости от активного экрана
    public void UpdateCameraPosition()
    {
        playerCamera.Priority = 
            (mainMenuScreen.activeInHierarchy 
            || settingsScreen.activeInHierarchy) ? 1 : 10;
    }

    // Переключает на указанный экран и скрывает остальные
    void GoToScreen(GameObject screen)
    {
        ScreenStatuses statuses = new ScreenStatuses(this);

        screen.SetActive(true);

        foreach (ScreenStatus otherStatus in statuses.Statuses)
            if (otherStatus.screen != screen)
                otherStatus.screen.SetActive(false);

        UpdateCameraPosition();
    }

    // Размер клетки по оси X
    public float squareSizeX
    {
        get { return 2.0f * bounds.x / chessBoard.iSize; }
    }
    
    // Размер клетки по оси Z
    public float squareSizeZ
    {
        get { return 2.0f * bounds.x / chessBoard.iSize; }
    }

    // Получает позицию центра клетки по координатам
    public Vector3 GetSquareCenterPosition(BoardCoords coords)
    {
        return GetSquareCenterPosition(coords.i, coords.j);
    }

    // Получает позицию центра клетки по индексам i, j
    public Vector3 GetSquareCenterPosition(int i, int j)
    {
        Vector3 pos = new Vector3(0, 0, 0);
        pos.x = -bounds.x + i * squareSizeX + squareSizeX * 0.5f;
        pos.z = -bounds.z + j * squareSizeZ + squareSizeZ * 0.5f;
        return pos;
    }

    // Получает координаты доски по позиции клика мышью
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

    // Создает новую 3D фигуру на доске
    public ChessPiece CreateNewPiece3D(int prefabIndex, BoardCoords coords)
    {
        GameObject prefab = piecesPrefabs[prefabIndex];
        Vector3 startPos = GetSquareCenterPosition(coords);
        GameObject newPiece = Instantiate(prefab, startPos, prefab.transform.rotation, GameObject.Find("ActivePieces").transform);
        ChessPiece chessPiece = newPiece.GetComponent<ChessPiece>();
        chessPiece.targetPos = startPos;

        return chessPiece;
    }

    // Обрабатывает нажатие на клетку доски или фигуру
    //void OnBoardSquareOrPieceDown(BoardCoords clickedCoords)
    //{
    //    Debug.Log("board coords: " + clickedCoords.ToString());
    //    ChessPiece pieceOnClickedSquare = chessBoard.GetPiece(clickedCoords);


    //    if (selectedPiece != null && selectedPiece != pieceOnClickedSquare)
    //        selectedPiece.Deselect();

    //    // if there is a piece on clicked position and it could be selected
    //    if (!chessBoard.IsSquareEmpty(clickedCoords) && pieceOnClickedSquare.IsAvailableForMoving()) {

    //        pieceOnClickedSquare.SwitchSelection();
    //        if (pieceOnClickedSquare.isSelected)
    //            selectedPiece = pieceOnClickedSquare;
    //        else
    //            selectedPiece = null;
    //    }
    //    // if there is a selected and no the same color piece on clicked square
    //    else if (selectedPiece != null) {

    //        ChessMove move = new ChessMove(selectedPiece, clickedCoords);
    //        ChessBoard virtualBoard = chessBoard.VirtualBoardAfterFreeMove(move);

    //        if (move.IsLegal() && !(virtualBoard.IsCheckOnTheBoard(selectedPiece.pieceColor)))
    //            // moving selected piece if possible and no check
    //            MakeInteractiveMove(move);

    //        virtualBoard.Clear(); // destroying temp pieces
    //    }

    //    piecesCounter.Count(chessBoard);
    //    counterText.text = piecesCounter.ToString();
    //}

    

    /// Обрабатывает нажатие на клетку доски или шахматную фигуру
    /// <param name="clickedCoords">Координаты клика на доске</param>
    void OnBoardSquareOrPieceDown(BoardCoords clickedCoords)
    {
        Debug.Log($"Клик на координатах: {clickedCoords}");

        // Получаем фигуру на выбранной клетке
        ChessPiece pieceOnClickedSquare = chessBoard.GetPiece(clickedCoords);

        // 1. Обрабатываем отмену выбора предыдущей фигуры, если нужно
        HandlePreviousSelection(pieceOnClickedSquare);

        // 2. Пытаемся выбрать фигуру или сделать ход
        if (IsSelectablePiece(clickedCoords, pieceOnClickedSquare)) {
            HandlePieceSelection(pieceOnClickedSquare);
        }
        else if (selectedPiece != null) {
            TryMakeMove(clickedCoords);
        }

        // 3. Обновляем счетчик фигур
        UpdatePiecesCounter();
    }

    /// Обрабатывает предыдущий выбор фигуры
    private void HandlePreviousSelection(ChessPiece clickedPiece)
    {
        // Если была выбрана фигура и мы кликнули не по ней же
        if (selectedPiece != null && selectedPiece != clickedPiece) {
            selectedPiece.Deselect();
        }
    }

    /// Проверяет, является ли фигура доступной для выбора
    private bool IsSelectablePiece(BoardCoords coords, ChessPiece piece)
    {
        return !chessBoard.IsSquareEmpty(coords) && piece.IsAvailableForMoving();
    }


    /// Обрабатывает выбор фигуры
    private void HandlePieceSelection(ChessPiece piece)
    {
        piece.SwitchSelection();
        selectedPiece = piece.isSelected ? piece : null;
    }

    /// Пытается сделать ход выбранной фигурой
    private void TryMakeMove(BoardCoords targetCoords)
    {
        ChessMove move = new ChessMove(selectedPiece, targetCoords);

        // Создаем виртуальную доску для проверки валидности хода
        ChessBoard virtualBoard = chessBoard.VirtualBoardAfterFreeMove(move);

        try {
            bool isLegalMove = move.IsLegal();
            bool causesCheck = virtualBoard.IsCheckOnTheBoard(selectedPiece.pieceColor);

            if (isLegalMove && !causesCheck) {
                MakeInteractiveMove(move);
            }
            else if (!isLegalMove) {
                Debug.Log("Недопустимый ход!");
            }
            else if (causesCheck) {
                Debug.Log("Ход приведет к шаху своему королю!");
            }
        }
        finally {
            // Всегда очищаем виртуальную доску
            virtualBoard.Clear();
        }
    }

    /// Обновляет счетчик фигур и отображает его на UI
    private void UpdatePiecesCounter()
    {
        piecesCounter.Count(chessBoard);
        counterText.text = piecesCounter.ToString();
    }

    // Обрабатывает изменение уровня сложности
    public void OnLevelChanged()
    {
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

    // Обрабатывает нажатие на кнопку настроек
    public void OnSettingsClick()
    {
        screenStatusStack.Push(new ScreenStatuses(this));
        GoToScreen(settingsScreen);
    }

    // Переключает режим игры между человеком и движком
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

    // Обрабатывает ввод игрока
    void OnPlayerInput()
    {
        HandleSystemInput();
        HandleCameraControls();
        HandleGameInput();
    }

    // Обрабатывает ввод игрока
    void HandleSystemInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            HandleEscapeKey();
        }

        if (Input.GetKeyDown(KeyCode.E)) {
            CancelEngineCalculation();
            SwitchEngineEnabled();
        }
    }
    // Обрабатывает отмену расчета движка
    private void CancelEngineCalculation()
    {
        cancelEngineCalculationRequested = true;
        engineInterface.StopCalculation();
    }

    // Обрабатывает нажатие на кнопку ESC
    private void HandleEscapeKey()
    {
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
    }

    // Обрабатывает ввод игрока для управления камерой
    void HandleCameraControls()
    {
        if (Input.GetMouseButtonDown(1)) // Right mouse click
            FlipBoard();

        float horInput = Input.GetAxis("Horizontal");
        GameObject.Find("TargetPoint").transform.Rotate(Vector3.up, -horInput * CAMERA_ROTATION_SPEED * Time.deltaTime);
    }

    void HandleGameInput()
    {
        if (chessBoard.isActive && !isEngineTurn && Input.GetMouseButtonDown(0)) {
            Vector3 mousePos = Input.mousePosition;
            BoardCoords coords = GetBoardCoordinates(mousePos);
            if (coords.IsInsideBoard(chessBoard))
                OnBoardSquareOrPieceDown(coords);
            else
                Debug.Log("out of board!");
        }
    }
    //void OnPlayerInput()
    //{
    //    // Independent input
    //    if (Input.GetKeyDown(KeyCode.Escape)) {
    //        ScreenStatuses status = new ScreenStatuses(this);

    //        if (gameControlScreen.activeInHierarchy) {
    //            screenStatusStack.Push(status);
    //            GoToScreen(mainMenuScreen);
    //        }
    //        else if (screenStatusStack.Count > 0) {
    //            screenStatusStack.Pop().RecoverToGameController(this);
    //        }
    //        else {
    //            screenStatusStack.Push(status);
    //            GoToScreen(mainMenuScreen);

    //            cancelEngineCalculationRequested = true;
    //            engineInterface.StopCalculation();
    //        }

    //    }

    //    if (Input.GetKeyDown(KeyCode.E)) {
    //        cancelEngineCalculationRequested = true;
    //        engineInterface.StopCalculation();
    //        SwitchEngineEnabled();
    //    }

    //    if (Input.GetMouseButtonDown(1)) // Right mouse click
    //        FlipBoard();
    //    float horInput = Input.GetAxis("Horizontal");
    //    GameObject.Find("TargetPoint").transform.Rotate(Vector3.up, -horInput * 40 * Time.deltaTime);

    //    // Conditional input
    //    if (chessBoard.isActive && !isEngineTurn) {
    //        if (Input.GetMouseButtonDown(0)) // Left mouse click
    //        {
    //            Vector3 mousePos = Input.mousePosition;
    //            BoardCoords coords = GetBoardCoordinates(mousePos);
    //            if (coords.IsInsideBoard(chessBoard))
    //                OnBoardSquareOrPieceDown(coords);
    //            else
    //                Debug.Log("out of board!");

    //        }
    //    }


    //}

    // Корутина, запускаемая после завершения движения фигуры
    IEnumerator OnFinishPieceMovement()
    {
        while (!selectedPiece.isOnTargetPosition()) {
            //yield return new YieldInstruction();
            yield return null;
        }


        if (whiteController == blackController) // ' human vs human ' OR ' engine vs engine '
            FlipBoard();

        if (!pieceChoiceScreen.activeSelf)
            chessBoard.isActive = true;
    }

    // Делает ход без трансформации пешки
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


    // Делает интерактивный ход игрока
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

    // Запрашивает трансформацию пешки
    void AskPawnTransformation(BoardCoords coords)
    {
        GoToScreen(pieceChoiceScreen);
        pawnTransformCoord = coords;
    }

    // Объединение методов TransformPawn:
    public void TransformPawn(ChessPiece piece, PieceType toPieceType, BoardCoords coords)
    {
        // replace old piece with a new one
        chessBoard.PutNewPiece(piece.pieceColor, toPieceType, coords);
        Destroy(piece.gameObject);
    }

    // Трансформирует пешку в выбранную фигуру
    public void TransformPawn(ChessPiece selectedPiece, PieceType toPieceType)
    {
        TransformPawn(selectedPiece, toPieceType, pawnTransformCoord);
    }

    // Трансформирует пешку согласно информации в ходе
    public void TransformPawn(ChessMove move)
    {
        TransformPawn(move.piece, move.requestedTransformPiece, move.to);
    }

    // Обрабатывает клик по кнопке выбора фигуры для трансформации пешки
    public void PawnTransformationOnClick(int pieceType)
    {
        // replace old piece with a new one
        TransformPawn(selectedPiece, (PieceType)pieceType);
        chessBoard.isActive = true;
        GoToScreen(gameControlScreen);
    }

    // Выполняет ход, сделанный движком
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

    // Асинхронно получает ход от шахматного движка
    private Task<ChessMove> EngineCalculatingMove()
    {
        Task<ChessMove> task = Task.Run(() => {

            ChessMove move = engineInterface.GetRecommendedMove(chessBoard, ENGINE_MAX_CALCULATION_TIME_MS);

            return move; // 60 sec maximun            
        });

        return task;
    }

    // Обрабатывает ввод от движка
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

    // Проверяет, находится ли выбранная фигура на целевой позиции
    bool IsSelectedPieceOnTargetPosition()
    {
        return (selectedPiece != null && selectedPiece.isOnTargetPosition());
    }


    // Метод, вызываемый при старте
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

    // Метод, вызываемый каждый кадр
    // Update is called once per frame
    void Update()
    {
        OnPlayerInput();

        if (chessBoard.isActive && isEngineTurn)
            if (selectedPiece == null || IsSelectedPieceOnTargetPosition())
                OnEngineInput();
    }
}
