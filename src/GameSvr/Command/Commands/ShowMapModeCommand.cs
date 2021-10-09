﻿using SystemModule;
using GameSvr.CommandSystem;

namespace GameSvr
{
    /// <summary>
    /// 显示指定地图信息
    /// </summary>
    [GameCommand("ShowMapMode", "显示指定地图信息", "地图号", 10)]
    public class ShowMapModeCommand : BaseCommond
    {
        [DefaultCommand]
        public void ShowMapMode(string[] @Params, TPlayObject PlayObject)
        {
            var sMapName = @Params.Length > 0 ? @Params[0] : "";
            string sMsg;
            if (PlayObject.m_btPermission < 6)
            {
                return;
            }
            if (sMapName == "")
            {
                PlayObject.SysMsg(CommandAttribute.CommandHelp(), TMsgColor.c_Red, TMsgType.t_Hint);
                return;
            }
            var Envir = M2Share.g_MapManager.FindMap(sMapName);
            if (Envir == null)
            {
                PlayObject.SysMsg(sMapName + " 不存在!!!", TMsgColor.c_Red, TMsgType.t_Hint);
                return;
            }
            sMsg = "地图模式: " + Envir.GetEnvirInfo();
            PlayObject.SysMsg(sMsg, TMsgColor.c_Blue, TMsgType.t_Hint);
        }
    }
}