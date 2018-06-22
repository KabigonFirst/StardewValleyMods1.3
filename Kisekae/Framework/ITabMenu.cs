using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kisekae.Framework {
    abstract class ITabMenu : IClickableMenu {
        public ITabMenu(int x, int y, int width, int height, bool showUpperRightCloseButton = false) : base(x,y,width,height,showUpperRightCloseButton) {
        }
        public virtual void onSwitchBack() {
            updateLayout();
        }
        public virtual void updateLayout() {
        }
    }
}
