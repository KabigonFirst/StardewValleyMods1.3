using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
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
                //shake player to warn low Stamina
                if((double)Game1.player.Stamina <= 20.0 && !(t is MeleeWeapon)) {
                    shakePlayer();
                }
                //reset tool power when begin using the tool
                if(Game1.player.toolPower > 0) {
                    Game1.player.toolPower = 0;
                }
                Game1.player.BeginUsingTool();
            } else if (t is SObject so) {
                if(so.Edibility > 0) {
                    Game1.player.eatObject(so);
                    if(--t.Stack == 0) {
                        Game1.player.removeItemFromInventory(t);
                    }
                } else if (so.Name.Contains("Totem")){
                    so.performUseAction(Game1.player.currentLocation);
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

        public static void updateUse(int time) {
            if(isCurrentToolChargable()) {
                if((double)Game1.player.Stamina < 1.0) {
                    return;
                }
                if(Game1.player.toolHold <= 0 && canIncreaseToolPower()) {
                    Game1.player.toolHold = 600;
                } else if(canIncreaseToolPower()) {
                    Game1.player.toolHold -= time;
                    if(Game1.player.toolHold <= 0)
                        Game1.player.toolPowerIncrease();
                }
            } else if (!isPlayerBusy() && canCurrentItemContiniouslyUse()) {
                fastUse(Game1.player.CurrentToolIndex);
            }
        }

        public static void endUse() {
            Item t = Game1.player.Items[Game1.player.CurrentToolIndex];
            if(t is Tool tool && Game1.player.canReleaseTool) {
                Game1.player.EndUsingTool();
            }
        }

        public static bool canCurrentItemContiniouslyUse() {
            Tool t = Game1.player.CurrentTool;
            if(t == null) {
                return false;
            }
            if(t is MilkPail || t is Shears || t is Pan || t is FishingRod) {
                return false;
            }
            return true;
        }

        public static bool isCurrentToolChargable() {
            Tool t = Game1.player.CurrentTool;
            if (t == null) {
                return false;
            }
            if (t is Hoe ||t is WateringCan) {
                return true;
            }
            return false;
        }

        public static bool canIncreaseToolPower() {
#if DEBUG
            return ((int)(Game1.player.CurrentTool.UpgradeLevel) > Game1.player.toolPower);
#endif
#if !DEBUG
            return ((int)(Game1.player.CurrentTool.upgradeLevel) > Game1.player.toolPower);
#endif
        }

        public static void shakePlayer() {
            Game1.staminaShakeTimer = 1000;
            for(int index = 0; index < 4; ++index) {
                Game1.screenOverlayTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(366, 412, 5, 6), new Vector2((float)(Game1.random.Next(32) + Game1.viewport.Width - 56), (float)(Game1.viewport.Height - 224 - 16 - (int)((double)(Game1.player.MaxStamina - 270) * 0.715))), false, 0.012f, Color.SkyBlue) {
                    motion = new Vector2(-2f, -10f),
                    acceleration = new Vector2(0.0f, 0.5f),
                    local = true,
                    scale = (float)(4 + Game1.random.Next(-1, 0)),
                    delayBeforeAnimationStart = index * 30
                });
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
