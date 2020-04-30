using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Testing3D.Noise;

namespace Testing3D.Terrain.Quadtree
{
    public class CelestialNode
    {
        #region Enumeration

        //Since im noob and dont understand the enumeration in c#
        public byte FrontFace = 0;
        public byte RightFace = 1;
        public byte LeftFace = 2;
        public byte BackFace = 3;
        public byte TopFace = 4;
        public byte BottomFace = 5;

        public byte TopLeft = 0;
        public byte TopRight = 1;
        public byte BottomLeft = 2;
        public byte BottomRight = 3;

        public byte TopEdge = 0;
        public byte RightEdge = 1;
        public byte BottomEdge = 2;
        public byte LeftEdge = 3;

        #endregion

        #region Fields
        

        //Game related information
        Game game;
        Camera camera;
        GraphicsDevice device;
        BasicEffect basicEffect;


        //Pointer to the planet/asteriod/star
        CelestialQuadtree celestialParent;
        CelestialNode parentNode;
        CelestialNode[] childNode;
        CelestialNode[] neighborNode;


        int RootFace;
        int nodeHeight;
        int nodeWidth;
        int MaxDepth;

        VertexBuffer nodeVertexBuffer;
        IndexBuffer nodeIndexBuffer;

        // For more space in memory this can be taken away and only use Buffers
        VertexTexturesNormal[] vertices;
        int[] indices;

        Texture2D[] Textures;
        public int splitDepth;
        Texture2D sandTexture;
        
        Vector3 nodeCenter;
        Vector3 testCenter;
        Vector3 axisX;
        Vector3 axisZ;
        Vector3 nodeColor;
        public BoundingBox nodeBoundingBox;

        Random random = new Random();
        
        bool isVisible = true;
        bool isLeafNode = true;
        #endregion

        #region Properties

        public CelestialNode[] Neighbors
        {
            get { return neighborNode; }
        }
        #endregion

        #region Constructor / Destructor

        /// <summary>
        /// Quadtree Node Made for spherical objects
        /// </summary>
        /// <param name="g"></param>
        /// <param name="cam"></param>
        /// <param name="basicEffect"></param>
        /// <param name="ParentTree"></param>
        /// <param name="center"> Center of the Node used for the mesh building</param>
        /// <param name="axisX"></param>
        /// <param name="axisZ"></param>
        /// <param name="MaxDepth"></param>
        /// <param name="NodeWidth"></param>
        /// <param name="splitDepth"></param>
        /// <param name="RootFace"></param>
        public CelestialNode(Game g, Camera cam, BasicEffect basicEffect, CelestialQuadtree ParentTree, Vector3 center, Vector3 axisX, Vector3 axisZ, int MaxDepth, int NodeWidth, int splitDepth , int RootFace)
        {
            game = g;
            camera = cam;
            this.basicEffect = basicEffect;
            device = (GraphicsDevice)game.Services.GetService(typeof(GraphicsDevice));

            celestialParent = ParentTree;
            neighborNode = new CelestialNode[4];

            nodeWidth = NodeWidth;
            nodeHeight = NodeWidth;
            nodeCenter = center;
            this.axisX = axisX;
            this.axisZ = axisZ;  
            this.MaxDepth = MaxDepth;
            this.splitDepth = splitDepth;
            this.RootFace = RootFace;

            isLeafNode = true;

            testCenter = nodeCenter;
            celestialParent.celestialNoise.SphereMapping(ref testCenter);
            testCenter *= celestialParent.Radius;

            sandTexture = game.Content.Load<Texture2D>("Textures/sand02");
            //sandTexture = game.Content.Load<Texture2D>("grass");

            float col = (float)(random.NextDouble() + 1) / 2;
            nodeColor = new Vector3(col, col, col);
            
            //texture = game.Content.Load<Texture2D>("grass");

            BuildMesh(nodeCenter, axisX, axisZ);
            nodeBoundingBox = CreateBoundingBox(vertices);
            SetUpIndices();
            CalculateNormals();
            CopyVertexToBuffers();
            CopyIndicesToBuffers();
        }

        public void InitQuadrantTable()
        {
            /*
            // When neighbors cross from one face of the cube to another, a mapping is needed to find the correct quadrant
	        static const unsigned char nTopLeftTopEdge[6] = { BottomRight, TopLeft, TopRight, BottomLeft, BottomLeft, TopRight };
	        static const unsigned char nTopLeftLeftEdge[6] = { TopRight, TopRight, TopLeft, BottomRight, TopRight, TopRight };
	        static const unsigned char nTopRightTopEdge[6] = { TopRight, BottomLeft, TopLeft, BottomRight, BottomRight, TopLeft };
	        static const unsigned char nTopRightRightEdge[6] = { TopLeft, TopLeft, TopRight, BottomLeft, TopLeft, TopLeft };
	        static const unsigned char nBottomLeftBottomEdge[6] = { TopRight, BottomLeft, TopLeft, BottomRight, TopLeft, BottomRight };
	        static const unsigned char nBottomLeftLeftEdge[6] = { BottomRight, BottomRight, TopRight, BottomLeft, BottomRight, BottomRight };
	        static const unsigned char nBottomRightBottomEdge[6] = { BottomRight, TopLeft, TopRight, BottomLeft, TopRight, BottomLeft };
	        static const unsigned char nBottomRightRightEdge[6] = { BottomLeft, BottomLeft, TopLeft, BottomRight, BottomLeft, BottomLeft };
             * */
        }

