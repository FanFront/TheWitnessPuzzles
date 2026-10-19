using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TheWitnessPuzzles;
using System.Linq;
using BloomFX;
using Microsoft.Xna.Framework.Content;

namespace TWP_Shared
{
    public class PanelGameScreen : GameScreen
    {
        bool drawDebug = false;
        
        // If this flag is true, then no 'Next Panel' button will be spawned
        readonly bool IsStandalonePanel = false;
        readonly int TutorialIndex = -1;
        readonly Action<int> onTutorialAdvance;
        bool continuePending;
        TextButton successContinueButton;
        SpriteFont successFont;
        Rectangle successCardBounds;
        Rectangle successActionBounds;

        PanelRenderer renderer;

        Puzzle panel = null;
        SolutionLine line = null;
        SolutionLine lineMirror = null;
        PanelState panelState = new PanelState();

        List<Rectangle> startPoints;
        List<EndPoint> endPoints;
        List<Rectangle> walls;

        SpriteBatch internalBatch;
        SpriteFont fntDebug = null;
        Texture2D texPixel, texCircle, texClose, texDelete, texNext, texSeed;
        Texture2D[] texLike = new Texture2D[2];
        
        RenderTarget2D backgroundTexture;
        RenderTarget2D lineTexture;
        RenderTarget2D tempLineTexture;
        RenderTarget2D lineFadeTexture;
        RenderTarget2D errorsBlinkTexture;
        RenderTarget2D eliminatedErrorsTexture;

        List<TouchButton> buttons = new List<TouchButton>();
        FadeTransition fade = new FadeTransition(15, 15, 10);

        // Lines are of a darker tint when they are not finished
        static readonly Color linesTint = new Color(230, 230, 230);
        static readonly Color linesFinishTracingTint = new Color(new Vector3(1f) - linesTint.ToVector3());

        BloomFilter bloomFilter;

        public PanelGameScreen(Point screenSize, GraphicsDevice device, Dictionary<string, Texture2D> TextureProvider, Dictionary<string, SpriteFont> FontProvider, ContentManager Content, params object[] data)
            : this(data?[0] as Puzzle,
                   (data?.Length > 1 && data[1] is bool) ? Convert.ToBoolean(data[1]) : false,
                   (data?.Length > 2 && data[2] is int) ? (int)data[2] : -1,
                   (data?.Length > 3 ? data[3] as Action<int> : null),
                   screenSize, device, TextureProvider, FontProvider, Content) { }
        public PanelGameScreen(Puzzle panel, Point screenSize, GraphicsDevice device, Dictionary<string, Texture2D> TextureProvider, Dictionary<string, SpriteFont> FontProvider, ContentManager Content)
            : this(panel, false, -1, null, screenSize, device, TextureProvider, FontProvider, Content) { }
        public PanelGameScreen(Puzzle panel, bool standalonePanel, int tutorialIndex, Point screenSize, GraphicsDevice device, Dictionary<string, Texture2D> TextureProvider, Dictionary<string, SpriteFont> FontProvider, ContentManager Content)
            : this(panel, standalonePanel, tutorialIndex, null, screenSize, device, TextureProvider, FontProvider, Content) { }

        public PanelGameScreen(Puzzle panel, bool standalonePanel, int tutorialIndex, Action<int> tutorialAdvance, Point screenSize, GraphicsDevice device, Dictionary<string, Texture2D> TextureProvider, Dictionary<string, SpriteFont> FontProvider, ContentManager Content) 
            : base(screenSize, device, TextureProvider, FontProvider, Content)
        {
            if (panel == null)
                panel = new Puzzle(2, 2);

            this.panel = panel;
            IsStandalonePanel = standalonePanel;
            TutorialIndex = tutorialIndex;
            onTutorialAdvance = tutorialAdvance;
            internalBatch = new SpriteBatch(GraphicsDevice);
            renderer = new PanelRenderer(panel, screenSize, TextureProvider, FontProvider, device);
            startPoints = renderer.StartPoints;
            endPoints = renderer.EndPoints;
            walls = renderer.Walls;

            texPixel = TextureProvider["img/pixel"];
            texCircle = TextureProvider["img/circle"];
            texClose = TextureProvider["img/close"];
            texLike[0] = TextureProvider["img/like0"];
            texLike[1] = TextureProvider["img/like1"];
            texNext = TextureProvider["img/next"];
            texDelete = TextureProvider["img/delete"];
            texSeed = TextureProvider["img/seed"];
            fntDebug = FontProvider["font/fnt_small"];
            successFont = Localization.Font(FontProvider["font/fnt_constantia_big"]);

            // Fullscreen textures
            backgroundTexture = new RenderTarget2D(GraphicsDevice, screenSize.X, screenSize.Y);
            tempLineTexture = new RenderTarget2D(GraphicsDevice, screenSize.X, screenSize.Y);
            lineTexture = new RenderTarget2D(GraphicsDevice, screenSize.X, screenSize.Y);
            lineFadeTexture = new RenderTarget2D(GraphicsDevice, screenSize.X, screenSize.Y);
            errorsBlinkTexture = new RenderTarget2D(GraphicsDevice, screenSize.X, screenSize.Y);
            eliminatedErrorsTexture = new RenderTarget2D(GraphicsDevice, screenSize.X, screenSize.Y);

            // Load bloom shader
            if (SettingsManager.BloomFX)
            {
                bloomFilter = new BloomFilter();
                bloomFilter.Load(GraphicsDevice, Content, screenSize.X, screenSize.Y);
                bloomFilter.BloomPreset = BloomFilter.BloomPresets.Small;
                bloomFilter.BloomThreshold = -1;
                bloomFilter.BloomStrengthMultiplier = 0.75f;
            }

            renderer.RenderPanelToTexture(backgroundTexture, true);

            SpawnButtons();
            string successAction = TutorialIndex >= 0
                ? (TutorialIndex + 1 < TutorialContent.Lessons.Length ? "Next lesson" : "Back to lessons")
                : (IsStandalonePanel ? "Back to library" : "Next puzzle");
            successContinueButton = new TextButton(new Rectangle(), successFont, successAction, texPixel);
            successContinueButton.Click += ContinueAfterSuccess;
            UpdateSuccessLayout();
            UpdateButtonsColor();
        }

