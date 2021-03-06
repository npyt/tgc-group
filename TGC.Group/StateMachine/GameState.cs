﻿using Microsoft.DirectX.DirectInput;
using System.Drawing;
using System;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;
using TGC.Group.Camara;
using TGC.Core.Terrain;
using TGC.Core.Collision;
using TGC.Core.Text;
using TGC.Core.Sound;
using TGC.Group.Stats;
using TGC.Group.Core2d;
using System.Collections.Generic;
using TGC.Group.VertexMovement;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TGC.Core.Camara;
using TGC.Group.Model;
using TGC.Core.Shaders;
using Font = System.Drawing.Font;
using Microsoft.DirectX.Direct3D;

namespace TGC.Group.StateMachine
{
    class GameState : State
    {
        // Nave principal
        private TgcMesh Ship { get; set; }
        TGCBox ShipCollision { get; set; }
        VertexMovementManager shipManager;
        private Microsoft.DirectX.Direct3D.Effect shipEffect;
        private Microsoft.DirectX.Direct3D.Effect planetEffect;
        private Microsoft.DirectX.Direct3D.Effect skyEffect;
        private Microsoft.DirectX.Direct3D.Effect trackEffect;
        private Microsoft.DirectX.Direct3D.Effect bloomEffect;

        bool gameRunning = false;
        TimeManager gameTime = new TimeManager();
        TimeManager totalTime = new TimeManager();

        // PowerBoxes
        private List<PowerBox> power_boxes;
        private List<Boolean> power_boxes_states;
        float powerBoxElapsed = 0f;

        // GodBox (box para la camara god)
        private TGCBox godBox { get; set; }

        // SuperPowerBox
        private TgcMesh superPowerBox { get; set; }
        private bool superPowerStatus;
        private float superPowerTime;
        private const float SUPERPOWER_MOVEMENT_SPEED =202f;
        VertexMovementManager powerManager;

        // Lista de tracks
        private List<TgcMesh> tracks;
        private List<TgcMesh> blooms;
        private List<Boolean> tracks_is_curve;

        // Lista de paths
        private List<TgcMesh> paths;

        int q_blocks, q_curves;

        // Mesh: tierra
        private TgcMesh mTierra { get; set; }
        // Mesh: sol
        private TgcMesh mSol { get; set; }
        // Mesh: marte
        private TgcMesh mMarte { get; set; }
        // Mesh: skydome
        private TGCSphere skyBox { get; set; }
        static int scale = 10000;
        TGCVector3 sphereScale = new TGCVector3(scale, scale, scale);

        // Camara principal (tercera persona)
        private TgcThirdPersonCamera camaraInterna;

        // Cámara god (tercera persona)
        private TgcThirdPersonCamera godCamera;

        // Musica
        private TgcMp3Player mp3Player;
        String mp3Path;

        // Movimiento y rotacion
        private const float MOVEMENT_SPEED = 105f;
        private const float VERTEX_MIN_DISTANCE = 0.3f;

        private TGCBox test_hit { get; set; }

        TGCVector3 currentRot;
        TGCVector3 originalRot;
        TGCVector3 directionXZ;
        TGCVector3 directionYZ;
        float prevAngleXZ = 0;
        float prevAngleYZ = 0;
        TGCVector3 shipMovement;

        // Textos 2D
        private TgcText2D tButtons;
        private TgcText2D multiplyPointsGUI;
        private TgcText2D totalPoints;
        private TgcText2D subPoints;

        // Boundingbox status
        private bool BoundingBox { get; set; }

        // Player creation
        Stat stat = new Stat("PLAYER");

        // GUI
        private bool helpGUI;
        private bool developerModeGUI;
        private float totalPointsGUItime;
        private float multiplyPointsGUItime;
        private bool penalty;
        private float penaltyTime;
        private CustomSprite blacksprite;

        private string levelFolder;
        
        // Bloom
        private Texture g_pRenderTarget;
        private Texture g_pRenderTarget4;
        private Texture g_pRenderTarget4Aux;
        private Surface g_pDepthStencil;
        private VertexBuffer g_pVBV3D;
        private bool activate_bloom = false;
        private bool distortion = true;

        public GameState(GameModel mparent, string folder) : base(mparent)
        {
            levelFolder = folder;
            init();
        }

