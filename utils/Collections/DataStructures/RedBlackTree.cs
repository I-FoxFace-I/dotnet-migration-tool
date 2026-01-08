using System.Collections;

namespace Utils.Collections.DataStructures
{
    /// <summary>
    /// Red-black tree for storing values.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    public class RedBlackTree<T> : IEnumerable<T> where T : IComparable<T>
    {
        private enum NodeColor
        {
            Red,
            Black
        }

        private class Node
        {
            public T Value { get; set; }
            public Node Left { get; set; }
            public Node Right { get; set; }
            public Node Parent { get; set; }
            public NodeColor Color { get; set; }

            public Node(T value, NodeColor color)
            {
                Value = value;
                Color = color;
                Left = null;
                Right = null;
                Parent = null;
            }

            public bool IsLeftChild => Parent != null && Parent.Left == this;
            public bool IsRightChild => Parent != null && Parent.Right == this;
        }

        private Node root;
        private int count;

        /// <summary>
        /// Number of elements in the tree.
        /// </summary>
        public int Count => count;

        /// <summary>
        /// Indicates whether the tree is empty.
        /// </summary>
        public bool IsEmpty => count == 0;

        /// <summary>
        /// Creates a new instance of an empty red-black tree.
        /// </summary>
        public RedBlackTree()
        {
            root = null;
            count = 0;
        }

        /// <summary>
        /// Inserts a value into the tree.
        /// </summary>
        /// <param name="value">Value to insert.</param>
        /// <returns>True if the value was inserted; False if it already exists.</returns>
        public bool Add(T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (root == null)
            {
                // First inserted node is always black
                root = new Node(value, NodeColor.Black);
                count = 1;
                return true;
            }

            Node current = root;
            Node parent = null;
            int comparison = 0;

            // Find insertion location
            while (current != null)
            {
                parent = current;
                comparison = value.CompareTo(current.Value);

                if (comparison < 0)
                    current = current.Left;
                else if (comparison > 0)
                    current = current.Right;
                else
                {
                    // Value already exists
                    return false;
                }
            }

            // Create new node (always red)
            Node newNode = new Node(value, NodeColor.Red);
            newNode.Parent = parent;

            // Attach new node to parent
            if (comparison < 0)
                parent.Left = newNode;
            else
                parent.Right = newNode;

            count++;

            // Fix the tree if red-black tree rules are violated
            BalanceAfterInsertion(newNode);
            return true;
        }

        /// <summary>
        /// Verifies whether the tree contains the given value.
        /// </summary>
        /// <param name="value">Value to search for.</param>
        /// <returns>True if value exists; otherwise False.</returns>
        public bool Contains(T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return FindNode(value) != null;
        }

        /// <summary>
        /// Tries to remove a value from the tree.
        /// </summary>
        /// <param name="value">Value to remove.</param>
        /// <returns>True if the value was removed; otherwise False.</returns>
        public bool Remove(T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Node nodeToRemove = FindNode(value);
            if (nodeToRemove == null)
                return false;

            RemoveNode(nodeToRemove);
            count--;
            return true;
        }

        /// <summary>
        /// Clears the tree.
        /// </summary>
        public void Clear()
        {
            root = null;
            count = 0;
        }

        /// <summary>
        /// Finds the node by value.
        /// </summary>
        private Node FindNode(T value)
        {
            Node current = root;
            while (current != null)
            {
                int comparison = value.CompareTo(current.Value);
                if (comparison == 0)
                    return current;

                current = comparison < 0 ? current.Left : current.Right;
            }
            return null;
        }

        /// <summary>
        /// Performs a left rotation around the given node.
        /// </summary>
        private void RotateLeft(Node node)
        {
            if (node == null || node.Right == null)
                return;

            Node rightChild = node.Right;
            node.Right = rightChild.Left;

            if (rightChild.Left != null)
                rightChild.Left.Parent = node;

            rightChild.Parent = node.Parent;

            if (node.Parent == null)
                root = rightChild;
            else if (node.IsLeftChild)
                node.Parent.Left = rightChild;
            else
                node.Parent.Right = rightChild;

            rightChild.Left = node;
            node.Parent = rightChild;
        }

        /// <summary>
        /// Performs a right rotation around the given node.
        /// </summary>
        private void RotateRight(Node node)
        {
            if (node == null || node.Left == null)
                return;

            Node leftChild = node.Left;
            node.Left = leftChild.Right;

            if (leftChild.Right != null)
                leftChild.Right.Parent = node;

            leftChild.Parent = node.Parent;

            if (node.Parent == null)
                root = leftChild;
            else if (node.IsRightChild)
                node.Parent.Right = leftChild;
            else
                node.Parent.Left = leftChild;

            leftChild.Right = node;
            node.Parent = leftChild;
        }

        /// <summary>
        /// Balancing the tree after insertion.
        /// </summary>
        private void BalanceAfterInsertion(Node node)
        {
            // Node is red after insertion
            // If node is the root, change it to black and stop
            if (node == root)
            {
                node.Color = NodeColor.Black;
                return;
            }

            // While parent of the node is red (violation of rule 4)
            while (node != root && node.Parent.Color == NodeColor.Red)
            {
                if (node.Parent.IsLeftChild)
                {
                    // Parent is the left child of its parent
                    Node uncle = node.Parent.Parent.Right;

                    // Case 1: Uncle is red - recoloring
                    if (uncle != null && uncle.Color == NodeColor.Red)
                    {
                        node.Parent.Color = NodeColor.Black;
                        uncle.Color = NodeColor.Black;
                        node.Parent.Parent.Color = NodeColor.Red;
                        node = node.Parent.Parent;
                    }
                    else
                    {
                        // Case 2: Node is the right child - left rotation
                        if (node.IsRightChild)
                        {
                            node = node.Parent;
                            RotateLeft(node);
                        }

                        // Case 3: Node is the left child - right rotation
                        node.Parent.Color = NodeColor.Black;
                        node.Parent.Parent.Color = NodeColor.Red;
                        RotateRight(node.Parent.Parent);
                    }
                }
                else
                {
                    // Parent is the right child of its parent (mirror case)
                    Node uncle = node.Parent.Parent.Left;

                    // Case 1: Uncle is red - recoloring
                    if (uncle != null && uncle.Color == NodeColor.Red)
                    {
                        node.Parent.Color = NodeColor.Black;
                        uncle.Color = NodeColor.Black;
                        node.Parent.Parent.Color = NodeColor.Red;
                        node = node.Parent.Parent;
                    }
                    else
                    {
                        // Case 2: Node is the left child - right rotation
                        if (node.IsLeftChild)
                        {
                            node = node.Parent;
                            RotateRight(node);
                        }

                        // Case 3: Node is the right child - left rotation
                        node.Parent.Color = NodeColor.Black;
                        node.Parent.Parent.Color = NodeColor.Red;
                        RotateLeft(node.Parent.Parent);
                    }
                }
            }

            // Ensure the root is always black
            root.Color = NodeColor.Black;
        }

        /// <summary>
        /// Removing a node from the tree.
        /// </summary>
        private void RemoveNode(Node nodeToRemove)
        {
            Node successor = nodeToRemove;
            Node child;
            NodeColor originalColor = successor.Color;

            // Case 1: Node has no left child
            if (nodeToRemove.Left == null)
            {
                child = nodeToRemove.Right;
                Transplant(nodeToRemove, nodeToRemove.Right);
            }
            // Case 2: Node has no right child
            else if (nodeToRemove.Right == null)
            {
                child = nodeToRemove.Left;
                Transplant(nodeToRemove, nodeToRemove.Left);
            }
            // Case 3: Node has both children
            else
            {
                // Find successor (minimum in the right subtree)
                successor = FindMinimum(nodeToRemove.Right);
                originalColor = successor.Color;
                child = successor.Right;

                if (successor.Parent == nodeToRemove)
                {
                    if (child != null)
                        child.Parent = successor;
                }
                else
                {
                    Transplant(successor, successor.Right);
                    successor.Right = nodeToRemove.Right;
                    if (successor.Right != null)
                        successor.Right.Parent = successor;
                }

                Transplant(nodeToRemove, successor);
                successor.Left = nodeToRemove.Left;
                successor.Left.Parent = successor;
                successor.Color = nodeToRemove.Color;
            }

            // If the removed node was black, we must fix the tree
            if (originalColor == NodeColor.Black && child != null)
            {
                BalanceAfterDeletion(child);
            }
        }

        /// <summary>
        /// Replacing one node with another node.
        /// </summary>
        private void Transplant(Node u, Node v)
        {
            if (u.Parent == null)
                root = v;
            else if (u.IsLeftChild)
                u.Parent.Left = v;
            else
                u.Parent.Right = v;

            if (v != null)
                v.Parent = u.Parent;
        }

        /// <summary>
        /// Finds the node with the minimum value in the subtree.
        /// </summary>
        private Node FindMinimum(Node node)
        {
            while (node.Left != null)
                node = node.Left;
            return node;
        }

        /// <summary>
        /// Finds the node with the maximum value in the subtree.
        /// </summary>
        private Node FindMaximum(Node node)
        {
            while (node.Right != null)
                node = node.Right;
            return node;
        }

        /// <summary>
        /// Gets the minimum value in the tree.
        /// </summary>
        /// <returns>Minimum value.</returns>
        /// <exception cref="InvalidOperationException">When the tree is empty.</exception>
        public T Min()
        {
            if (root == null)
                throw new InvalidOperationException("Tree is empty.");
            return FindMinimum(root).Value;
        }

        /// <summary>
        /// Gets the maximum value in the tree.
        /// </summary>
        /// <returns>Maximum value.</returns>
        /// <exception cref="InvalidOperationException">When the tree is empty.</exception>
        public T Max()
        {
            if (root == null)
                throw new InvalidOperationException("Tree is empty.");
            return FindMaximum(root).Value;
        }

        /// <summary>
        /// Balancing the tree after deletion.
        /// </summary>
        private void BalanceAfterDeletion(Node node)
        {
            while (node != root && node.Color == NodeColor.Black)
            {
                if (node.IsLeftChild)
                {
                    Node sibling = node.Parent.Right;

                    // Case 1: Sibling is red
                    if (sibling?.Color == NodeColor.Red)
                    {
                        sibling.Color = NodeColor.Black;
                        node.Parent.Color = NodeColor.Red;
                        RotateLeft(node.Parent);
                        sibling = node.Parent.Right;
                    }

                    // Case 2: Both children of the sibling are black
                    if ((sibling?.Left == null || sibling.Left.Color == NodeColor.Black) &&
                        (sibling?.Right == null || sibling.Right.Color == NodeColor.Black))
                    {
                        if (sibling != null)
                            sibling.Color = NodeColor.Red;
                        node = node.Parent;
                    }
                    else
                    {
                        // Case 3: Right child of the sibling is black
                        if (sibling?.Right == null || sibling.Right.Color == NodeColor.Black)
                        {
                            if (sibling.Left != null)
                                sibling.Left.Color = NodeColor.Black;
                            sibling.Color = NodeColor.Red;
                            RotateRight(sibling);
                            sibling = node.Parent.Right;
                        }

                        // Case 4: Right child of the sibling is red
                        if (sibling != null)
                        {
                            sibling.Color = node.Parent.Color;
                            node.Parent.Color = NodeColor.Black;
                            if (sibling.Right != null)
                                sibling.Right.Color = NodeColor.Black;
                            RotateLeft(node.Parent);
                            node = root;
                        }
                    }
                }
                else
                {
                    // Mirror case for right child
                    Node sibling = node.Parent.Left;

                    // Case 1: Sibling is red
                    if (sibling?.Color == NodeColor.Red)
                    {
                        sibling.Color = NodeColor.Black;
                        node.Parent.Color = NodeColor.Red;
                        RotateRight(node.Parent);
                        sibling = node.Parent.Left;
                    }

                    // Case 2: Both children of the sibling are black
                    if ((sibling?.Left == null || sibling.Left.Color == NodeColor.Black) &&
                        (sibling?.Right == null || sibling.Right.Color == NodeColor.Black))
                    {
                        if (sibling != null)
                            sibling.Color = NodeColor.Red;
                        node = node.Parent;
                    }
                    else
                    {
                        // Case 3: Left child of the sibling is black
                        if (sibling?.Left == null || sibling.Left.Color == NodeColor.Black)
                        {
                            if (sibling.Right != null)
                                sibling.Right.Color = NodeColor.Black;
                            sibling.Color = NodeColor.Red;
                            RotateLeft(sibling);
                            sibling = node.Parent.Left;
                        }

                        // Case 4: Left child of the sibling is red
                        if (sibling != null)
                        {
                            sibling.Color = node.Parent.Color;
                            node.Parent.Color = NodeColor.Black;
                            if (sibling.Left != null)
                                sibling.Left.Color = NodeColor.Black;
                            RotateRight(node.Parent);
                            node = root;
                        }
                    }
                }
            }

            // Ensure the node is black
            node.Color = NodeColor.Black;
        }

        /// <summary>
        /// Gets an enumerator for iterating through values in the tree (inorder).
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            if (root != null)
            {
                Stack<Node> stack = new Stack<Node>();
                Node current = root;

                while (stack.Count > 0 || current != null)
                {
                    // Go as far left as possible
                    while (current != null)
                    {
                        stack.Push(current);
                        current = current.Left;
                    }

                    // Process the current node
                    current = stack.Pop();
                    yield return current.Value;

                    // Move to the right subtree
                    current = current.Right;
                }
            }
        }