        private void ContinueAfterSuccess()
        {
            // The regular Next button still owns generated-panel transitions, seed
            // changes and persistence. Reuse it instead of duplicating that logic.
            if (!panelState.State.HasFlag(PanelStates.Solved) || fade.IsActive || continuePending)
                return;

            continuePending = true;
            if (TutorialIndex >= 0)
            {
                int nextLesson = TutorialIndex + 1;
                SoundManager.PlayOnce(Sound.ButtonNextSuccess);
                if (nextLesson < TutorialContent.Lessons.Length)
                {
                    onTutorialAdvance?.Invoke(nextLesson);
                    ScreenManager.Instance.AddScreen<PanelGameScreen>(
                        true, true, TutorialContent.CreatePuzzle(nextLesson), true,
                        nextLesson, onTutorialAdvance);
                }
                else
                {
                    ScreenManager.Instance.GoBack();
                }
            }
            else if (IsStandalonePanel)
            {
                SoundManager.PlayOnce(Sound.MenuEscape);
                ScreenManager.Instance.GoBack();
            }
            else
            {
                buttons[2].Press();
            }
        }

        private void UpdateSuccessLayout()
        {
            int shortSide = Math.Min(ScreenSize.X, ScreenSize.Y);
            Rectangle puzzle = renderer.PuzzleConfig;
            int cardWidth, cardHeight, x, y;

            if (ScreenSize.X > ScreenSize.Y)
            {
                // Wide phones: the free margin to the LEFT of the puzzle,
                // opposite the original panel controls.
                int available = Math.Max(0, puzzle.Left);
                cardWidth = Math.Max(120, Math.Min((int)(shortSide * 0.68f), (int)(available * 0.84f)));
                cardHeight = Math.Max(105, (int)(shortSide * 0.43f));
                x = Math.Max(8, (available - cardWidth) / 2);
                y = (ScreenSize.Y - cardHeight) / 2;
            }
            else
            {
                // Tall phones: above the puzzle and below the close icon,
                // leaving the solved line visible for review.
                int available = Math.Max(0, puzzle.Top);
                cardWidth = (int)(ScreenSize.X * 0.80f);
                cardHeight = Math.Max(105, Math.Min((int)(available * 0.63f), (int)(shortSide * 0.37f)));
                x = (ScreenSize.X - cardWidth) / 2;
                y = Math.Max((int)(shortSide * 0.15f), (available - cardHeight) / 2);
            }

            successCardBounds = new Rectangle(x, y, cardWidth, cardHeight);
            int horizontalInset = Math.Max(8, cardWidth / 12);
            successActionBounds = new Rectangle(
                x + horizontalInset, y + (int)(cardHeight * 0.48f),
                cardWidth - horizontalInset * 2, Math.Max(46, (int)(cardHeight * 0.43f)));
            successContinueButton.SetPositionAndSize(successActionBounds.Location, successActionBounds.Size);
        }

