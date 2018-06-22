using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Kisekae.Config;

namespace Kisekae.Framework {
    /// <summary>The menu which lets the player customise their character's appearance.</summary>
    internal class MenuFarmerMakeup : IClickableMenu {
        /*********
        ** Properties
        *********/
        #region Metadata
        /// <summary>Global Mod Interface.</summary>
        private readonly IMod m_env;
        /// <summary>Encapsulates the underlying mod texture management.</summary>
        private readonly ContentHelper m_contentHelper;
        /// <summary>The current per-save config settings.</summary>
        private readonly LocalConfig PlayerConfig;
        /// <summary>The global config settings.</summary>
        private readonly GlobalConfig GlobalConfig;
        /// <summary>Core component to manipulate player appearance.</summary>
        private readonly FarmerMakeup m_farmerMakeup;
        #endregion

        #region GUI Components
        /// <summary>The customisation menu tabs.</summary>
        private enum MenuTab {
            /// <summary>The main character customisation screen.</summary>
            Customise = 0,
            /// <summary>The mange favorites screen.</summary>
            ManageFavorites,
            /// <summary>The main 'favorites' screen.</summary>
            Favorites,
            /// <summary>The favorites 'extra outfits' submenu.</summary>
            FavoritesExtras,
            /// <summary>The main 'about' screen.</summary>
            About,
        }
        /// <summary>Tabs and subtabs.</summary>
        private List<ITabMenu> m_tabs = new List<ITabMenu>();
        /// <summary>Parent of Tab.</summary>
        private List<int> m_tabParents = new List<int>();
        /// <summary>The tabs used to switch submenu.</summary>
        private List<ClickableTextureComponent> m_tabMenus = new List<ClickableTextureComponent>();

        private ClickableTextureComponent CancelButton;
        /// <summary>A floating arrow which brings attention to the 'favorites' tab.</summary>
        private TemporaryAnimatedSprite FavTabArrow;
        /// <summary>A 'new' sprite that brings attention to the tabs.</summary>
        private TemporaryAnimatedSprite FloatingNew;
        #endregion

        #region States
        /// <summary>Whether to show the <see cref="FavTabArrow"/>.</summary>
        public bool ShowFavTabArrow = false;
        /// <summary>The current tab being viewed.</summary>
        private int m_curTab = (int)MenuTab.Customise;
        /// <summary>The tooltip text to draw next to the cursor.</summary>
        private string HoverText;
        /// <summary>The zoom level before the menu was opened.</summary>
        private readonly float PlayerZoomLevel;
        #endregion

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="contentHelper">Encapsulates the underlying mod texture management.</param>
        /// <param name="modHelper">Provides simplified APIs for writing mods.</param>
        /// <param name="modVersion">The current mod version.</param>
        /// <param name="globalConfig">The global config settings.</param>
        /// <param name="playerConfig">The current per-save config settings.</param>
        /// <param name="zoomLevel">The zoom level before the menu was opened.</param>
        public MenuFarmerMakeup(IMod env, ContentHelper contentHelper, GlobalConfig globalConfig, LocalConfig playerConfig)
            : base(
                  x: Game1.viewport.Width / 2 - (680 + IClickableMenu.borderWidth * 2) / 2,
                  y: Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2 - Game1.tileSize,
                  width: 632 + IClickableMenu.borderWidth * 2,
                  height: 500 + IClickableMenu.borderWidth * 4 + Game1.tileSize
            ) {
            // save metadata
            m_env = env;
            m_contentHelper = contentHelper;
            this.GlobalConfig = globalConfig;
            this.PlayerConfig = playerConfig;
            m_farmerMakeup = new FarmerMakeup(env, contentHelper);
            m_farmerMakeup.m_farmer = Game1.player;
            m_farmerMakeup.m_config = playerConfig;
            this.PlayerZoomLevel = Game1.options.zoomLevel;
            exitFunction = exit;

            // override zoom level
            Game1.options.zoomLevel = this.GlobalConfig.MenuZoomOut ? 0.75f : 1f;
            Game1.overrideGameMenuReset = true;
            Game1.game1.refreshWindowSettings();
            m_tabs.Add(new MenuCustomize(env, contentHelper, globalConfig, playerConfig, m_farmerMakeup, this));
            m_tabParents.Add(-1);
            m_tabs.Add(null);
            m_tabParents.Add(-1);
            m_tabs.Add(new MenuFavorites(env, contentHelper, globalConfig, m_farmerMakeup, this));
            m_tabParents.Add((int)MenuTab.ManageFavorites);
            m_tabs.Add(new MenuFavoritesExtras(env, contentHelper, globalConfig, m_farmerMakeup));
            m_tabParents.Add((int)MenuTab.ManageFavorites);
            m_tabs.Add(new MenuAbout(env, contentHelper, globalConfig));
            m_tabParents.Add(-1);
            this.updateLayout();
            Game1.player.faceDirection(2);
            Game1.player.FarmerSprite.StopAnimation();
        }

