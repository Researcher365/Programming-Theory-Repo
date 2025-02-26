using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using Unity.VisualScripting;
    using UnityEngine;


    public class ChessEngineInterface: MonoBehaviour
    {
        private Process chessEngine;

        [SerializeField] public int depth;
        [SerializeField] public int ratingELO;

        // Путь к движку (например, "stockfish.exe")
        public string enginePath = "d:/chess/stockfish-windows-x86-64-avx2.exe";

        void Start()
        {
            // Запускаем движок
            chessEngine = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = enginePath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            chessEngine.Start();
            SetEngineOptions();
        }

        public void SetEngineOptions()
        {
            depth = 99; // глубина расчёта
            ratingELO = 1500; // рейтинг ЭЛО

            // Установить уровень игры
            //SetEngineOption("UCI_Elo", "2250");
            SetEngineOption("UCI_LimitStrength", "true");
            SetEngineOption("Hash", "1024");
            SetEngineOption("Threads", "4");
        }

        public void SetEngineOption(string name, string value)
        {
            chessEngine.StandardInput.WriteLine($"setoption name {name} value {value}"); // Установить уровень игры на 1500 рейтинга
        }

        public void StopCalculation()
        {
            chessEngine.StandardInput.WriteLine("stop");
        }

        public ChessMove GetRecommendedMove(ChessBoard board, int msec)
        {
            StreamWriter writer = chessEngine.StandardInput;
            StreamReader reader = new StreamReader(chessEngine.StandardOutput.BaseStream, System.Text.Encoding.UTF8);

            // Преобразуем матрицу в строку FEN для передачи движку
            string fen = ConvertBoardToFEN(board);

            // Передаем FEN позицию движку
            writer.WriteLine("position fen " + fen);
            writer.WriteLine($"go wtime {msec} btime {msec} depth {depth}");
            writer.Flush();
            //chessEngine.StandardInput.WriteLine($"go movetime {msec}");

            // Читаем ответ движка
            string bestMove = null;
            string line;
            ChessMove move = null;
            while (move == null) {
                line = reader.ReadLine();
                if (!string.IsNullOrEmpty(line)) {
                    UnityEngine.Debug.Log(line);
                    if (line.StartsWith("bestmove")) {
                        bestMove = line.Split(' ')[1];
                        // Преобразуем ответ UCI в объект ChessMove
                        move = ParseUCIMoveToChessMove(bestMove, board);
                        break;
                    }
                }
            }
            return move;       
        }

    private string ConvertBoardToFEN(ChessBoard board)
    {
        // Реализация преобразования матрицы в строку FEN
        string fen = "";
        for (int i = 0; i < 8; i++) {
            int emptyCount = 0;
            for (int j = 0; j < 8; j++) {
                ChessPiece piece = board.GetPiece(j, 7 - i);
                if (piece == null) {
                    emptyCount++;
                }
                else {
                    if (emptyCount > 0) {
                        fen += emptyCount;
                        emptyCount = 0;
                    }
                    fen += ConvertPieceToFEN(piece);
                }
            }
            if (emptyCount > 0) {
                fen += emptyCount;
            }
            if (i < 7) {
                fen += "/";
            }
        }
        // Определяем очередь хода
        fen += " " + (board.whiteToMove ? "w" : "b") + " ";

        // Учет возможности рокировки
        string castlingAvailability = "";

        // Проверяем, не двигался ли белый король и ладьи (для рокировки)
        if (!board.whiteKing.MovedInGame()) {

            ChessPiece rookA1 = board.GetPiece(0, 0);
            ChessPiece rookH1 = board.GetPiece((int)board.iSize - 1, 0);

            if (rookA1 != null && !rookA1.MovedInGame()) // Ладья на a1
                castlingAvailability += "Q";
            if (rookH1 != null && !rookH1.MovedInGame()) // Ладья на h1
                castlingAvailability += "K";
        }

        // Проверяем, не двигался ли черный король и ладьи
        if (!board.blackKing.MovedInGame()) {
            ChessPiece rookA8 = board.GetPiece(0, (int)board.jSize - 1);
            ChessPiece rookH8 = board.GetPiece((int)board.iSize - 1, (int)board.jSize - 1);
            if (rookA8 != null && !rookA8.MovedInGame()) // Ладья на a8
                castlingAvailability += "q";
            if (rookH8 != null && !rookH8.MovedInGame()) // Ладья на h8
                castlingAvailability += "k";
        }

        // Если нет возможности рокировки, добавляем '-'
        fen += string.IsNullOrEmpty(castlingAvailability) ? "-" : castlingAvailability;

        // Добавляем оставшуюся часть строки FEN
        fen += " ";

        if (board.enPassant.isActive)
            fen += board.enPassant.notation;
        else fen += "-";

        fen += " 0 1";

        return fen;
    }

    private string ConvertPieceToFEN(ChessPiece piece)
    {
        if (piece == null)
            return string.Empty;
        // Определяем символ на основе типа фигуры
        char symbol = ChessPiece.PieceTypeSymbol(piece.pieceType);

        // Возвращаем символ в верхнем или нижнем регистре в зависимости от цвета
        return piece.pieceColor == PieceColor.White ? char.ToUpper(symbol).ToString() : symbol.ToString();
    }


    private ChessMove ParseUCIMoveToChessMove(string uciMove, ChessBoard board)
    {
        BoardCoords from = new BoardCoords {
            i = uciMove[0] - 'a', // Перевод UCI-формата в индексы массива
            j = int.Parse(uciMove[1].ToString()) - 1
        };

        BoardCoords to = new BoardCoords {
            i = uciMove[2] - 'a',
            j = int.Parse(uciMove[3].ToString()) - 1,
        };

        ChessMove move = new ChessMove(from, to, board);
        move.number = board.moveNumber;

        // Проверяем наличие символа превращения (например, "e7e8q")
        if (uciMove.Length > 4) {
            char promotionChar = uciMove[4];
            move.requestedTransformPiece = ChessPiece.ParsePieceType(promotionChar);
        }
        return move;
    }

    void OnDestroy()
    {
        if (chessEngine != null && !chessEngine.HasExited) {
            chessEngine.Kill();
        }
    }
}
}