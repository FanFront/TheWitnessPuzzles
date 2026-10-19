using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TWP_Shared
{
    /// <summary>A readable guide and a launcher for real, playable example panels.</summary>
    public sealed class TutorialGameScreen : GameScreen
    {
        private readonly bool tutorialMode;
        private int index;
        private readonly SpriteFont font;
        private readonly SpriteFont headingFont;
        private readonly Texture2D pixel;
        private readonly List<TextButton> buttons = new List<TextButton>();
        private TextButton previous, next, play, back;

        public TutorialGameScreen(Point screenSize, GraphicsDevice device,
            Dictionary<string, Texture2D> textures, Dictionary<string, SpriteFont> fonts,
            ContentManager content, params object[] data)
            : base(screenSize, device, textures, fonts, content, data)
        {
            tutorialMode = data.Length == 0 || !(data[0] is bool) || (bool)data[0];
            font = Localization.Font(fonts["font/fnt_small"]);
            headingFont = Localization.Font(fonts["font/fnt_constantia_big"]);
            pixel = textures["img/pixel"];

            previous = MakeButton("Previous", () => ChangePage(-1));
            next = MakeButton("Next", () => ChangePage(1));
            play = MakeButton("Play example", () =>
            {
                ScreenManager.Instance.AddScreen<PanelGameScreen>(
                    false, true, TutorialContent.CreatePuzzle(index), true, index);
                SoundManager.PlayOnce(Sound.MenuEnter);
            });
            back = MakeButton("Back", () =>
            {
                ScreenManager.Instance.GoBack();
                SoundManager.PlayOnce(Sound.MenuEscape);
            });
            UpdateButtonPositions();
        }

        private TextButton MakeButton(string label, Action onClick)
        {
            var button = new TextButton(new Rectangle(), headingFont, label, pixel);
            button.Click += onClick;
            buttons.Add(button);
            return button;
        }

        private void ChangePage(int direction)
        {
            index = (index + direction + TutorialContent.Lessons.Length) % TutorialContent.Lessons.Length;
            SoundManager.PlayOnce(Sound.MenuTick);
        }

        private void UpdateButtonPositions()
        {
            int h = Math.Max(32, (int)(ScreenSize.Y * 0.065f));
            int width = (int)(ScreenSize.X * 0.82f);
            int left = (ScreenSize.X - width) / 2;
            play.SetPositionAndSize(new Point(left, (int)(ScreenSize.Y * 0.73f)), new Point(width, h));
            int space = Math.Max(8, (int)(ScreenSize.X * 0.03f));
            int half = (width - space) / 2;
            previous.SetPositionAndSize(new Point(left, (int)(ScreenSize.Y * 0.82f)), new Point(half, h));
            next.SetPositionAndSize(new Point(left + half + space, (int)(ScreenSize.Y * 0.82f)), new Point(half, h));
            back.SetPositionAndSize(new Point(left, (int)(ScreenSize.Y * 0.91f)), new Point(width, h));
        }

        public override void SetScreenSize(Point screenSize)
        {
            base.SetScreenSize(screenSize);
            UpdateButtonPositions();
        }

        public override void Update(GameTime gameTime)
        {
            Point? tap = InputManager.GetTapPosition();
            foreach (var button in buttons) button.Update(tap);
            base.Update(gameTime);
        }

        // Word wrap text to the actual width of the phone, including Chinese characters.
        private List<string> Wrap(string text, float scale, float maxWidth)
        {
            var result = new List<string>();
            foreach (string paragraph in text.Split('\n'))
            {
                string line = "";
                foreach (char c in paragraph)
                {
                    string candidate = line + c;
                    if (line.Length > 0 && font.MeasureString(candidate).X * scale > maxWidth)
                    {
                        result.Add(line.TrimEnd());
                        line = c == ' ' ? "" : c.ToString();
                    }
                    else line = candidate;
                }
                result.Add(line);
            }
            return result;
        }

        private void DrawCentered(SpriteBatch batch, SpriteFont selected, string text,
            float y, float maxWidth, float maxHeight, Color color)
        {
            Vector2 size = selected.MeasureString(text);
            float scale = Math.Min(maxWidth / Math.Max(size.X, 1f), maxHeight / Math.Max(size.Y, 1f));
            batch.DrawString(selected, text,
                new Vector2((ScreenSize.X - size.X * scale) / 2, y),
                color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public override void Draw(SpriteBatch batch)
        {
            batch.Draw(pixel, new Rectangle(Point.Zero, ScreenSize), Color.Black);

            TutorialContent.Lesson lesson = TutorialContent.Lessons[index];
            float w = ScreenSize.X * 0.86f;
            float min = Math.Min(ScreenSize.X, ScreenSize.Y);

            DrawCentered(batch, headingFont,
                Localization.T(tutorialMode ? "Learn to play" : "Rule guide"),
                ScreenSize.Y * 0.055f, w, min * 0.075f, Color.White);
            DrawCentered(batch, font, (index + 1) + " / " + TutorialContent.Lessons.Length,
                ScreenSize.Y * 0.145f, w, min * 0.035f, Color.Gray);
            DrawCentered(batch, headingFont, lesson.Title,
                ScreenSize.Y * 0.205f, w, min * 0.065f, Color.White);

            float descriptionScale = Math.Min(min * 0.038f / Math.Max(1, font.LineSpacing),
                                             w / Math.Max(1f, font.MeasureString("A").X * 32f));
            var lines = Wrap(lesson.Description, descriptionScale, w);
            float lineHeight = font.LineSpacing * descriptionScale * 1.2f;
            float y = ScreenSize.Y * 0.31f;
            foreach (string line in lines)
            {
                if (y + lineHeight > ScreenSize.Y * 0.59f) break;
                batch.DrawString(font, line, new Vector2((ScreenSize.X - font.MeasureString(line).X * descriptionScale) / 2f, y),
                    Color.LightGray, 0, Vector2.Zero, descriptionScale, SpriteEffects.None, 0f);
                y += lineHeight;
            }

            Texture2D icon = TextureProvider[lesson.Icon];
            int iconSize = (int)(min * 0.105f);
            batch.Draw(icon, new Rectangle((ScreenSize.X - iconSize) / 2,
                (int)(ScreenSize.Y * 0.60f), iconSize, iconSize), Color.White);

            foreach (var button in buttons) button.Draw(batch);
            base.Draw(batch);
        }
    }
}