        public override void init()
        {
            helpGUI = true;
            developerModeGUI = false;

            penalty = false;

            blacksprite = new CustomSprite();
            blacksprite.Bitmap = new CustomBitmap(parent.MediaDir + "\\GUI\\black.png", D3DDevice.Instance.Device);
            float sx = Screen.PrimaryScreen.Bounds.Size.Width / 1920;
            float sy = Screen.PrimaryScreen.Bounds.Size.Height / 1080;
            blacksprite.Position = new TGCVector2(0, 0);
            blacksprite.Scaling = new TGCVector2(sx, sy);



            tButtons = new TgcText2D();
            tButtons.Align = TgcText2D.TextAlign.RIGHT;
            tButtons.Color = Color.White;
            tButtons.Size = new Size(200, 240);
            tButtons.Position = new Point(Screen.PrimaryScreen.Bounds.Right - tButtons.Size.Width - 30, Screen.PrimaryScreen.Bounds.Bottom - tButtons.Size.Height);
            tButtons.changeFont(new Font("BigNoodleTitling", 16, FontStyle.Italic));

            totalPoints = new TgcText2D();
            totalPoints.Align = TgcText2D.TextAlign.LEFT;
            totalPoints.Size = new Size(200, 300);
            totalPoints.Position = new Point(40, Screen.PrimaryScreen.Bounds.Bottom - totalPoints.Size.Height - 20);
            totalPoints.Color = Color.White;
            totalPoints.changeFont(new Font("BigNoodleTitling", 180, FontStyle.Regular));

            multiplyPointsGUI = new TgcText2D();
            multiplyPointsGUI.Align = TgcText2D.TextAlign.LEFT;
            multiplyPointsGUI.Size = new Size(150, 150);
            multiplyPointsGUI.Position = new Point(50, Screen.PrimaryScreen.Bounds.Bottom - multiplyPointsGUI.Size.Height - 220);
            multiplyPointsGUI.Color = Color.White;
            multiplyPointsGUI.changeFont(new Font("BigNoodleTitling", 59, FontStyle.Italic));

            subPoints = new TgcText2D();
            subPoints.Text = "-10";
            subPoints.Align = TgcText2D.TextAlign.LEFT;
            subPoints.Size = new Size(150, 150);
            subPoints.Position = new Point(180, Screen.PrimaryScreen.Bounds.Bottom - multiplyPointsGUI.Size.Height - 220);
            subPoints.Color = Color.Red;
            subPoints.changeFont(new Font("BigNoodleTitling", 59, FontStyle.Italic));

            // Listas de tracks, paths y powerBoxes
            tracks = new List<TgcMesh>();
            blooms = new List<TgcMesh>();
            tracks_is_curve = new List<bool>();
            paths = new List<TgcMesh>();
            power_boxes = new List<PowerBox>();
            power_boxes_states = new List<Boolean>();

            // Device de DirectX para crear primitivas.
            var d3dDevice = D3DDevice.Instance.Device;

            // Textura de la carperta Media.
            var pathTexturaCaja = parent.MediaDir + Game.Default.TexturaCaja;

            // Textura para pruebas
            var texture = TgcTexture.createTexture(pathTexturaCaja);
            var size = new TGCVector3(5, 5, 10);

            test_hit = TGCBox.fromSize(new TGCVector3(1, 1, 1), texture);
            test_hit.Position = new TGCVector3(0, 0, 0);
            test_hit.Transform = TGCMatrix.Identity;

            // Caja god para la camara god
            var sizeGodBox = new TGCVector3(10, 2, 10);
            godBox = TGCBox.fromSize(sizeGodBox, texture);
            godBox.Position = new TGCVector3(0, 0, 0);
            godBox.Transform = TGCMatrix.Identity;

            // Loader para los mesh
            var loader = new TgcSceneLoader();

            // Nave
            Ship = loader.loadSceneFromFile(parent.MediaDir + "Test\\ship-TgcScene.xml").Meshes[0];
            Ship.Move(0, 5, 0);
            Ship.Scale = new TGCVector3(0.75f, 0.75f, -0.75f);
            originalRot = new TGCVector3(0, 0, 1);
            currentRot = originalRot;
            ShipCollision = TGCBox.fromSize(new TGCVector3(1, 10, 1));
            ShipCollision.Move(new TGCVector3(0, 10, 0));
            ShipCollision.Scale = Ship.Scale;

            shipEffect = TGCShaders.Instance.LoadEffect(parent.ShadersDir + "ShipShader.fx");
            Ship.Effect = shipEffect;
            Ship.Technique = "RenderScene";
            //shipEffect.SetValue("diffuseMap", Ship.DiffuseMaps[0]);


            trackEffect = TGCShaders.Instance.LoadEffect(parent.ShadersDir + "TrackShader.fx");


            planetEffect = TGCShaders.Instance.LoadEffect(parent.ShadersDir + "PlanetShader.fx");

            // Tierra
            mTierra = loader.loadSceneFromFile(parent.MediaDir + "Test\\pSphere1-TgcScene.xml").Meshes[0];
            mTierra.Move(0, 0, 0);

            skyEffect = TGCShaders.Instance.LoadEffect(parent.ShadersDir + "SkyShader.fx");

            // Sol
            mSol = loader.loadSceneFromFile(parent.MediaDir + "Test\\pSphere3-TgcScene.xml").Meshes[0];
            mSol.Move(0, 0, 0);

            mSol.Effect = planetEffect;
            mSol.Technique = "RenderScene";

            // Marte
            mMarte = loader.loadSceneFromFile(parent.MediaDir + "Test\\pSphere2-TgcScene.xml").Meshes[0];
            mMarte.Move(0, 0, 0);

            mMarte.Effect = planetEffect;
            mMarte.Technique = "RenderScene";


            // Camara a la ship
            camaraInterna = new TgcThirdPersonCamera(Ship.Position, shipMovement + Ship.Position, 15, -60);

            // Camara god
            godCamera = new TgcThirdPersonCamera(godBox.Position, 60, -135);

            // Seteo de camara principal
            parent.Camara = camaraInterna;

            // Asignar target inicial a la ship
            shipManager = new VertexMovementManager(Ship.Position, Ship.Position, MOVEMENT_SPEED);
            powerManager = null;

            loadLevel(loader, levelFolder);

            // SuperPower
            superPowerBox = loader.loadSceneFromFile(parent.MediaDir + "power_sphere.xml").Meshes[0];
            superPowerBox.Position = new TGCVector3(0, 0, 0);
            superPowerBox.Transform = TGCMatrix.Identity;
            superPowerBox.Effect = TGCShaders.Instance.LoadEffect(parent.ShadersDir + "PowerShader.fx"); ;
            superPowerBox.Technique = "RenderScene";
            superPowerBox.Scale = new TGCVector3(1.2f, 1.2f, 1.2f);
            superPowerStatus = false;
            superPowerTime = 0;

            //Skybox
            var textureSky = TgcTexture.createTexture(parent.MediaDir + "SkyBox\\universe2.png");

            skyBox = new TGCSphere();
            skyBox.Position = new TGCVector3(0, 0, 0);
            skyBox.Color = Color.White;
            skyBox.setTexture(textureSky);
            skyBox.LevelOfDetail = 2;
            skyBox.updateValues();
            skyBox.RotateY(FastMath.PI / 2);


            skyBox.Effect = skyEffect;
            skyBox.Technique = "RenderScene";

            directionXZ = TGCVector3.Empty;
            directionYZ = TGCVector3.Empty;



            // BLOOM

            string compilationErrors;
            bloomEffect = Microsoft.DirectX.Direct3D.Effect.FromFile(d3dDevice, parent.ShadersDir + "GaussianBlur.fx", null, null, ShaderFlags.PreferFlowControl,
                null, out compilationErrors);
            if (bloomEffect == null)
            {
                throw new System.Exception("Error al cargar shader. Errores: " + compilationErrors);
            }
            bloomEffect.Technique = "DefaultTechnique";


            g_pDepthStencil = d3dDevice.CreateDepthStencilSurface(d3dDevice.PresentationParameters.BackBufferWidth,
                d3dDevice.PresentationParameters.BackBufferHeight,
                DepthFormat.D24S8, MultiSampleType.None, 0, true);
            
            g_pRenderTarget = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget, Format.X8R8G8B8,
                Pool.Default);

