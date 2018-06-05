using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace MineAssist.Framework {
    class StardewWrap {

        public static void pause() {
            if (!Game1.IsMasterGame) {
                Game1.chatBox.addErrorMessage(Game1.content.LoadString("Strings\\UI:Chat_HostOnlyCommand"));
                return;
            }
#if !DEBUG
            Game1.netWorldState.Value.IsPaused = !Game1.netWorldState.Value.IsPaused;
            /*
            if (Game1.netWorldState.Value.IsPaused) {
                Game1.chatBox.addInfoMessage("Paused");
                return;
            }
            Game1.chatBox.addInfoMessage("Resumed");
            //*/
#endif
        }
        public static void openJournalMenu() {
            Game1.activeClickableMenu = (IClickableMenu)new QuestLog();
        }

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

        public enum UseCondition {
            StaminaAtLeast,
            StaminaAtMost,
            HealthAtLeast,
            HealthAtMost,
            QualityAtLeast,
            QualityAtMost,
            PriceAtLeast,
            PriceAtMost,
            NumberCompare
        }
        public static bool satisfyCondition(ref Item item, ref string[] condition) {
            bool not = false;
            int cmdi = 0;
            if (condition.Length<2) {
                return true;
            }

            if (condition.Length > 2) {
                if ("not".Equals(condition[0].ToLower())) {
                    not = true;
                    ++cmdi;
                }
            }

            UseCondition con;
            if (!Enum.TryParse<UseCondition>(condition[cmdi], true, out con)) {
                return not ^ false;
            }
            ++cmdi;

            bool isobj = false;
            SObject so = null; 
            if (item is SObject) {
                so = (SObject)item;
                isobj = true;
            }
            double cmpVar = 0;
            bool cmpLarger = true;
            switch (con) {
                case UseCondition.StaminaAtLeast:
                    if (isobj) {
                        cmpLarger = true;
                        cmpVar = getStaminaHealthFromObject(ref so).X;
                    } else {
                        return not ^ false;
                    }
                    goto case UseCondition.NumberCompare;
                case UseCondition.StaminaAtMost:
                    if (isobj) {
                        cmpLarger = false;
                        cmpVar = getStaminaHealthFromObject(ref so).X;
                    } else {
                        return not ^ false;
                    }
                    goto case UseCondition.NumberCompare;
                case UseCondition.HealthAtLeast:
                    if (isobj) {
                        cmpLarger = true;
                        cmpVar = getStaminaHealthFromObject(ref so).Y;
                    } else {
                        return not ^ false;
                    }
                    goto case UseCondition.NumberCompare;
                case UseCondition.HealthAtMost:
                    if (isobj) {
                        cmpLarger = false;
                        cmpVar = getStaminaHealthFromObject(ref so).X;
                    } else {
                        return not ^ false;
                    }
                    goto case UseCondition.NumberCompare;
                case UseCondition.QualityAtLeast:
                    cmpLarger = true;
                    if (isobj) {
                        cmpVar = so.Quality;
                    } else if (item is MeleeWeapon) {
                        cmpVar = ((MeleeWeapon)item).getItemLevel();
                    } else if (item is Tool) {
                        cmpVar = ((Tool)item).UpgradeLevel;
                    } else {
                        return not ^ false;
                    }
                    goto case UseCondition.NumberCompare;
                case UseCondition.QualityAtMost:
                    cmpLarger = true;
                    if (isobj) {
                        cmpVar = so.Quality;
                    } else if (item is MeleeWeapon) {
                        cmpVar = ((MeleeWeapon)item).getItemLevel();
                    } else if (item is Tool) {
                        cmpVar = ((Tool)item).UpgradeLevel;
                    } else {
                        return not ^ false;
                    }
                    goto case UseCondition.NumberCompare;
                case UseCondition.PriceAtLeast:
                    cmpLarger = true;
                    cmpVar = item.salePrice();
                    goto case UseCondition.NumberCompare;
                case UseCondition.PriceAtMost:
                    cmpLarger = false;
                    cmpVar = item.salePrice();
                    goto case UseCondition.NumberCompare;
                case UseCondition.NumberCompare:
                    double par = Convert.ToDouble(condition[cmdi]);
                    if (cmpLarger) {
                        return not ^ (cmpVar >= par);
                    } else {
                        return not ^ (cmpVar <= par);
                    }
            }
            return not ^ false;
        }

        public enum UseOrder {
            StaminaLowest,
            StaminaHighest,
            HelthLowest,
            HealthHighest,
            QualityLowest,
            QualityHighest,
            PriceLowest,
            PriceHighest
        }
        public static int itemChallengeByOrder(ref Item baseItem, ref Item challengeItem, string order) {
            if (baseItem == null) {
                return 1;
            }

            UseOrder con;
            if (!Enum.TryParse<UseOrder>(order, true, out con)) {
                return -1;
            }

            bool isobj = false;
            SObject bi = null, ci = null;
            if (baseItem is SObject) {
                bi = (SObject)baseItem;
                isobj = true;
            }
            if (challengeItem is SObject) {
                ci = (SObject)challengeItem;
                isobj = isobj & true;
            }
            double bv = 0, cv = 0;

            switch (con) {
                case UseOrder.StaminaLowest:
                    if (isobj) {
                        cv = getStaminaHealthFromObject(ref ci).X;
                        bv = getStaminaHealthFromObject(ref bi).X;
                        if (cv < bv) {
                            return 1;
                        } else if (cv == bv) {
                            return 0;
                        } else {
                            return -1;
                        }
                    } else {
                        return -2;
                    }
                case UseOrder.StaminaHighest:
                    if (isobj) {
                        cv = getStaminaHealthFromObject(ref ci).X;
                        bv = getStaminaHealthFromObject(ref bi).X;
                        if (cv > bv) {
                            return 1;
                        } else if (cv == bv) {
                            return 0;
                        } else {
                            return -1;
                        }
                    } else {
                        return -2;
                    }
                case UseOrder.HelthLowest:
                    if (isobj) {
                        cv = getStaminaHealthFromObject(ref ci).Y;
                        bv = getStaminaHealthFromObject(ref bi).Y;
                        if (cv < bv) {
                            return 1;
                        } else if (cv == bv) {
                            return 0;
                        } else {
                            return -1;
                        }
                    } else {
                        return -2;
                    }
                case UseOrder.HealthHighest:
                    if (isobj) {
                        cv = getStaminaHealthFromObject(ref ci).Y;
                        bv = getStaminaHealthFromObject(ref bi).Y;
                        if (cv > bv) {
                            return 1;
                        } else if (cv == bv) {
                            return 0;
                        } else {
                            return -1;
                        }
                    } else {
                        return -2;
                    }
                case UseOrder.QualityLowest:
                    if (isobj) {
                        cv = ci.Quality;
                        bv = bi.Quality;
                    } else if (challengeItem is MeleeWeapon && baseItem is MeleeWeapon) {
                        cv = ((MeleeWeapon)challengeItem).getItemLevel();
                        bv = ((MeleeWeapon)baseItem).getItemLevel();
                    } else if (challengeItem is Tool && baseItem is Tool) {
                        cv = ((Tool)challengeItem).UpgradeLevel;
                        bv = ((Tool)baseItem).UpgradeLevel;
                    } else {
                        return -2;
                    }
                    if (cv < bv) {
                        return 1;
                    } else if (cv == bv) {
                        return 0;
                    } else {
                        return -1;
                    }
                case UseOrder.QualityHighest:
                    if (isobj) {
                        cv = ci.Quality;
                        bv = bi.Quality;
                    } else if (challengeItem is MeleeWeapon && baseItem is MeleeWeapon) {
                        cv = ((MeleeWeapon)challengeItem).getItemLevel();
                        bv = ((MeleeWeapon)baseItem).getItemLevel();
                    } else if (challengeItem is Tool && baseItem is Tool) {
                        cv = ((Tool)challengeItem).UpgradeLevel;
                        bv = ((Tool)baseItem).UpgradeLevel;
                    } else {
                        return -2;
                    }
                    if (cv > bv) {
                        return 1;
                    } else if (cv == bv) {
                        return 0;
                    } else {
                        return -1;
                    }
                case UseOrder.PriceLowest:
                    bv = baseItem.salePrice();
                    cv = challengeItem.salePrice();
                    if (cv < bv) {
                        return 1;
                    } else if (cv == bv) {
                        return 0;
                    } else {
                        return -1;
                    }
                case UseOrder.PriceHighest:
                    bv = baseItem.salePrice();
                    cv = challengeItem.salePrice();
                    if (cv > bv) {
                        return 1;
                    } else if (cv == bv) {
                        return 0;
                    } else {
                        return -1;
                    }
            }

            return -2;
        }

        public static Vector2 getStaminaHealthFromObject(ref SObject so) {
            Vector2 ret = new Vector2();
            int num = (int)Math.Ceiling((double)so.Edibility * 2.5) + (int)(so.Quality) * so.Edibility;
            ret.X = num;
            ret.Y = so.Edibility < 0 ? 0 : (int)((double)num * 0.45);
            return ret;
        }

        public static bool isItemTheName(ref string itemName, int i) {
            Item t = Game1.player.Items[i];
            if(t == null) {
                return false;
            }
            if ("Edible".Equals(itemName, System.StringComparison.CurrentCultureIgnoreCase)) {
                if (t is SObject so && so.Edibility > 0) {
                    return true;
                } else {
                    return false;
                }
            }
            if ("Weapon".Equals(itemName, System.StringComparison.CurrentCultureIgnoreCase)) {
                if(t is MeleeWeapon && !"Scythe".Equals(t.Name)) {
                    return true;
                } else {
                    return false;
                }
            }
            if(t.Name.Equals(itemName, System.StringComparison.CurrentCultureIgnoreCase)) {
                return true;
            }
            if(t is Tool tool && tool.BaseName.Equals(itemName, System.StringComparison.CurrentCultureIgnoreCase)) {
                return true;
            }
            return false;
        }

        public static int findItemByName(ref string itemName, int startIndex = 0) {
            for(int i = startIndex; i < Game1.player.MaxItems; ++i) {
                if(isItemTheName(ref itemName, i)) {
                    return i;
                }
            }
            return -1;
        }

        public static int findItem(ref string itemName, ref string condition, ref string order) {
            int i = -1;
            string[] cons = null;
            if (condition != null) {
                cons = condition.Split(' ');
            }
            Item it = null;
            for (int curi = 0; curi >= 0; ++curi) {
                curi = findItemByName(ref itemName, curi);
                if (curi < 0) {
                    break;
                }
                Item cur = Game1.player.Items[curi];
                if (condition != null && !satisfyCondition(ref cur, ref cons)) {
                    continue;
                }
                if (order == null && it != null) {
                    return i;
                }
                if (itemChallengeByOrder(ref it, ref cur, order) > 0) {
                    it = cur;
                    i = curi;
                }
            }
            return i;
        }

        public static void fastUse(ref string itemName, ref string condition, ref string order) {
            int i = findItem(ref itemName, ref condition, ref order);
            if (i >= 0) {
                fastUse(i);
            }
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

        public static void updateUse(int time, ref string itemName, ref string condition, ref string order) {
            int position = findItem(ref itemName, ref condition, ref order);
            if(position >= 0) {
                Game1.player.CurrentToolIndex = position;
                updateUse(time);
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
