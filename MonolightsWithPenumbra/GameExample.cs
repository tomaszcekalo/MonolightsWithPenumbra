﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monolights;
using Penumbra;
using System;

namespace LightingEngine
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class GameExample : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SpriteFont _debugfont;

        private Random _rand = new Random();

        public float NextFloat()
        {
            return (float)_rand.NextDouble();
        }

        private Monolights.Monolights _monoLights;
        private Effect _lightEffect;
        private Effect _deferrectLightEffect;
        private Texture2D _diffuse;
        private Texture2D _normal;
        private RenderTarget2D _frameBuffer;
        private Monolights.PointLight _pointlight;
        private SpotLight _spotlight;

        private MouseState _oldMouseState;
        private KeyboardState _oldKeyboardState;

        private Monolights.PointLight _floatinglight;
        private float _floatingllightangle;

        private float _zValue = 30f;
        private float _spotRotation = 0;
        private float _power = 0.5f;
        private int _lightDecay = 100;
        private float _spotBeamwidth = 9;

        //Some values to calculate the framerate.
        private double _elapsed = 1;

        private int _drawCount = 0;
        private int _drawRate = 0;

        private bool _drawDebugTargets = false;

        // Store reference to lighting system.
        private PenumbraComponent penumbra;

        // Create sample light source and shadow hull.
        private Penumbra.Light light = new Penumbra.PointLight
        {
            Scale = new Vector2(1000f), // Range of the light source (how far the light will travel)
            ShadowType = ShadowType.Solid // Will not lit hulls themselves
        };

        private Hull hull = new Hull(new Vector2(1.0f), new Vector2(-1.0f, 1.0f), new Vector2(-1.0f), new Vector2(1.0f, -1.0f))
        {
            Position = new Vector2(320, 240f),
            Scale = new Vector2(50f),
            Origin = new Vector2(0.5f)
        };

        private Point _screenSize = new Point(1024, 1024);
        private Rectangle _monolightsRectangle = new Rectangle(0, 0, 1024, 1024);

        public GameExample()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            graphics.PreferredBackBufferWidth = _screenSize.X;
            graphics.PreferredBackBufferHeight = _screenSize.Y;
            Content.RootDirectory = "Content";

            //graphics.SynchronizeWithVerticalRetrace = false;
            //IsFixedTimeStep = false;
            // Create the lighting system and add sample light and hull.
            penumbra = new PenumbraComponent(this);
            penumbra.Lights.Add(light);
            penumbra.Hulls.Add(hull);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();

            _oldMouseState = Mouse.GetState();
            _oldKeyboardState = Keyboard.GetState();

            // Initialize the lighting system.
            penumbra.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            _debugfont = Content.Load<SpriteFont>("debugfont");

            // These effects are used by the MonoLights class.
            // I need to figure out how to include them in the class so it is no longer needed to load here.
            // not sure if that is even possible.
            _lightEffect = Content.Load<Effect>("LightEffect");
            _deferrectLightEffect = Content.Load<Effect>("DeferredLightEffect");

            // Initialize MonoLights.
            _monoLights = new Monolights.Monolights(GraphicsDevice, _lightEffect, _deferrectLightEffect);

            // Load the regular content in the game.
            _diffuse = Content.Load<Texture2D>("brickwall"); //The diffuse map.
            _normal = Content.Load<Texture2D>("brickwall_normal"); //The normal map.
            _frameBuffer = new RenderTarget2D(this.GraphicsDevice, _monolightsRectangle.Width, _monolightsRectangle.Height);
            //_frameBuffer2 = new RenderTarget2D(this.GraphicsDevice, _monolightsRectangle.Width, _monolightsRectangle.Height);
            _monoLights.InvertYNormal = false; //this normalmap has the Y normal in the usual direction.

            //Create a few lights:
            _spotlight = new SpotLight()
            {
                IsEnabled = false,
                Color = Color.White,
                Power = 0.5f,
                LightDecay = 100,
                Position = new Vector3(100, 100, 20),
                SpotBeamWidthExponent = 9,
            };
            _spotlight.DirectionZ = -0.25f; //point it slightly more steeper onto the surface.
            _spotRotation = MathHelper.PiOver2; //point the spot downwards.

            _pointlight = new Monolights.PointLight()
            {
                Color = Color.White,
                Power = 0.5f,
                LightDecay = 300,
                Position = new Vector3(20, 20, 20),
                IsEnabled = true
            };

            //This is a light, gently floating over the background.
            _floatinglight = new Monolights.PointLight()
            {
                Color = Color.Orange,
                Power = 0.5f,
                LightDecay = 100,
                Position = new Vector3(20, 20, 20),
                IsEnabled = true
            };

            //Add the lights to the scene.
            _monoLights.AddLight(_spotlight);
            _monoLights.AddLight(_pointlight);
            //_monoLights.AddLight(_floatinglight);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            MouseState m = Mouse.GetState();
            KeyboardState k = Keyboard.GetState();

            _floatingllightangle += 0.5f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            _floatinglight.Position = new Vector3(320, 240, 0) + new Vector3((float)Math.Cos(_floatingllightangle) * 200, (float)Math.Sin(2 * _floatingllightangle) * 125, 20);

            if (m.RightButton == ButtonState.Pressed && _oldMouseState.RightButton == ButtonState.Released)
            {
                _spotlight.IsEnabled = !_spotlight.IsEnabled;
                _pointlight.IsEnabled = !_pointlight.IsEnabled;
            }

            if (m.LeftButton == ButtonState.Pressed && _oldMouseState.LeftButton == ButtonState.Released)
            {
                if (new Rectangle(0, 0, 640, 480).Contains(m.Position))
                {
                    _monoLights.AddLight(new Monolights.PointLight()
                    {
                        Color = new Color(NextFloat(), NextFloat(), NextFloat(), 1.0f),
                        Power = 0.5f + (NextFloat() * 0.5f),
                        LightDecay = 100 + (100 * NextFloat()),
                        Position = new Vector3(m.Position.X, m.Position.Y, 20),
                        IsEnabled = true
                    }
                    );
                }
            }

            if (k.IsKeyDown(Keys.Z))
                _zValue -= 1f;
            if (k.IsKeyDown(Keys.A))
                _zValue += 1f;
            if (_zValue < 1)
                _zValue = 1f;

            if (k.IsKeyDown(Keys.Left))
                _spotRotation -= 0.1f;
            if (k.IsKeyDown(Keys.Right))
                _spotRotation += 0.1f;
            _spotRotation = _spotRotation % MathHelper.TwoPi;

            if (k.IsKeyDown(Keys.Down))
                _power -= 0.01f;
            if (k.IsKeyDown(Keys.Up))
                _power += 0.01f;
            if (_power < 0.01)
                _power = 0.001f;
            if (_power > 1.5)
                _power = 1.5f;

            if (k.IsKeyDown(Keys.X))
                _lightDecay -= 1;
            if (k.IsKeyDown(Keys.S))
                _lightDecay += 1;
            if (_lightDecay < 1)
                _lightDecay = 1;

            if (k.IsKeyDown(Keys.D))
                _spotBeamwidth -= 0.1f;
            if (k.IsKeyDown(Keys.C))
                _spotBeamwidth += 0.1f;
            if (_spotBeamwidth < 0.1f)
                _spotBeamwidth = 0.1f;

            if (k.IsKeyDown(Keys.F1) && _oldKeyboardState.IsKeyUp(Keys.F1))
            {
                _drawDebugTargets = !_drawDebugTargets;
            }

            _pointlight.Position = new Vector3(m.Position.X, m.Position.Y, _zValue);

            //light.Position = new Vector2(_pointlight.Position.X * 2, _pointlight.Position.Y * 2);//ratio between _screenSize and monolightsRectangle
            light.Position = new Vector2(_pointlight.Position.X, _pointlight.Position.Y);//ratio between _screenSize and monolightsRectangle

            _spotlight.Position = new Vector3(m.Position.X, m.Position.Y, _zValue);
            _spotlight.SpotRotation = _spotRotation;
            _spotlight.Power = _power;
            _pointlight.Power = _power;
            _pointlight.LightDecay = _lightDecay;
            _spotlight.LightDecay = _lightDecay;
            _spotlight.SpotBeamWidthExponent = _spotBeamwidth;
            _oldMouseState = m;
            _oldKeyboardState = k;

            _elapsed -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_elapsed < 0)
            {
                _elapsed += 1d;
                _drawRate = _drawCount;
                _drawCount = 0;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            //First we render the game, as one would normally.
            //the rendertarget is the one in the Monolights class, it is used to process the light effect later.
            GraphicsDevice.SetRenderTarget(_monoLights.Colormap);
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();
            spriteBatch.Draw(_diffuse, _diffuse.Bounds, Color.White);
            spriteBatch.End();

            //Next we draw the game again, except the graphics use the normalmap data.
            GraphicsDevice.SetRenderTarget(_monoLights.Normalmap);
            spriteBatch.Begin();
            spriteBatch.Draw(_normal, _normal.Bounds, Color.White);
            spriteBatch.End();

            //Finally draw the combined scene.
            //the rendertarget is now 'null' to draw to the backbuffer. You can also draw to a rendertarget of your own if you want to postprocess it.
            _monoLights.Draw(_frameBuffer, spriteBatch, _monolightsRectangle);

            //Debug: show the rendertargets in the Monolights class.
            if (_drawDebugTargets)
                _monoLights.DrawDebugRenderTargets(spriteBatch);

            //used to calculate the framerate.
            ++_drawCount;

            base.Draw(gameTime);

            //The last drawcall is any HUD stuff, things that are not affected by the lights.
            spriteBatch.Begin();
            DrawDebugtext(spriteBatch);
            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            penumbra.BeginDraw();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp/*, transformMatrix: Matrix.CreateScale(6)*/);
            spriteBatch.Draw(_frameBuffer, GraphicsDevice.Viewport.Bounds, Color.White);
            spriteBatch.End();//*/
            penumbra.Draw(gameTime);
        }

        /// <summary>
        /// Draws some properties as additional information.
        /// </summary>
        /// <param name="spritebatch">The spritebatch to use.</param>
        private void DrawDebugtext(SpriteBatch spritebatch)
        {
            spritebatch.DrawString(_debugfont, string.Format("Position: {0}", _spotlight.Position), new Vector2(5, 5), Color.White);
            spritebatch.DrawString(_debugfont, string.Format("Power: {0:0.00}", _power), new Vector2(5, 15), Color.White);
            spritebatch.DrawString(_debugfont, string.Format("Decay: {0}", _lightDecay), new Vector2(5, 25), Color.White);
            spritebatch.DrawString(_debugfont, string.Format("Specular: {0}", _monoLights.SpecularStrength), new Vector2(5, 35), Color.White);
            if (_spotlight.IsEnabled)
            {
                spritebatch.DrawString(_debugfont, string.Format("SpotBeam: {0:0.00}", _spotlight.SpotBeamWidthExponent), new Vector2(5, 45), Color.White);
                spritebatch.DrawString(_debugfont, string.Format("Rotation: {0:0.00}", _spotlight.SpotRotation), new Vector2(5, 55), Color.White);
            }

            spritebatch.DrawString(_debugfont, string.Format("FPS: {0}", _drawRate), new Vector2(500, 5), Color.White);
            spritebatch.DrawString(_debugfont, string.Format("Lights: {0}", _monoLights.LightsCount), new Vector2(500, 15), Color.White);
        }
    }
}