using System.Collections;

namespace IvoEngine.Collections.DataStructures
{
    /// <summary>
    /// Red-black tree for storing key-value pairs.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    public class RedBlackTreeMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : IComparable<TKey>
    {
        private enum NodeColor
        {
            Red,
            Black
        }

        private class Node
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }
            public Node Left { get; set; }
            public Node Right { get; set; }
            public Node Parent { get; set; }
            public NodeColor Color { get; set; }

            public Node(TKey key, TValue value, NodeColor color)
            {
                Key = key;
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
        public RedBlackTreeMap()
        {
            root = null;
            count = 0;
        }

        /// <summary>
        /// Inserts or updates an element in the tree.
        /// </summary>
        /// <param name="key">Element key.</param>
        /// <param name="value">Element value.</param>
        public void Add(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (root == null)
            {
                // First inserted node is always black
                root = new Node(key, value, NodeColor.Black);
                count = 1;
                return;
            }

            Node current = root;
            Node parent = null;
            int comparison = 0;

            // Find insertion location
            while (current != null)
            {
                parent = current;
                comparison = key.CompareTo(current.Key);

                if (comparison < 0)
                    current = current.Left;
                else if (comparison > 0)
                    current = current.Right;
                else
                {
                    // Key already exists, update value
                    current.Value = value;
                    return;
                }
            }

            // Create new node (always red)
            Node newNode = new Node(key, value, NodeColor.Red);
            newNode.Parent = parent;

            // Attach new node to parent
            if (comparison < 0)
                parent.Left = newNode;
            else
                parent.Right = newNode;

            count++;

            // Fix the tree if red-black tree rules are violated
            BalanceAfterInsertion(newNode);
        }

        /// <summary>
        /// Tries to get the value associated with the given key.
        /// </summary>
        /// <param name="key">Key to search for.</param>
        /// <param name="value">Found value if key exists.</param>
        /// <returns>True if key exists; otherwise False.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            Node node = FindNode(key);
            if (node != null)
            {
                value = node.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Gets the value associated with the given key.
        /// </summary>
        /// <param name="key">Key to search for.</param>
        /// <returns>Value associated with the key.</returns>
        /// <exception cref="KeyNotFoundException">When key was not found.</exception>
        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out TValue value))
                    return value;
                throw new KeyNotFoundException($"Key '{key}' was not found.");
            }
            set
            {
                Add(key, value);
            }
        }

        /// <summary>
        /// Verifies whether the tree contains the given key.
        /// </summary>
        /// <param name="key">Key to search for.</param>
        /// <returns>True if key exists; otherwise False.</returns>
        public bool ContainsKey(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return FindNode(key) != null;
        }

        /// <summary>
        /// Tries to remove an element with the given key.
        /// </summary>
        /// <param name="key">Key of element to remove.</param>
        /// <returns>True if element was removed; otherwise False.</returns>
        public bool Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            Node nodeToRemove = FindNode(key);
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
        /// Finds the node by key.
        /// </summary>
        private Node FindNode(TKey key)
        {
            Node current = root;
            while (current != null)
            {
                int comparison = key.CompareTo(current.Key);
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
            // If node is root, change it to black and end
            if (node == root)
            {
                node.Color = NodeColor.Black;
                return;
            }

            // While parent of node is red (violation of rule 4)
            while (node != root && node.Parent.Color == NodeColor.Red)
            {
                if (node.Parent.IsLeftChild)
                {
                    // Parent is left child of its parent
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
                        // Case 2: Node is right child - left rotation
                        if (node.IsRightChild)
                        {
                            node = node.Parent;
                            RotateLeft(node);
                        }

                        // Case 3: Node is left child - right rotation
                        node.Parent.Color = NodeColor.Black;
                        node.Parent.Parent.Color = NodeColor.Red;
                        RotateRight(node.Parent.Parent);
                    }
                }
                else
                {
                    // Parent is right child of its parent (mirror case)
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
                        // Case 2: Node is left child - right rotation
                        if (node.IsLeftChild)
                        {
                            node = node.Parent;
                            RotateRight(node);
                        }

                        // Case 3: Node is right child - left rotation
                        node.Parent.Color = NodeColor.Black;
                        node.Parent.Parent.Color = NodeColor.Red;
                        RotateLeft(node.Parent.Parent);
                    }
                }
            }

            // Ensure root is always black
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
                // Find successor (minimum in right subtree)
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
        /// Finds the node with the minimum key in the subtree.
        /// </summary>
        private Node FindMinimum(Node node)
        {
            while (node.Left != null)
                node = node.Left;
            return node;
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

                    // Case 2: Both children of sibling are black
                    if ((sibling?.Left == null || sibling.Left.Color == NodeColor.Black) &&
                        (sibling?.Right == null || sibling.Right.Color == NodeColor.Black))
                    {
                        if (sibling != null)
                            sibling.Color = NodeColor.Red;
                        node = node.Parent;
                    }
                    else
                    {
                        // Case 3: Right child of sibling is black
                        if (sibling?.Right == null || sibling.Right.Color == NodeColor.Black)
                        {
                            if (sibling.Left != null)
                                sibling.Left.Color = NodeColor.Black;
                            sibling.Color = NodeColor.Red;
                            RotateRight(sibling);
                            sibling = node.Parent.Right;
                        }

                        // Case 4: Right child of sibling is red
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

                    // Case 2: Both children of sibling are black
                    if ((sibling?.Left == null || sibling.Left.Color == NodeColor.Black) &&
                        (sibling?.Right == null || sibling.Right.Color == NodeColor.Black))
                    {
                        if (sibling != null)
                            sibling.Color = NodeColor.Red;
                        node = node.Parent;
                    }
                    else
                    {
                        // Case 3: Left child of sibling is black
                        if (sibling?.Left == null || sibling.Left.Color == NodeColor.Black)
                        {
                            if (sibling.Right != null)
                                sibling.Right.Color = NodeColor.Black;
                            sibling.Color = NodeColor.Red;
                            RotateLeft(sibling);
                            sibling = node.Parent.Left;
                        }

                        // Case 4: Left child of sibling is red
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

            // Ensure node is black
            node.Color = NodeColor.Black;
        }

        /// <summary>
        /// Gets an enumerator for iterating through key-value pairs in the tree (inorder).
        /// </summary>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
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

                    // Process current node
                    current = stack.Pop();
                    yield return new KeyValuePair<TKey, TValue>(current.Key, current.Value);

                    // Move to right subtree
                    current = current.Right;
                }
            }
        }

        /// <summary>
        /// Gets an enumerator for iterating through key-value pairs in the tree (inorder).
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns all keys in the tree.
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                foreach (var pair in this)
                {
                    yield return pair.Key;
                }
            }
        }

        /// <summary>
        /// Returns all values in the tree.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var pair in this)
                {
                    yield return pair.Value;
                }
            }
        }

        /// <summary>
        /// Verifies that the tree is a valid red-black tree.
        /// </summary>
        public bool ValidateRedBlackProperties()
        {
            if (root == null)
                return true;

            // Rule 1: Every node is either red or black
            // Rule 2: Root is black
            if (root.Color != NodeColor.Black)
                return false;

            // Rule 3: All leaf nodes (null) are black
            // Rule 4: Every red node has black children
            // Rule 5: Every path from root to leaf has the same number of black nodes

            // Check rule 4 and determine number of black nodes on each path
            if (!ValidateRedNodes(root))
                return false;

            // Check rule 5
            int blackCount = -1;
            return ValidateBlackHeight(root, 0, ref blackCount);
        }

        /// <summary>
        /// Verifies that the tree satisfies the property that a red node has only black children.
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
        /// Verifies that the tree satisfies the property that all paths from root to leaf have the same number of black nodes.
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

            // Add 1 to height if current node is black
            if (node.Color == NodeColor.Black)
                blackHeight++;

            return ValidateBlackHeight(node.Left, blackHeight, ref expectedBlackHeight) &&
                   ValidateBlackHeight(node.Right, blackHeight, ref expectedBlackHeight);
        }
    }
}
