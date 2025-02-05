﻿using GameSrv.Player;
using SystemModule.Enums;

namespace GameSrv.GameCommand.Commands {
    [Command("DelDenyChrNameLogon", "", "人物名称", 10)]
    public class DelDenyChrNameLogonCommand : GameCommand {
        [ExecuteCommand]
        public void Execute(string[] @params, PlayObject playObject) {
            if (@params == null) {
                return;
            }
            string sChrName = @params.Length > 0 ? @params[0] : "";
            if (string.IsNullOrEmpty(sChrName)) {
                playObject.SysMsg(Command.CommandHelp, MsgColor.Red, MsgType.Hint);
                return;
            }
            bool boDelete = false;
            try {
                for (int i = 0; i < M2Share.DenyChrNameList.Count; i++) {
                    //if ((sChrName).CompareTo((M2Share.g_DenyChrNameList[i])) == 0)
                    //{
                    //    //if (((int)M2Share.g_DenyChrNameList[i]) != 0)
                    //    //{
                    //    //    M2Share.SaveDenyChrNameList();
                    //    //}
                    //    M2Share.g_DenyChrNameList.RemoveAt(i);
                    //    PlayObject.SysMsg(sChrName + "已从禁止登录人物列表中删除。", TMsgColor.c_Green, TMsgType.t_Hint);
                    //    boDelete = true;
                    //    break;
                    //}
                }
            }
            finally {
            }
            if (!boDelete) {
                playObject.SysMsg(sChrName + "没有被禁止登录。", MsgColor.Green, MsgType.Hint);
            }
        }
    }
}