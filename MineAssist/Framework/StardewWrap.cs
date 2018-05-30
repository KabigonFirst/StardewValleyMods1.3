using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;

namespace MineAssist.Framework {
    class StardewWrap {
        /// <summary>Craft item(Staircase) quickly.</summary>
        /// <param name="itemName">item to craft.</param>
        /// <param name="toPosition">position to put crafted item or -1 to be default.</param>
        public static void fastCraft(string itemName = "Staircase", int toPosition = -1) {
            //construcet the recipe
            CraftingRecipe recipe = new CraftingRecipe(itemName, false);

            //Check if player can craft the item( 1.recipe is obtained, and 2.ingredients are enough)
            if (!Game1.player.knowsRecipe(itemName)) {
                Game1.showRedMessage($"Not able to craft {itemName}!");
                return;
            }
            /*/
            if (!Game1.player.craftingRecipes.ContainsKey(itemName)) {
                Game1.showRedMessage($"Not able to craft {itemName}!");
                return;
            }
            //*/

            if (!recipe.doesFarmerHaveIngredientsInInventory()) {
                Game1.showRedMessage("No enough ingredients!");
                return;
            }

            //craft the item
            recipe.consumeIngredients();
            Item craftedItem = recipe.createItem();
#if !DEBUG
            //*
            Game1.player.craftingRecipes[itemName] += recipe.numberProducedPerCraft;
            //*/
#endif

            //update related events
            Game1.player.checkForQuestComplete((NPC)null, -1, -1, craftedItem, (string)null, 2, -1);
            Game1.stats.checkForCraftingAchievements();

            //clear position to place new crafted item if possible and necessary
            if (toPosition >= 0 && toPosition < Game1.player.MaxItems && Game1.player.Items[toPosition] != null) {
                Item originItem = Game1.player.Items[toPosition];
                //if new item can not be staked, then try to move it to other palce
                if (!(craftedItem is SObject && originItem is SObject && (originItem.Stack + craftedItem.Stack <= originItem.maximumStackSize() && (originItem as SObject).canStackWith(craftedItem)))) {
                    int i;
                    //find first empty slot
                    for (i = 0; i < Game1.player.Items.Count; i++) {
                        if (Game1.player.Items[i] == null) {
                            break;
                        }
                    }
                    //if found, place the original item to the slot
                    if (i < Game1.player.Items.Count) {
                        Game1.player.Items[i] = Game1.player.Items[2];
                        Game1.player.Items[2] = null;
                    }
                }
            }
            /*
            int times = ++Game1.player.craftingRecipes["Staircase"];
            Game1.showGlobalMessage($"creafted {times} times");
            */
            //add new crafted item
            Game1.player.addItemByMenuIfNecessary(craftedItem);
        }

        /// <summary>Directly use item(tool/weapon/foods/placealbe) quickly.</summary>
        /// <param name="itemIndex">The index of item that intend to use.</param>
        public static void fastUse(int itemIndex) {
            //GameLocation.openCraftingMenu("Starcase");
            Item t = Game1.player.Items[itemIndex];
            if (t == null) {
                return;
            }
            Game1.player.CurrentToolIndex = itemIndex;
            if (t is Tool) {
                Game1.player.BeginUsingTool();
            } else if (t is SObject so) {
                if (so.Edibility > 0) {
                    Game1.player.eatObject(so);
                    if (--t.Stack == 0) {
                        Game1.player.removeItemFromInventory(t);
                    }
                } else if (so.isPlaceable()) {
                    //calculate place position based on player position and facing direction
                    Vector2 placePos = Game1.player.getTileLocation();
                    int d = Game1.player.FacingDirection;
                    if ((d & 1) == 1) {
                        placePos.X += 2 - d;
                    } else {
                        placePos.Y += d - 1;
                    }
                    //Game1.showGlobalMessage($"POS:{(int)placePos.X}, {(int)placePos.Y}");
                    Utility.tryToPlaceItem(Game1.currentLocation, so, (int)placePos.X * 64 + 32, (int)placePos.Y * 64 + 32);
                }
            }
        }

        public enum SDirection {
            UP = 0,
            RIGHT = 1,
            DOWN = 2,
            LEFT = 3
        }
        public static void setMove(SDirection m, bool isStart) {
            Game1.player.setMoving((byte)((isStart?0:32) + (1<<(byte)m)));
        }
        public static void inGameMessage(string msg) {
            Game1.showGlobalMessage(msg);
        }
        public static bool isPlayerReady() {
            return (Context.IsWorldReady && Context.IsPlayerFree);
        }
        /// <summary>Check if local player is busy with something.</summary>
        public static bool isPlayerBusy() {
            return(Game1.player.UsingTool || Game1.player.isEating);
        }
    }
}