        private void SpawnButtons()
        {
            TouchButton btnClose = new TouchButton(new Rectangle(), texClose, null);
            btnClose.Click += () => {
                AbortTracing();
                SoundManager.PlayOnce(Sound.MenuOpen);
                ScreenManager.Instance.GoBack();
            };
            buttons.Add(btnClose);

            ToggleButton btnLike = new ToggleButton(new Rectangle(), texLike[1], texLike[0], null, FileStorageManager.IsPanelInFavourites(panel));
            btnLike.Click += () =>
            {
                // Add panel the list of favourite panels (or remove from it)
                if (btnLike.IsActivated)
                    FileStorageManager.AddPanelToFavourites(panel);
                else
                    FileStorageManager.DeletePanelFromFavourites(panel);

                SoundManager.PlayOnce(btnLike.IsActivated ? Sound.ButtonLike : Sound.ButtonUnlike);
            };
            buttons.Add(btnLike);

            // When panel is standalone, it cannot have Next button
            // It won't have a seed button either
            if (!IsStandalonePanel)
            {
                TwoStateButton btnNext = new TwoStateButton(new Rectangle(), texDelete, texNext, null, null, null, false);
                btnNext.Click += () =>
                {
                    Action callback = null;
                    callback = () =>
                    {
                        int? seed = null;
                        if (SettingsManager.IsSequentialMode)
                        {
                            // Advance sequential seed if the panel wasn't solved, otherwise it was already incremented at the time of solution
                            if (!panelState.State.HasFlag(PanelStates.Solved))
                                SettingsManager.CurrentSequentialSeed++;
                            
                            seed = SettingsManager.CurrentSequentialSeed;
                            SettingsManager.SaveSettings();
                        }

                        AbortTracing();
                        Puzzle nextPanel = DI.Get<PanelGenerator>().GeneratePanel(seed);
                        FileStorageManager.SaveCurrentPanel(nextPanel);
                        LoadNewPanel(nextPanel);
                        btnNext.StateActive = false;
                        btnLike.Deactivate();
                        fade.FadeOutComplete -= callback;
                    };

                // Add panel to the list of last 10 discarded panels
                if (!btnNext.StateActive)
                        FileStorageManager.AddPanelToDiscardedList(panel);

                    SoundManager.PlayOnce(btnNext.StateActive ? Sound.ButtonNextSuccess : Sound.ButtonNext);
                    fade.FadeOutComplete += callback;
                    fade.Restart();
                };
                buttons.Add(btnNext);

                // Disable seed button in sequential mode because it interferes with progression and it doesn't have a simple fix
                if (!SettingsManager.IsSequentialMode)
                {
                    TouchButton btnSeed = new TouchButton(new Rectangle(), texSeed, null);
                    btnSeed.Click += () => {
                        SoundManager.PlayOnce(Sound.ButtonNext);
                        ScreenManager.Instance.AddScreen<GameScreens.EnterSeedGameScreen>(
                            replaceCurrent: false, 
                            doFadeAnimation: true, 
                            panelState.State == PanelStates.Solved ? null : panel,
                            panel.WallsColor,
                            panel.ButtonsColor
                        );
                    };
                    buttons.Add(btnSeed);
                }
            }

            UpdateButtonsPosition();
        }
        private void UpdateButtonsPosition()
        {
            int screenMin = Math.Min(ScreenSize.X, ScreenSize.Y);
            bool screenHorizontal = ScreenSize.X > ScreenSize.Y;

            // Close
            int minOffset = Math.Min((int)(screenMin * 0.05f), 50);
            int smallBtnSize = (int)(screenMin * 0.1f);
            buttons[0].SetPositionAndSize(new Point(minOffset), new Point(smallBtnSize));

            int buttonSize = (int) (screenMin * 0.16f);
            // Horizontal screen
            if (screenHorizontal)
            {
                int marginX = renderer.PuzzleConfig.X + renderer.PuzzleConfig.Width;
                int freeSpaceX = ScreenSize.X - marginX;
                marginX += freeSpaceX / 2 - buttonSize / 2;

                if (!IsStandalonePanel)
                {
                    if (!SettingsManager.IsSequentialMode)
                    {
                        int marginY = ScreenSize.Y / 4 * 3 - buttonSize / 2;
                        // Like
                        buttons[1].SetPositionAndSize(new Point(marginX, marginY), new Point(buttonSize));
                        // Next
                        buttons[2].SetPositionAndSize(new Point(marginX, ScreenSize.Y - marginY - buttonSize), new Point(buttonSize));
                        // Seed
                        buttons[3].SetPositionAndSize(new Point(marginX, ScreenSize.Y / 2 - buttonSize / 2), new Point(buttonSize));
                    }
                    else
                    {
                        // Like
                        buttons[1].SetPositionAndSize(new Point(marginX, (int)(ScreenSize.Y / 2 + buttonSize * 0.4f)), new Point(buttonSize));
                        // Next
                        buttons[2].SetPositionAndSize(new Point(marginX, (int)(ScreenSize.Y / 2 - buttonSize * 1.4f)), new Point(buttonSize));
                    }
                }
                else
                {
                    int marginY = ScreenSize.Y / 2 - buttonSize / 2;
                    // Like
                    buttons[1].SetPositionAndSize(new Point(marginX, marginY), new Point(buttonSize));
                }
            }
            // Vertical screen
            else
            {
                int marginY = renderer.PuzzleConfig.Y + renderer.PuzzleConfig.Height;
                int freeSpaceY = ScreenSize.Y - marginY;
                marginY += freeSpaceY / 2 - buttonSize / 2;
                

                if (!IsStandalonePanel)
                {
                    if (!SettingsManager.IsSequentialMode)
                    {
                        int marginX = ScreenSize.X / 4 - buttonSize / 2;
                        // Like
                        buttons[1].SetPositionAndSize(new Point(marginX, marginY), new Point(buttonSize));
                        // Next
                        buttons[2].SetPositionAndSize(new Point(ScreenSize.X - marginX - buttonSize, marginY), new Point(buttonSize));
                        // Seed
                        buttons[3].SetPositionAndSize(new Point(ScreenSize.X / 2 - buttonSize / 2, marginY), new Point(buttonSize));
                    }
                    else
                    {
                        // Like
                        buttons[1].SetPositionAndSize(new Point((int)(ScreenSize.X / 2 - buttonSize * 1.4f), marginY), new Point(buttonSize));
                        // Next
                        buttons[2].SetPositionAndSize(new Point((int)(ScreenSize.X / 2 + buttonSize * 0.4f), marginY), new Point(buttonSize));
                    }
                }
                else
                {
                    int marginX = ScreenSize.X / 2 - buttonSize / 2;
                    // Like
                    buttons[1].SetPositionAndSize(new Point(marginX, marginY), new Point(buttonSize));
                }
            }
        }
        private void UpdateButtonsColor()
        {
            foreach (var button in buttons)
                button.ChangeTint(panel.ButtonsColor);
        }

