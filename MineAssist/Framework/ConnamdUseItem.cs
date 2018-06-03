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
            Position,
            ItemName
        }
        private int m_position = -1;
        private string m_itemName = null;
        DateTime gt;

        public override void exec(Dictionary<string, string> par) {
            if(par.ContainsKey(Paramter.IsContinuous.ToString())) {
                isContinuous = par[Paramter.IsContinuous.ToString()].Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            if(par.ContainsKey(Paramter.Position.ToString())) {
                m_position = Convert.ToInt32(par[Paramter.Position.ToString()]) - 1;
            } else if(par.ContainsKey(Paramter.ItemName.ToString())) {
                m_itemName = par[Paramter.ItemName.ToString()];
            } else {
                m_position = Game1.player.CurrentToolIndex;
            }

            if(m_itemName == null) {
                StardewWrap.fastUse(m_position);
            } else {
                StardewWrap.fastUse(ref m_itemName);
            }
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
            if(m_itemName == null) {
                StardewWrap.updateUse(ms);
            } else {
                StardewWrap.updateUse(ms, ref m_itemName);
            }
        }

        public override void end() {
            StardewWrap.endUse();
        }
    }
}