        #region EventHandler
        /// <summary>The method called when the game window changes size.</summary>
        /// <param name="oldBounds">The former viewport.</param>
        /// <param name="newBounds">The new viewport.</param>
        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            updateLayout();
        }

        /// <summary>The method invoked when the player presses the left mouse button.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveLeftClick(int x, int y, bool playSound = true) {
            // tabs
            for (int i = 0; i < m_tabMenus.Count; ++i) {
                if (IsCurTab(i) && m_tabMenus[i].containsPoint(x, y)) {
                    if (i == m_tabParents[m_curTab]) {
                        return;
                    }
                    if (m_tabs[i] == null) {
                        ++i;
                    }
                    Game1.playSound("smallSelect");
                    this.SetTab(i);
                    return;
                }
            }
            if (this.CancelButton.containsPoint(x, y)) {
                exitThisMenuNoSound();
            }
            // hide 'new' button
            if (this.GlobalConfig.ShowIntroBanner) {
                this.GlobalConfig.ShowIntroBanner = false;
                m_env.Helper.WriteConfig(this.GlobalConfig);
            }
            // tab contents
            m_tabs[m_curTab].receiveLeftClick(x, y, playSound);
        }

        /// <summary>The method invoked while the player is holding down the left mouse button.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        public override void leftClickHeld(int x, int y) {
            try {
                m_tabs[m_curTab].leftClickHeld(x, y);
            } catch { }
        }

        /// <summary>The method invoked when the player releases the left mouse button.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        public override void releaseLeftClick(int x, int y) {
            try {
                m_tabs[m_curTab].releaseLeftClick(x, y);
            } catch { }
        }

        /// <summary>The method invoked when the player presses the left mouse button.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveRightClick(int x, int y, bool playSound = true) {
            try {
                m_tabs[m_curTab].receiveRightClick(x, y, playSound);
            } catch { }
        }

        /// <summary>The method invoked when the player presses a keyboard button.</summary>
        /// <param name="key">The key that was pressed.</param>
        public override void receiveKeyPress(Keys key) {
            // exit menu
            if (Game1.options.menuButton.Contains(new InputButton(key)) && this.readyToClose()) {
                exitThisMenuNoSound();
                return;
            }
            try {
                m_tabs[m_curTab].receiveKeyPress(key);
            } catch { }
        }

        /// <summary>Update the menu state.</summary>
        /// <param name="time">The elapsed game time.</param>
        public override void update(GameTime time) {
            base.update(time);

            m_tabs[m_curTab].update(time);

            // update tab arrows
            this.FavTabArrow?.update(time);
            this.FloatingNew?.update(time);
        }

        /// <summary>The method invoked when the cursor is over a given position.</summary>
        /// <param name="x">The X mouse position.</param>
        /// <param name="y">The Y mouse position.</param>
        public override void performHoverAction(int x, int y) {
            base.performHoverAction(x, y);

            // reset hover text
            this.HoverText = "";

            // check subtab hovers
            foreach (ClickableComponent current in m_tabMenus) {
                if (current.containsPoint(x, y)) {
                    if (current.name.Equals("Quick Outfits") || current.name.Equals("Extra Outfits")) {
                        if (this.m_curTab == (int)MenuTab.Favorites || this.m_curTab == (int)MenuTab.FavoritesExtras)
                            this.HoverText = current.name;
                    }
                    return;
                }
            }

            // cancel buttons
            this.CancelButton.tryHover(x, y, 0.25f);
            this.CancelButton.tryHover(x, y, 0.25f);

            // tab contents
            m_tabs[m_curTab].performHoverAction(x, y);
        }

        /// <summary>Draw the menu to the screen.</summary>
        /// <param name="spriteBatch">The sprite batch to which to draw.</param>
        public override void draw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);

            // menu background
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width + 50, height, false, true);

            // tabs
            {
                // check selected tab
                bool isCustomiseTab = this.m_curTab == (int)MenuTab.Customise;
                bool isFavoriteTab = this.m_curTab == (int)MenuTab.Favorites || this.m_curTab == (int)MenuTab.FavoritesExtras;
                bool isAboutTab = this.m_curTab == (int)MenuTab.About;
                bool isMainOutfitsTab = this.m_curTab == (int)MenuTab.Favorites;
                bool isExtraOutfitsTab = this.m_curTab == (int)MenuTab.FavoritesExtras;

                // get tab positions
                Vector2 character = new Vector2(xPositionOnScreen + 45+64*0, yPositionOnScreen + (isCustomiseTab ? 25 : 16));
                Vector2 favorites = new Vector2(xPositionOnScreen + 45+64*1, yPositionOnScreen + (isFavoriteTab  ? 25 : 16));
                Vector2 about     = new Vector2(xPositionOnScreen + 45+64*2, yPositionOnScreen + (isAboutTab     ? 25 : 16));
                Vector2 quickFavorites = new Vector2(xPositionOnScreen - (isMainOutfitsTab  ? 40 : 47), yPositionOnScreen + 107);
                Vector2 extraFavorites = new Vector2(xPositionOnScreen - (isExtraOutfitsTab ? 40 : 47), yPositionOnScreen + 171);

                // customise tab
                spriteBatch.Draw(Game1.mouseCursors, character, new Rectangle(16, 368, 16, 16), Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.0001f);
                Game1.player.FarmerRenderer.drawMiniPortrat(spriteBatch, new Vector2(xPositionOnScreen + 53, yPositionOnScreen + (Game1.player.isMale ? (isCustomiseTab ? 35 : 26) : (isCustomiseTab ? 32 : 23))), 0.00011f, 3f, 2, Game1.player);
                // favorite tab
                spriteBatch.Draw(Game1.mouseCursors, favorites, new Rectangle(16, 368, 16, 16), Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.0001f);
                m_tabMenus[(int)MenuTab.ManageFavorites].draw(spriteBatch);
                // about tab
                spriteBatch.Draw(Game1.mouseCursors, about, new Rectangle(16, 368, 16, 16), Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.0001f);
                m_tabMenus[(int)MenuTab.About].draw(spriteBatch);
                // favorite subtabs
                if (isFavoriteTab) {
                    spriteBatch.Draw(m_contentHelper.m_menuTextures, quickFavorites, new Rectangle(52, 202, 16, 16), Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.0001f);
                    spriteBatch.Draw(m_contentHelper.m_menuTextures, extraFavorites, new Rectangle(52, 202, 16, 16), Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.0001f);
                    m_tabMenus[(int)MenuTab.Favorites].draw(spriteBatch);
                    m_tabMenus[(int)MenuTab.FavoritesExtras].draw(spriteBatch);
                }
            }

            // cancel button
            this.CancelButton.draw(spriteBatch);

            // tab floaters
            if (this.GlobalConfig.ShowIntroBanner)
                FloatingNew?.draw(spriteBatch, true, 400, 950);
            if (this.ShowFavTabArrow)
                FavTabArrow?.draw(spriteBatch, true, 400, 950);

            // tab contents
            m_tabs[m_curTab].draw(spriteBatch);

            // cursor
            IClickableMenu.drawHoverText(spriteBatch, this.HoverText, Game1.smallFont);
        }
        #endregion

        /*********
        ** Private methods
        *********/
        /// <summary>Update the menu layout for a change in the zoom level or viewport size.</summary>
        private void updateLayout() {
            // reset window position
            this.xPositionOnScreen = Game1.viewport.Width / 2 - (680 + IClickableMenu.borderWidth * 2) / 2;
            this.yPositionOnScreen = Game1.viewport.Height / 2 - (500 + IClickableMenu.borderWidth * 2) / 2 - Game1.tileSize;

            // initialise all components
            Texture2D menuTextures = m_contentHelper.m_menuTextures;

            // tabs
            m_tabMenus.Clear();
            m_tabMenus.Add(new ClickableTextureComponent("Customize Character", new Rectangle(xPositionOnScreen + 62, yPositionOnScreen + 40, 50, 50), "", "", menuTextures, new Rectangle(9, 48, 8, 11), Game1.pixelZoom));
            m_tabMenus.Add(new ClickableTextureComponent("Manage Favorites", new Rectangle(xPositionOnScreen + 125, yPositionOnScreen + 40, 50, 50), "", "", menuTextures, new Rectangle(24, 26, 8, 8), Game1.pixelZoom));
            m_tabMenus.Add(new ClickableTextureComponent("Quick Outfits", new Rectangle(xPositionOnScreen - 23, yPositionOnScreen + 122, 50, 50), "", "", menuTextures, new Rectangle(8, 26, 8, 8), Game1.pixelZoom));
            m_tabMenus.Add(new ClickableTextureComponent("Extra Outfits", new Rectangle(xPositionOnScreen - 23, yPositionOnScreen + 186, 50, 50), "", "", menuTextures, new Rectangle(0, 26, 8, 8), Game1.pixelZoom));
            m_tabMenus.Add(new ClickableTextureComponent("About", new Rectangle(xPositionOnScreen + 188, yPositionOnScreen + 33, 50, 50), "", "", menuTextures, new Rectangle(0, 48, 8, 11), Game1.pixelZoom));

            // tab positions
            this.UpdateTabFloaters();
            this.UpdateTabPositions();

            // cancel button
            this.CancelButton = new ClickableTextureComponent(new Rectangle((xPositionOnScreen + 675) + Game1.pixelZoom * 12, (yPositionOnScreen - 100) + Game1.tileSize + Game1.pixelZoom * 14, Game1.pixelZoom * 10, Game1.pixelZoom * 10), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), Game1.pixelZoom);

            m_tabs[m_curTab].updateLayout();
        }

        /// <summary>Reinitialise components which bring attention to tabs.</summary>
        private void UpdateTabFloaters() {
            this.FloatingNew = new TemporaryAnimatedSprite("menuTextures", new Rectangle(0, 102, 23, 9), 115, 5, 1, new Vector2(xPositionOnScreen - 90, yPositionOnScreen + 35), false, false, 0.89f, 0f, Color.White, Game1.pixelZoom, 0f, 0f, 0f, true) {
                totalNumberOfLoops = 1,
                yPeriodic = true,
                yPeriodicLoopTime = 1500f,
                yPeriodicRange = Game1.tileSize / 8f
            };

            this.FavTabArrow = new TemporaryAnimatedSprite("menuTextures", new Rectangle(0, 120, 12, 14), 100f, 3, 5, new Vector2(xPositionOnScreen + 120, yPositionOnScreen), false, false, 0.89f, 0f, Color.White, 3f, 0f, 0f, 0f, true) {
                yPeriodic = true,
                yPeriodicLoopTime = 1500f,
                yPeriodicRange = Game1.tileSize / 8f
            };
        }

        /// <summary>Recalculate the positions for all tabs and subtabs.</summary>
        private void UpdateTabPositions() {
            m_tabMenus[(int)MenuTab.Customise].bounds.Y = this.yPositionOnScreen + (this.m_curTab == (int)MenuTab.Customise ? 50 : 40);
            m_tabMenus[(int)MenuTab.ManageFavorites].bounds.Y = this.yPositionOnScreen + (this.m_curTab == (int)MenuTab.Favorites || this.m_curTab == (int)MenuTab.FavoritesExtras ? 50 : 40);
            m_tabMenus[(int)MenuTab.Favorites].bounds.X = this.xPositionOnScreen - (this.m_curTab == (int)MenuTab.Favorites ? 16 : 23);
            m_tabMenus[(int)MenuTab.FavoritesExtras].bounds.X = this.xPositionOnScreen - (this.m_curTab == (int)MenuTab.FavoritesExtras ? 16 : 23);
            m_tabMenus[(int)MenuTab.About].bounds.Y = this.yPositionOnScreen + (this.m_curTab == (int)MenuTab.About ? 43 : 33);
        }

        private bool IsCurTab(int tabIndex) {
            return (m_tabParents[tabIndex] < 0 || m_tabParents[tabIndex] == m_tabParents[m_curTab]);
        }
        /// <summary>Switch to the given tab.</summary>
        /// <param name="tab">The tab to display.</param>
        private void SetTab(int tab) {
            if (this.m_curTab == tab)
                return;
            this.m_curTab = tab;
            m_tabs[tab].onSwitchBack();
            this.UpdateTabPositions();
        }

        private void exit() {
            for (int i = 0; i < m_tabs.Count; ++i) {
                if (m_tabs[i]?.exitFunction != null) {
                    m_tabs[i].exitFunction();
                }
            }
            Game1.playSound("yoba");

            Game1.options.zoomLevel = this.PlayerZoomLevel;
            Game1.overrideGameMenuReset = true;
            Game1.game1.refreshWindowSettings();
            Game1.player.canMove = true;
            Game1.flashAlpha = 1f;
        }
    }
}