        public void LoadNewPanel(Puzzle panel)
        {
            if (renderer.SetPanel(panel))
            {
                line = lineMirror = null;
                this.panel = panel;
                panelState.ResetToNeutral();
                continuePending = false;
                startPoints = renderer.StartPoints;
                endPoints = renderer.EndPoints;
                walls = renderer.Walls;
                renderer.RenderPanelToTexture(backgroundTexture, true);
                UpdateButtonsColor();
                UpdateButtonsPosition();
                UpdateSuccessLayout();
            }
        }

        private void AbortTracing()
        {
            if (SoundManager.IsPlaying(SoundLoop.Tracing))
                SoundManager.PlayOnce(Sound.AbortTracing);
            SoundManager.StopLoop(SoundLoop.Tracing);
            SoundManager.StopLoop(SoundLoop.PathComplete);
            panelState.ResetToNeutral();
            line = lineMirror = null;
        }

        public override void SetScreenSize(Point screenSize)
        {
            base.SetScreenSize(screenSize);

            // Save some info needed to rebuild lines
            int mainStartPointIndex = -1, mirrorStartPointIndex = -1;
            if (line != null)
                mainStartPointIndex = startPoints.FindIndex(x => x == line.StartCircle);
            if (lineMirror != null)
                mirrorStartPointIndex = startPoints.FindIndex(x => x == lineMirror.StartCircle);

            Point oldPanelZeroPoint = renderer.PuzzleConfig.Location;
            int oldNodePadding = renderer.NodePadding;

            // Update renderer
            renderer.SetScreenSize(screenSize);
            startPoints = renderer.StartPoints;
            endPoints = renderer.EndPoints;
            walls = renderer.Walls;

            // Update textures size
            backgroundTexture = new RenderTarget2D(GraphicsDevice, screenSize.X, screenSize.Y);
            tempLineTexture = new RenderTarget2D(GraphicsDevice, screenSize.X, screenSize.Y);
            lineTexture = new RenderTarget2D(GraphicsDevice, screenSize.X, screenSize.Y);
            lineFadeTexture = new RenderTarget2D(GraphicsDevice, screenSize.X, screenSize.Y);
            errorsBlinkTexture = new RenderTarget2D(GraphicsDevice, screenSize.X, screenSize.Y);
            eliminatedErrorsTexture = new RenderTarget2D(GraphicsDevice, screenSize.X, screenSize.Y);

            // Update bloom fx
            bloomFilter?.UpdateResolution(screenSize.X, screenSize.Y);
            // Redraw background
            renderer.RenderPanelToTexture(backgroundTexture, true);

            UpdateButtonsPosition();
            UpdateSuccessLayout();

            // Update lines
            if (line != null)
                line.UpdateHitboxes(renderer.LineWidth, startPoints[mainStartPointIndex], panel.Width, oldPanelZeroPoint, oldNodePadding, renderer.PuzzleConfig.Location, renderer.NodePadding);
            if (lineMirror != null)
            {
                lineMirror.UpdateHitboxes(renderer.LineWidth, startPoints[mirrorStartPointIndex], panel.Width, oldPanelZeroPoint, oldNodePadding, renderer.PuzzleConfig.Location, renderer.NodePadding);
                SolutionLine.SynchronizeLines(line, lineMirror, renderer.PuzzleConfig.Location, renderer.NodePadding);
            }
        }

        public override void Update(GameTime gameTime)
        {
            // Don't let the same tap that submits a solution also activate the
            // success button. It becomes actionable on the following input.
            bool wasSolved = panelState.State.HasFlag(PanelStates.Solved);
            HandleScreenTap();
            MoveLine(GetMoveVector());
            panelState.Update();

            if (!panelState.State.HasFlag(PanelStates.ErrorHappened) && !panelState.State.HasFlag(PanelStates.Solved))
            {
                // If line touched end point => Produce sound
                bool lineAtEndpoint = IsLineAtEndPoint();
                if (lineAtEndpoint)
                {
                    RenderLinesToTexture(lineFadeTexture, Color.White);

                    if (!panelState.State.HasFlag(PanelStates.FinishTracing))
                    {
                        panelState.SetFinishTracing(true);
                        SoundManager.PlayOnce(Sound.FinishTracing);
                        SoundManager.PlayLoop(SoundLoop.PathComplete);
                    }
                }
                // If line left end point => Produce another sound
                else if (!lineAtEndpoint && panelState.State.HasFlag(PanelStates.FinishTracing))
                {
                    panelState.SetFinishTracing(false);
                    SoundManager.PlayOnce(Sound.AbortFinishTracing);
                    SoundManager.StopLoop(SoundLoop.PathComplete);
                }
            }

            Point? tap = InputManager.GetTapPosition();
            bool onSuccessCard = tap.HasValue &&
                                 panelState.State.HasFlag(PanelStates.Solved) &&
                                 successCardBounds.Contains(tap.Value);
            if (!onSuccessCard)
                foreach (var button in buttons)
                    button.Update(tap);
            if (wasSolved && panelState.State.HasFlag(PanelStates.Solved))
                successContinueButton.Update(tap);

            fade.Update();

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.N))
                drawDebug = !drawDebug;
            
