using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewModdingAPI;
using Kisekae.Config;

namespace Kisekae.Framework {
    class MenuFavoritesExtras : ITabMenu {
        /*********
        ** Properties
        *********/
        /// <summary>Global Mod Interface.</summary>
        private readonly IMod m_env;
        /// <summary>Encapsulates the underlying mod texture management.</summary>
        private readonly ContentHelper ContentHelper;
        /// <summary>The global config settings.</summary>
        private readonly GlobalConfig GlobalConfig;
        /// <summary>Core component to manipulate player appearance.</summary>
        private readonly FarmerMakeup m_farmerMakeup;

        /// <summary>The messages to display on the screen.</summary>
        private readonly List<Alert> Alerts = new List<Alert>();
        /// <summary>The field labels.</summary>
        private readonly List<ClickableComponent> Labels = new List<ClickableComponent>();
        /// <summary>The additional favorite icons in the extended 'manage favourites' submenu.</summary>
        private readonly List<ClickableTextureComponent> ExtraFavButtons = new List<ClickableTextureComponent>();
        /// <summary>The 'load' button on the 'manage favorites' extended subtab when a favorite is selected.</summary>
        private ClickableTextureComponent LoadFavButton;
        /// <summary>The 'save' button on the 'manage favorites' extended subtab when a favorite is selected.</summary>
        private ClickableTextureComponent SaveFavButton;

        /// <summary>The current selected favorite to load or save (or <c>-1</c> if none selected).</summary>
        private int CurrentFav = -1;

        /*********
        ** Public methods
        *********/
        public MenuFavoritesExtras(IMod env, ContentHelper contentHelper, GlobalConfig globalConfig, FarmerMakeup makeup) : base(
                  x: Game1.viewport.Width / 2 - (680 + IClickableMenu.borderWidth * 2) / 2,
                  y: Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2 - Game1.tileSize,
                  width: 632 + IClickableMenu.borderWidth * 2,
                  height: 500 + IClickableMenu.borderWidth * 4 + Game1.tileSize
            ) {
            m_env = env;
            m_farmerMakeup = makeup;
            this.ContentHelper = contentHelper;
            this.GlobalConfig = globalConfig;
            updateLayout();
        }

