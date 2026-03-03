using Silk.NET.Input;
using Silk.NET.Maths;
using System;
using System.Numerics;

namespace PETRenderer
{
    public class Camera
    {
        public Vector3 Position = new Vector3(0.0f, 0.0f, 3.0f);
        public Vector3 Front = new Vector3(0.0f, 0.0f, -1.0f);
        public Vector3 Up = Vector3.UnitY;
        public Vector3 Direction = Vector3.Zero;

        public float Yaw = -90f;
        public float Pitch = 0f;
        public float Zoom = 45f;
        public float MoveSpeed = 5.0f;
        public float LookSensitivity = 0.1f;

        public bool IsPerspective = true;
        public bool IsDebug = false;
        public float OrthoScaler = 0.05f;

        private Vector2 _lastMousePosition;

        public void ProcessKeyboard(IKeyboard keyboard, float deltaTime) {
            if (IsDebug) return;

            var moveSpeed = MoveSpeed * deltaTime;

            if (keyboard.IsKeyPressed(Key.W))
                Position += moveSpeed * Front;
            if (keyboard.IsKeyPressed(Key.S))
                Position -= moveSpeed * Front;
            if (keyboard.IsKeyPressed(Key.A))
                Position -= Vector3.Normalize(Vector3.Cross(Front, Up)) * moveSpeed;
            if (keyboard.IsKeyPressed(Key.D))
                Position += Vector3.Normalize(Vector3.Cross(Front, Up)) * moveSpeed;
        }

        public void ProcessMouseMove(Vector2 position) {
            if (IsDebug) return;

            if (_lastMousePosition == default) {
                _lastMousePosition = position;
                return;
            }

            var xOffset = (position.X - _lastMousePosition.X) * LookSensitivity;
            var yOffset = (position.Y - _lastMousePosition.Y) * LookSensitivity;
            _lastMousePosition = position;

            Yaw += xOffset;
            Pitch = Math.Clamp(Pitch - yOffset, -89.0f, 89.0f);

            Direction.X = MathF.Cos(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));
            Direction.Y = MathF.Sin(MathHelper.DegreesToRadians(Pitch));
            Direction.Z = MathF.Sin(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));
            Front = Vector3.Normalize(Direction);
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
    }
}