            base.Update(gameTime);
        }

        private void HandleScreenTap()
        {
            Point? tap = GetTapPosition();
            if (tap.HasValue)
            {
                // Taps on the completion overlay must not begin tracing again.
                if (panelState.State.HasFlag(PanelStates.Solved) &&
                    successCardBounds.Contains(tap.Value))
                    return;

                if (!(panelState.State.HasFlag(PanelStates.EliminationStarted) && !panelState.State.HasFlag(PanelStates.EliminationFinished)))
                {
                    if (line == null || panelState.State.HasFlag(PanelStates.Solved))
                    {
                        foreach (Rectangle startPoint in startPoints)
                            if (startPoint.Contains(tap.Value))
                            {
                                InputManager.LockMouse();
                                panelState.ResetToNeutral();
                                SoundManager.PlayOnce(Sound.StartTracing);
                                SoundManager.PlayLoop(SoundLoop.Tracing);
                                line = new SolutionLine(startPoint.Center - (new Point(renderer.LineWidth / 2)), renderer.LineWidth, startPoint);
                                if (panel is SymmetryPuzzle symPanel)
                                    if (symPanel.Y_Mirrored)
                                    {
                                        Point mirPoint = renderer.PuzzleConfig.Location + renderer.PuzzleConfig.Location + renderer.PuzzleConfig.Size - startPoint.Center - (new Point(renderer.LineWidth / 2));
                                        Rectangle mirStartPoint = startPoints.Find(x => x.Contains(mirPoint));
                                        lineMirror = new SolutionLine(RectifyLinePosition(mirPoint), renderer.LineWidth, mirStartPoint);
                                    }
                                    else
                                    {
                                        Point mirPoint = new Point(renderer.PuzzleConfig.Location.X * 2 + renderer.PuzzleConfig.Size.X - startPoint.Center.X - renderer.LineWidth / 2, startPoint.Center.Y - renderer.LineWidth / 2);
                                        Rectangle mirStartPoint = startPoints.Find(x => x.Contains(mirPoint));
                                        lineMirror = new SolutionLine(RectifyLinePosition(mirPoint), renderer.LineWidth, mirStartPoint);
                                    }

                                break;
                            }
                    }
                    else
                    {
                        InputManager.UnlockMouse();
                        SoundManager.StopLoop(SoundLoop.Tracing);
                        SoundManager.StopLoop(SoundLoop.PathComplete);

                        // Check if line head is in the end point
                        foreach (var endPoint in endPoints)
                            if (endPoint.IntercetionPercent(line.Head) > 0.4f)
                            {
                                // Place line head rignt in the end, nice and tight
                                Point headOffset = endPoint.Rectangle.Location - line.Head.Location;
                                MoveLine(headOffset.ToVector2());

                                // Retrieve Solution from the line and check for errors
                                List<Error> errors = null;
                                List<int> solution = line.GetSolution(panel.Width, renderer.PuzzleConfig.Location, renderer.NodePadding);
                                if (panel.SetSolution(solution))
                                    errors = panel.CheckForErrors();

                                // Split errors into true errors and the ones, that are eliminated by respective rules
                                var trueErrors = errors.Where(x => x.IsEliminated == false).ToList();
                                var eliminatedErrors = errors.Where(x => x.IsEliminated == true).ToList();

                                // If there are eliminated errors, initialize elimination process
                                if (eliminatedErrors.Count() > 0)
                                {
                                    SoundManager.PlayOnce(Sound.PotentialFailure);
                                    // Draw all errors in red as for now
                                    renderer.RenderErrorsToTexture(errorsBlinkTexture, errors, false);

                                    // Begin black line fade out as if there was an error
                                    RenderLinesToTexture(lineFadeTexture);
                                    panelState.InvokeFadeOut(true);
                                    // And start elimination timer
                                    panelState.InitializeElimination();

                                    // This event will be executed a few seconds later, after elimination is complete
                                    Action handler = null;
                                    handler = () =>
                                    {
                                        SoundManager.PlayOnce(Sound.EliminatorApply);
                                    // Re-draw true errors in red and eliminated errors separately
                                    renderer.RenderErrorsToTexture(errorsBlinkTexture, trueErrors, false);
                                        renderer.RenderErrorsToTexture(eliminatedErrorsTexture, eliminatedErrors, true);

                                        // If there are no true errors, then panel is solved
                                        if (trueErrors.Count() == 0)
                                        {
                                            // Setting success will stop lines fading process, but retain eliminated errors intact
                                            SetPanelSuccess();
                                        }
                                        else
                                        {
                                            // If there are true errors => delete the lines
                                            // They will continue to fade out and errors will continue to blink
                                            line = lineMirror = null;
                                            SoundManager.PlayOnce(Sound.Failure);
                                        }

                                    // Don't forget to unsubscribe ffs!
                                    panelState.EliminationFinished -= handler;
                                    };
                                    panelState.EliminationFinished += handler;
                                }
                                // If there are no eliminated errors, everything happens instantly
                                else
                                {
                                    // If there are no true errors, then panel is solved
                                    if (trueErrors.Count() == 0)
                                    {
                                        SetPanelSuccess();
                                    }
                                    else
                                    {
                                        // If there are true errors => start fading process and delete the lines
                                        RenderLinesToTexture(lineFadeTexture);
                                        renderer.RenderErrorsToTexture(errorsBlinkTexture, errors, false);
                                        panelState.InvokeFadeOut(true);
                                        line = lineMirror = null;
                                        SoundManager.PlayOnce(Sound.Failure);
                                    }
                                }

                                return;
                            }

                        // If we looked through every endpoint and line head is not in any of them
                        // Then simply delete lines and fade them out without errors
                        RenderLinesToTexture(lineFadeTexture);
                        panelState.InvokeFadeOut(false);
                        line = lineMirror = null;
                        SoundManager.PlayOnce(Sound.AbortTracing);
                    } 
                }
            }
        }
        private void SetPanelSuccess()
        {
            panelState.SetSuccess();
            SoundManager.PlayOnce(Sound.Success);
            if (!IsStandalonePanel)
                (buttons[2] as TwoStateButton).StateActive = true;

            // Tutorial (and other standalone) panels must not change normal progression.
            if (!IsStandalonePanel)
            {
                FileStorageManager.AddPanelToSolvedList(panel);
                FileStorageManager.DeleteCurrentPanel();
            }

            // In sequential mode advance seed by 1
            if (SettingsManager.IsSequentialMode && !IsStandalonePanel)
            {
                SettingsManager.CurrentSequentialSeed++;
                SettingsManager.SaveSettings();
            }
        }
        private void MoveLine(Vector2 moveVector)   
        {
            if (moveVector != Vector2.Zero && line != null && !panelState.State.HasFlag(PanelStates.EliminationStarted) && !panelState.State.HasFlag(PanelStates.Solved))
            {
                Vector2 firstMove, secondMove;
                if (Math.Abs(moveVector.X) > Math.Abs(moveVector.Y))
                {
                    firstMove = new Vector2(moveVector.X, 0);
                    secondMove = new Vector2(0, moveVector.Y);
                }
                else
                {
                    firstMove = new Vector2(0, moveVector.Y);
                    secondMove = new Vector2(moveVector.X, 0);
                }

                IEnumerable<Rectangle> hitboxes;
                if (panel is SymmetryPuzzle)
                    hitboxes = line.Hitboxes.Concat(lineMirror.Hitboxes).Concat(walls);
                else
                    hitboxes = line.Hitboxes.Concat(walls);

                Func<Vector2, IEnumerable<Rectangle>, bool> moveFunc;
                if (panel is SymmetryPuzzle)
                    moveFunc = MoveBothLines;
                else
                    moveFunc = MoveOneLine;

                MoveLinePrecise();

                // MoveLineCheap() is removed, because it can't be really used, for it is not wall-clipping safe
                /// <summary>
                /// More comfortable line control, but more CPU cost
                /// </summary>
                void MoveLinePrecise()
                {
                    // firstMove is the bigger part of vector (X or Y part)
                    bool firstMoveDone = false;
                    // If not successful, then try to move gradually by 1 pixel
                    Vector2 firstMoveStep = firstMove;
                    firstMoveStep.Normalize();
                    float firstMoveLength = Math.Max(Math.Abs(firstMove.X), Math.Abs(firstMove.Y));
                    // Move by 1 pixel until we hit the wall
                    for (int i = 0; i < firstMoveLength; i++)
                    {
                        bool stepSuccessful = moveFunc(firstMoveStep, hitboxes);

                        if (stepSuccessful)
                            firstMoveDone = true;
                        else
                            break;
                    }

                    // If we couldn't make the first move in larger axis, then try moving in lesser axis
                    if(!firstMoveDone)
                    {
                        // If lesser axis is [almost] non existent, then check in both directions if there's a corner nearby
                        // If there is, then move in that direction 75% of the main axis distance
                        float secondMoveLength = Math.Max(Math.Abs(secondMove.X), Math.Abs(secondMove.Y));
                        if (secondMoveLength <= firstMoveLength * 0.1f)
                            secondMove = line.GetMoveVectorNearCorner(moveVector, walls) * (firstMoveLength * 0.75f);

                        if(secondMove != Vector2.Zero)
                        {
                            // Try to move by 1 pixel in lesser axis and after each step try to move in bigger axis
                            Vector2 secondMoveStep = secondMove;
                            secondMoveLength = Math.Max(Math.Abs(secondMove.X), Math.Abs(secondMove.Y));
                            secondMoveStep.Normalize();
                            // Move by 1 pixel until we hit the wall
                            for (int i = 0; i < secondMoveLength; i++)
                            {
                                bool stepSuccessful = moveFunc(secondMoveStep, hitboxes);

                                if (stepSuccessful)
                                {
                                    // If we managed to move in first direction after a step in second direction, then stop
                                    // We should move by 1 pixel to not clip through walls
                                    bool firstMoved = false;
                                    for (int k = 0; k < firstMoveLength; k++)
                                    {
                                        bool substepSuccessful = moveFunc(firstMoveStep, hitboxes);

                                        if (!substepSuccessful)
                                            break;

                                        firstMoved = true;
                                    }
                                    if (firstMoved)
                                        break;
                                }
                                // If we hit the wall while stepping in second direction, then stop too
                                else
                                    break;
                            }
                        }
                    }

                }
            }

            bool MoveOneLine(Vector2 moveVect, IEnumerable<Rectangle> hitboxes) => line.Move(moveVect, hitboxes);
            bool MoveBothLines(Vector2 moveVect, IEnumerable<Rectangle> hitboxes)
            {
                // Move first line
                if (line.Move(moveVect, hitboxes.Append(lineMirror.Head)))
                {
                    // If first line succeeded, then move second line
                    Vector2 mirroredVector;
                    if ((panel as SymmetryPuzzle).Y_Mirrored)
                        mirroredVector = -moveVect;
                    else
                        mirroredVector = new Vector2(-moveVect.X, moveVect.Y);

                    if (!lineMirror.Move(mirroredVector, hitboxes.Append(line.Head)))
                    {
                        // If second line failed, move first line back
                        line.Move(-moveVect, hitboxes);
                        return false;
                    }
                    return true;
                }
                else return false;
            }
        }

        private Point RectifyLinePosition(Point pos)
        {
            Point zeroedPos = pos - renderer.PuzzleConfig.Location;
            return new Point(zeroedPos.X / renderer.NodePadding * renderer.NodePadding, zeroedPos.Y / renderer.NodePadding * renderer.NodePadding) + renderer.PuzzleConfig.Location;
        }
        private Point? GetTapPosition() => InputManager.GetTapPosition();
        private Vector2 GetMoveVector() => InputManager.GetDragVector();
        private bool IsLineAtEndPoint() => line != null ? endPoints.Any(x => x.IntercetionPercent(line.Head) > 0.4f) : false;

        #region ===== RENDER REGION =====
        public override void Draw(SpriteBatch spriteBatch)
        {
            // Draw lines to texture and apply blink-highlighting effect if line is at end point
            RenderLinesToTexture(lineTexture);

            // Draw bloom when elimination is not not started or when panel is solved
            if (line != null && (!panelState.State.HasFlag(PanelStates.EliminationStarted) || panelState.State.HasFlag(PanelStates.Solved)))
            {
                // We are using separate SpriteBatch to draw bloom, so we have to draw background in this batch too, so it will be below bloom texture
                internalBatch.Begin();
                internalBatch.Draw(backgroundTexture, GraphicsDevice.Viewport.Bounds, Color.White);

                if (SettingsManager.BloomFX)
                {
                    // Idk, more weird majiks, but if i call this Draw outside of this batch or inside next (additive) batch, background goes black
                    // So yeah... I guess it stays here now...
                    Texture2D bloom = bloomFilter.Draw(lineTexture, ScreenSize.X, ScreenSize.Y);
                    
                    internalBatch.End();

                    // Now draw bloom texture in additive blend mode
                    internalBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
                    internalBatch.Draw(bloom, GraphicsDevice.Viewport.Bounds, Color.White * 0.8f);
                }
                internalBatch.End();
            }
            else
            {
                // If we don't draw bloom, then we can use the same SpriteBatch to draw background texture
                spriteBatch.Draw(backgroundTexture, GraphicsDevice.Viewport.Bounds, Color.White);
            }

            // Draw line to the backbuffer
            spriteBatch.Draw(lineTexture, GraphicsDevice.Viewport.Bounds, Color.White);
            
            // Draw fading line after aborting tracing or submitting wrong solution
            if (panelState.State.HasFlag(PanelStates.LineFade) && lineFadeTexture != null)
                spriteBatch.Draw(lineFadeTexture, GraphicsDevice.Viewport.Bounds, (panelState.State.HasFlag(PanelStates.ErrorHappened) ? Color.Black : Color.White) * panelState.LineFadeOpacity);

            // Draw red blinking rules after submitting the wrong solution
            if (panelState.State.HasFlag(PanelStates.ErrorBlink) && errorsBlinkTexture != null)
                spriteBatch.Draw(errorsBlinkTexture, GraphicsDevice.Viewport.Bounds, Color.White * panelState.ErrorBlinkOpacity);

            // Animate fading into gray of eliminated rules
            if (panelState.State.HasFlag(PanelStates.EliminationFinished) && eliminatedErrorsTexture != null)
                spriteBatch.Draw(eliminatedErrorsTexture, GraphicsDevice.Viewport.Bounds, Color.White * panelState.EliminationFadeOpacity);

            // Draw buttons
            foreach (var button in buttons)
                button.Draw(spriteBatch);

            if (TutorialIndex >= 0 && TutorialIndex < TutorialContent.Lessons.Length)
            {
                string hint = panelState.State.HasFlag(PanelStates.Solved)
                    ? Localization.T("Solved!") : TutorialContent.Lessons[TutorialIndex].Hint;
                SpriteFont hintFont = Localization.Font(fntDebug);
                Vector2 extent = hintFont.MeasureString(hint);
                float scale = Math.Min(Math.Min(ScreenSize.X, ScreenSize.Y) * 0.036f / Math.Max(1f, extent.Y),
                    ScreenSize.X * 0.70f / Math.Max(1f, extent.X));
                spriteBatch.DrawString(hintFont, hint,
                    new Vector2((ScreenSize.X - extent.X * scale) / 2f, ScreenSize.Y * 0.025f),
                    Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }

            if (panelState.State.HasFlag(PanelStates.Solved))
                DrawSuccessCard(spriteBatch);

            // Draw node IDs
            if (drawDebug)
                DebugDrawNodeIDs(spriteBatch);

            if (fade.IsActive)
                spriteBatch.Draw(texPixel, new Rectangle(Point.Zero, ScreenSize), Color.Black * fade.Opacity);
            
            base.Draw(spriteBatch);
        }

        private void DrawSuccessCard(SpriteBatch spriteBatch)
        {
            // Strong contrast and a large touch target: no new image assets.
            Rectangle outer = successCardBounds;
            spriteBatch.Draw(texPixel, outer, new Color(102, 226, 149));
            Rectangle inner = new Rectangle(outer.X + 3, outer.Y + 3,
                                            Math.Max(1, outer.Width - 6),
                                            Math.Max(1, outer.Height - 6));
            spriteBatch.Draw(texPixel, inner, new Color(13, 41, 31));
            spriteBatch.Draw(texPixel, successActionBounds, new Color(25, 143, 83));

            string message = Localization.T("Puzzle solved!");
            Vector2 textSize = successFont.MeasureString(message);
            float scale = Math.Min((outer.Width * 0.89f) / Math.Max(1f, textSize.X),
                                   (outer.Height * 0.27f) / Math.Max(1f, textSize.Y));
            Vector2 pos = new Vector2(outer.Center.X - textSize.X * scale / 2f,
                                      outer.Y + outer.Height * 0.13f);
            spriteBatch.DrawString(successFont, message, pos, new Color(180, 255, 203),
                                   0, Vector2.Zero, scale, SpriteEffects.None, 0);
            successContinueButton.Draw(spriteBatch);
        }

        private void DrawLines(SpriteBatch spriteBatch, Color? fillColor = null)
        {
            if (line != null)
                line.Draw(spriteBatch, texCircle, texPixel, fillColor ?? panel.MainColor);

            if (lineMirror != null)
            {
                var symPanel = panel as SymmetryPuzzle;
                if (symPanel.MirrorColorAlpha > 0)
                    lineMirror.Draw(spriteBatch, texCircle, texPixel, (fillColor ?? symPanel.MirrorColor) * symPanel.MirrorColorAlpha);
            }
        }
        private void RenderLinesToTexture(RenderTarget2D canvas, Color? fillColor = null)
        {
            // First draw the lines to the temp texture
            GraphicsDevice.SetRenderTarget(tempLineTexture);
            GraphicsDevice.Clear(Color.Transparent);

            internalBatch.Begin(SpriteSortMode.Texture);
            DrawLines(internalBatch, fillColor);
            internalBatch.End();

            // Now switch render target to the actual target texture and draw temp one onto it with color tint if necessary
            GraphicsDevice.SetRenderTarget(canvas);
            GraphicsDevice.Clear(Color.Transparent);

            internalBatch.Begin();
            internalBatch.Draw(tempLineTexture, canvas.Bounds, (fillColor == null && !panelState.State.HasFlag(PanelStates.Solved)) ? linesTint : Color.White);
            internalBatch.End();

            // Add finish line pulsation when line is at the end point
            if (panelState.State.HasFlag(PanelStates.FinishTracing))
            {
                // When finish tracing is happening, lineFadeTexture has the image of lines in white color
                // Draw this image with required tint in additive blend mode
                internalBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
                internalBatch.Draw(lineFadeTexture, lineTexture.Bounds, linesFinishTracingTint * panelState.FinishTracingBlinkOpacity);
                internalBatch.End();
            }

            // Drop the render target
            GraphicsDevice.SetRenderTarget(null);
        }

        private void DebugDrawNodeIDs(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < panel.Width + 1; i++)
            {
                for (int j = 0; j < panel.Height + 1; j++)
                {
                    int id = j * (panel.Width + 1) + i;
                    spriteBatch.DrawString(fntDebug, id.ToString(), (renderer.PuzzleConfig.Location + new Point(i, j).Multiply(renderer.NodePadding)).ToVector2(), Color.Black);
                }
            }
        }

        #endregion
    }
}
