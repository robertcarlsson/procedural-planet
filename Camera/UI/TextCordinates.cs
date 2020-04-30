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
    public class TextCordinates : Microsoft.Xna.Framework.DrawableGameComponent
    {
        SpriteBatch sBatch;
        GraphicsDevice device;
        SpriteFont font;
        Camera camera;

        Vector2 startTextPos;
        Vector2 textSpace;
        int nextPos;

        RenderTarget2D renderTarget;
        Texture2D shadowMap;

        public TextCordinates(Game game, Camera cam)
            : base(game)
        {
            device = (GraphicsDevice)game.Services.GetService(typeof(GraphicsDevice));
            sBatch = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));
            camera = cam;

            PresentationParameters pp = device.PresentationParameters;
            renderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, SurfaceFormat.Alpha8, DepthFormat.Depth16);

            startTextPos = new Vector2(20, 20);
            textSpace = new Vector2(0, 10);
            font = game.Content.Load<SpriteFont>("coordinates");
            // TODO: Construct any child components here
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here

            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // TODO: Add your update code here

            base.Update(gameTime);
        }
        public override void Draw(GameTime gameTime)
        {
            nextPos = 1;
            //DrawCubeCoordinates();
            DrawXYZ();
            DrawCameraPosition();
            //DrawPrimes();
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.Default;

            base.Draw(gameTime);
        }
        private void DrawCameraPosition()
        {
            sBatch.Begin();
            sBatch.DrawString(font, "Cam pos x: " + camera.cameraPosition.X, (startTextPos + nextPos++ * textSpace), Color.Black);
            sBatch.DrawString(font, "Cam pos y: " + camera.cameraPosition.Y, (startTextPos + nextPos++ * textSpace), Color.Black);
            sBatch.DrawString(font, "Cam pos z: " + camera.cameraPosition.Z, (startTextPos + nextPos++ * textSpace), Color.Black);
            sBatch.End();
        }

        public void DrawString(string text, Color color)
        {
            sBatch.Begin();
            sBatch.DrawString(font, text, (startTextPos + nextPos++ * textSpace), color);
            sBatch.End();
        }
        private void DrawPrimes()
        {
            List<int> primes = new List<int>();
            bool testPrime;
            for (int x = 9000; x < 10000; x += 7)
            {
                testPrime = true;
                for (int y = x; y > 0; y--)
                {
                    if ((y != x) && (y != 1))
                    {
                        if ((x % y) == 0)
                        {
                            testPrime = false;
                            break;
                        }
                    }
                }
                if (testPrime)
                    primes.Add(x);
            }
            sBatch.Begin();
            Vector2 startTextPos = new Vector2(400, 20);
            Vector2 textSpaceHeight = new Vector2(0,10);
            Vector2 textSpaceWidth = new Vector2(10,0);
            string startString = "Primes : ";
            int startLenght = startString.Length;
            int next = 0;
            sBatch.DrawString(font, startString , startTextPos - (startLenght * textSpaceWidth), Color.Black);
            foreach (int number in primes)
            {
                sBatch.DrawString(font, number.ToString() + " : ", startTextPos + textSpaceHeight * next, Color.Black); 
                next++;
            }
            sBatch.End();
        }
        private void DrawCubeCoordinates()
        {
            sBatch.Begin();
            int coord = 20;
            for (int x = -coord; x < coord+1; x++)
            {
                for (int y = -coord; y < coord+1; y++)
                {
                    for (int z = -coord; z < coord+1; z++)
                    {
                        Vector3 position = new Vector3(x, y, z);
                        Vector3 distance = position - camera.cameraPosition;
                        float d = distance.Length();
                        if (d < 15)
                        {
                            if (//(x == 0 && y == 0) || (x == 0 && z == 0) || (y == 0 && z == 0) ||
                                (Math.Abs(x) == Math.Abs(y) && (Math.Abs(y) == Math.Abs(z) && Math.Abs(x) < 2 )) ||
                                (Math.Abs(x) == Math.Abs(y) && z == 0 && Math.Abs(x) < 2) ||
                                (Math.Abs(y) == Math.Abs(z) && x == 0 && Math.Abs(y) < 2) ||
                                (Math.Abs(x) == Math.Abs(z) && y == 0 && Math.Abs(x) < 2))
                            {

                                Vector3 screenSpace = device.Viewport.Project(Vector3.Zero, camera.projectionMatrix, camera.viewMatrix,
                                    Matrix.CreateTranslation(position));

                                BoundingBox transformedBBox = new BoundingBox(position, position);
                                ContainmentType cameraNodeContainment = camera.cameraFrustum.Contains(transformedBBox);
                                if (cameraNodeContainment != ContainmentType.Disjoint)
                                {
                                    Vector2 textPosition = new Vector2(screenSpace.X, screenSpace.Y);
                                    string text = "Pos: " +
                                        position.X.ToString() + " : " +
                                        position.Y.ToString() + " : " +
                                        position.Z.ToString();

                                    Vector2 stringCenter = font.MeasureString(text) * 0.5f;

                                    textPosition.X = (textPosition.X - stringCenter.X);
                                    textPosition.Y = (textPosition.Y - stringCenter.Y);

                                    sBatch.DrawString(font, text, textPosition, Color.Black);
                                }
                            }
                        }
                    }
                }
            }


            sBatch.End();

        }

        private void DrawTest()
        {

        }
        private void DrawTest2()
        {
            device.SetRenderTarget(renderTarget);
        }
        private void DrawXYZ()
        {
            sBatch.Begin();
            float distance = camera.cameraPosition.Length();
            int spacing;


            if (distance < 5)
            {
                spacing = 1;
                distance = 5;
            }
            else if (distance < 10)
            {
                spacing = 2;
                distance = 10;
            }
            else if (distance < 20)
            {
                spacing = 4;
                distance = 20;
            }
            else
            {
                spacing = (int)(distance / 5);
            }

            if ( spacing <= 0)
            {
                spacing = 1;
            }

            for (int x = (int)-distance; x < distance; x += spacing)
            {
                Vector3 position = new Vector3(x, 0, 0);
                Vector3 screenSpace = device.Viewport.Project(Vector3.Zero, camera.projectionMatrix, camera.viewMatrix,
                                Matrix.CreateTranslation(position));

                BoundingBox transformedBBox = new BoundingBox(position, position);
                ContainmentType cameraNodeContainment = camera.cameraFrustum.Contains(transformedBBox);
                if (cameraNodeContainment != ContainmentType.Disjoint && Math.Abs(x) >= spacing)
                {
                    Vector2 textPosition = new Vector2(screenSpace.X, screenSpace.Y);
                    string text = "Pos: " +
                        position.X.ToString() + " : " +
                        position.Y.ToString() + " : " +
                        position.Z.ToString();

                    Vector2 stringCenter = font.MeasureString(text) * 0.5f;

                    textPosition.X = (textPosition.X - stringCenter.X);
                    textPosition.Y = (textPosition.Y - stringCenter.Y);

                    sBatch.DrawString(font, text, textPosition, Color.Black);
                }
            }
            for (int y = (int)-distance; y < distance; y += spacing)
            {
                Vector3 position = new Vector3(0, y, 0);
                Vector3 screenSpace = device.Viewport.Project(Vector3.Zero, camera.projectionMatrix, camera.viewMatrix,
                                Matrix.CreateTranslation(position));

                BoundingBox transformedBBox = new BoundingBox(position, position);
                ContainmentType cameraNodeContainment = camera.cameraFrustum.Contains(transformedBBox);
                if (cameraNodeContainment != ContainmentType.Disjoint && Math.Abs(y) >= spacing)
                {
                    Vector2 textPosition = new Vector2(screenSpace.X, screenSpace.Y);
                    string text = "Pos: " +
                        position.X.ToString() + " : " +
                        position.Y.ToString() + " : " +
                        position.Z.ToString();

                    Vector2 stringCenter = font.MeasureString(text) * 0.5f;

                    textPosition.X = (textPosition.X - stringCenter.X);
                    textPosition.Y = (textPosition.Y - stringCenter.Y);

                    sBatch.DrawString(font, text, textPosition, Color.Black);
                }
            }
            for (int z = (int)-distance; z < distance; z += spacing)
            {
                Vector3 position = new Vector3(0, 0, z);
                Vector3 screenSpace = device.Viewport.Project(Vector3.Zero, camera.projectionMatrix, camera.viewMatrix,
                                Matrix.CreateTranslation(position));

                BoundingBox transformedBBox = new BoundingBox(position, position);
                ContainmentType cameraNodeContainment = camera.cameraFrustum.Contains(transformedBBox);
                if (cameraNodeContainment != ContainmentType.Disjoint && Math.Abs(z) >= spacing)
                {
                    Vector2 textPosition = new Vector2(screenSpace.X, screenSpace.Y);
                    string text = "Pos: " +
                        position.X.ToString() + " : " +
                        position.Y.ToString() + " : " +
                        position.Z.ToString();

                    Vector2 stringCenter = font.MeasureString(text) * 0.5f;

                    textPosition.X = (textPosition.X - stringCenter.X);
                    textPosition.Y = (textPosition.Y - stringCenter.Y);

                    sBatch.DrawString(font, text, textPosition, Color.Black);
                }
            }
            sBatch.End();
        }
    }
}
