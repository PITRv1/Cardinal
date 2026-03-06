using Silk.NET.Input;
using Silk.NET.Maths;
using System;
using System.Numerics;

namespace PETRenderer
{
    public class Camera
    {
        public Vector3 Position = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 Front = new Vector3(0.0f, 0.0f, -1.0f);
        public Vector3 Up = Vector3.UnitY;
        public Vector3 Direction = Vector3.Zero;

        public float Yaw = -137f;
        public float Pitch = -45f;
        public float Zoom = 45f;
        public float MoveSpeed = 5.0f;
        public float LookSensitivity = 0.1f;

        public bool IsPerspective = false;
        private bool _isDragging = false;
        public float OrthoScaler = 0.1f;

        private Vector2 _lastMousePosition;

        public Camera() {
            UpdateCameraDirection(Yaw, Pitch);
        }


        public void BeginDrag() => _isDragging = false;
        public void EndDrag() => _isDragging = false;

        public void ProcessMouseMove(Vector2 position) {
            if (!_isDragging) {
                _lastMousePosition = position;
                _isDragging = true;
                return;
            }

            var xOffset = (position.X - _lastMousePosition.X) * OrthoScaler;
            var yOffset = (position.Y - _lastMousePosition.Y) * OrthoScaler;
            _lastMousePosition = position;

            var right = -Vector3.Normalize(Vector3.Cross(Front, Up));
            var forward = Vector3.Normalize(new Vector3(Front.X, 0, Front.Z));

            Position += right * xOffset;
            Position += forward * yOffset;
        }

        public Matrix4x4 GetViewMatrix() {
            return Matrix4x4.CreateLookAt(Position, Position + Front, Up);
        }

        public Matrix4x4 GetProjectionMatrix(Vector2D<int> size) {
            if (IsPerspective)
                return Matrix4x4.CreatePerspectiveFieldOfView(
                    MathHelper.DegreesToRadians(Zoom),
                    (float)size.X / size.Y,
                    0.1f, 100.0f);
            else
                return Matrix4x4.CreateOrthographic(
                    size.X * OrthoScaler,
                    size.Y * OrthoScaler,
                    0.1f, 100.0f);
        }

        private void UpdateCameraDirection(float Yaw, float Pitch) {
            Direction.X = MathF.Cos(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));
            Direction.Y = MathF.Sin(MathHelper.DegreesToRadians(Pitch));
            Direction.Z = MathF.Sin(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));
            Front = Vector3.Normalize(Direction);
        }
    }
}