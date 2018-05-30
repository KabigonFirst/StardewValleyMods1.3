using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;

namespace MineAssist.Framework {
    class ConnamdUseItem : Command {
        public static string name = "UseItem";
        public new enum Paramter {
            IsContinuous,
            Position
        }
        private int position;

        public override void exec(Dictionary<string, string> par) {
            if (par.ContainsKey(Paramter.IsContinuous.ToString())) {
                isContinuous = par[Paramter.IsContinuous.ToString()].Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            if (par.ContainsKey(Paramter.Position.ToString())) {
                position = Convert.ToInt32(par[Paramter.Position.ToString()]) - 1;
            } else {
                position = Game1.player.CurrentToolIndex;
            }
            StardewWrap.fastUse(position);
        }

        public override void update() {
            if (!isContinuous || StardewWrap.isPlayerBusy()) {
                return;
            }
            StardewWrap.fastUse(position);
        }

        public override void end() {
            Item t = Game1.player.Items[position];
            if (t is Tool tool && Game1.player.canReleaseTool) {
                Game1.player.EndUsingTool();
            }
        }
    }
}
