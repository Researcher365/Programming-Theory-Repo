using Assets.Scripts;
using System.Collections.Generic;
using System.Text;

public class MoveNode
{
    public ChessMove move;
    public MoveNode parent;
    public List<MoveNode> variations = new List<MoveNode>();
    public string comment;
}

// Структура для хранения информации о разветвлении
public struct BranchInfo
{
    public MoveNode node;        // Позиция, в которой произошло разветвление
    public int variationIndex;   // Выбранный вариант

    public BranchInfo(MoveNode node, int variationIndex)
    {
        this.node = node;
        this.variationIndex = variationIndex;
    }
}

public class ChessGame
{
    private ChessBoard startingPosition;
    private MoveNode rootNode;
    private MoveNode currentNode;

    private Stack<BranchInfo> branchStack = new Stack<BranchInfo>();

    public void NewGame()
    {
        startingPosition = new ChessBoard(8, 8);
        startingPosition.SetStartChessPosition();
        rootNode = new MoveNode();
        currentNode = rootNode;
    }

    // Переход к заданному полуходу в основной линии (нумерация с 1)
    public bool GoToHalfMove(int halfMoveNumber)
    {
        // Возвращаемся к начальной позиции
        while (currentNode != rootNode) {
            GoToPreviousMove();
        }

        // Если запрошен нулевой или отрицательный номер хода - остаёмся в начальной позиции
        if (halfMoveNumber <= 0)
            return true;

        // Двигаемся вперёд до нужного полухода
        int currentHalfMove = 0;
        while (currentHalfMove < halfMoveNumber) {
            // Если следующего хода нет - значит такой номер полухода недостижим
            if (!GoToNextMoveWithTracking(0))
                return false;
            currentHalfMove++;
        }

        return true;
    }


    public ChessBoard GetCurrentPosition()
    {
        ChessBoard current = startingPosition.GetCopy();
        MoveNode node = currentNode;
        List<ChessMove> movesToMake = new List<ChessMove>();

        while (node != rootNode && node != null) {
            movesToMake.Insert(0, node.move);
            node = node.parent;
        }

        foreach (var move in movesToMake) {
            current = current.VirtualBoardAfterFreeMove(move);
        }

        return current;
    }

    public void AddMove(ChessMove move)
    {
        MoveNode newNode = new MoveNode {
            move = move,
            parent = currentNode
        };

        currentNode.variations.Add(newNode);
        currentNode = newNode;
    }

    public void AddVariation(ChessMove move)
    {
        if (currentNode == rootNode) return;

        MoveNode parentNode = currentNode.parent;
        MoveNode newNode = new MoveNode {
            move = move,
            parent = parentNode
        };

        parentNode.variations.Add(newNode);
        currentNode = newNode;
    }

    // Повышает текущий вариант, делая его основной линией
    public void Promote()
    {
        if (currentNode == rootNode || currentNode.parent == rootNode)
            return;

        MoveNode parentNode = currentNode.parent;
        List<MoveNode> variations = parentNode.variations;

        // Находим индекс текущего варианта
        int currentIndex = variations.IndexOf(currentNode);
        if (currentIndex <= 0)
            return;

        // Меняем местами с первым вариантом
        variations[currentIndex] = variations[0];
        variations[0] = currentNode;
    }

    // Переход к следующему ходу с запоминанием позиции разветвления
    public bool GoToNextMoveWithTracking(int variationIndex = 0)
    {
        if (currentNode.variations.Count > variationIndex) {
            // Если есть больше одного варианта, сохраняем информацию о разветвлении
            if (currentNode.variations.Count > 1) {
                branchStack.Push(new BranchInfo(currentNode, variationIndex));
            }
            currentNode = currentNode.variations[variationIndex];
            return true;
        }
        return false;
    }

    public bool GoToPreviousMove()
    {
        if (currentNode.parent != null) {
            // Проверяем, не возвращаемся ли мы к позиции из стека разветвлений
            if (branchStack.Count > 0 && currentNode == branchStack.Peek().node.variations[branchStack.Peek().variationIndex]) {
                branchStack.Pop();
            }

            currentNode = currentNode.parent;
            return true;
        }
        return false;
    }

    // Возврат к последнему разветвлению
    public bool ReturnToLastBranch()
    {
        if (branchStack.Count == 0)
            return false;

        BranchInfo lastBranch = branchStack.Pop();
        currentNode = lastBranch.node;
        return true;
    }

    private void ExportNodeToPGN(MoveNode node, StringBuilder pgn, bool isMainLine)
    {
        foreach (var variation in node.variations) {
            if (!isMainLine) pgn.Append("(");

            if (variation.move.piece.pieceColor == PieceColor.White)
                pgn.Append($"{variation.move.number}. ");

            pgn.Append($"{variation.move.notation} ");

            if (variation.comment != null)
                pgn.Append($"{{{variation.comment}}} ");

            ExportNodeToPGN(variation, pgn, isMainLine && variation == node.variations[0]);

            if (!isMainLine) pgn.Append(") ");
        }
    }
}