        /// <summary>The method invoked when the player presses the left mouse button.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveLeftClick(int x, int y, bool playSound = true) {
            // save favorite button
            if (this.SaveFavButton.containsPoint(x, y) && this.CurrentFav > -1) {
                // set favorites
                m_farmerMakeup.SaveFavorite(this.CurrentFav + 1);

                // show 'favorite saved' alert
                this.ExtraFavButtons[this.CurrentFav - 6].sourceRect.Y = 26;
                this.Alerts.Add(new Alert(Game1.mouseCursors, new Rectangle(310, 392, 16, 16), Game1.viewport.Width / 2 - (700 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2, "Favorite Saved To Slot " + (this.CurrentFav + 1) + " .", 1200, false));
                Game1.playSound("purchase");
                return;
            }

            // load favorite button
            if (this.LoadFavButton.containsPoint(x, y) && this.CurrentFav > -1) {
                if (m_farmerMakeup.LoadFavorite(this.CurrentFav + 1)) {
                    Game1.playSound("yoba");
                } else {
                    this.Alerts.Add(new Alert(Game1.mouseCursors, new Rectangle(268, 470, 16, 16), Game1.viewport.Width / 2 - (700 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2, "Uh oh! No Favorite is Set!", 1000, false));
                    //this.UpdateTabFloaters();
                }
                return;
            }

            // extra favorite icons
            for (int i = 0; i < this.ExtraFavButtons.Count; i++) {
                if (this.ExtraFavButtons[i].containsPoint(x, y)) {
                    foreach (ClickableTextureComponent bigFavButton in this.ExtraFavButtons)
                        bigFavButton.drawShadow = false;
                    this.ExtraFavButtons[i].drawShadow = true;
                    this.CurrentFav = i + 6;
                    break;
                }
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
            this.LoadFavButton.tryHover(x, y, 0.25f);
            this.LoadFavButton.tryHover(x, y, 0.25f);

            this.SaveFavButton.tryHover(x, y, 0.25f);
            this.SaveFavButton.tryHover(x, y, 0.25f);
        }

        /// <summary>Draw the menu to the screen.</summary>
        /// <param name="spriteBatch">The sprite batch to which to draw.</param>
        public override void draw(SpriteBatch spriteBatch) {
            //spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);

            // menu background
            //Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width + 50, height, false, true);

            // header
            //SpriteText.drawString(spriteBatch, "Manage Favorites:", xPositionOnScreen + 55, yPositionOnScreen + 115);

            int yOffset = yPositionOnScreen +IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder - Game1.tileSize / 4;
            // portrait
            spriteBatch.Draw(Game1.daybg, new Vector2(xPositionOnScreen + 430 + Game1.tileSize + Game1.tileSize * 2 / 3 - 2, yOffset + Game1.tileSize / 2), Color.White);
            Game1.player.FarmerRenderer.draw(spriteBatch, Game1.player.FarmerSprite.CurrentAnimationFrame, Game1.player.FarmerSprite.CurrentFrame, Game1.player.FarmerSprite.SourceRect, new Vector2(xPositionOnScreen + 428 + Game1.tileSize * 2 / 3 + Game1.tileSize * 2 - Game1.tileSize / 2, yOffset + Game1.tileSize / 2  + Game1.tileSize / 2), Vector2.Zero, 0.8f, Color.White, 0f, 1f, Game1.player);

            // labels
            Utility.drawTextWithShadow(spriteBatch, "You can set up to 30 additional favorite appearance", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "configurations for each character.", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 25), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "Your current appearance is shown on", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 110), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "the right, select a favorite below to", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 135), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "save your appearance in it or load the", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 160), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "appearance saved in it :", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 185), Color.Black);

            // favorite icons
            foreach (ClickableTextureComponent icon in this.ExtraFavButtons)
                icon.draw(spriteBatch);

            if (this.CurrentFav > -1) {
                string label = "Currently selected: " + (this.CurrentFav + 1) + "."; // <-- String for printing currently selected favorite
                Utility.drawTextWithShadow(spriteBatch, label, Game1.smallFont, new Vector2(xPositionOnScreen + 140, yOffset + 285), Color.Black);
                this.SaveFavButton.draw(spriteBatch);

                Utility.drawTextWithShadow(spriteBatch, "Overwrite Fav. Slot", Game1.smallFont, new Vector2(xPositionOnScreen + 140, yOffset + 335), Color.Black);
                this.LoadFavButton.draw(spriteBatch);
            } else {
                string whatever = "Please select a favorite...";
                Utility.drawTextWithShadow(spriteBatch, whatever, Game1.smallFont, new Vector2(xPositionOnScreen + 140, yOffset + 285), Color.Black);
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

            // remove current components
            this.Labels.Clear();
            this.ExtraFavButtons.Clear();

            // 'manage favorites' extra outfits buttons
            {
                int xOffset = this.xPositionOnScreen + 80 + Game1.pixelZoom * 12;
                //int yOffset = this.yPositionOnScreen + Game1.tileSize + Game1.pixelZoom * 14;
                int yOffset = yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder - Game1.tileSize / 4 + 385;

                this.LoadFavButton = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + 475 + Game1.pixelZoom * 12, yOffset - 100, Game1.pixelZoom * 20, Game1.pixelZoom * 10), menuTextures, new Rectangle(0, 207, 26, 11), 3f);
                this.SaveFavButton = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + 475 + Game1.pixelZoom * 12, yOffset - 50, Game1.pixelZoom * 20, Game1.pixelZoom * 10), menuTextures, new Rectangle(0, 193, 26, 11), 3f);

                int size = Game1.pixelZoom * 10;
                const float zoom = 4.5f;
                for (int i = 0; i < 10; i++) {
                    int y = m_farmerMakeup.m_config.HasFavSlot(i + 7) ? 26 : 67;
                    this.ExtraFavButtons.Add(new ClickableTextureComponent(new Rectangle(xOffset + i * 50, yOffset, size, size), menuTextures, new Rectangle(0, y, 8, 8), zoom));
                }
                for (int i = 0; i < 10; i++) {
                    int y = m_farmerMakeup.m_config.HasFavSlot(i + 17) ? 26 : 67;
                    this.ExtraFavButtons.Add(new ClickableTextureComponent(new Rectangle(xOffset + i * 50, yOffset + 50, size, size), menuTextures, new Rectangle(0, y, 8, 8), zoom));
                }
                for (int i = 0; i < 10; i++) {
                    int y = m_farmerMakeup.m_config.HasFavSlot(i + 27) ? 26 : 67;
                    this.ExtraFavButtons.Add(new ClickableTextureComponent(new Rectangle(xOffset + i * 50, yOffset + 100, size, size), menuTextures, new Rectangle(0, y, 8, 8), zoom));
                }
            }
        }

        public override void onSwitchBack() {
            this.Alerts.Clear();
            this.CurrentFav = -1;
            foreach (ClickableTextureComponent extraFavButton in this.ExtraFavButtons) {
                extraFavButton.drawShadow = false;
            }

            base.onSwitchBack();
        }
        /*********
        ** Private methods
        *********/
    }
}
