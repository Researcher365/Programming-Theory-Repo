using System.Collections.Generic;

namespace Assets.Scripts
{
    public class TreeNode<T>
    {
        public T Value { get; set; }
        public List<TreeNode<T>> Children { get; set; }

        public TreeNode(T value)
        {
            Value = value;
            Children = new List<TreeNode<T>>();         
        }

        // Добавление дочернего узла
        public TreeNode<T> AddChild(T value)
        {
            var childNode = new TreeNode<T>(value);
            Children.Add(childNode);
            return childNode;
        }

        // Удаление дочернего узла
        public void RemoveChild(TreeNode<T> node)
        {
            Children.Remove(node);
        }
    }
}