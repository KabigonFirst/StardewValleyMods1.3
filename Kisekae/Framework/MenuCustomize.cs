using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewModdingAPI;
using Kisekae.Config;

namespace Kisekae.Framework {
    internal class MenuCustomize : ITabMenu {
        /*********
        ** Properties
        *********/
        /// <summary>Global Mod Interface.</summary>
        private readonly IMod m_env;
        /// <summary>Encapsulates the underlying mod texture management.</summary>
        private readonly ContentHelper m_contentHelper;
        /// <summary>The global config settings.</summary>
        private readonly GlobalConfig m_globalConfig;
        /// <summary>Core component to manipulate player appearance.</summary>
        private readonly FarmerMakeup m_farmerMakeup;

        private readonly IClickableMenu m_parent;

        /// <summary>The messages to display on the screen.</summary>
        private readonly List<Alert> Alerts = new List<Alert>();
        /// <summary>The color picker for the character's pants.</summary>
        private ColorPicker PantsColorPicker;
        /// <summary>The color picker for the character's hair.</summary>
        private ColorPicker HairColorPicker;
        /// <summary>The color picker for the character's eyes.</summary>
        private ColorPicker EyeColorPicker;
        /// <summary>The field labels.</summary>
        private readonly List<ClickableComponent> Labels = new List<ClickableComponent>();
        /// <summary>The labels for arrow selectors, which also show the currently selected value.</summary>
        private readonly List<ClickableComponent> SelectorLabels = new List<ClickableComponent>();
        /// <summary>The arrow buttons for selectors.</summary>
        private readonly List<ClickableTextureComponent> ArrowButtons = new List<ClickableTextureComponent>();
        /// <summary>The gender chooser buttons.</summary>
        private ClickableTextureComponent[] GenderButtons = new ClickableTextureComponent[0];
        /// <summary>The button on the main submenu which randomises the character.</summary>
        private ClickableTextureComponent RandomButton;
        /// <summary>The button which saves changes.</summary>
        private ClickableTextureComponent OkButton;
        /// <summary>The outline around the male option in <see cref="GenderButtons"/> when it's selected.</summary>
        private ClickableTextureComponent MaleOutlineButton;
        /// <summary>The outline around the female option in <see cref="GenderButtons"/> when it's selected.</summary>
        private ClickableTextureComponent FemaleOutlineButton;
        /// <summary>The multiplayer button.</summary>
        private ClickableTextureComponent MultiplayerButton;
        /// <summary>The 'quick load favorite' buttons.</summary>
        private ClickableTextureComponent[] QuickLoadFavButtons = new ClickableTextureComponent[0];

        /// <summary>The last color picker the player interacted with.</summary>
        private ColorPicker LastHeldColorPicker;
        /// <summary>The delay until the the character preview should be updated with the last colour picker change.</summary>
        private int ColorPickerTimer;
        /// <summary>The tooltip text to draw next to the cursor.</summary>
        private string HoverText = "";
        /// <summary>How many times the player pressed the 'random' buttom since the menu was opened.</summary>
        private int TimesRandomised;
        /// <summary>The zoom level before the menu was opened.</summary>
        private readonly float PlayerZoomLevel;

        /*********
        ** Public methods
        *********/
        public MenuCustomize(IMod env, ContentHelper contentHelper, GlobalConfig globalConfig, LocalConfig playerConfig, FarmerMakeup farmerMakeup, IClickableMenu parent = null) : base(
                  x: Game1.viewport.Width / 2 - (680 + IClickableMenu.borderWidth * 2) / 2,
                  y: Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2 - Game1.tileSize,
                  width: 632 + IClickableMenu.borderWidth * 2,
                  height: 500 + IClickableMenu.borderWidth * 4 + Game1.tileSize
            ) {
            m_env = env;
            m_contentHelper = contentHelper;
            m_globalConfig = globalConfig;
            this.PlayerZoomLevel = Game1.options.zoomLevel;
            m_farmerMakeup = farmerMakeup;
            m_parent = parent;
            exitFunction = exit;

            updateLayout();
        }