        /// <summary>
        /// Gets an enumerator for iterating through values in the tree (inorder).
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns all values in the tree in sorted order.
        /// </summary>
        public List<T> ToList()
        {
            List<T> result = new List<T>(count);
            foreach (T value in this)
            {
                result.Add(value);
            }
            return result;
        }

        /// <summary>
        /// Verifies that the tree is a valid red-black tree.
        /// </summary>
        public bool ValidateRedBlackProperties()
        {
            if (root == null)
                return true;

            // Rule 1: Every node is either red or black
            // Rule 2: The root is black
            if (root.Color != NodeColor.Black)
                return false;

            // Rule 3: All leaf nodes (null) are black
            // Rule 4: Every red node has black children
            // Rule 5: Every path from root to leaf has the same number of black nodes

            // Check rule 4 and determine the number of black nodes on each path
            if (!ValidateRedNodes(root))
                return false;

            // Check rule 5
            int blackCount = -1;
            return ValidateBlackHeight(root, 0, ref blackCount);
        }

        /// <summary>
        /// Checks if the tree satisfies the property that a red node has only black children.
        /// </summary>
        private bool ValidateRedNodes(Node node)
        {
            if (node == null)
                return true;

            if (node.Color == NodeColor.Red)
            {
                if (node.Left != null && node.Left.Color == NodeColor.Red ||
                    node.Right != null && node.Right.Color == NodeColor.Red)
                {
                    return false;
                }
            }

            return ValidateRedNodes(node.Left) && ValidateRedNodes(node.Right);
        }

        /// <summary>
        /// Checks if the tree satisfies the property that all paths from root to leaf have the same number of black nodes.
        /// </summary>
        private bool ValidateBlackHeight(Node node, int blackHeight, ref int expectedBlackHeight)
        {
            if (node == null)
            {
                // Reached a leaf node (null)
                if (expectedBlackHeight == -1)
                {
                    expectedBlackHeight = blackHeight;
                    return true;
                }
                return blackHeight == expectedBlackHeight;
            }

            // Add 1 to height if the current node is black
            if (node.Color == NodeColor.Black)
                blackHeight++;

            return ValidateBlackHeight(node.Left, blackHeight, ref expectedBlackHeight) &&
                   ValidateBlackHeight(node.Right, blackHeight, ref expectedBlackHeight);
        }
    }
}
