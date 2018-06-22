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
    internal class MenuAbout : ITabMenu {
        /*********
        ** Properties
        *********/
        /// <summary>Global Mod Interface.</summary>
        private readonly IMod m_env;
        /// <summary>Encapsulates the underlying mod texture management.</summary>
        private readonly ContentHelper ContentHelper;
        /// <summary>The global config settings.</summary>
        private readonly GlobalConfig GlobalConfig;

        /// <summary>The 'set' button to change the hotkey.</summary>
        private ClickableTextureComponent SetAccessKeyButton;
        /// <summary>The 'set' button which toggles whether skirts are shown for male characters.</summary>
        private ClickableTextureComponent ToggleMaleSkirtsButton;
        /// <summary>The 'set' button which toggles whether gender can be changed.</summary>
        private ClickableTextureComponent CanChangeGenderButton;
        /// <summary>The button which zooms out the menu.</summary>
        private ClickableTextureComponent ZoomOutButton;
        /// <summary>The button which zooms im the menu.</summary>
        private ClickableTextureComponent ZoomInButton;
        /// <summary>The button which reset global settings to their default.</summary>
        private ClickableTextureComponent ResetConfigButton;
        /// <summary>The messages to display on the screen.</summary>
        private readonly List<Alert> Alerts = new List<Alert>();

        /// <summary>Whether the player is currently setting the menu key via <see cref="SetAccessKeyButton"/>.</summary>
        private bool IsSettingAccessMenuKey;


        /*********
        ** Public methods
        *********/
        public MenuAbout(IMod env, ContentHelper contentHelper, GlobalConfig globalConfig) : base(
                  x: Game1.viewport.Width / 2 - (680 + IClickableMenu.borderWidth * 2) / 2,
                  y: Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2 - Game1.tileSize,
                  width: 632 + IClickableMenu.borderWidth * 2,
                  height: 500 + IClickableMenu.borderWidth * 4 + Game1.tileSize
            ) {
            m_env = env;
            this.ContentHelper = contentHelper;
            this.GlobalConfig = globalConfig;
            updateLayout();
        }

        /// <summary>The method invoked when the player presses the left mouse button.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveLeftClick(int x, int y, bool playSound = true) {
            Texture2D menuTextures = this.ContentHelper.m_menuTextures;

            // set access button
            if (this.SetAccessKeyButton.containsPoint(x, y)) {
                this.IsSettingAccessMenuKey = true;
                Game1.playSound("breathin");
                return;
            }

            // toggle male skirts button
            if (this.ToggleMaleSkirtsButton.containsPoint(x, y)) {
                this.GlobalConfig.HideMaleSkirts = !this.GlobalConfig.HideMaleSkirts;
                m_env.Helper.WriteConfig(this.GlobalConfig);
                this.Alerts.Add(new Alert(menuTextures, new Rectangle(this.GlobalConfig.HideMaleSkirts ? 48 : 80, 144, 16, 16), Game1.viewport.Width / 2 - (700 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2, "Skirts " + (this.GlobalConfig.HideMaleSkirts ? "Hidden" : "Unhidden") + " for Males.", 1200, false));
                Game1.playSound("coin");
                return;
            }

            // can change gender button
            if (this.CanChangeGenderButton.containsPoint(x, y)) {
                this.GlobalConfig.CanChangeGender = !this.GlobalConfig.CanChangeGender;
                m_env.Helper.WriteConfig(this.GlobalConfig);
                this.Alerts.Add(new Alert(menuTextures, new Rectangle(!this.GlobalConfig.CanChangeGender ? 48 : 80, 144, 16, 16), Game1.viewport.Width / 2 - (700 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2, (this.GlobalConfig.CanChangeGender ? "Enable" : "Disable") + " gender change.", 1200, false));
                Game1.playSound("axe");
                return;
            }

            // zoom in button
            if (this.ZoomInButton.containsPoint(x, y) && this.GlobalConfig.MenuZoomOut) {
                Game1.options.zoomLevel = 1f;
                Game1.overrideGameMenuReset = true;
                Game1.game1.refreshWindowSettings();

                this.updateLayout();

                this.GlobalConfig.MenuZoomOut = false;
                m_env.Helper.WriteConfig(this.GlobalConfig);

                this.ZoomInButton.sourceRect.Y = 177;
                this.ZoomOutButton.sourceRect.Y = 167;

                Game1.playSound("drumkit6");
                this.Alerts.Add(new Alert(menuTextures, new Rectangle(80, 144, 16, 16), Game1.viewport.Width / 2 - (700 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2, "Zoom Setting Changed.", 1200, false, 200));
                return;
            }

            // zoom out button
            if (this.ZoomOutButton.containsPoint(x, y) && !this.GlobalConfig.MenuZoomOut) {
                Game1.options.zoomLevel = 0.75f;
                Game1.overrideGameMenuReset = true;
                Game1.game1.refreshWindowSettings();

                this.updateLayout();

                this.GlobalConfig.MenuZoomOut = true;
                m_env.Helper.WriteConfig(this.GlobalConfig);

                this.ZoomInButton.sourceRect.Y = 167;
                this.ZoomOutButton.sourceRect.Y = 177;

                Game1.playSound("coin");
                this.Alerts.Add(new Alert(menuTextures, new Rectangle(80, 144, 16, 16), Game1.viewport.Width / 2 - (700 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2, "Zoom Setting Changed.", 1200, false, 200));
                return;
            }

            // reset config button
            if (this.ResetConfigButton.containsPoint(x, y)) {
                this.GlobalConfig.HideMaleSkirts = false;
                this.GlobalConfig.MenuAccessKey = SButton.C;
                Game1.options.zoomLevel = 1f;
                Game1.overrideGameMenuReset = true;
                Game1.game1.refreshWindowSettings();
                this.updateLayout();
                this.GlobalConfig.MenuZoomOut = false;
                this.GlobalConfig.CanChangeGender = false;
                m_env.Helper.WriteConfig(this.GlobalConfig);
                this.Alerts.Add(new Alert(menuTextures, new Rectangle(160, 144, 16, 16), Game1.viewport.Width / 2 - (700 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2 - Game1.tileSize, "Options Reset to Default", 1200, false, 200));
                Game1.playSound("coin");
            }
        }

        /// <summary>The method invoked when the player releases the left mouse button.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        public override void releaseLeftClick(int x, int y) { }

        /// <summary>The method invoked when the player presses the left mouse button.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveRightClick(int x, int y, bool playSound = true) { }

        /// <summary>The method invoked when the player presses a keyboard button.</summary>
        /// <param name="key">The key that was pressed.</param>
        public override void receiveKeyPress(Keys key) {
            if (this.IsSettingAccessMenuKey) { // set key
                this.GlobalConfig.MenuAccessKey = (SButton)key;
                m_env.Helper.WriteConfig(this.GlobalConfig);
                this.IsSettingAccessMenuKey = false;
                this.Alerts.Add(new Alert(this.ContentHelper.m_menuTextures, new Rectangle(96, 144, 16, 16), Game1.viewport.Width / 2 - (700 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2, "Menu Access Key Changed.", 1200, false));
                Game1.playSound("coin");
            }
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
            this.SetAccessKeyButton.tryHover(x, y, 0.25f);
            this.SetAccessKeyButton.tryHover(x, y, 0.25f);

            this.ToggleMaleSkirtsButton.tryHover(x, y, 0.25f);
            this.ToggleMaleSkirtsButton.tryHover(x, y, 0.25f);

            this.ResetConfigButton.tryHover(x, y, 0.25f);
            this.ResetConfigButton.tryHover(x, y, 0.25f);

            if (this.GlobalConfig.MenuZoomOut) {
                this.ZoomInButton.tryHover(x, y, 0.25f);
                this.ZoomInButton.tryHover(x, y, 0.25f);
            } else {
                this.ZoomOutButton.tryHover(x, y, 0.25f);
                this.ZoomOutButton.tryHover(x, y, 0.25f);
            }
        }

        /// <summary>Draw the menu to the screen.</summary>
        /// <param name="spriteBatch">The sprite batch to which to draw.</param>
        public override void draw(SpriteBatch spriteBatch) {
            //spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);

            // menu background
            //Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width + 50, height, false, true);

            // header
            //SpriteText.drawString(spriteBatch, "About This Mod:", xPositionOnScreen + 55, yPositionOnScreen + 115);

            // info
            int yOffset = this.yPositionOnScreen + 100;
            SpriteText.drawString(spriteBatch, "Kisekae", xPositionOnScreen + 55, yOffset);
            //Utility.drawTextWithShadow(spriteBatch, "Kisekae", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset), Color.Black);
            yOffset += 50;
            Utility.drawTextWithShadow(spriteBatch, "A modified version of Get Dressed to work with SDV 1.3", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, $"You are using version:  {m_env.ModManifest.Version}", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 40), Color.Black);
            SpriteText.drawString(spriteBatch, "Settings:", xPositionOnScreen + 55, yOffset + 80);
            Utility.drawTextWithShadow(spriteBatch, "Face Types (M-F): " + this.GlobalConfig.MaleFaceTypes + "-" + this.GlobalConfig.FemaleFaceTypes, Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 150), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "Nose Types (M-F): " + this.GlobalConfig.MaleNoseTypes + "-" + this.GlobalConfig.FemaleNoseTypes, Game1.smallFont, new Vector2(xPositionOnScreen + 400, yOffset + 150), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "Bottoms Types (M-F): " + (this.GlobalConfig.HideMaleSkirts ? 2 : this.GlobalConfig.MaleBottomsTypes) + "-" + this.GlobalConfig.FemaleBottomsTypes, Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 200), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "Shoes Types (M-F): " + this.GlobalConfig.MaleShoeTypes + "-" + this.GlobalConfig.FemaleShoeTypes, Game1.smallFont, new Vector2(xPositionOnScreen + 400, yOffset + 200), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "Show Dresser: " + this.GlobalConfig.ShowDresser, Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 250), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "Stove in Corner: " + this.GlobalConfig.StoveInCorner, Game1.smallFont, new Vector2(xPositionOnScreen + 400, yOffset + 250), Color.Black);
            // set menu access key
            Utility.drawTextWithShadow(spriteBatch, "Open Menu Key:  " + this.GlobalConfig.MenuAccessKey, Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 300), Color.Black);
            this.SetAccessKeyButton.draw(spriteBatch);
            // toggle skirs for male characters
            Utility.drawTextWithShadow(spriteBatch, "Toggle Skirts for Male Characters  ", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 350), Color.Black);
            this.ToggleMaleSkirtsButton.draw(spriteBatch);
            // set gender change
            Utility.drawTextWithShadow(spriteBatch, "Can Change Gender ", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 400), Color.Black);
            this.CanChangeGenderButton.draw(spriteBatch);
            // set zoom level
            Utility.drawTextWithShadow(spriteBatch, "Change Zoom Level  ", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 450), Color.Black);
            this.ZoomOutButton.draw(spriteBatch);
            this.ZoomInButton.draw(spriteBatch);
            // reset config options
            Utility.drawTextWithShadow(spriteBatch, "Reset Options to Default  ", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 500), Color.Black);
            this.ResetConfigButton.draw(spriteBatch);

            // set menu access key overlay
            if (this.IsSettingAccessMenuKey) {
                spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.90f);
                spriteBatch.DrawString(Game1.dialogueFont, "Press new key...", new Vector2(xPositionOnScreen + 225, yPositionOnScreen + 290), Color.White);
            }

            // alerts
            foreach (Alert alert in this.Alerts) {
                alert.Draw(spriteBatch, Game1.smallFont);
            }

            // cursor
            spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()), Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 0, 16, 16), Color.White, 0f, Vector2.Zero, Game1.pixelZoom + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
        }

        /// <summary>Update the menu layout for a change in the zoom level or viewport size.</summary>
        public override void updateLayout() {
            // reset window position
            this.xPositionOnScreen = Game1.viewport.Width / 2 - (680 + IClickableMenu.borderWidth * 2) / 2;
            this.yPositionOnScreen = Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2 - Game1.tileSize;

            Texture2D menuTextures = this.ContentHelper.m_menuTextures;

            // about menu
            this.SetAccessKeyButton = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + 610, this.yPositionOnScreen + 450, Game1.pixelZoom * 15, Game1.pixelZoom * 10), Game1.mouseCursors, new Rectangle(294, 428, 21, 11), 3f);
            this.ToggleMaleSkirtsButton = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + 610, this.yPositionOnScreen + 500, Game1.pixelZoom * 15, Game1.pixelZoom * 10), Game1.mouseCursors, new Rectangle(294, 428, 21, 11), 3f);
            this.CanChangeGenderButton = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + 610, this.yPositionOnScreen + 550, Game1.pixelZoom * 15, Game1.pixelZoom * 10), Game1.mouseCursors, new Rectangle(294, 428, 21, 11), 3f);
            this.ZoomOutButton = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + 610, this.yPositionOnScreen + 600, Game1.pixelZoom * 10, Game1.pixelZoom * 10), menuTextures, new Rectangle(0, this.GlobalConfig.MenuZoomOut ? 177 : 167, 7, 9), 3f);
            this.ZoomInButton = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + 650, this.yPositionOnScreen + 600, Game1.pixelZoom * 10, Game1.pixelZoom * 10), menuTextures, new Rectangle(10, this.GlobalConfig.MenuZoomOut ? 167 : 177, 7, 9), 3f);
            this.ResetConfigButton = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + 610, this.yPositionOnScreen + 650, Game1.pixelZoom * 15, Game1.pixelZoom * 10), Game1.mouseCursors, new Rectangle(294, 428, 21, 11), 3f);
        }

        public override void onSwitchBack() {
            this.Alerts.Clear();

            base.onSwitchBack();
        }
    }
}
