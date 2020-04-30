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
    public class Raket : Microsoft.Xna.Framework.DrawableGameComponent
    {
        KeyboardState oldKeyState;
        GraphicsDevice device;
        Camera camera;
        //BasicEffect effect;

        Vector3 moveVector;
        Vector3 position;
        float angle;

        Model raket;
        public Raket(Game game , Camera cam)
            : base(game)
        {
            device = (GraphicsDevice) game.Services.GetService(typeof(GraphicsDevice));
            oldKeyState = Keyboard.GetState();
            camera = cam;

            raket = game.Content.Load<Model>("Ship");

            position = new Vector3(0, 0, 0);
            angle = 0;
            //effect = new BasicEffect(device);
            /*
            foreach (ModelMesh mesh in raket.Meshes)
            {
                
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    meshPart.Effect = effect;
                }
            }
            */
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
            Input();
            position += moveVector;
            angle += 0.03f;
            base.Update(gameTime);
        }

        public void Input()
        {
            KeyboardState newKeyState = Keyboard.GetState();

            if (newKeyState.IsKeyDown(Keys.I))
            {
                moveVector = moveVector + new Vector3(0f, 0.001f, 0f);
            }
            if (newKeyState.IsKeyDown(Keys.K))
            {
                moveVector = moveVector + new Vector3(0f, -0.001f, 0f);
            }
            if (newKeyState.IsKeyDown(Keys.J))
            {
                moveVector = moveVector + new Vector3(0.001f, 0f, 0f);
            }
            if (newKeyState.IsKeyDown(Keys.L))
            {
                moveVector = moveVector + new Vector3(-0.001f, 0f, 0f);
            }
        }
        public override void Draw(GameTime gameTime)
        {
            Matrix[] raketTransforms = new Matrix[raket.Bones.Count];
            raket.CopyAbsoluteBoneTransformsTo(raketTransforms);
            float size = 1f;
            Matrix worldMatrix = Matrix.CreateScale(size, size, size) * Matrix.CreateRotationY(angle) * Matrix.CreateTranslation(position);

            foreach (ModelMesh mesh in raket.Meshes)
            {

                foreach (BasicEffect effect in mesh.Effects)
                {
                    
                    effect.World = worldMatrix;
                    effect.View = camera.viewMatrix;
                    effect.Projection = camera.projectionMatrix;
                    effect.EnableDefaultLighting();
                    /*
                    effect.VertexColorEnabled = false;
                    effect.TextureEnabled = true;

                    
                    effect.SpecularColor = (Vector3)Color.Red.ToVector3();
                    effect.SpecularPower = 1f;
                    effect.DirectionalLight0.Direction = new Vector3(1, -1, 1);
                    effect.DirectionalLight0.Enabled = true;
                    effect.AmbientLightColor = new Vector3(1f, 1f, 1f);
                    effect.DiffuseColor = new Vector3(0.5f, 0.6f, 0.7f);
                    effect.DirectionalLight1.Enabled = false;
                    effect.DirectionalLight2.Enabled = false;
                    */
                }
                mesh.Draw();
            }
            
            base.Draw(gameTime);
        }
    }
}