            g_pRenderTarget4 = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth / 4
               , d3dDevice.PresentationParameters.BackBufferHeight / 4, 1, Usage.RenderTarget,
               Format.X8R8G8B8, Pool.Default);

            g_pRenderTarget4Aux = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth / 4
                , d3dDevice.PresentationParameters.BackBufferHeight / 4, 1, Usage.RenderTarget,
                Format.X8R8G8B8, Pool.Default);


            bloomEffect.SetValue("g_RenderTarget", g_pRenderTarget);

            bloomEffect.SetValue("screen_dx", d3dDevice.PresentationParameters.BackBufferWidth);
            bloomEffect.SetValue("screen_dy", d3dDevice.PresentationParameters.BackBufferHeight);

            CustomVertex.PositionTextured[] vertices =
            {
                new CustomVertex.PositionTextured(-1, 1, 1, 0, 0),
                new CustomVertex.PositionTextured(1, 1, 1, 1, 0),
                new CustomVertex.PositionTextured(-1, -1, 1, 0, 1),
                new CustomVertex.PositionTextured(1, -1, 1, 1, 1)
            };

            g_pVBV3D = new VertexBuffer(typeof(CustomVertex.PositionTextured),
                4, d3dDevice, Usage.Dynamic | Usage.WriteOnly,
                CustomVertex.PositionTextured.Format, Pool.Default);
            g_pVBV3D.SetData(vertices, 0, LockFlags.None);

            
        }

        string csv_out = "";

        

        public override void update(float ElapsedTime)
        {
            totalTime.update(ElapsedTime);

            /*powerBoxElaped += ElapsedTime;
            if(powerBoxElaped > 1.5)
            {
                foreach (PowerBox p in power_boxes)
                {
                    p.Position = getPositionAtMiliseconds(p.miliseconds_in_path);
                }

                powerBoxElaped = 0f;
            }*/

            gameTime.update(ElapsedTime);

            // Apretar M para activar musica
            if(mp3Player.getStatus() == TgcMp3Player.States.Stopped)
            {
                mp3Player.closeFile();
                parent.returnToMenu();
            }

            if (parent.Input.keyPressed(Key.S))
            {
                System.IO.File.WriteAllText(parent.MediaDir + "\\hits_output.csv", csv_out);
            }

            if (parent.Input.keyPressed(Key.U))
            {
                csv_out += ((int)(gameTime.sum_elapsed * 1000)) + ";";
            }

            float rotate = 0;

            // Para que no surja el artifact del skybox
            D3DDevice.Instance.Device.Transform.Projection = TGCMatrix.PerspectiveFovLH(D3DDevice.Instance.FieldOfView, D3DDevice.Instance.AspectRatio,
                    D3DDevice.Instance.ZNearPlaneDistance, D3DDevice.Instance.ZFarPlaneDistance * 2f).ToMatrix();

            // Deteccion entre poderes y ship al apretar ESPACIO
            if (parent.Input.keyPressed(Key.Space))
            {
                int noTouching = 0;
                for (int a = 0; a < power_boxes.Count; a++)
                {
                    TGCBox ShortPowerBox = power_boxes[a];
                    if (TgcCollisionUtils.testAABBAABB(ShipCollision.BoundingBox, power_boxes[a].BoundingBox))
                    {
                        //power_boxes[a].Color = Color.Yellow;
                        //power_boxes[a].updateValues();
                        power_boxes[a].Color = Color.Red;
                        power_boxes[a].updateValues();
                        if (!power_boxes_states[a])
                        {
                            power_boxes_states[a] = true;
                            stat.addMultiply();
                            stat.addPoints(10);
                            
                            totalPointsGUItime = gameTime.sum_elapsed;
                            multiplyPointsGUItime = gameTime.sum_elapsed;
                        }
                    }
                    else
                    {
                        noTouching++;
                        power_boxes[a].updateValues();
                    }
                }
                if (noTouching == power_boxes.Count)
                {

                    stat.cancelMultiply();
                    if (stat.totalPoints != 0)
                    {
                        subPoints.Text = "-10";
                        penalty = true;
                    }
                    else if (stat.totalPoints == 0)
                    {
                        penalty = true;
                        subPoints.Text = "X";
                    }

                    stat.addPoints(-10);

                    activate_bloom = true;
                    totalPointsGUItime = gameTime.sum_elapsed;
                    penaltyTime = gameTime.sum_elapsed;
                    multiplyPointsGUItime = gameTime.sum_elapsed;
                }
            }

            if(parent.Input.keyPressed(Key.B))
            {
                for (int a = 0; a < tracks.Count; a++)
                {
                    if (tracks_is_curve[a])
                    {
                        if(TgcCollisionUtils.testAABBAABB(ShipCollision.BoundingBox, tracks[a].BoundingBox))
                        {
                            curve_hitted();
                            tracks_is_curve[a] = false;
                        }
                    }
                }
            }

            if (penalty)
            {
                if (gameTime.sum_elapsed - penaltyTime > 0.5f)
                {
                    penalty = false;
                    activate_bloom = false;
                }
            }

            // Activar bounding box al presionar F
            if (parent.Input.keyPressed(Key.F))
            {
                BoundingBox = !BoundingBox;
            }

            if (parent.Input.keyPressed(Key.D))
            {
                distortion = !distortion;
            }

            // Cambiar cámara al presionar TAB
            if (parent.Input.keyPressed(Key.Tab))
            {
                if (parent.Camara.Equals(camaraInterna))
                {
                    parent.Camara = godCamera;
                }
                else
                {
                    parent.Camara = camaraInterna;
                };
            }

            if (parent.Input.keyPressed(Key.H))
            {
                helpGUI = !helpGUI;
            }

            if (parent.Input.keyPressed(Key.K))
            {
                developerModeGUI = !developerModeGUI;
            }

            if (parent.Input.keyPressed(Key.B))
            {
                activate_bloom = !activate_bloom;
            }

            // SuperPower al presionar SHIFT IZQUIERDO cada 2 segundos
            if (parent.Input.keyPressed(Key.LeftShift))
                {
                    if (gameTime.sum_elapsed - superPowerTime > 3.0f || superPowerTime == 0)
                    {
                        stat.duplicatePoints();
                        superPowerStatus = true;
                        superPowerBox.Position = Ship.Position;
                        superPowerTime = gameTime.sum_elapsed;

                        powerManager = new VertexMovementManager(superPowerBox.Position, superPowerBox.Position, SUPERPOWER_MOVEMENT_SPEED, shipManager.vertex_pool);
                        powerManager.init();
                    }
                }

                if (superPowerStatus)
                {
                    var superPowerMovement = powerManager.update(ElapsedTime, superPowerBox.Position);
                    superPowerBox.Move(superPowerMovement);


                    if (gameTime.sum_elapsed - superPowerTime > 2.7f)
                    {
                        superPowerStatus = false;
                    }
                }
                
                // Movimiento de la godCamera con W A S D ESPACIO C
                var movementGod = TGCVector3.Empty;
                float moveForward = 0;

                if (parent.Camara.Equals(godCamera))
                {
                    if (parent.Input.keyDown(Key.Up) || parent.Input.keyDown(Key.W))
                    {
                        moveForward = 1;
                    }
                    else if (parent.Input.keyDown(Key.Down) || parent.Input.keyDown(Key.S))
                    {
                        moveForward = -1;
                    }
                    else if (parent.Input.keyDown(Key.Space))
                    {
                        movementGod.Y = 1 * ElapsedTime * MOVEMENT_SPEED;
                    }
                    else if (parent.Input.keyDown(Key.C))
                    {
                        movementGod.Y = -1 * ElapsedTime * MOVEMENT_SPEED;
                    }
                    else if (parent.Input.keyDown(Key.D) || parent.Input.keyPressed(Key.Right))
                    {
                        rotate = 2 * ElapsedTime;

                    }
                    else if (parent.Input.keyDown(Key.A) || parent.Input.keyPressed(Key.Left))
                    {
                        rotate = -2 * ElapsedTime;
                    }
                }

                // Rotación de la GodCamera
                godBox.Rotation += new TGCVector3(0, rotate, 0);
                godCamera.rotateY(rotate);
                float moveF = moveForward * ElapsedTime * MOVEMENT_SPEED;
                movementGod.Z = (float)Math.Cos(godBox.Rotation.Y) * moveF;
                movementGod.X = (float)Math.Sin(godBox.Rotation.Y) * moveF;

                godBox.Move(movementGod);

                // Movimiento de la nave (Ship)
                shipMovement = shipManager.update(ElapsedTime, Ship.Position);
                Ship.Move(shipMovement);
                ShipCollision.Move(shipMovement);

                // Rotacion de la camara junto a la Ship por el camino
                float angleXZ = 0;
                float angleYZ = 0;

                TGCVector3 vectorMovementXZ = new TGCVector3(shipMovement.X, 0, shipMovement.Z);
                TGCVector3 vectorMovementYZ = new TGCVector3(0, shipMovement.Y, shipMovement.Z);

                if (vectorMovementXZ.Length() != 0)
                {
                    directionXZ = TGCVector3.Normalize(vectorMovementXZ);
                }
                if (vectorMovementYZ.Length() != 0)
                {
                    directionYZ = TGCVector3.Normalize(vectorMovementYZ);
                }

                angleXZ = -FastMath.Acos(TGCVector3.Dot(originalRot, directionXZ));
                angleYZ = -FastMath.Acos(TGCVector3.Dot(originalRot, directionYZ));
                if (directionXZ.X > 0)
                {
                    angleXZ *= -1;
                }
                if (directionYZ.Y < 0)
                {
                    angleYZ *= -1;
                }
                if (!float.IsNaN(angleXZ) && !float.IsNaN(angleYZ))
                {
                    if (angleXZ - prevAngleXZ < 0 && angleYZ - prevAngleYZ < 0)
                    {

                        float orRotY = Ship.Rotation.Y;
                        float angIntY = orRotY * (1.0f - 0.2f) + angleXZ * 0.2f;

                        float orRotX = Ship.Rotation.X;
                        float angIntX = orRotX * (1.0f - 0.2f) + angleYZ * 0.2f;

                        Ship.Rotation = new TGCVector3(angIntX, angIntY, 0);
                        ShipCollision.Rotation = Ship.Rotation;
                        currentRot = directionXZ + directionYZ;

                        float originalRotationY = camaraInterna.RotationY;
                        float anguloIntermedio = originalRotationY * (1.0f - 0.07f) + angleXZ * 0.07f;
                        camaraInterna.RotationY = anguloIntermedio;
                    }
                    prevAngleXZ = angleXZ;
                    prevAngleYZ = angleYZ;
                }

                // Texto informativo de botones
                tButtons.Text = "CÁMARA: TAB \nMUSICA: M \nNOTAS: ESPACIO \nSUPERPODER: LEFT SHIFT \nDEVMOD: K\nPOSTPROCESADO: B \nOcultar ayuda con H";

                // Texto informativo del player
                totalPoints.Text = stat.totalPoints.ToString();
                multiplyPointsGUI.Text = "x" + stat.totalMultiply;

                // Transformaciones
                Ship.Transform = TGCMatrix.Scaling(Ship.Scale) * TGCMatrix.RotationYawPitchRoll(Ship.Rotation.Y, Ship.Rotation.X, Ship.Rotation.Z) * TGCMatrix.Translation(Ship.Position);
                ShipCollision.Transform = TGCMatrix.Scaling(Ship.Scale) * TGCMatrix.RotationYawPitchRoll(Ship.Rotation.Y, Ship.Rotation.X, Ship.Rotation.Z) * TGCMatrix.Translation(Ship.Position);
                godBox.Transform = TGCMatrix.Scaling(godBox.Scale) * TGCMatrix.RotationYawPitchRoll(godBox.Rotation.Y, godBox.Rotation.X, godBox.Rotation.Z) * TGCMatrix.Translation(godBox.Position);
                superPowerBox.Transform = TGCMatrix.Scaling(superPowerBox.Scale) * TGCMatrix.RotationYawPitchRoll(superPowerBox.Rotation.Y, superPowerBox.Rotation.X, superPowerBox.Rotation.Z) * TGCMatrix.Translation(superPowerBox.Position);
                skyBox.Transform = TGCMatrix.Scaling(sphereScale) * TGCMatrix.RotationYawPitchRoll(skyBox.Rotation.Y, skyBox.Rotation.X, skyBox.Rotation.Z) * TGCMatrix.Translation(skyBox.Position);
            
                for (int a = 0; a < power_boxes.Count; a++)
                {
                    TGCBox ShortPowerBox = power_boxes[a];
                    ShortPowerBox.Transform = TGCMatrix.Scaling(ShortPowerBox.Scale) * TGCMatrix.RotationYawPitchRoll(ShortPowerBox.Rotation.Y, ShortPowerBox.Rotation.X, ShortPowerBox.Rotation.Z) * TGCMatrix.Translation(ShortPowerBox.Position);
                }
                test_hit.Transform = TGCMatrix.Scaling(test_hit.Scale) * TGCMatrix.RotationYawPitchRoll(test_hit.Rotation.Y, test_hit.Rotation.X, test_hit.Rotation.Z) * TGCMatrix.Translation(test_hit.Position);

                // Posición de cámaras
                camaraInterna.TargetDisplacement = Ship.Position + shipMovement;
                godCamera.Target = godBox.Position;

        }
        public override void render(float ElapsedTime)
        {
            
            var device = D3DDevice.Instance.Device;
            
            bloomEffect.Technique = "DefaultTechnique";
            var pOldRT = device.GetRenderTarget(0);
            var pSurf = g_pRenderTarget.GetSurfaceLevel(0);
            if (distortion || activate_bloom)
                device.SetRenderTarget(0, pSurf);
            var pOldDS = device.DepthStencilSurface;
            if (distortion || activate_bloom)
                device.DepthStencilSurface = g_pDepthStencil;

            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            device.BeginScene();


            

            //Iniciar dibujado de todos los Sprites de la escena (en este caso es solo uno)
            parent.drawer2D.BeginDrawSprite();

            //Finalizar el dibujado de Sprites
            parent.drawer2D.EndDrawSprite();
            
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;      // Horizontal Alignment
            stringFormat.LineAlignment = StringAlignment.Center;  // Vertical Alignment

            // Renders
            skyBox.Render();

            shipEffect.SetValue("camaraX", 0);
            shipEffect.SetValue("camaraY", 0);
            shipEffect.SetValue("camaraZ", 1);
            Ship.Render();
            for (int a = 0; a < tracks.Count; a++)
            {
                tracks[a].Render();
                // blooms[a].Render(); OJO, PONGO ESTO PARA PODER HACER EL EFECTO
            }

            asteroids[0].Effect.SetValue("time", gameTime.sum_elapsed);
            for (int a = 0; a < asteroids.Count; a++)
            {
                asteroids[a].Render();
            }

            mTierra.Render();
            mSol.Render();
            mMarte.Render();
            
            for (int a = 0; a < power_boxes.Count; a++)
            {
                TGCBox ShortPowerBox = power_boxes[a];
                ShortPowerBox.Render();
            }
            if (BoundingBox)
            {
                Ship.BoundingBox.Render();

                for (int a = 0; a < power_boxes.Count; a++)
                {
                    TGCBox ShortPowerBox = power_boxes[a];
                    ShortPowerBox.BoundingBox.Render();
                }
                godBox.BoundingBox.Render();
            }
            superPowerBox.Effect.SetValue("time", gameTime.sum_elapsed);
            if (superPowerStatus)
            {
                superPowerBox.Render();
            }

            device.EndScene();
            
            pSurf.Dispose();
            if (distortion)
            {
                device.DepthStencilSurface = pOldDS;
                device.SetRenderTarget(0, pOldRT);
                device.BeginScene();

                bloomEffect.Technique = "Pincusion";
                device.VertexFormat = CustomVertex.PositionTextured.Format;
                device.SetStreamSource(0, g_pVBV3D, 0);
                bloomEffect.SetValue("g_RenderTarget", g_pRenderTarget);

                device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
                bloomEffect.Begin(FX.None);
                bloomEffect.BeginPass(0);
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                bloomEffect.EndPass();
                bloomEffect.End();
                device.EndScene();
            }
            if (activate_bloom)
            {
                device.DepthStencilSurface = pOldDS;
                device.SetRenderTarget(0, pOldRT);

                device.BeginScene();
                bloomEffect.Technique = "GaussianBlur";
                device.VertexFormat = CustomVertex.PositionTextured.Format;
                device.SetStreamSource(0, g_pVBV3D, 0);
                bloomEffect.SetValue("g_RenderTarget", g_pRenderTarget);

                device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
                bloomEffect.Begin(FX.None);
                bloomEffect.BeginPass(0);
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                bloomEffect.EndPass();
                bloomEffect.End();
                device.EndScene();
            }

            parent.drawer2D.BeginDrawSprite();
            parent.drawer2D.DrawSprite(blacksprite);
            parent.drawer2D.EndDrawSprite();

            totalPoints.render();
            multiplyPointsGUI.render();
            

            // Especificaciones en pantalla: posición de la nave y de la cámara
            if (developerModeGUI)
            {
                parent.DrawText.drawText("Ship Position: \n" + Ship.Position, 5, 20, Color.Yellow);
                parent.DrawText.drawText("Medium Elapsed: \n" + gameTime.medium_elapsed, 145, 20, Color.Yellow);
                parent.DrawText.drawText("Camera Position: \n" + parent.Camara.Position, 5, 100, Color.Yellow);
                parent.DrawText.drawText("Elapsed: \n" + ElapsedTime, 145, 60, Color.Yellow);
                parent.DrawText.drawText("PUNTAJE ACTUAL: " + stat.totalPoints +
                "\nMULTIPLICADOR ACTUAL: " + stat.totalMultiply +
                "\nMULTIPLICADOR PARCIAL: " + stat.partialMultiply +
                "\nSUPERPODER: " + ((!superPowerStatus) ? "TRUÉ" : "FALSE" + ElapsedTime), 5, 180, Color.Yellow);
                parent.DrawText.drawText("SumElapsed: \n" + gameTime.sum_elapsed, 145, 100, Color.White);
            }
            if (helpGUI)
            {
                tButtons.render();
            }
            if (penalty)
            {
                subPoints.render();
            }

            

            device.Present();

        }

        private void curve_hitted()
        {
            //TODO SUMAR PUNTOS AL PRESIONAR B EN CURVA
        }

        public override void dispose()
        {
            /*Ship.Dispose();
            mp3Player.closeFile();
            for (int a = 0; a < power_boxes.Count; a++)
            {
                power_boxes[a].Dispose();
            }*/

            g_pRenderTarget.Dispose();
            g_pVBV3D.Dispose();
            g_pDepthStencil.Dispose();
        }

        public TGCVector3 getPositionAtMiliseconds(int miliseconds)
        {
            /*if (gameTime.medium_elapsed == 0)
            {
                return TGCVector3.Empty;
            }*/

            List<TGCVector3> mypool = null;
            float local_elapsed = 0;
            TGCVector3 simulated_ship_position = TGCVector3.Empty;

            mypool = new List<TGCVector3>(shipManager.permanent_pool);

            VertexMovementManager mymanager = new VertexMovementManager(simulated_ship_position, simulated_ship_position, MOVEMENT_SPEED, mypool);
            mymanager.init();

            float ElapsedInAccount = 0.0018f;
            //float ElapsedInAccount = gameTime.medium_elapsed;

            while (local_elapsed < miliseconds / 1000)
            {
                simulated_ship_position.Add(mymanager.update(ElapsedInAccount, simulated_ship_position));
                local_elapsed += ElapsedInAccount;
            }

            simulated_ship_position.Y += 3;
            return simulated_ship_position;
        }

        private void concatTrack(TgcMesh track, TgcMesh path, TgcMesh bloom, Boolean isCurve)
        {
            TGCVector3 lastVertex = TGCVector3.Empty;

            if (tracks.Count != 0)
            {
                lastVertex = shipManager.vertex_pool[0];
                for (int a = 0; a < shipManager.vertex_pool.Count; a++)
                {
                    TGCVector3 thisvertex = shipManager.vertex_pool[a];
                    if (thisvertex.Z >= lastVertex.Z)
                    {
                        lastVertex = thisvertex;
                    }
                }
            }

            track.Effect = trackEffect;
            track.Technique = "RenderScene";

            track.Move(lastVertex);
            tracks.Add(track);

            bloom.Move(lastVertex);
            blooms.Add(bloom);

            tracks_is_curve.Add(isCurve);
            

            path.Move(lastVertex);
            paths.Add(path);

            shipManager.addVertexCollection(path.getVertexPositions(), lastVertex);
        }

        private void addPowerBox(int miliseconds)
        {
            var pathTexturaCaja = parent.MediaDir + "Test\\Textures\\white_wall.jpg";
            var texture = TgcTexture.createTexture(pathTexturaCaja);

            TGCVector3 position = getPositionAtMiliseconds(miliseconds);

            var sizePowerBox = new TGCVector3(8, 1, 10);
            PowerBox powerbox = new PowerBox(miliseconds);
            powerbox.setPositionSize(TGCVector3.Empty, sizePowerBox);
            powerbox.Color = Color.Pink;
            powerbox.Position = position;
            powerbox.Transform = TGCMatrix.Identity;
            powerbox.updateValues();

            power_boxes.Add(powerbox);
            power_boxes_states.Add(false);
        }

        List<TgcMesh> asteroids;

        public void loadLevel(TgcSceneLoader loader, string name)
        {
            string csv_config = File.ReadAllText(parent.MediaDir + "Tracks\\config.csv", Encoding.UTF8);
            string separator_config = ";";
            string[] values_config = Regex.Split(csv_config, separator_config);
            q_blocks = int.Parse(values_config[0]);
            q_curves = int.Parse(values_config[1]);

            Random r = new Random();
            var loader2 = new TgcSceneLoader();

            // Init de tracks
            int q_tracks = 20;
            for (int i = 0; i < q_tracks; i++)
            {
                Boolean isCurve = r.Next() % 100 < 25;
                int number = (isCurve) ? r.Next(q_curves) + 1 : r.Next(q_blocks) + 1;
                string file_name = number + "_";
                string folder = (isCurve) ? "Tracks\\Curves\\" : "Tracks\\Blocks\\";

                concatTrack(loader.loadSceneFromFile(parent.MediaDir + folder + file_name + "track.xml").Meshes[0],
                            loader.loadSceneFromFile(parent.MediaDir + folder + file_name + "path.xml").Meshes[0],
                            loader.loadSceneFromFile(parent.MediaDir + folder + file_name + "bloom.xml").Meshes[0],
                            isCurve);
            }

            // Asignar proximo target de la nave
            shipManager.init();
            System.Diagnostics.Debug.WriteLine("Ship Manager Ready");

            // Init de poderes
            string csv_hits = File.ReadAllText(parent.MediaDir + "Levels\\" + name + "\\hits.csv", Encoding.UTF8);
            if (csv_hits != "")
            {
                string separator_hits = ";";
                string[] values_hits = Regex.Split(csv_hits, separator_hits);
                for (int i = 0; i < values_hits.Length; i++)
                {
                    values_hits[i] = values_hits[i].Trim('\"');
                    addPowerBox(int.Parse(values_hits[i]));
                    System.Diagnostics.Debug.WriteLine(i + " / " + values_hits.Length);
                }
            }
            System.Diagnostics.Debug.WriteLine("Hits Ready");


            loader2 = new TgcSceneLoader();
            asteroids = new List<TgcMesh>();
            int mdisplacement = 17;

            Microsoft.DirectX.Direct3D.Effect asteroidEffect = TGCShaders.Instance.LoadEffect(parent.ShadersDir + "AsteroidShader.fx");

            for (int i=0; i< q_tracks; i++)
            {
                TgcMesh asteroid = loader.loadSceneFromFile(parent.MediaDir + "Asteroid\\asteroid" + r.Next(2) + ".xml").Meshes[0];
                float scale = ((float)r.NextDouble()) * 2.6f + 0.75f;
                asteroid.Scale = new TGCVector3(scale, scale, scale);
                TGCVector3 uv = shipManager.permanent_pool[r.Next(shipManager.permanent_pool.Count)];
                switch(r.Next(5))
                {
                    case 0:
                        uv.Add(new TGCVector3(r.Next(mdisplacement) + 30.6f * r.Next(6), r.Next(mdisplacement) + 36.4f * r.Next(6), 0));
                        break;
                    case 1:
                        uv.Add(new TGCVector3(r.Next(mdisplacement) + 34.8f * r.Next(6), -(r.Next(mdisplacement) + 36.4f * r.Next(6)), 0));
                        break;
                    case 2:
                        uv.Add(new TGCVector3(-(r.Next(mdisplacement) + 36.8f * r.Next(6)), r.Next(mdisplacement) + 37.3f * r.Next(6), 0));
                        break;
                    case 3:
                        uv.Add(new TGCVector3(-(r.Next(mdisplacement) + 39.6f * r.Next(6)), -(r.Next(mdisplacement) + 37.4f * r.Next(6)), 0));
                        break;
                }
                asteroid.Position = uv;
                asteroids.Add(asteroid);

                asteroid.Effect = asteroidEffect;
                asteroid.Technique = "RenderScene";
            }
            // Nave

            // Musica
            mp3Path = parent.MediaDir + "Levels\\" + name + "\\music.wav";
            mp3Player = new TgcMp3Player();
            mp3Player.FileName = mp3Path;
            mp3Player.play(false);

            System.Diagnostics.Debug.WriteLine("Music Ready");
            System.Diagnostics.Debug.WriteLine("LEVEL READY");
        }

    }
}