        /// <summary>The method invoked when the player presses the left mouse button.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveLeftClick(int x, int y, bool playSound = true) {
            // gender buttons
            if (m_globalConfig.CanChangeGender) {
                foreach (ClickableTextureComponent button in this.GenderButtons) {
                    if (button.containsPoint(x, y)) {
                        this.HandleButtonAction(button.name);
                        button.scale -= 0.5f;
                        button.scale = Math.Max(3.5f, button.scale);
                        break;
                    }
                }
            }

            // arrow buttons
            foreach (ClickableTextureComponent current in this.ArrowButtons) {
                if (current.containsPoint(x, y)) {
                    this.HandleSelectorChange(current.name, Convert.ToInt32(current.hoverText));
                    m_farmerMakeup.ChangeEyeColor(this.EyeColorPicker.getSelectedColor());
                    current.scale -= 0.25f;
                    current.scale = Math.Max(0.75f, current.scale);
                    break;
                }
            }

            // OK button
            if (this.OkButton.containsPoint(x, y)) {
                this.HandleButtonAction(this.OkButton.name);
                this.OkButton.scale -= 0.25f;
                this.OkButton.scale = Math.Max(0.75f, this.OkButton.scale);
                return;
            }

            // color pickers
            if (this.HairColorPicker.containsPoint(x, y)) {
                m_farmerMakeup.ChangeHairColor(this.HairColorPicker.click(x, y));
                this.LastHeldColorPicker = this.HairColorPicker;
                return;
            }
            if (this.PantsColorPicker.containsPoint(x, y)) {
                m_farmerMakeup.ChangeBottomsColor(this.PantsColorPicker.click(x, y));
                this.LastHeldColorPicker = this.PantsColorPicker;
                return;
            }
            if (this.EyeColorPicker.containsPoint(x, y)) {
                m_farmerMakeup.ChangeEyeColor(this.EyeColorPicker.click(x, y));
                this.LastHeldColorPicker = this.EyeColorPicker;
                return;
            }

            // quick favorites
            for (int i = 0; i < this.QuickLoadFavButtons.Length; i++) {
                if (!this.QuickLoadFavButtons[i].containsPoint(x, y)) {
                    continue;
                }
                if (m_farmerMakeup.LoadFavorite(i + 1)) {
                    Game1.playSound("yoba");
                } else {
                    this.Alerts.Add(new Alert(Game1.mouseCursors, new Rectangle(268, 470, 16, 16), Game1.viewport.Width / 2 - (700 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2, "Uh oh! No Favorite is Set!", 1000, false));
                    if (m_parent is MenuFarmerMakeup mk) {
                        mk.ShowFavTabArrow = true;
                    }
                }
                break;
            }

            // MutiplayerFix selector
            if(this.MultiplayerButton.containsPoint(x,y)) {
                m_farmerMakeup.TougleMultiplayerFix();
                this.MultiplayerButton.sourceRect.X = this.MultiplayerButton.sourceRect.X == 227 ? 236 : 227;
                Game1.playSound("drumkit6");
            }

            // random button
            if (this.RandomButton.containsPoint(x, y)) {
                this.RandomiseCharacter();
                this.RandomButton.scale = Game1.pixelZoom - 0.5f;
                this.EyeColorPicker.setColor(Game1.player.newEyeColor);
                this.HairColorPicker.setColor(Game1.player.hairstyleColor);
                this.PantsColorPicker.setColor(Game1.player.pantsColor);
            }
        }

