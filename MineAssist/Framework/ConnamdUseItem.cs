using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using Microsoft.Xna.Framework;

namespace MineAssist.Framework {
    class ConnamdUseItem : Command {
        public static string name = "UseItem";
        public new enum Paramter {
            IsContinuous,
            Position
        }
        private int position;
        DateTime gt;

        public override void exec(Dictionary<string, string> par) {
            if(par.ContainsKey(Paramter.IsContinuous.ToString())) {
                isContinuous = par[Paramter.IsContinuous.ToString()].Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            if(par.ContainsKey(Paramter.Position.ToString())) {
                position = Convert.ToInt32(par[Paramter.Position.ToString()]) - 1;
            } else {
                position = Game1.player.CurrentToolIndex;
            }
            StardewWrap.fastUse(position);
            if(StardewWrap.isCurrentToolChargable()) {
                gt = DateTime.Now;
            }
        }

        public override void update() {
            if (!isContinuous) {
                return;
            }
            int ms = (DateTime.Now - gt).Milliseconds;
            gt = DateTime.Now;
            StardewWrap.updateUse(ms);
        }

        public override void end() {
            StardewWrap.endUse();
        }
    }
}
