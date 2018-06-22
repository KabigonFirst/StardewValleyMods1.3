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
    internal class MenuFavorites : ITabMenu {
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

        private readonly IClickableMenu m_parent;

        /// <summary>The messages to display on the screen.</summary>
        private readonly List<Alert> Alerts = new List<Alert>();
        /// <summary>The field labels.</summary>
        private readonly List<ClickableComponent> Labels = new List<ClickableComponent>();
        /// <summary>The 'set' buttons on the 'manage favorites' submenu.</summary>
        private ClickableTextureComponent[] SetFavButtons = new ClickableTextureComponent[0];

        /*********
        ** Public methods
        *********/
        public MenuFavorites(IMod env, ContentHelper contentHelper, GlobalConfig globalConfig, FarmerMakeup makeup, IClickableMenu parent) : base(
                  x: Game1.viewport.Width / 2 - (680 + IClickableMenu.borderWidth * 2) / 2,
                  y: Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2 - Game1.tileSize,
                  width: 632 + IClickableMenu.borderWidth * 2,
                  height: 500 + IClickableMenu.borderWidth * 4 + Game1.tileSize
            ) {
            m_env = env;
            m_farmerMakeup = makeup;
            this.ContentHelper = contentHelper;
            this.GlobalConfig = globalConfig;
            m_parent = parent;
            updateLayout();
        }

        /// <summary>The method invoked when the player presses the left mouse button.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveLeftClick(int x, int y, bool playSound = true) {
            // quick favorite buttons
            for (int i = 0; i < this.SetFavButtons.Length; i++) {
                if (this.SetFavButtons[i].containsPoint(x, y)) {
                    // set favorite
                    m_farmerMakeup.SaveFavorite(i + 1);

                    // show 'favorite saved' alert
                    this.Alerts.Add(new Alert(Game1.mouseCursors, new Rectangle(310, 392, 16, 16), Game1.viewport.Width / 2 - (700 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2, "New Favorite Saved.", 1200, false));
                    Game1.playSound("purchase");
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
            // set buttons
            foreach (ClickableTextureComponent button in this.SetFavButtons) {
                button.tryHover(x, y, 0.25f);
                button.tryHover(x, y, 0.25f);
            }
        }

        /// <summary>Draw the menu to the screen.</summary>
        /// <param name="spriteBatch">The sprite batch to which to draw.</param>
        public override void draw(SpriteBatch spriteBatch) {
            //spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);

            // menu background
            //Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width + 50, height, false, true);

            // header
            //SpriteText.drawString(spriteBatch, "Manage Favorites:", xPositionOnScreen + 55, yPositionOnScreen + 115);

            int yOffset = this.yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder - Game1.tileSize/4;
            // portrait
            spriteBatch.Draw(Game1.daybg, new Vector2(xPositionOnScreen + 430 + Game1.tileSize + Game1.tileSize * 2 / 3 - 2, yOffset + Game1.tileSize / 2), Color.White);
            Game1.player.FarmerRenderer.draw(spriteBatch, Game1.player.FarmerSprite.CurrentAnimationFrame, Game1.player.FarmerSprite.CurrentFrame, Game1.player.FarmerSprite.SourceRect, new Vector2(xPositionOnScreen + 428 + Game1.tileSize * 2 / 3 + Game1.tileSize * 2 - Game1.tileSize / 2, yOffset + Game1.tileSize / 2 + Game1.tileSize / 2), Vector2.Zero, 0.8f, Color.White, 0f, 1f, Game1.player);

            // labels
            Utility.drawTextWithShadow(spriteBatch, "You can set up to 6 quick favorite appearance", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "configurations for each character.", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 25), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "Your current appearance is shown on", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 75), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "the right, use one of the buttons below", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 100), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "to set it as a favorite :", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 125), Color.Black);

            yOffset += Game1.tileSize + Game1.tileSize * 3;
            // favorite icons
            Utility.drawTextWithShadow(spriteBatch, "1st Favorite", Game1.smallFont, new Vector2(xPositionOnScreen + 90, yOffset), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "2nd Favorite", Game1.smallFont, new Vector2(xPositionOnScreen + 90, yOffset +  75), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "3rd Favorite", Game1.smallFont, new Vector2(xPositionOnScreen + 90, yOffset + 150), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "4th Favorite", Game1.smallFont, new Vector2(xPositionOnScreen + 467, yOffset), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "5th Favorite", Game1.smallFont, new Vector2(xPositionOnScreen + 467, yOffset + 75), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "6th Favorite", Game1.smallFont, new Vector2(xPositionOnScreen + 467, yOffset + 150), Color.Black);

            Utility.drawTextWithShadow(spriteBatch, "Hint: Click the SET button lined up with each Favorite to", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yOffset + 225), Color.Black);
            Utility.drawTextWithShadow(spriteBatch, "set your current appearance as that Favorite.", Game1.smallFont, new Vector2(xPositionOnScreen + 110, yOffset + 250), Color.Black);

            foreach (ClickableTextureComponent saveFavButton in this.SetFavButtons)
                saveFavButton.draw(spriteBatch);


            // alerts
            foreach (Alert alert in this.Alerts) {
                alert.Draw(spriteBatch, Game1.smallFont);
            }

            //*
            // cursor
            spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()), Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 0, 16, 16), Color.White, 0f, Vector2.Zero, Game1.pixelZoom + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
            //IClickableMenu.drawHoverText(spriteBatch, this.HoverText, Game1.smallFont);
            //*/
        }

        /// <summary>Update the menu layout for a change in the zoom level or viewport size.</summary>
        public override void updateLayout() {
            // reset window position
            this.xPositionOnScreen = Game1.viewport.Width / 2 - (680 + IClickableMenu.borderWidth * 2) / 2;
            this.yPositionOnScreen = Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2 - Game1.tileSize;
            
            int xOffset = this.xPositionOnScreen + Game1.pixelZoom * 12;
            int yOffset = this.yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + Game1.tileSize * 3 / 4 + Game1.tileSize * 3;
            int xSize = Game1.pixelZoom * 15;
            int ySize = Game1.pixelZoom * 10;
            int zoom = 3;
            this.SetFavButtons = new[] {
                new ClickableTextureComponent(new Rectangle(xOffset + 225, yOffset, xSize, ySize), Game1.mouseCursors, new Rectangle(294, 428, 21, 11), zoom),
                new ClickableTextureComponent(new Rectangle(xOffset + 225, yOffset + 75, xSize, ySize), Game1.mouseCursors, new Rectangle(294, 428, 21, 11), zoom),
                new ClickableTextureComponent(new Rectangle(xOffset + 225, yOffset + 150, xSize, ySize), Game1.mouseCursors, new Rectangle(294, 428, 21, 11), zoom),

                new ClickableTextureComponent(new Rectangle(xOffset + 595, yOffset, xSize, ySize), Game1.mouseCursors, new Rectangle(294, 428, 21, 11), zoom),
                new ClickableTextureComponent(new Rectangle(xOffset + 595, yOffset + 75, xSize, ySize), Game1.mouseCursors, new Rectangle(294, 428, 21, 11), zoom),
                new ClickableTextureComponent(new Rectangle(xOffset + 595, yOffset + 150, xSize, ySize), Game1.mouseCursors, new Rectangle(294, 428, 21, 11), zoom)
            };
        }

        public override void onSwitchBack() {
            this.Alerts.Clear();
            if (m_parent is MenuFarmerMakeup mk) {
                mk.ShowFavTabArrow = false;
            }

            base.onSwitchBack();
        }
    }
}