        /// <summary>The method invoked while the player is holding down the left mouse button.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        public override void leftClickHeld(int x, int y) {
            // update color pickers
            this.ColorPickerTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
            if (this.ColorPickerTimer <= 0) {
                if (this.LastHeldColorPicker == null) {
                    return;
                }
                if (this.LastHeldColorPicker.Equals(this.HairColorPicker)) {
                    m_farmerMakeup.ChangeHairColor(this.HairColorPicker.clickHeld(x, y));
                } else if (this.LastHeldColorPicker.Equals(this.PantsColorPicker)) {
                    m_farmerMakeup.ChangeBottomsColor(this.PantsColorPicker.clickHeld(x, y));
                } else if (this.LastHeldColorPicker.Equals(this.EyeColorPicker)) {
                    m_farmerMakeup.ChangeEyeColor(this.EyeColorPicker.clickHeld(x, y));
                }
                this.ColorPickerTimer = 100;
            }
        }

        /// <summary>The method invoked when the player releases the left mouse button.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        public override void releaseLeftClick(int x, int y) {
            // update color pickers
            this.HairColorPicker.releaseClick();
            this.PantsColorPicker.releaseClick();
            this.EyeColorPicker.releaseClick();
            this.LastHeldColorPicker = null;
        }

        /// <summary>The method invoked when the player presses the left mouse button.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveRightClick(int x, int y, bool playSound = true) { }

        /// <summary>The method invoked when the player presses a keyboard button.</summary>
        /// <param name="key">The key that was pressed.</param>
        public override void receiveKeyPress(Keys key) {
        }

        /// <summary>Update the menu state.</summary>
        /// <param name="time">The elapsed game time.</param>
        public override void update(GameTime time) {
            base.update(time);

            // update alert messages
            for (int i = this.Alerts.Count - 1; i >= 0; i--) {
                if (this.Alerts.ElementAt(i).Update(time))
                    this.Alerts.RemoveAt(i);
            }
        }

        /// <summary>The method called when the game window changes size.</summary>
        /// <param name="oldBounds">The former viewport.</param>
        /// <param name="newBounds">The new viewport.</param>
        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            this.updateLayout();
        }

        /// <summary>The method invoked when the cursor is over a given position.</summary>
        /// <param name="x">The X mouse position.</param>
        /// <param name="y">The Y mouse position.</param>
        public override void performHoverAction(int x, int y) {
            // reset hover text
            this.HoverText = "";

            // arrow buttons
            foreach (ClickableTextureComponent button in this.ArrowButtons) {
                button.scale = button.containsPoint(x, y)
                    ? Math.Min(button.scale + 0.02f, button.baseScale + 0.1f)
                    : Math.Max(button.scale - 0.02f, button.baseScale);
            }
            foreach (ClickableTextureComponent button in this.GenderButtons) {
                button.scale = button.containsPoint(x, y)
                    ? Math.Min(button.scale + 0.02f, button.baseScale + 0.1f)
                    : Math.Max(button.scale - 0.02f, button.baseScale);
            }

            // random button
            this.RandomButton.tryHover(x, y, 0.25f);
            this.RandomButton.tryHover(x, y, 0.25f);

            // OK button
            this.OkButton.scale = this.OkButton.containsPoint(x, y)
                ? Math.Min(this.OkButton.scale + 0.02f, this.OkButton.baseScale + 0.1f)
                : Math.Max(this.OkButton.scale - 0.02f, this.OkButton.baseScale);

            // quick favorites
            for (int i = 0; i < this.QuickLoadFavButtons.Length; i++) {
                this.QuickLoadFavButtons[i].tryHover(x, y, 0.25f);
                this.QuickLoadFavButtons[i].tryHover(x, y, 0.25f);
                if (this.QuickLoadFavButtons[i].containsPoint(x, y))
                    this.HoverText = m_farmerMakeup.m_config.HasFavSlot(i + 1) ? "" : "No Favorite Is Set";
            }
        }

        /// <summary>Draw the menu to the screen.</summary>
        /// <param name="spriteBatch">The sprite batch to which to draw.</param>
        public override void draw(SpriteBatch spriteBatch) {
            // header
            //SpriteText.drawString(spriteBatch, "Customize Character:", xPositionOnScreen + 55, yPositionOnScreen + 115);

            // portrait
            spriteBatch.Draw(Game1.daybg, new Vector2(
                xPositionOnScreen + Game1.tileSize + Game1.tileSize * 2 / 3 - 2,
                yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder - Game1.tileSize / 4
                ), Color.White);
            Game1.player.FarmerRenderer.draw(spriteBatch, Game1.player.FarmerSprite.CurrentAnimationFrame, Game1.player.FarmerSprite.CurrentFrame, Game1.player.FarmerSprite.SourceRect, new Vector2(
                xPositionOnScreen + Game1.tileSize + Game1.tileSize * 2 / 3 - 2 + Game1.tileSize / 2,
                yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder - Game1.tileSize / 4 + Game1.tileSize / 2),
                Vector2.Zero, 0.8f, Color.White, 0f, 1f, Game1.player);

            // gender buttons
            if (m_globalConfig.CanChangeGender) {
                foreach (ClickableTextureComponent textureComponent in this.GenderButtons) {
                    textureComponent.draw(spriteBatch);
                    if (textureComponent.name.Equals("Male") && Game1.player.isMale)
                        this.MaleOutlineButton.draw(spriteBatch);
                    if (textureComponent.name.Equals("Female") && !Game1.player.isMale)
                        this.FemaleOutlineButton.draw(spriteBatch);
                }
            }

            // favorite buttons
            foreach (ClickableTextureComponent button in this.QuickLoadFavButtons) {
                button.draw(spriteBatch);
            }
            // arrow buttons
            foreach (ClickableTextureComponent button in this.ArrowButtons) {
                button.draw(spriteBatch);
            }
            // labels
            foreach (ClickableComponent label in this.Labels) {
                Utility.drawTextWithShadow(spriteBatch, label.name, Game1.smallFont, new Vector2(label.bounds.X, label.bounds.Y), Game1.textColor);
            }
            // selector labels
            foreach (ClickableComponent label in this.SelectorLabels) {
                string text = "";
                Color color = Game1.textColor;
                switch (label.name) {
                    case "Shirt":
                        text = string.Concat(Game1.player.shirt + 1);
                        break;
                    case " Skin":
                        text = string.Concat(Game1.player.skin + 1);
                        break;
                    case " Hair":
                        text = string.Concat(Game1.player.hair + 1);
                        break;
                    case " Acc.":
                        text = string.Concat(m_farmerMakeup.AccIndexToLogic(Game1.player.accessory));
                        break;
                    case "Face Type":
                        text = string.Concat(m_farmerMakeup.m_config.ChosenFace[0] + 1);
                        break;
                    case "Nose Type":
                        text = string.Concat(m_farmerMakeup.m_config.ChosenNose[0] + 1);
                        break;
                    case " Bottoms ":
                        text = string.Concat(m_farmerMakeup.m_config.ChosenBottoms[0] + 1);
                        break;
                    case "Shoe Type":
                        text = string.Concat(m_farmerMakeup.m_config.ChosenShoes[0] + 1);
                        break;
                    case "Shoes":
                        if (m_farmerMakeup.m_config.ChosenShoeColor[0] == -1) {
                            text = "Boot";
                        } else {
                            text = string.Concat(m_farmerMakeup.m_config.ChosenShoeColor[0] + 1);
                        }
                        break;
                    default:
                        color = Game1.textColor;
                        break;
                }
                Utility.drawTextWithShadow(spriteBatch, label.name, Game1.smallFont, new Vector2(label.bounds.X, label.bounds.Y), color);
                float wordLength = Game1.smallFont.MeasureString(label.name.Length > 5 ? "Face Type" : "Skirt").X;
                float texeLength = Game1.smallFont.MeasureString(text).X;
                if (text.Length>3) {
                    texeLength = texeLength * 3 / 4;
                }
                Utility.drawTextWithShadow(spriteBatch, text, Game1.smallFont, new Vector2(label.bounds.X + wordLength / 2f + Game1.smallFont.MeasureString("00").X / 2 - texeLength, label.bounds.Y + Game1.tileSize / 2), color);
            }


            // buttons
            this.OkButton.draw(spriteBatch);
            this.RandomButton.draw(spriteBatch);
            this.MultiplayerButton.draw(spriteBatch);

            // color pickers
            this.HairColorPicker.draw(spriteBatch);
            this.PantsColorPicker.draw(spriteBatch);
            this.EyeColorPicker.draw(spriteBatch);

            // alerts
            foreach (Alert alert in this.Alerts) {
                alert.Draw(spriteBatch, Game1.smallFont);
            }

            // cursor
            if (this.HoverText.Equals("No Favorite Is Set")) {
                spriteBatch.Draw(m_contentHelper.m_menuTextures, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()), Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 6, 16, 16), Color.White, 0f, Vector2.Zero, Game1.pixelZoom + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
                IClickableMenu.drawHoverText(spriteBatch, this.HoverText, Game1.smallFont, 20, 20);
            } else {
                spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()), Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 0, 16, 16), Color.White, 0f, Vector2.Zero, Game1.pixelZoom + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
                //IClickableMenu.drawHoverText(spriteBatch, this.HoverText, Game1.smallFont);
            }
        }

        /// <summary>Update the menu layout for a change in the zoom level or viewport size.</summary>
        public override void updateLayout() {
            // reset window position
            this.xPositionOnScreen = Game1.viewport.Width / 2 - (680 + IClickableMenu.borderWidth * 2) / 2;
            this.yPositionOnScreen = Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2 - Game1.tileSize;
            int xBase = xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearSideBorder;
            int yBase = yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder;

            Texture2D menuTextures = m_contentHelper.m_menuTextures;
            this.Labels.Clear();
            this.SelectorLabels.Clear();
            this.ArrowButtons.Clear();

            // random button
            this.RandomButton = new ClickableTextureComponent(new Rectangle(
                xBase,
                yBase - Game1.tileSize/4,
                Game1.pixelZoom * 10, Game1.pixelZoom * 10), Game1.mouseCursors, new Rectangle(381, 361, 10, 10), Game1.pixelZoom);
            // direction buttons
            {
                int xOffset = xBase + Game1.tileSize / 4;
                int yOffset = yBase + Game1.tileSize * 2;
                this.ArrowButtons.Add(new ClickableTextureComponent("Direction", new Rectangle(xOffset, yOffset, Game1.tileSize, Game1.tileSize), "", "-1", Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 44), 1f));
                this.ArrowButtons.Add(new ClickableTextureComponent("Direction", new Rectangle(xOffset + Game1.tileSize * 2, yOffset, Game1.tileSize, Game1.tileSize), "", "1", Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 33), 1f));
            }
            // gender buttons
            if (m_globalConfig.CanChangeGender) {
                int scale = Game1.pixelZoom / 2;
                this.GenderButtons = new[] {
                    new ClickableTextureComponent("Male", new Rectangle(
                        xBase + Game1.tileSize * 3,
                        yBase - Game1.tileSize/4,
                        Game1.tileSize, Game1.tileSize), null, "Male", Game1.mouseCursors, new Rectangle(128, 192, 16, 16), scale),
                    new ClickableTextureComponent("Female", new Rectangle(
                        xBase + Game1.tileSize * 3,
                        yBase - Game1.tileSize/4 + Game1.tileSize,
                        Game1.tileSize, Game1.tileSize), null, "Female", Game1.mouseCursors, new Rectangle(144, 192, 16, 16), scale)
                };
                this.MaleOutlineButton = new ClickableTextureComponent("", new Rectangle(
                    xBase + Game1.tileSize * 3 - 3,
                    yBase - Game1.tileSize / 4 - 3,
                    Game1.tileSize, Game1.tileSize), "", "", menuTextures, new Rectangle(19, 38, 19, 19), scale);
                this.FemaleOutlineButton = new ClickableTextureComponent("", new Rectangle(
                    xBase + Game1.tileSize * 3 - 3,
                    yBase - Game1.tileSize / 4 + Game1.tileSize - 3,
                    Game1.tileSize, Game1.tileSize), "", "", menuTextures, new Rectangle(19, 38, 19, 19), scale);
            }
            // color pickers
            {
                // eye color
                int xOffset = xBase + Game1.tileSize * 4 + Game1.tileSize / 4;
                int xPickerOffset = Game1.tileSize * 3 - Game1.tileSize / 4;
                int yOffset = yBase - Game1.tileSize / 4;
                int yLabelOffset = 16;
                this.Labels.Add(new ClickableComponent(new Rectangle(xOffset, yOffset + yLabelOffset, 1, 1), "Eye Color:"));
                this.EyeColorPicker = new ColorPicker(xOffset + xPickerOffset, yOffset);
                this.EyeColorPicker.setColor(Game1.player.newEyeColor);
                // hair color
                yOffset += Game1.tileSize + 8;
                this.Labels.Add(new ClickableComponent(new Rectangle(xOffset, yOffset + yLabelOffset, 1, 1), "Hair Color:"));
                this.HairColorPicker = new ColorPicker(xOffset + xPickerOffset, yOffset);
                this.HairColorPicker.setColor(Game1.player.hairstyleColor);
                // pants color
                yOffset += Game1.tileSize + 8;
                this.Labels.Add(new ClickableComponent(new Rectangle(xOffset, yOffset + yLabelOffset, 1, 1), "Pants Color:"));
                this.PantsColorPicker = new ColorPicker(xOffset + xPickerOffset, yOffset);
                this.PantsColorPicker.setColor(Game1.player.pantsColor);
            }
            // type selectors
            {
                int xOffset = Game1.tileSize / 4;
                int xGap = Game1.tileSize * 4 - Game1.tileSize / 4;
                int[] xSelectorOffset = new int[] { Game1.tileSize / 4, Game1.tileSize, Game1.tileSize * 2 };
                int[] xSelectorRightOffset = new int[] { Game1.tileSize / 4, Game1.tileSize + 8, Game1.tileSize * 3 };
                int yOffset = Game1.tileSize * 3 + Game1.tileSize / 4;
                selectorLayout(xBase + xOffset, xSelectorOffset, yBase + yOffset, " Hair");
                this.MultiplayerButton = new ClickableTextureComponent("Multiplayer", new Rectangle(
                    xBase + xGap + xSelectorRightOffset[0],
                    yBase + yOffset + Game1.tileSize / 4,
                    36, 36), "", "", Game1.mouseCursors, new Rectangle(m_farmerMakeup.m_config.MutiplayerFix?236:227, 425, 9, 9), 4f, false);
                this.Labels.Add(new ClickableComponent(new Rectangle(xBase + xGap + xSelectorRightOffset[1], yBase + yOffset + Game1.tileSize / 4, 1, 1), "Multiplayer Fix"));

                yOffset += Game1.tileSize + 4;
                selectorLayout(xBase + xOffset, xSelectorOffset, yBase + yOffset, " Skin");
                selectorLayout(xBase + xGap, xSelectorRightOffset, yBase + yOffset, "Face Type");
                yOffset += Game1.tileSize + 4;
                selectorLayout(xBase + xOffset, xSelectorOffset, yBase + yOffset, "Shirt");
                selectorLayout(xBase + xGap, xSelectorRightOffset, yBase + yOffset, "Nose Type");
                yOffset += Game1.tileSize + 4;
                selectorLayout(xBase + xOffset, xSelectorOffset, yBase + yOffset, "Shoes");
                selectorLayout(xBase + xGap, xSelectorRightOffset, yBase + yOffset, " Bottoms ");
                yOffset += Game1.tileSize + 4;
                selectorLayout(xBase + xOffset, xSelectorOffset, yBase + yOffset, " Acc.");
                selectorLayout(xBase + xGap, xSelectorRightOffset, yBase + yOffset, "Shoe Type");
            }
            // quick favorite star buttons
            {
                int yOffset = yBase + Game1.tileSize * 3 + Game1.tileSize / 4; // yBase + Game1.tileSize * 4 - 25/2;
                // text above quick favorite buttons
                this.Labels.Add(new ClickableComponent(new Rectangle(
                    xBase + 296 + Game1.tileSize / 4 + Game1.tileSize * 4,
                    yOffset,
                    1, 1), "Load"));
                this.Labels.Add(new ClickableComponent(new Rectangle(
                    xBase + 279 + Game1.tileSize / 4 + Game1.tileSize * 4,
                    yOffset + 25,
                    1, 1), "Favorite"));

                int xOffset = this.xPositionOnScreen + Game1.pixelZoom * 12 + 565;
                yOffset += 75;
                int size = Game1.pixelZoom * 10;
                int zoom = Game1.pixelZoom;
                int y1 = m_farmerMakeup.m_config.HasFavSlot(1) ? 26 : 67;
                int y2 = m_farmerMakeup.m_config.HasFavSlot(2) ? 26 : 67;
                int y3 = m_farmerMakeup.m_config.HasFavSlot(3) ? 26 : 67;
                int y4 = m_farmerMakeup.m_config.HasFavSlot(4) ? 26 : 67;
                int y5 = m_farmerMakeup.m_config.HasFavSlot(5) ? 26 : 67;
                int y6 = m_farmerMakeup.m_config.HasFavSlot(6) ? 26 : 67;
                this.QuickLoadFavButtons = new[] {
                    new ClickableTextureComponent(new Rectangle(xOffset +  0, yOffset +   0, size, size), menuTextures, new Rectangle(24, y1, 8, 8), zoom),
                    new ClickableTextureComponent(new Rectangle(xOffset +  0, yOffset + Game1.tileSize + 4, size, size), menuTextures, new Rectangle(8, y2, 8, 8), zoom),
                    new ClickableTextureComponent(new Rectangle(xOffset +  0, yOffset + (Game1.tileSize + 4)*2, size, size), menuTextures, new Rectangle(0, y3, 8, 8), zoom),
                    new ClickableTextureComponent(new Rectangle(xOffset + 45, yOffset +   0, size, size), menuTextures, new Rectangle(24, y4, 8, 8), zoom),
                    new ClickableTextureComponent(new Rectangle(xOffset + 45, yOffset +  Game1.tileSize + 4, size, size), menuTextures, new Rectangle(8, y5, 8, 8), zoom),
                    new ClickableTextureComponent(new Rectangle(xOffset + 45, yOffset + (Game1.tileSize + 4)*2, size, size), menuTextures, new Rectangle(0, y6, 8, 8), zoom)
                };
                this.OkButton = new ClickableTextureComponent("OK", new Rectangle(
                    xOffset,
                    yOffset + (Game1.tileSize + 4) * 3 - Game1.tileSize/4,
                    Game1.tileSize, Game1.tileSize), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), 1f);
            }
        }

        public override void onSwitchBack() {
            this.Alerts.Clear();

            base.onSwitchBack();
        }

        /*********
        ** Private methods
        *********/
        private void selectorLayout(int xBase, int[] selectorOffset, int y, string name) {
            this.ArrowButtons.Add(new ClickableTextureComponent(name, new Rectangle(xBase + selectorOffset[0], y, Game1.tileSize, Game1.tileSize), "", "-1", Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 44), 0.75f));
            this.SelectorLabels.Add(new ClickableComponent(new Rectangle(xBase + selectorOffset[1], y, 1, 1), name));
            this.ArrowButtons.Add(new ClickableTextureComponent(name, new Rectangle(xBase + selectorOffset[2], y, Game1.tileSize, Game1.tileSize), "", "1", Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 33), 0.75f));
        }
        /// <summary>Randomise the character attributes.</summary>
        private void RandomiseCharacter() {
            // play sound
            string cueName = "drumkit6";
            string[] sounds = new string[] { "drumkit1", "dirtyHit" , "axchop", "hoeHit", "fishSlap", "drumkit6" , "drumkit5", "drumkit6", "junimoMeep1", "coin", "axe", "hammer", "drumkit2", "drumkit4", "drumkit3" };
            if (this.TimesRandomised > 0) {
                cueName = sounds[Game1.random.Next(15)];
            }
            Game1.playSound(cueName);
            this.TimesRandomised++;

            m_farmerMakeup.Randomize();
        }

        /// <summary>Perform the action associated with a button.</summary>
        /// <param name="name">The button name.</param>
        private void HandleButtonAction(string name) {
            switch (name) {
                case "Male":
                    m_farmerMakeup.ChangeGender(true);
                    m_farmerMakeup.ChangeHairStyle(0);
                    break;

                case "Female":
                    m_farmerMakeup.ChangeGender(false);
                    m_farmerMakeup.ChangeHairStyle(16);
                    break;

                case "OK":
                    if (m_parent != null) {
                        m_parent.exitThisMenuNoSound();
                    } else {
                        exitThisMenuNoSound();
                    }
                    break;
            }
            Game1.playSound("coin");
        }

        /// <summary>Perform the action associated with a selector.</summary>
        /// <param name="name">The selector name.</param>
        /// <param name="change">The change value.</param>
        private void HandleSelectorChange(string name, int change) {
            switch (name) {
                case " Skin":
                    m_farmerMakeup.ChangeSkinColor(Game1.player.skin + change);
                    Game1.playSound("skeletonStep");
                    return;

                case " Hair":
                    m_farmerMakeup.ChangeHairStyle(Game1.player.hair + change);
                    Game1.playSound("grassyStep");
                    return;

                case "Shirt":
                    m_farmerMakeup.ChangeShirt(Game1.player.shirt + change);
                    Game1.playSound("coin");
                    return;

                case " Acc.":
                    m_farmerMakeup.ChangeAccessory(m_farmerMakeup.AccIndexToLogic(Game1.player.accessory) + change);
                    Game1.playSound("purchase");
                    return;

                case "Face Type":
                    m_farmerMakeup.ChangeFace(m_farmerMakeup.m_config.ChosenFace[0] + change);
                    Game1.playSound("skeletonStep");
                    return;

                case "Nose Type":
                    m_farmerMakeup.ChangeNose(m_farmerMakeup.m_config.ChosenNose[0] + change);
                    Game1.playSound("grassyStep");
                    return;

                case " Bottoms ":
                    m_farmerMakeup.ChangeBottoms(m_farmerMakeup.m_config.ChosenBottoms[0] + change);
                    Game1.playSound("coin");
                    return;

                case "Shoe Type":
                    m_farmerMakeup.ChangeShoes(m_farmerMakeup.m_config.ChosenShoes[0] + change);
                    Game1.playSound("purchase");
                    return;

                case "Shoes":
                    m_farmerMakeup.ChangeShoeColor(m_farmerMakeup.m_config.ChosenShoeColor[0] + change);
                    Game1.playSound("axe");
                    return;

                case "Direction":
                    Game1.player.faceDirection((Game1.player.facingDirection - change + 4) % 4);
                    Game1.player.FarmerSprite.StopAnimation();
                    Game1.player.completelyStopAnimatingOrDoingAction();
                    Game1.playSound("pickUpItem");
                    break;

                case "Direction2":
                    Game1.player.faceDirection((Game1.player.facingDirection - change + 4) % 4);
                    Game1.player.FarmerSprite.StopAnimation();
                    Game1.player.completelyStopAnimatingOrDoingAction();
                    Game1.playSound("pickUpItem");
                    break;
            }
        }

        /// <summary>Exit the menu.</summary>
        public void exit() {
            //m_env.Monitor.Log("customsize config saved");
            m_farmerMakeup.m_config.save();
        }
    }
}