        #endregion

        #region Neighbor Methods

        /// <summary>
        /// Automatic Neighbor setting depending on the RootFace
        /// </summary>
        public void SetRootNeighbors()
        {

            for (int i = 0; i < 4; i++)
            {
                //This sets the neighbors for the roots by checking the faceneighbor table and using it as index in the cubeRoot
                neighborNode[i] = celestialParent.CubeRoot[CelestialQuadtree.FaceNeighbor[RootFace, i]];
            }
        }
        public void SetNeighbors(CelestialNode Top, CelestialNode Right, CelestialNode Bottom, CelestialNode Left)
        {
            neighborNode[TopEdge] = Top;
            neighborNode[RightEdge] = Right;
            neighborNode[BottomEdge] = Bottom;
            neighborNode[LeftEdge] = Left;
        }
        public void FindQuadrantNeighbor(int Quadrant, int Edge)
        {

        }
        #endregion

        #region Methods
        public void MergeNode()
        {
            isLeafNode = true;
            childNode = null;
        }
        public void SplitNode()
        {
            isVisible = false;
            isLeafNode = false;
            childNode = new CelestialNode[4];
            Vector3 childAxisX = axisX / 2;
            Vector3 childAxisZ = axisZ / 2;

            Vector3 topLeftCenter = nodeCenter - childAxisX / 2 + childAxisZ / 2;
            childNode[0] = new CelestialNode(game, camera, basicEffect, celestialParent, topLeftCenter, childAxisX, childAxisZ, MaxDepth, nodeWidth, numberOfSplits + 1);

            Vector3 topRightCenter = nodeCenter + childAxisX / 2 + childAxisZ / 2;
            childNode[1] = new CelestialNode(game, camera, basicEffect, celestialParent, topRightCenter, childAxisX, childAxisZ, MaxDepth, nodeWidth, numberOfSplits + 1);

            Vector3 bottomLeftCenter = nodeCenter - childAxisX / 2 - childAxisZ / 2;
            childNode[2] = new CelestialNode(game, camera, basicEffect, celestialParent, bottomLeftCenter, childAxisX, childAxisZ, MaxDepth, nodeWidth, numberOfSplits + 1);

            Vector3 bottomRightCenter = nodeCenter + childAxisX / 2 - childAxisZ / 2;
            childNode[3] = new CelestialNode(game, camera, basicEffect, celestialParent, bottomRightCenter, childAxisX, childAxisZ, MaxDepth, nodeWidth, numberOfSplits + 1);

        }
        private void BuildMesh(Vector3 centerPos, Vector3 axisX, Vector3 axisZ)
        {
            // temp array for demonstraion
            VertexTexturesNormal[] tempVertices = new VertexTexturesNormal[nodeWidth * nodeHeight];
            Node.numberOfVertices += nodeWidth * nodeHeight;

            for (int u = 0; u < nodeWidth; u++)
            {
                for (int v = 0; v < nodeHeight; v++)
                {
                    Vector3 tempVector = centerPos + ((axisX / (nodeWidth - 1)) * (u - (nodeWidth - 1) / 2f)) + ((axisZ / (nodeHeight - 1)) * (v - (nodeHeight - 1) / 2f));
                    //Sphere mapping
                    celestialParent.celestialNoise.SphereMapping(ref tempVector);
                    tempVertices[u + v * nodeWidth].Position = tempVector;


                    float height = celestialParent.celestialNoise.fractal.BlendFractal(tempVertices[u + v * nodeWidth].Position, celestialParent.blendFractal) / 20;
                    //float height = celestialParent.celestialNoise.fractal.fBm(tempVector, 0.5f, 2.0f, 10) / 100;
                    
                    tempVertices[u + v * nodeWidth].Position *= celestialParent.Radius + height;

                    tempVertices[u + v * nodeWidth].TextureCoordinate.X = (float)(u / (nodeWidth * 30));
                    tempVertices[u + v * nodeWidth].TextureCoordinate.Y = (float)(v / (nodeHeight * 30));
                }
            }
            vertices = tempVertices;
        }
        private void SetUpIndices()
        {
            indices = new int[(nodeWidth - 1) * (nodeHeight - 1) * 6];
            int counter = 0;
            for (int y = 0; y < nodeHeight - 1; y++)
            {
                for (int x = 0; x < nodeWidth - 1; x++)
                {
                    /*
                    if (x == 0)
                    {
                        if (y % 2 == 1)
                        {
                            int stichTopLeft = x + (y + 1) * nodeWidth;
                            int stichTopRight = (x + 1) + (y + 2) * nodeWidth;
                            int stichMiddleRight = (x + 1) + (y + 1) * nodeWidth;
                            int stichLowerLeft = (x) + (y - 1) * nodeWidth;
                            int stichLowerRight = (x + 1) + y * nodeWidth;

                            indices[counter++] = stichTopLeft;
                            indices[counter++] = stichLowerLeft;
                            indices[counter++] = stichLowerRight;



                            if (y < nodeHeight - 2)
                            {
                                indices[counter++] = stichTopLeft;
                                indices[counter++] = stichLowerRight;
                                indices[counter++] = stichMiddleRight;
                            }


                            if (y < nodeHeight - 2)
                            {

                                indices[counter++] = stichTopRight;
                                indices[counter++] = stichTopLeft;
                                indices[counter++] = stichMiddleRight;

                            }
                        }
                    }
                    else if (y == 0)
                    {
                        if (x % 2 == 1)
                        {
                            int stichTopLeft = x + (y + 1) * width;
                            int stichTopRight = (x + 1) + (y + 1) * width;
                            int stichLowerLeft = (x - 1) + y * width;
                            int stichLowerRight = (x + 1) + y * width;
                            int stichTopRight2 = (x + 2) + (y + 1) * width; ;

                            indices[counter++] = stichTopLeft;
                            indices[counter++] = stichLowerLeft;
                            indices[counter++] = stichLowerRight;

                            if (x < width - 2)
                            {
                                indices[counter++] = stichTopLeft;
                                indices[counter++] = stichLowerRight;
                                indices[counter++] = stichTopRight;
                            }

                            if (x < width - 2)
                            {

                                indices[counter++] = stichTopRight;
                                indices[counter++] = stichLowerRight;
                                indices[counter++] = stichTopRight2;

                            }
                        }
                    }
                     * */
                    //This is for making two triangels out of a four courners
                    int lowerLeft = x + y * nodeWidth;
                    int lowerRight = (x + 1) + y * nodeWidth;
                    int topLeft = x + (y + 1) * nodeWidth;
                    int topRight = (x + 1) + (y + 1) * nodeWidth;

                    //Always make it clockwise, since computers cut away the rest with culling

                    indices[counter++] = topLeft;
                    indices[counter++] = lowerLeft;
                    indices[counter++] = lowerRight;

                    indices[counter++] = topLeft;
                    indices[counter++] = lowerRight;
                    indices[counter++] = topRight;

                }
            }
        }
        private void CopyVertexToBuffers()
        {
            nodeVertexBuffer = new VertexBuffer(device, VertexTexturesNormal.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
            nodeVertexBuffer.SetData(vertices);
        }
        private void CopyIndicesToBuffers()
        {
            nodeIndexBuffer = new IndexBuffer(device, typeof(int), indices.Length, BufferUsage.WriteOnly);
            nodeIndexBuffer.SetData(indices);
        }
        private void CalculateNormals()
        {
            VertexTexturesNormal[] tempVertices = new VertexTexturesNormal[(nodeWidth + 1)  * (nodeHeight + 1)];
            Node.numberOfVertices += nodeWidth * nodeHeight;

            for (int u = 0; u < nodeWidth + 1; u++)
            {
                for (int v = 0; v < nodeHeight + 1; v++)
                {
                    if (u == 0 || v == 0 || u == nodeWidth || v == nodeHeight)
                    {
                        Vector3 tempVector = nodeCenter + ((axisX / (nodeWidth - 1)) * ((u - 1) - (nodeWidth - 1) / 2f)) + ((axisZ / (nodeHeight - 1)) * ((v - 1) - (nodeHeight - 1) / 2f));
                        //Sphere mapping
                        celestialParent.celestialNoise.SphereMapping(ref tempVector);
                        tempVertices[u + v * nodeWidth].Position = tempVector;


                        float height = celestialParent.celestialNoise.fractal.BlendFractal(tempVertices[u + v * nodeWidth].Position, celestialParent.blendFractal) / 20;
                        //float height = celestialParent.celestialNoise.fractal.fBm(tempVector, 0.5f, 2.0f, 10) / 100;

                        tempVertices[u + v * nodeWidth].Position *= celestialParent.Radius + height;

                        tempVertices[u + v * nodeWidth].TextureCoordinate.X = u / (nodeWidth * 30);
                        tempVertices[u + v * nodeWidth].TextureCoordinate.Y = v / (nodeHeight * 30);
                    }
                    else
                    {
                        tempVertices[u + v * nodeWidth] = vertices[u + v * nodeWidth];
                    }
                }
            }
            for (int i = 0; i < indices.Length / 3; i++)
            {
                int index1 = indices[i * 3];
                int index2 = indices[i * 3 + 1];
                int index3 = indices[i * 3 + 2];

                if ((i * 3) / nodeWidth < 1)
                {

                }
                Vector3 side1 = vertices[index1].Position - vertices[index3].Position;
                Vector3 side2 = vertices[index1].Position - vertices[index2].Position;
                Vector3 normal = Vector3.Cross(side1, side2);

                vertices[index1].Normal += normal;
                vertices[index2].Normal += normal;
                vertices[index3].Normal += normal;
            }

            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal.Normalize();
        }
        private BoundingBox CreateBoundingBox(VertexTexturesNormal[] vertexArray)
        {
            List<Vector3> pointList = new List<Vector3>();
            foreach (VertexTexturesNormal vertex in vertexArray)
                pointList.Add(vertex.Position);

            BoundingBox nodeBoundingBox = BoundingBox.CreateFromPoints(pointList);
            return nodeBoundingBox;
        }
        private void Input()
        {
            KeyboardState newKeyState = Keyboard.GetState();
        }
        public bool SplitTest()
        {
            /*
            Vector3[] corners = nodeBoundingBox.GetCorners();
            foreach (Vector3 corner in corners)
            {
                Vector3 distance = corner - camera.cameraPosition;
                float d = distance.Length();

                if (d < 8f / (1 << numberOfSplits) && isLeafNode && numberOfSplits < MaxDepth)
                {
                    return true;
                }
                else if (d > 12.5f / (1 << numberOfSplits) && !(isLeafNode))
                {
                    MergeNode();
                }
            }
            return false;
             * */

            Vector3 distance = testCenter - camera.cameraPosition;
            float d = distance.Length();

            if (d < 8f / (1 << numberOfSplits) && isLeafNode && numberOfSplits < MaxDepth)
            {
                return true;
            }
            else if (d > 12.5f / (1 << numberOfSplits) && !(isLeafNode))
            {
                MergeNode();
            }
            return false;

        }
        public bool DrawTest(BoundingFrustum cameraFrustum)
        {
            BoundingBox transformedBBox = XNAUtils.TransformBoundingBox(nodeBoundingBox, Matrix.Identity);
            ContainmentType cameraNodeContainment = cameraFrustum.Contains(transformedBBox);

            return cameraNodeContainment != ContainmentType.Disjoint;
        }
        public void Update()
        {
            if (SplitTest())
            {
                celestialParent.processingNodes.Add(this);
            }
            if (childNode != null)
            {
                foreach (CelestialNode node in childNode)
                {
                    if (node != null)
                    {
                        node.Update();
                    }

                }
            }

        }
        public void Draw(BoundingFrustum cameraFrustum)
        {

            if (DrawTest(cameraFrustum))
            {
                if (isLeafNode && nodeVertexBuffer != null)
                {
                    DrawCurrentNode();
                }
                else
                {
                    foreach (CelestialNode node in childNode)
                    {
                        node.Draw(cameraFrustum);
                    }
                }
            }
        }
        public void DrawCurrentNode()
        {
            basicEffect.World = Matrix.Identity;
            basicEffect.View = camera.viewMatrix;
            basicEffect.Projection = camera.projectionMatrix;


            basicEffect.LightingEnabled = true;

            basicEffect.AmbientLightColor = nodeColor / 8; //new Vector3(0.2f, 0.2f, 0.2f);

            basicEffect.Texture = sandTexture;
            basicEffect.TextureEnabled = true;


            basicEffect.EnableDefaultLighting();

            basicEffect.SpecularColor = (Vector3)Color.White.ToVector3() / 2;
            basicEffect.SpecularPower = 0.5f;

            basicEffect.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f); // nodeColor / 4;  //
            basicEffect.DirectionalLight0.Direction = new Vector3(2, 2, 2);
            basicEffect.DirectionalLight0.Enabled = true;

            basicEffect.DirectionalLight1.Enabled = false;
            basicEffect.DirectionalLight2.Enabled = false;



            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.Default;


            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                device.Indices = nodeIndexBuffer;
                device.SetVertexBuffer(nodeVertexBuffer);
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, nodeVertexBuffer.VertexCount, 0, nodeIndexBuffer.IndexCount / 3);

            }
            //XNAUtils.DrawBoundingBox(nodeBoundingBox, device, basicEffect, Matrix.Identity, camera.viewMatrix, camera.projectionMatrix);
        }

        #endregion

        
        
    }
    public class Node
    {
        public static Random random;
        public static int numberOfVertices;
        public static int maxSplit = 3;
        public static int highestSplit;
        public static PlanetNoise planetNoise;
        public static Quadtree Planet;


        Game game;
        Camera camera;
        GraphicsDevice device;
        VertexBuffer nodeVertexBuffer;
        IndexBuffer nodeIndexBuffer;

        Texture2D texture;
        int textureWidth = 64;
        int textureHeight = 64;

        VertexPositionNormalTexture[] vertices;
        int[] indices;

        int height;
        int width;
        public int numberOfSplits;

        public BoundingBox nodeBoundingBox;
        Vector3 nodeCenter;
        Vector3 testCenter;
        Vector3 axisX;
        Vector3 axisZ;

        Vector3 color;

        Node parent;
        Node[] childNodes;

        BasicEffect basicEffect;
        bool isVisible = true;
        bool isLeafNode = true;
        public bool isPlane = false;

        public Node(Game g, Camera cam, BasicEffect basicEffect, Vector3 center, Vector3 axisX, Vector3 axisZ, int numberOfSplits)
        {
            this.game = g;
            camera = cam;
            device = (GraphicsDevice)game.Services.GetService(typeof(GraphicsDevice));
            height = 17;
            width = 17;

            if (numberOfSplits >= Node.highestSplit)
                Node.highestSplit = numberOfSplits;

            isLeafNode = true;
            this.numberOfSplits = numberOfSplits;
            nodeCenter = center;

            float x = center.X;
            float y = center.Y;
            float z = center.Z;

            testCenter.X = (float)(x * Math.Sqrt(1 - (y * y / 2) - (z * z / 2) + (y * y * z * z / 3)));
            testCenter.Y = (float)(y * Math.Sqrt(1 - (z * z / 2) - (x * x / 2) + (x * x * z * z / 3)));
            testCenter.Z = (float)(z * Math.Sqrt(1 - (y * y / 2) - (x * x / 2) + (y * y * x * x / 3)));

            this.axisX = axisX;
            this.axisZ = axisZ;

            this.basicEffect = basicEffect;
            //int seed = (int)(nodeCenter.Length() * 17 + axisX.Length() * 1317 + axisZ.Length() * 54341);
            float col = (float)(random.NextDouble() + 1) / 2;
            color = new Vector3(col, col, 0.0f);
            //texture = game.Content.Load<Texture2D>("grass");

            BuildMesh(nodeCenter, axisX, axisZ);
            //BuildTexture();
            SetUpIndices();
            CalculateNormals();
            CopyToBuffers();

            nodeBoundingBox = CreateBoundingBox(vertices);
        }

        public Node(Game g, Camera cam, BasicEffect basicEffect, Vector3 center, Vector3 axisX, Vector3 axisZ, int numberOfSplits, string NodeType)
        {
            isPlane = true;
            this.game = g;
            camera = cam;
            device = (GraphicsDevice)game.Services.GetService(typeof(GraphicsDevice));
            height = 9;
            width = 9;

            if (numberOfSplits >= Node.highestSplit)
                Node.highestSplit = numberOfSplits;

            isLeafNode = true;
            this.numberOfSplits = numberOfSplits;
            nodeCenter = center;

            testCenter = center;
            //float x = center.X;
            //float y = center.Y;
            //float z = center.Z;

            //testCenter.X = (float)(x * Math.Sqrt(1 - (y * y / 2) - (z * z / 2) + (y * y * z * z / 3)));
            //testCenter.Y = (float)(y * Math.Sqrt(1 - (z * z / 2) - (x * x / 2) + (x * x * z * z / 3)));
            //testCenter.Z = (float)(z * Math.Sqrt(1 - (y * y / 2) - (x * x / 2) + (y * y * x * x / 3)));

            this.axisX = axisX;
            this.axisZ = axisZ;

            this.basicEffect = basicEffect;
            //int seed = (int)(nodeCenter.Length() * 17 + axisX.Length() * 1317 + axisZ.Length() * 54341);

            color = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
            //texture = game.Content.Load<Texture2D>("grass");

            BuildMeshNoise(nodeCenter, axisX, axisZ);
            //BuildTexture();
            SetUpIndices();
            //CalculateNormals();
            CopyToBuffers();

            nodeBoundingBox = CreateBoundingBox(vertices);
        }

        public void BuildTexture()
        {
            Color[] textureColors = new Color[textureWidth * textureHeight];
            float noise;
            float noiseR;
            float noiseG;
            float noiseB;

            int index = 0;
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    noise = planetNoise.fractal.Noise3(new Vector3(x + nodeCenter.X, y + nodeCenter.Y, nodeCenter.Z));
                    noise = (noise + 2) / 4;
                    noiseR = noise;
                    noiseG = noise;
                    noiseB = noise;

                    textureColors[index] = new Color(noiseR, noiseG, noiseB);
                    index++;
                }
            }
            texture = new Texture2D(device, textureWidth, textureHeight);
            texture.SetData(textureColors);
        }
        public void MergeNode()
        {
            isLeafNode = true;
            childNodes = null;
            Node.numberOfVertices -= 4 * width * height;
        }
        public void SplitNode()
        {


            isVisible = false;
            isLeafNode = false;
            childNodes = new Node[4];
            Vector3 childAxisX = axisX / 2;
            Vector3 childAxisZ = axisZ / 2;

            Vector3 topLeftCenter = nodeCenter - childAxisX / 2 + childAxisZ / 2;
            childNodes[0] = new Node(game, camera, basicEffect, topLeftCenter, childAxisX, childAxisZ, numberOfSplits + 1);

            Vector3 topRightCenter = nodeCenter + childAxisX / 2 + childAxisZ / 2;
            childNodes[1] = new Node(game, camera, basicEffect, topRightCenter, childAxisX, childAxisZ, numberOfSplits + 1);

            Vector3 bottomLeftCenter = nodeCenter - childAxisX / 2 - childAxisZ / 2;
            childNodes[2] = new Node(game, camera, basicEffect, bottomLeftCenter, childAxisX, childAxisZ, numberOfSplits + 1);

            Vector3 bottomRightCenter = nodeCenter + childAxisX / 2 - childAxisZ / 2;
            childNodes[3] = new Node(game, camera, basicEffect, bottomRightCenter, childAxisX, childAxisZ, numberOfSplits + 1);

        }
        public void SplitNodePlane()
        {


            isVisible = false;
            isLeafNode = false;
            childNodes = new Node[4];
            Vector3 childAxisX = axisX / 2;
            Vector3 childAxisZ = axisZ / 2;

            Vector3 topLeftCenter = nodeCenter - childAxisX / 2 + childAxisZ / 2;
            childNodes[0] = new Node(game, camera, basicEffect, topLeftCenter, childAxisX, childAxisZ, numberOfSplits + 1, "planeNode");

            Vector3 topRightCenter = nodeCenter + childAxisX / 2 + childAxisZ / 2;
            childNodes[1] = new Node(game, camera, basicEffect, topRightCenter, childAxisX, childAxisZ, numberOfSplits + 1, "planeNode");

            Vector3 bottomLeftCenter = nodeCenter - childAxisX / 2 - childAxisZ / 2;
            childNodes[2] = new Node(game, camera, basicEffect, bottomLeftCenter, childAxisX, childAxisZ, numberOfSplits + 1, "planeNode");

            Vector3 bottomRightCenter = nodeCenter + childAxisX / 2 - childAxisZ / 2;
            childNodes[3] = new Node(game, camera, basicEffect, bottomRightCenter, childAxisX, childAxisZ, numberOfSplits + 1, "planeNode");

        }
        private void BuildMesh(Vector3 centerPos, Vector3 axisX, Vector3 axisZ)
        {

            // temp array for demonstraion
            VertexPositionNormalTexture[] tempVertices = new VertexPositionNormalTexture[width * height];
            Node.numberOfVertices += width * height;
            float[,] heightMap = planetNoise.GeneratePerlin(8, width, height);
            int index = 0;

            for (int u = 0; u < width; u++)
            {
                for (int v = 0; v < height; v++)
                {
                    Vector3 tempVector = centerPos + ((axisX / (width - 1)) * (u - (width - 1) / 2f)) + ((axisZ / (height - 1)) * (v - (height - 1) / 2f));
                    tempVertices[u + v * width].Position = tempVector;

                    //Sphere mapping

                    float x = tempVertices[u + v * width].Position.X;
                    float y = tempVertices[u + v * width].Position.Y;
                    float z = tempVertices[u + v * width].Position.Z;

                    tempVertices[u + v * width].Position.X = (float)(x * Math.Sqrt(1 - (y * y / 2) - (z * z / 2) + (y * y * z * z / 3)));
                    tempVertices[u + v * width].Position.Y = (float)(y * Math.Sqrt(1 - (z * z / 2) - (x * x / 2) + (x * x * z * z / 3)));
                    tempVertices[u + v * width].Position.Z = (float)(z * Math.Sqrt(1 - (y * y / 2) - (x * x / 2) + (y * y * x * x / 3)));



                    //tempVertices[index].Position.Normalize();  (float)numberOfSplits)
                    //float noise = (planetNoise.fBm(tempVertices[index].Position, 0.9f, 2f, 13f) / 30f);                
                    //noise += planetNoise.MultifBm(tempVertices[index].Position, 0.3f, 2f, (float)numberOfSplits, 0.3f) / 1000;
                    //float noise = (planetNoise.RidgedMF(tempVertices[index].Position, 0.4f, 0.5f, 1.1f, 10f, 0.4f, 1f) / 2);
                    //float noise2 = (planetNoise.RidgedMF(tempVertices[index].Position - new Vector3(11.1f, 11.1f, 11.1f), 0.4f, 0.5f, 1.1f, 10f, 0.3f, 1f) / 5);
                    //noise += (planetNoise.fBm(tempVertices[index].Position, 0.9f, 2f, 13f) / 30f);     

                    //float noise = (planetNoise.Perturbing(tempVertices[u + v * width].Position, tempVertices[u + v * width].Position.X, 0.5f, 2.0f, 8.0f) / 40);
                    //noise -= (planetNoise.RidgedMF(tempVertices[index].Position, 0.7f, 0.5f, 1.1f, 10f, 0.3f, 1f) / 6);
                    //noise -= (planetNoise.fBm(tempVertices[u + v * width].Position, 0.7f, 2f, 10f) / 100);

                    //tempVertices[u + v * width].Position *= 1 + noise;

                    tempVertices[u + v * width].Normal = -tempVertices[index].Position;
                    tempVertices[u + v * width].Normal.Normalize();

                    tempVertices[u + v * width].TextureCoordinate.X = u / (width);
                    tempVertices[u + v * width].TextureCoordinate.Y = v / (height);
                    index++;
                }
            }
            vertices = tempVertices;
        }
        private void BuildMeshNoise(Vector3 centerPos, Vector3 axisX, Vector3 axisZ)
        {

            // temp array for demonstraion
            VertexPositionNormalTexture[] tempVertices = new VertexPositionNormalTexture[width * height];
            Node.numberOfVertices += width * height;

            int index = 0;
            //float[,] heightMap = new float[width , height];


            for (int u = 0; u < width; u++)
            {
                for (int v = 0; v < height; v++)
                {

                    Vector3 tempVector = centerPos + ((axisX / (width - 1)) * (u - (width - 1) / 2f)) + ((axisZ / (height - 1)) * (v - (height - 1) / 2f));
                    tempVertices[index].Position = tempVector;

                    //Sphere mapping

                    //float x = tempVertices[index].Position.X;
                    //float y = tempVertices[index].Position.Y;
                    //float z = tempVertices[index].Position.Z;

                    //float size = RandomGenerator(tempVertices[index].Position);


                    //tempVertices[index].Position.X = (float)(x * Math.Sqrt(1 - (y * y / 2) - (z * z / 2) + (y * y * z * z / 3)));
                    //tempVertices[index].Position.Y = (float)(y * Math.Sqrt(1 - (z * z / 2) - (x * x / 2) + (x * x * z * z / 3)));
                    //tempVertices[index].Position.Z = (float)(z * Math.Sqrt(1 - (y * y / 2) - (x * x / 2) + (y * y * x * x / 3)));

                    //heightMap[v, u] = planetNoise.Noise(tempVertices[index].Position);
                    //tempVertices[index].Position.Normalize();

                    // Highlands... hills
                    //float heightPosition = (planetNoise.RidgedMF(tempVertices[index].Position, 0.5f, 0.5f, 1f, 10f, 0.2f, 1f) / 4);


                    //heightPosition += (planetNoise.fBm(tempVertices[index].Position, 0.9f, 2f, 10f) / 80);
                    //heightPosition += (planetNoise.HeteroTerrain(tempVertices[index].Position, 0.8f, 2f, 18f, 0.7f) / 50);
                    //heightPosition += (planetNoise.HeteroTerrain(tempVertices[index].Position, 0.8f, 2f, 18f, 0.7f) / 150);
                    //heightPosition += (planetNoise.HeteroTerrain(tempVertices[index].Position / 2, 0.9f, 2f, 12f, 0.5f) / 150);
                    /*
                     * planetNoise.Noise(tempVertices[index].Position);
                    (planetNoise.fBm(tempVertices[index].Position, 0.8f, 2f, 20f) / 40);

                heightPosition += (planetNoise.HeteroTerrain(tempVertices[index].Position, 0.6f, 2f, 18f, 0.7f) / 70);
                heightPosition += (planetNoise.HeteroTerrain(tempVertices[index].Position / 2, 0.9f, 2f, 12f, 0.5f) / 100);
                   */

                    //tempVertices[index].Position.Y *= 1 + heightPosition;



                    tempVertices[index].Normal = -tempVertices[index].Position;
                    tempVertices[index].Normal.Normalize();

                    tempVertices[index].TextureCoordinate.X = u / (width);
                    tempVertices[index].TextureCoordinate.Y = v / (height);
                    index++;
                }
            }
            vertices = tempVertices;
        }
        private void SetUpIndices()
        {
            indices = new int[(width - 1) * (height - 1) * 6];
            int counter = 0;
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    if (x == 0)
                    {
                        if (y % 2 == 1)
                        {
                            int stichTopLeft = x + (y + 1) * width;
                            int stichTopRight = (x + 1) + (y + 2) * width;
                            int stichMiddleRight = (x + 1) + (y + 1) * width;
                            int stichLowerLeft = (x) + (y - 1) * width;
                            int stichLowerRight = (x + 1) + y * width;

                            indices[counter++] = stichTopLeft;
                            indices[counter++] = stichLowerLeft;
                            indices[counter++] = stichLowerRight;



                            if (y < height - 2)
                            {
                                indices[counter++] = stichTopLeft;
                                indices[counter++] = stichLowerRight;
                                indices[counter++] = stichMiddleRight;
                            }


                            if (y < height - 2)
                            {

                                indices[counter++] = stichTopRight;
                                indices[counter++] = stichTopLeft;
                                indices[counter++] = stichMiddleRight;

                            }
                        }
                    }
                    else if (y == 0)
                    {
                        if (x % 2 == 1)
                        {
                            int stichTopLeft = x + (y + 1) * width;
                            int stichTopRight = (x + 1) + (y + 1) * width;
                            int stichLowerLeft = (x - 1) + y * width;
                            int stichLowerRight = (x + 1) + y * width;
                            int stichTopRight2 = (x + 2) + (y + 1) * width; ;

                            indices[counter++] = stichTopLeft;
                            indices[counter++] = stichLowerLeft;
                            indices[counter++] = stichLowerRight;

                            if (x < width - 2)
                            {
                                indices[counter++] = stichTopLeft;
                                indices[counter++] = stichLowerRight;
                                indices[counter++] = stichTopRight;
                            }

                            if (x < width - 2)
                            {

                                indices[counter++] = stichTopRight;
                                indices[counter++] = stichLowerRight;
                                indices[counter++] = stichTopRight2;

                            }
                        }
                    }
                    else
                    {
                        //This is for making two triangels out of a four courners
                        int lowerLeft = x + y * width;
                        int lowerRight = (x + 1) + y * width;
                        int topLeft = x + (y + 1) * width;
                        int topRight = (x + 1) + (y + 1) * width;

                        //Always make it clockwise, since computers cut away the rest with culling

                        indices[counter++] = topLeft;
                        indices[counter++] = lowerLeft;
                        indices[counter++] = lowerRight;

                        indices[counter++] = topLeft;
                        indices[counter++] = lowerRight;
                        indices[counter++] = topRight;



                    }


                }
            }
        }
        private void CopyToBuffers()
        {
            nodeVertexBuffer = new VertexBuffer(device, VertexPositionNormalTexture.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
            nodeVertexBuffer.SetData(vertices);

            nodeIndexBuffer = new IndexBuffer(device, typeof(int), indices.Length, BufferUsage.WriteOnly);
            nodeIndexBuffer.SetData(indices);
        }
        private void CalculateNormals()
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal = new Vector3(0, 0, 0);

            for (int i = 0; i < indices.Length / 3; i++)
            {
                int index1 = indices[i * 3];
                int index2 = indices[i * 3 + 1];
                int index3 = indices[i * 3 + 2];

                Vector3 side1 = vertices[index1].Position - vertices[index3].Position;
                Vector3 side2 = vertices[index1].Position - vertices[index2].Position;
                Vector3 normal = Vector3.Cross(side1, side2);

                vertices[index1].Normal += normal;
                vertices[index2].Normal += normal;
                vertices[index3].Normal += normal;
            }

            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal.Normalize();
        }
        private BoundingBox CreateBoundingBox(VertexPositionNormalTexture[] vertexArray)
        {
            List<Vector3> pointList = new List<Vector3>();
            foreach (VertexPositionNormalTexture vertex in vertexArray)
                pointList.Add(vertex.Position);

            BoundingBox nodeBoundingBox = BoundingBox.CreateFromPoints(pointList);
            return nodeBoundingBox;
        }
        private void Input()
        {
            KeyboardState newKeyState = Keyboard.GetState();
        }
        public bool SplitTest()
        {
            Vector3[] corners = nodeBoundingBox.GetCorners();
            foreach (Vector3 corner in corners)
            {
                Vector3 distance = corner - camera.cameraPosition;
                float d = distance.Length();

                if (d < 5f / (1 << numberOfSplits) && isLeafNode && numberOfSplits < maxSplit)
                {
                    return true;
                }
                else if (d > 8.5f / (1 << numberOfSplits) && !(isLeafNode))
                {
                    MergeNode();
                }
            }
            return false;
        }
        public bool DrawTest(BoundingFrustum cameraFrustum)
        {
            BoundingBox transformedBBox = XNAUtils.TransformBoundingBox(nodeBoundingBox, Matrix.Identity);
            ContainmentType cameraNodeContainment = cameraFrustum.Contains(transformedBBox);

            return cameraNodeContainment != ContainmentType.Disjoint;
        }
        public void Update()
        {
            Input();
            Vector3 distance = testCenter - camera.cameraPosition;
            float d = distance.Length();


            if (SplitTest())
            {
                Planet.processingNodes.Add(this);
            }
            if (childNodes != null)
            {
                foreach (Node node in childNodes)
                {
                    if (node != null)
                    {
                        node.Update();
                    }

                }
            }

        }
        public void Draw(BoundingFrustum cameraFrustum)
        {

            if (DrawTest(cameraFrustum))
            {
                if (isLeafNode && nodeVertexBuffer != null)
                {
                    DrawCurrentNode();
                }
                else
                {
                    foreach (Node node in childNodes)
                    {
                        node.Draw(cameraFrustum);
                    }
                }
            }
        }
        public void DrawCurrentNode()
        {
            basicEffect.World = Matrix.Identity;
            basicEffect.View = camera.viewMatrix;
            basicEffect.Projection = camera.projectionMatrix;


            basicEffect.LightingEnabled = true;
            basicEffect.AmbientLightColor = color / 8; //new Vector3(0.2f, 0.2f, 0.2f);

            //basicEffect.Texture = texture;
            //basicEffect.TextureEnabled = true;


            //basicEffect.EnableDefaultLighting();

            basicEffect.SpecularColor = (Vector3)Color.Orange.ToVector3();
            basicEffect.SpecularPower = 0.5f;

            basicEffect.DiffuseColor = color / 4;  //new Vector3(0.5f, 0.5f, 0.5f);
            basicEffect.DirectionalLight0.Direction = new Vector3(2, 2, 2);
            basicEffect.DirectionalLight0.Enabled = true;

            basicEffect.DirectionalLight1.Enabled = false;
            basicEffect.DirectionalLight2.Enabled = false;



            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.Default;


            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                device.Indices = nodeIndexBuffer;
                device.SetVertexBuffer(nodeVertexBuffer);
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, nodeVertexBuffer.VertexCount, 0, nodeIndexBuffer.IndexCount / 3);

            }
            //XNAUtils.DrawBoundingBox(nodeBoundingBox, device, basicEffect, Matrix.Identity, camera.viewMatrix, camera.projectionMatrix);
        }
    }
}
