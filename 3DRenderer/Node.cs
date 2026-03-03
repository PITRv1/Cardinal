using PETRenderer;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace PETRenderer 
{
    public abstract class Node : IDisposable
    {
        public string Name { get; set; } = "Node";
        public Node Parent { get; private set; }
        private List<Node> _children = new();
        public IReadOnlyList<Node> Children => _children;

        public Transform LocalTransform { get; set; } = new Transform();

        // World transform is computed by walking up the tree and combining matrices
        public Matrix4x4 WorldMatrix {
            get {
                if (Parent == null)
                    return LocalTransform.ViewMatrix;
                return LocalTransform.ViewMatrix * Parent.WorldMatrix;
            }
        }

        public void AddChild(Node child) {
            if (child.Parent != null)
                child.Parent.RemoveChild(child);

            child.Parent = this;
            _children.Add(child);
        }

        public void RemoveChild(Node child) {
            child.Parent = null;
            _children.Remove(child);
        }

        public virtual void Update(double time, double deltaTime) {
            foreach (var child in _children)
                child.Update(time, deltaTime);
        }

        public virtual void Render(Shader shader, Matrix4x4 view, Matrix4x4 projection) {
            foreach (var child in _children)
                child.Render(shader, view, projection);
        }

        public Node GetNode(string path) {
            var parts = path.Split('/');
            return GetNodeByParts(parts, 0);
        }

        public T GetNode<T>(string path) 
            where T : Node {
            var node = GetNode(path);
            if (node is T typed)
                return typed;

            throw new Exception($"Node at '{path}' is not of type {typeof(T).Name}");
        }

        public Node GetNodeByParts(string[] parts, int index) {
            if (index >= parts.Length) return this;

            foreach (var child in _children) {
                if (child.Name == parts[index]) {
                    return child.GetNodeByParts(parts, index + 1);
                }
            }

            throw new Exception($"Node not found at path segment '{parts[index]}'");
        }

        public void SetWorldPosition(Vector3 worldPosition) {
            if (Parent == null) {
                LocalTransform.Position = worldPosition;
                return;
            }

            // Invert the parent's world matrix to convert world position to local space
            Matrix4x4.Invert(Parent.WorldMatrix, out var invertedParent);
            LocalTransform.Position = Vector3.Transform(worldPosition, invertedParent);
        }

        public Vector3 GetWorldPosition() {
            if (Parent == null)
                return LocalTransform.Position;

            return Vector3.Transform(LocalTransform.Position, Parent.WorldMatrix);
        }

        /// <summary>
        /// Use Scene.QueueFree instead. Using this function on a Node directly might delete them mid draw which might lead to crashes.
        /// </summary>
        public void QueueFree() {
            Parent?.RemoveChild(this);
            Dispose();
        }

        public virtual void Dispose() {
            foreach (var child in _children)
                child.Dispose();
        }
    }
}