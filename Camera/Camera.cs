using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace Testing3D
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class Camera : Microsoft.Xna.Framework.GameComponent
    {
        public Vector3 cameraPosition = new Vector3(3, 3, 3);
        float leftrightRot = MathHelper.PiOver2;
        float updownRot = -MathHelper.Pi / 10.0f;
        const float rotationSpeed = 0.2f;
        const float moveSpeed = 30.0f;

        public Matrix viewMatrix;
        public Matrix projectionMatrix;
        MouseState originalMouseState;     
        GraphicsDevice device;

        public BoundingFrustum cameraFrustum;
        float speedKeyboard = 0.3f;

        public Camera(Game game)
            : base(game)
        {
            device = (GraphicsDevice) game.Services.GetService(typeof(GraphicsDevice));

            Initialize();
        }


        public override void Initialize()
        {
            Mouse.SetPosition(device.Viewport.Width / 2, device.Viewport.Height / 2);
            originalMouseState = Mouse.GetState();
            viewMatrix = Matrix.CreateLookAt(cameraPosition, new Vector3(0, 0, 0), new Vector3(0, -1, 0));
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView((float)Math.PI/3f, device.Viewport.AspectRatio, 0.000001f, 100.0f);

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {

            base.Update(gameTime);
        }

        private void UpdateViewMatrix()
        {
            Matrix cameraRotation = Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationY(leftrightRot);

            Vector3 cameraOriginalTarget = new Vector3(0, 0, -1);
            Vector3 cameraRotatedTarget = Vector3.Transform(cameraOriginalTarget, cameraRotation);
            Vector3 cameraFinalTarget = cameraPosition + cameraRotatedTarget;

            Vector3 cameraOriginalUpVector = new Vector3(0, 1, 0);
            Vector3 cameraRotatedUpVector = Vector3.Transform(cameraOriginalUpVector, cameraRotation);

            viewMatrix = Matrix.CreateLookAt(cameraPosition, cameraFinalTarget, cameraRotatedUpVector);
        }

        public void ProcessInput(float amount)
        {


            MouseState currentMouseState = Mouse.GetState();
            if (currentMouseState != originalMouseState)
            {
                float xDifference = currentMouseState.X - originalMouseState.X;
                float yDifference = currentMouseState.Y - originalMouseState.Y;
                leftrightRot -= rotationSpeed * xDifference * amount;
                updownRot -= rotationSpeed * yDifference * amount;
                Mouse.SetPosition(device.Viewport.Width / 2, device.Viewport.Height / 2);
                UpdateViewMatrix();
            }

            Vector3 moveVector = new Vector3(0, 0, 0);
            KeyboardState keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Up) || keyState.IsKeyDown(Keys.W))
                moveVector += new Vector3(0, 0, -speedKeyboard);
            if (keyState.IsKeyDown(Keys.Down) || keyState.IsKeyDown(Keys.S))
                moveVector += new Vector3(0, 0, speedKeyboard);
            if (keyState.IsKeyDown(Keys.Right) || keyState.IsKeyDown(Keys.D))
                moveVector += new Vector3(speedKeyboard, 0, 0);
            if (keyState.IsKeyDown(Keys.Left) || keyState.IsKeyDown(Keys.A))
                moveVector += new Vector3(-speedKeyboard, 0, 0);
            if (keyState.IsKeyDown(Keys.Q))
                moveVector += new Vector3(0, speedKeyboard, 0);
            if (keyState.IsKeyDown(Keys.Z))
                moveVector += new Vector3(0, -speedKeyboard, 0);

            if (keyState.IsKeyDown(Keys.Add))
            {
                if (speedKeyboard < 0.002f)
                    speedKeyboard += 0.00001f;
                else if (speedKeyboard < 0.2f)
                    speedKeyboard += 0.001f;
                else
                    speedKeyboard += 0.10f;

                if (speedKeyboard > 10f)
                    speedKeyboard = 10f;
            }

            if (keyState.IsKeyDown(Keys.Subtract))
            {
                if (speedKeyboard < 0.002f)
                    speedKeyboard -= 0.00001f;
                else if (speedKeyboard < 0.2f)
                    speedKeyboard -= 0.001f;
                else
                    speedKeyboard -= 0.10f;

                if (speedKeyboard < 0)
                    speedKeyboard = 0;
            }

            AddToCameraPosition(moveVector * amount);
            cameraFrustum = new BoundingFrustum(viewMatrix * projectionMatrix);
        }
        private void AddToCameraPosition(Vector3 vectorToAdd)
        {
            Matrix cameraRotation = Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationY(leftrightRot);
            Vector3 rotatedVector = Vector3.Transform(vectorToAdd, cameraRotation);
            cameraPosition += moveSpeed * rotatedVector;
            UpdateViewMatrix();
        }
    }
}
