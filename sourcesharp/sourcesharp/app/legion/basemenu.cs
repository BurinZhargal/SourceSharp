using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using vgui
using System.Threading.Tasks;

namespace sourcesharp.app.legion
{
    public class CBaseMenu : Panel
    {
        public CBaseMenu(Panel pParent, string pPanelName) : base(pParent, pPanelName)
        {
            SetKeyBoardInputEnabled(true);
            SetMouseInputEnabled(true);
            SetSizeable(false);
            SetMoveable(false);
        }

        ~CBaseMenu()
        {
        }
        public override void OnKeyCodeTyped(vgui.KeyCode code)
        {
            base.OnKeyCodeTyped(code);
            bool shift = (vgui.input().IsKeyDown(vgui.KEY_LSHIFT) || vgui.input().IsKeyDown(vgui.KEY_RSHIFT));
            bool ctrl = (vgui.input().IsKeyDown(vgui.KEY_LCONTROL) || vgui.input().IsKeyDown(vgui.KEY_RCONTROL));
            bool alt = (vgui.input().IsKeyDown(vgui.KEY_LALT) || vgui.input().IsKeyDown(vgui.KEY_RALT));
            if (ctrl && shift && alt && code == vgui.KEY_B)
            {
                // enable build mode
                ActivateBuildMode();
            }
        }
        public override void OnCommand(string pCommand)
        {
            if (string.Equals(pCommand, "quit", StringComparison.OrdinalIgnoreCase))
            {
                IGameManager.Stop();
                return;
            }

            if (string.Equals(pCommand, "popmenu", StringComparison.OrdinalIgnoreCase))
            {
                g_pMenuManager.PopMenu();
                return;
            }

            if (pCommand.StartsWith("popallmenus ", StringComparison.OrdinalIgnoreCase))
            {
                g_pMenuManager.PopAllMenus();
                return;
            }

            if (pCommand.StartsWith("pushmenu ", StringComparison.OrdinalIgnoreCase))
            {
                string pMenuName = pCommand.Substring(9).TrimStart();
                g_pMenuManager.PushMenu(pMenuName);
                return;
            }

            if (pCommand.StartsWith("switchmenu ", StringComparison.OrdinalIgnoreCase))
            {
                string pMenuName = pCommand.Substring(11).TrimStart();
                g_pMenuManager.SwitchToMenu(pMenuName);
                return;
            }

            base.OnCommand(pCommand);
        }
        public override void OnKeyCodeTyped(vgui.KeyCode code)
        {

        }


    }



}
