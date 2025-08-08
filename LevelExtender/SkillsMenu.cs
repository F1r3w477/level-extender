// File: SkillsMenu.cs
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace LevelExtender
{
    /// <summary>
    /// In-game menu that lists extended skills with icons, name, level, and an XP bar.
    /// Hover shows a tooltip with per-level XP breakdown and percent.
    /// Supports paging if there are more skills than fit on one screen.
    /// </summary>
    internal sealed class SkillsMenu : IClickableMenu
    {
        private readonly List<Skill> _skills;

        // Layout
        private const int PanelWidth = 700;
        private const int PanelHeight = 460;

        private const int RowHeight = 56;          // room for name + bar
        private const int Padding = 32;

        private const int IconSize = 40;           // displayed size
        private const int IconScale = 4;           // mouseCursors icons are 10x10

        private const int NameOffsetX = 56;        // left of row to name
        private const int NameOffsetY = 6;         // name baseline
        private const int BarTopMargin = 30;       // moved slightly lower
        private const int BarHeightPx = 12;
        private const int RightPadding = 24;       // right margin before panel edge
        private const int BarRightGap = 12;        // gap between bar end and level text

        // Paging
        private const int RowsPerPage = 10;
        private int _pageIndex;
        private readonly ClickableTextureComponent? _btnPrev;
        private readonly ClickableTextureComponent? _btnNext;

        // Hover state (per visible row)
        private readonly List<Rectangle> _rowHitboxes = new();
        private int _hoverRowIndex = -1;

        public SkillsMenu(List<Skill> skills)
            : base(
                (Game1.uiViewport.Width - PanelWidth) / 2,
                (Game1.uiViewport.Height - PanelHeight) / 2,
                PanelWidth,
                PanelHeight,
                showUpperRightCloseButton: true)
        {
            _skills = skills ?? new List<Skill>();

            if (_skills.Count > RowsPerPage)
            {
                _btnPrev = new ClickableTextureComponent(
                    new Rectangle(xPositionOnScreen + 60, yPositionOnScreen + height - 60, 44, 44),
                    Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f);

                _btnNext = new ClickableTextureComponent(
                    new Rectangle(xPositionOnScreen + width - 104, yPositionOnScreen + height - 60, 44, 44),
                    Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f);
            }
        }

        #region Drawing

        public override void draw(SpriteBatch b)
        {
            // Dim background
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.35f);

            // Panel
            IClickableMenu.drawTextureBox(
                b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
                xPositionOnScreen, yPositionOnScreen, width, height,
                Color.White, 4f, drawShadow: true);

            // Title
            const string title = "Extended Skills";
            Vector2 tSize = Game1.dialogueFont.MeasureString(title);
            Utility.drawTextWithShadow(
                b, title, Game1.dialogueFont,
                new Vector2(xPositionOnScreen + (width - tSize.X) / 2, yPositionOnScreen + 20),
                Game1.textColor);

            // List bounds
            _rowHitboxes.Clear();
            int listTop = yPositionOnScreen + 72;
            int listLeft = xPositionOnScreen + Padding;
            int listRight = xPositionOnScreen + width - Padding;

            int start = _pageIndex * RowsPerPage;
            int end = Math.Min(start + RowsPerPage, _skills.Count);

            for (int i = start; i < end; i++)
            {
                Skill s = _skills[i];
                int localIndex = i - start;
                int rowY = listTop + localIndex * RowHeight;

                // Full row hitbox (for hover)
                var rowRect = new Rectangle(listLeft - 8, rowY - 4, listRight - listLeft + 16, RowHeight);
                _rowHitboxes.Add(rowRect);

                // Subtle hover background
                if (_hoverRowIndex == localIndex)
                    b.Draw(Game1.staminaRect, rowRect, new Color(255, 255, 255, 24));

                // Icon
                var iconRect = GetIconRectForSkill(s.Key);
                b.Draw(Game1.mouseCursors,
                    new Vector2(listLeft, rowY + (RowHeight - IconSize) / 2),
                    iconRect, Color.White,
                    0f, Vector2.Zero, IconScale, SpriteEffects.None, 1f);

                // --- XP bar FIRST (so text draws on top) ---
                int nameX = listLeft + NameOffsetX;
                int barX = nameX;
                int barY = rowY + BarTopMargin;
                string lvl = $"Lvl {s.Level}";
                Vector2 lvlSize = Game1.smallFont.MeasureString(lvl);
                int lvlX = listRight - RightPadding - (int)lvlSize.X;

                int barMaxRight = lvlX - BarRightGap;
                int barWidth = Math.Max(1, barMaxRight - barX);

                // thresholds & percent
                int prevLevelXp = s.GetRequiredExperienceForLevel(s.Level - 1);
                int levelXp = s.GetRequiredExperienceForLevel(s.Level);
                int percent = ComputeLevelPercent(prevLevelXp, levelXp, s.Experience);

                // bar background
                b.Draw(Game1.staminaRect, new Rectangle(barX, barY, barWidth, BarHeightPx), Color.Black * 0.35f);

                // bar fill (percent → pixels)
                int fill = (int)(barWidth * (percent / 100f));
                if (fill > 0)
                    b.Draw(Game1.staminaRect, new Rectangle(barX, barY, fill, BarHeightPx), new Color(15, 122, 255));

                // --- Then draw text on top ---
                // Level (right-aligned)
                Utility.drawTextWithShadow(b, lvl, Game1.smallFont, new Vector2(lvlX, rowY + NameOffsetY), Color.LimeGreen);

                // Name (left)
                Utility.drawTextWithShadow(b, s.Name, Game1.smallFont, new Vector2(nameX, rowY + NameOffsetY), Game1.textColor);
            }

            // Paging buttons
            if (_skills.Count > RowsPerPage)
            {
                bool showPrev = _pageIndex > 0;
                bool showNext = (start + RowsPerPage) < _skills.Count;
                if (showPrev) _btnPrev!.draw(b);
                if (showNext) _btnNext!.draw(b);
            }

            // Tooltip for hover row
            if (_hoverRowIndex >= 0 && _hoverRowIndex < (end - start))
            {
                var s = _skills[start + _hoverRowIndex];
                var (prev, next, need, have, pct) = ComputeLevelProgress(s);
                int percent = (int)(pct * 100f);

                string tip =
                    $"{s.Name}\n" +
                    $"Level: {s.Level}\n" +
                    $"XP this level: {have:n0} / {need:n0}  ({percent:0.#}%)\n" +
                    $"Total XP: {s.Experience:n0}";

                drawHoverText(b, tip, Game1.smallFont);
            }

            drawMouse(b);
        }

        #endregion

        #region Input

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);

            _hoverRowIndex = -1;

            for (int i = 0; i < _rowHitboxes.Count; i++)
            {
                if (_rowHitboxes[i].Contains(x, y))
                {
                    _hoverRowIndex = i;
                    break;
                }
            }

            _btnPrev?.tryHover(x, y, 0.25f);
            _btnNext?.tryHover(x, y, 0.25f);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (_skills.Count > RowsPerPage)
            {
                if (_btnPrev != null && _btnPrev.containsPoint(x, y) && _pageIndex > 0)
                {
                    _pageIndex--;
                    Game1.playSound("shwip");
                }
                else if (_btnNext != null && _btnNext.containsPoint(x, y) &&
                         ((_pageIndex + 1) * RowsPerPage) < _skills.Count)
                {
                    _pageIndex++;
                    Game1.playSound("shwip");
                }
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            Game1.exitActiveMenu();
        }

        public override void receiveKeyPress(Keys key)
        {
            if (key == Keys.Escape)
            {
                Game1.exitActiveMenu();
                return;
            }

            if (_skills.Count > RowsPerPage)
            {
                if (key == Keys.Left && _pageIndex > 0)
                {
                    _pageIndex--;
                    Game1.playSound("shwip");
                }
                else if (key == Keys.Right && ((_pageIndex + 1) * RowsPerPage) < _skills.Count)
                {
                    _pageIndex++;
                    Game1.playSound("shwip");
                }
            }

            base.receiveKeyPress(key);
        }

        #endregion

        #region Helpers

        private static Rectangle GetIconRectForSkill(int key) => key switch
        {
            0 => new Rectangle(10, 428, 10, 10),  // Farming
            1 => new Rectangle(20, 428, 10, 10),  // Fishing
            2 => new Rectangle(60, 428, 10, 10),  // Foraging
            3 => new Rectangle(30, 428, 10, 10),  // Mining
            4 => new Rectangle(120, 428, 10, 10), // Combat
            _ => new Rectangle(50, 428, 10, 10),  // Luck/default
        };

    #if DEBUG
        // expose constants for tests (read-only)
        internal const int Debug_NameOffsetX = NameOffsetX;
        internal const int Debug_RightPadding = RightPadding;
        internal const int Debug_BarRightGap = BarRightGap;
        internal const int Debug_RowHeight = RowHeight;
        internal const int Debug_BarTopMargin = BarTopMargin;
        internal const int Debug_BarHeightPx = BarHeightPx;

        /// <summary>Compute the page range [start, end) for a given page.</summary>
        internal static (int start, int end) ComputePageRange(int pageIndex, int rowsPerPage, int count)
            => (pageIndex * rowsPerPage, Math.Min(pageIndex * rowsPerPage + rowsPerPage, count));

        /// <summary>Compute the width of the XP bar, leaving room for the right-aligned level label.</summary>
        internal static int ComputeBarWidth(int listLeft, int listRight, int levelTextWidth)
        {
            int nameX = listLeft + NameOffsetX;
            int lvlX  = listRight - RightPadding - levelTextWidth;
            int barMaxRight = lvlX - BarRightGap;
            return Math.Max(1, barMaxRight - nameX);
        }

        /// <summary>Math used for tooltip values: previous/next thresholds, need/have, and percent (0..1).</summary>
        internal static (int prev, int next, int need, int have, float pct) ComputeLevelProgress(Skill s)
        {
            int prev = s.GetRequiredExperienceForLevel(s.Level - 1);
            int next = s.GetRequiredExperienceForLevel(s.Level);
            int need = Math.Max(1, next - prev);
            int have = Math.Max(0, s.Experience - prev);
            float pct = Math.Clamp(have / (float)need, 0f, 1f);
            return (prev, next, need, have, pct);
        }

        /// <summary>
        /// Compute progress as an integer percentage (0..100). Friendly for tests & UI.
        /// </summary>
        internal static int ComputeLevelPercent(int prevLevelXp, int levelXp, int currentXp)
        {
            int need = Math.Max(1, levelXp - prevLevelXp);
            int have = Math.Max(0, currentXp - prevLevelXp);
            float pct = Math.Clamp(have / (float)need, 0f, 1f);
            return (int)(pct * 100f);
        }

        /// <summary>Convenience overload using a <see cref="Skill"/>.</summary>
        internal static int ComputeLevelPercent(Skill s)
            => ComputeLevelPercent(
                s.GetRequiredExperienceForLevel(s.Level - 1),
                s.GetRequiredExperienceForLevel(s.Level),
                s.Experience
            );

        // make icon mapper testable
        internal static Rectangle Debug_GetIconRectForSkill(int key) => GetIconRectForSkill(key);
    #endif

        #endregion
    }
}
