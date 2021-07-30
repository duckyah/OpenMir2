using System;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using SystemModule;
using SystemModule.Packages;
using SystemModule.Sockets;
using SystemModule.Sockets.AsyncSocketClient;
using SystemModule.Sockets.AsyncSocketServer;
using SystemModule.Sockets.Event;

namespace RunGate
{
    public class ServerApp
    {
        private long dwShowMainLogTick = 0;
        private bool boShowLocked = false;
        private ArrayList TempLogList = null;
        private long dwCheckClientTick = 0;
        private long dwProcessPacketTick = 0;
        private bool boServerReady = false;
        private long dwLoopCheckTick = 0;
        private long dwLoopTime = 0;
        private long dwProcessServerMsgTime = 0;
        private long dwProcessClientMsgTime = 0;
        private long dwReConnectServerTime = 0;
        private long dwRefConsolMsgTick = 0;
        private int nBufferOfM2Size = 0;
        private long dwRefConsoleMsgTick = 0;
        private int nReviceMsgSize = 0;
        private int nDeCodeMsgSize = 0;
        private int nSendBlockSize = 0;
        private int nProcessMsgSize = 0;
        private int nHumLogonMsgSize = 0;
        private int nHumPlayMsgSize = 0;
        private IClientScoket ClientSocket;
        private ISocketServer ServerSocket;
        private Timer decodeTimer;

        public ServerApp()
        {
            TempLogList = new ArrayList();
            dwLoopCheckTick = HUtil32.GetTickCount();
        }
        
        private void SendSocket(string SendBuffer, int nLen)
        {
            if (ClientSocket.IsConnected)
            {
                ClientSocket.SendText(SendBuffer);
            }
        }
        
        private void SendSocket(byte[] SendBuffer)
        {
            if (ClientSocket.IsConnected)
            {
                ClientSocket.Send(SendBuffer);
            }
        }

        private void SendServerMsg(ushort nIdent, ushort wSocketIndex, int nSocket, short nUserListIndex, int nLen, string Data)
        {
            var GateMsg = new TMsgHeader();
            GateMsg.dwCode = Grobal2.RUNGATECODE;
            GateMsg.nSocket = nSocket;
            GateMsg.wGSocketIdx = wSocketIndex;
            GateMsg.wIdent = nIdent;
            GateMsg.wUserListIndex = nUserListIndex;
            GateMsg.nLength = nLen;
            var nBuffLen = nLen + 20;
            var SendBuffer = new byte[nBuffLen];
            // Move(GateMsg, SendBuffer, 20);
            // if (Data != null)
            // {
            //     Move(Data, SendBuffer[20], nLen);
            // }
            SendSocket(SendBuffer);
            Console.WriteLine("todo SendServerMsg");
        }

        public void DecodeTimer(object obj)
        {
            long dwLoopProcessTime;
            long dwProcessReviceMsgLimiTick;
            TSendUserData UserData= null;
            TSendUserData tUserData = null;
            TSessionInfo UserSession= null;
            ShowMainLogMsg();
            if (!GateShare.boDecodeMsgLock)
            {
                try
                {
                    if ((HUtil32.GetTickCount() - dwRefConsoleMsgTick) >= 1000)
                    {
                        dwRefConsoleMsgTick = HUtil32.GetTickCount();
                        if (!GateShare.boShowBite)
                        {
                            // LabelReviceMsgSize.Text = "接收: " + (nReviceMsgSize / 1024).ToString() + " KB";
                            // LabelBufferOfM2Size.Text = "服务器通讯: " + (nBufferOfM2Size / 1024).ToString() + " KB";
                            // LabelProcessMsgSize.Text = "编码: " + (nProcessMsgSize / 1024).ToString() + " KB";
                            // LabelLogonMsgSize.Text = "登录: " + (nHumLogonMsgSize / 1024).ToString() + " KB";
                            // LabelPlayMsgSize.Text = "普通: " + (nHumPlayMsgSize / 1024).ToString() + " KB";
                            // LabelDeCodeMsgSize.Text = "解码: " + (nDeCodeMsgSize / 1024).ToString() + " KB";
                            // LabelSendBlockSize.Text = "发送: " + (nSendBlockSize / 1024).ToString() + " KB";
                        }
                        else
                        {
                            // LabelReviceMsgSize.Text = "接收: " + (nReviceMsgSize).ToString() + " B";
                            // LabelBufferOfM2Size.Text = "服务器通讯: " + (nBufferOfM2Size).ToString() + " B";
                            // LabelSelfCheck.Text = "通讯自检: " + (GateShare.dwCheckServerTimeMin).ToString() + "/" + (GateShare.dwCheckServerTimeMax).ToString();
                            // LabelProcessMsgSize.Text = "编码: " + (nProcessMsgSize).ToString() + " B";
                            // LabelLogonMsgSize.Text = "登录: " + (nHumLogonMsgSize).ToString() + " B";
                            // LabelPlayMsgSize.Text = "普通: " + (nHumPlayMsgSize).ToString() + " B";
                            // LabelDeCodeMsgSize.Text = "解码: " + (nDeCodeMsgSize).ToString() + " B";
                            // LabelSendBlockSize.Text = "发送: " + (nSendBlockSize).ToString() + " B";
                            if (GateShare.dwCheckServerTimeMax > 1)
                            {
                                GateShare.dwCheckServerTimeMax -= 1;
                            }
                        }
                        nBufferOfM2Size = 0;
                        nReviceMsgSize = 0;
                        nDeCodeMsgSize = 0;
                        nSendBlockSize = 0;
                        nProcessMsgSize = 0;
                        nHumLogonMsgSize = 0;
                        nHumPlayMsgSize = 0;
                    }
                    try
                    {
                        dwProcessReviceMsgLimiTick = HUtil32.GetTickCount();
                        while (true)
                        {
                            if (GateShare.ReviceMsgList.Count <= 0)
                            {
                                break;
                            }
                            UserData = GateShare.ReviceMsgList[0];
                            GateShare.ReviceMsgList.RemoveAt(0);
                            ProcessUserPacket(UserData);
                            UserData = null;
                            if ((HUtil32.GetTickCount() - dwProcessReviceMsgLimiTick) > GateShare.dwProcessReviceMsgTimeLimit)
                            {
                                break;
                            }
                        }
                    }
                    catch (Exception E)
                    {
                        GateShare.AddMainLogMsg("[Exception] DecodeTimerTImer->ProcessUserPacket", 1);
                    }
                    try
                    {
                        dwProcessReviceMsgLimiTick = HUtil32.GetTickCount();
                        while (true)
                        {
                            if (GateShare.SendMsgList.Count <= 0)
                            {
                                break;
                            }
                            UserData = GateShare.SendMsgList[0];
                            GateShare.SendMsgList.RemoveAt(0);
                            ProcessPacket(UserData);
                            UserData = null;
                            if ((HUtil32.GetTickCount() - dwProcessReviceMsgLimiTick) > GateShare.dwProcessSendMsgTimeLimit)
                            {
                                break;
                            }
                        }
                    }
                    catch (Exception E)
                    {
                        GateShare.AddMainLogMsg("[Exception] DecodeTimerTImer->ProcessPacket", 1);
                    }
                    try
                    {
                        dwProcessReviceMsgLimiTick = HUtil32.GetTickCount();
                        if ((HUtil32.GetTickCount() - dwProcessPacketTick) > 300)
                        {
                            dwProcessPacketTick = HUtil32.GetTickCount();
                            if (GateShare.ReviceMsgList.Count > 0)
                            {
                                if (GateShare.dwProcessReviceMsgTimeLimit < 300)
                                {
                                    GateShare.dwProcessReviceMsgTimeLimit++;
                                }
                            }
                            else
                            {
                                if (GateShare.dwProcessReviceMsgTimeLimit > 30)
                                {
                                    GateShare.dwProcessReviceMsgTimeLimit -= 1;
                                }
                            }
                            if (GateShare.SendMsgList.Count > 0)
                            {
                                if (GateShare.dwProcessSendMsgTimeLimit < 300)
                                {
                                    GateShare.dwProcessSendMsgTimeLimit++;
                                }
                            }
                            else
                            {
                                if (GateShare.dwProcessSendMsgTimeLimit > 30)
                                {
                                    GateShare.dwProcessSendMsgTimeLimit -= 1;
                                }
                            }
                            for (var i = 0; i < GateShare.GATEMAXSESSION; i++)
                            {
                                UserSession = GateShare.SessionArray[i];
                                if ((UserSession.Socket != null) && (UserSession.sSendData != ""))
                                {
                                    tUserData.nSocketIdx = i;
                                    tUserData.nSocketHandle = UserSession.nSckHandle;
                                    tUserData.sMsg = "";
                                    ProcessPacket(tUserData);
                                    if ((HUtil32.GetTickCount() - dwProcessReviceMsgLimiTick) > 20)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception E)
                    {
                        GateShare.AddMainLogMsg("[Exception] DecodeTimerTImer->ProcessPacket 2", 1);
                    }
                    // 每二秒向游戏服务器发送一个检查信号
                    if ((HUtil32.GetTickCount() - dwCheckClientTick) > 2000)
                    {
                        dwCheckClientTick = HUtil32.GetTickCount();
                        if (GateShare.boGateReady)
                        {
                            SendServerMsg(Grobal2.GM_CHECKCLIENT, 0, 0, 0, 0, null);
                        }
                        if ((HUtil32.GetTickCount() - GateShare.dwCheckServerTick) > GateShare.dwCheckServerTimeOutTime)
                        {
                            GateShare.boCheckServerFail = true;
                            ClientSocket.Disconnect();
                        }
                        if (dwLoopTime > 30)
                        {
                            dwLoopTime -= 20;
                        }
                        if (dwProcessServerMsgTime > 1)
                        {
                            dwProcessServerMsgTime -= 1;
                        }
                        if (dwProcessClientMsgTime > 1)
                        {
                            dwProcessClientMsgTime -= 1;
                        }
                    }
                    GateShare.boDecodeMsgLock = false;
                }
                catch (Exception E)
                {
                    GateShare.AddMainLogMsg("[Exception] DecodeTimer", 1);
                    GateShare.boDecodeMsgLock = false;
                }
                dwLoopProcessTime = HUtil32.GetTickCount() - dwLoopCheckTick;
                dwLoopCheckTick = HUtil32.GetTickCount();
                if (dwLoopTime < dwLoopProcessTime)
                {
                    dwLoopTime = dwLoopProcessTime;
                }
                if ((HUtil32.GetTickCount() - dwRefConsolMsgTick) > 1000)
                {
                    dwRefConsolMsgTick = HUtil32.GetTickCount();
                    // LabelLoopTime.Text = (dwLoopTime).ToString();
                    // LabelReviceLimitTime.Text = "接收处理限制: " + (GateShare.dwProcessReviceMsgTimeLimit).ToString();
                    // LabelSendLimitTime.Text = "发送处理限制: " + (GateShare.dwProcessSendMsgTimeLimit).ToString();
                    // LabelReceTime.Text = "接收: " + (dwProcessClientMsgTime);
                    // LabelSendTime.Text = "发送: " + (dwProcessServerMsgTime);
                }
            }
        }

        private void ProcessUserPacket(TSendUserData UserData)
        {
            string sMsg = string.Empty;
            string sData = string.Empty;
            string sDefMsg = string.Empty;
            string sDataMsg = string.Empty;
            string sDataText = string.Empty;
            string sHumName = string.Empty;
            string Buffer = string.Empty;
            int nOPacketIdx;
            int nPacketIdx;
            int nDataLen;
            int n14;
            TDefaultMessage DefMsg;
            try
            {
                n14 = 0;
                nProcessMsgSize += UserData.sMsg.Length;
                if ((UserData.nSocketIdx >= 0) && (UserData.nSocketIdx < GateShare.GATEMAXSESSION))
                {
                    if ((UserData.nSocketHandle == GateShare.SessionArray[UserData.nSocketIdx].nSckHandle) && (GateShare.SessionArray[UserData.nSocketIdx].nPacketErrCount < 10))
                    {
                        if (GateShare.SessionArray[UserData.nSocketIdx].sSocData.Length > GateShare.MSGMAXLENGTH)
                        {
                            GateShare.SessionArray[UserData.nSocketIdx].sSocData = "";
                            GateShare.SessionArray[UserData.nSocketIdx].nPacketErrCount = 99;
                            UserData.sMsg = "";
                        }
                        sMsg = GateShare.SessionArray[UserData.nSocketIdx].sSocData + UserData.sMsg;
                        while (true)
                        {
                            sData = "";
                            sMsg = HUtil32.ArrestStringEx(sMsg, "#", "!", ref sData);
                            if (sData.Length > 2)
                            {
                                nPacketIdx = HUtil32.Str_ToInt(sData[1].ToString(), 99); // 将数据名第一位的序号取出
                                if (GateShare.SessionArray[UserData.nSocketIdx].nPacketIdx == nPacketIdx)
                                {
                                    // 如果序号重复则增加错误计数
                                    GateShare.SessionArray[UserData.nSocketIdx].nPacketErrCount++;
                                }
                                else
                                {
                                    nOPacketIdx = GateShare.SessionArray[UserData.nSocketIdx].nPacketIdx;
                                    GateShare.SessionArray[UserData.nSocketIdx].nPacketIdx = nPacketIdx;
                                    sData = sData.Substring(2 - 1, sData.Length - 1);
                                    nDataLen = sData.Length;
                                    if ((nDataLen >= Grobal2.DEFBLOCKSIZE))
                                    {
                                        if (GateShare.SessionArray[UserData.nSocketIdx].boStartLogon)// 第一个人物登录数据包
                                        {
                                            nHumLogonMsgSize += sData.Length;
                                            GateShare.SessionArray[UserData.nSocketIdx].boStartLogon = false;
                                            sData = "#" + (nPacketIdx).ToString() + sData + "!";
                                            //GetMem(Buffer, sData.Length + 1);
                                            //Move(sData[1], Buffer, sData.Length + 1);
                                            //SendServerMsg(Grobal2.GM_DATA, UserData.nSocketIdx, GateShare.SessionArray[UserData.nSocketIdx].Socket.SocketHandle, GateShare.SessionArray[UserData.nSocketIdx].nUserListIndex, sData.Length + 1, Buffer);
                                            //FreeMem(Buffer);
                                        }
                                        else
                                        {
                                            // 普通数据包
                                            nHumPlayMsgSize += sData.Length;
                                            if (nDataLen == Grobal2.DEFBLOCKSIZE)
                                            {
                                                sDefMsg = sData;
                                                sDataMsg = "";
                                            }
                                            else
                                            {
                                                sDefMsg = sData.Substring(1 - 1, Grobal2.DEFBLOCKSIZE);
                                                sDataMsg = sData.Substring(Grobal2.DEFBLOCKSIZE + 1 - 1, sData.Length - Grobal2.DEFBLOCKSIZE);
                                            }
                                            DefMsg = EDcode.DecodeMessage(sDefMsg);
                                            // 检查数据
                                            if (sDataMsg != "")
                                            {
                                                if (DefMsg.Ident == Grobal2.CM_SAY)
                                                {
                                                    // 控制发言间隔时间
                                                    sDataText = EDcode.DeCodeString(sDataMsg);
                                                    if (sDataText != "")
                                                    {
                                                        if (sDataText[1] == '/')
                                                        {
                                                            sDataText = HUtil32.GetValidStr3(sDataText, ref sHumName, new string[] { " " });
                                                            // 限制最长可发字符长度
                                                            FilterSayMsg(ref sDataText);
                                                            sDataText = sHumName + " " + sDataText;
                                                        }
                                                        else
                                                        {
                                                            if (sDataText[1] != '@')
                                                            {
                                                                // 限制最长可发字符长度
                                                                FilterSayMsg(ref sDataText);
                                                            }
                                                        }
                                                    }
                                                    sDataMsg = EDcode.EncodeString(sDataText);
                                                }
                                                //GetMem(Buffer, sDataMsg.Length + 12 + 1);
                                                //Move(DefMsg, Buffer, 12);
                                                //Move(sDataMsg[1], Buffer[12], sDataMsg.Length + 1);
                                                //SendServerMsg(Grobal2.GM_DATA, UserData.nSocketIdx, GateShare.SessionArray[UserData.nSocketIdx].Socket.SocketHandle, GateShare.SessionArray[UserData.nSocketIdx].nUserListIndex, sDataMsg.Length + 12 + 1, Buffer);
                                                //FreeMem(Buffer);
                                            }
                                            else
                                            {
                                                //GetMem(Buffer, 12);
                                                //Move(DefMsg, Buffer, 12);
                                                //SendServerMsg(Grobal2.GM_DATA, UserData.nSocketIdx, GateShare.SessionArray[UserData.nSocketIdx].Socket.SocketHandle, GateShare.SessionArray[UserData.nSocketIdx].nUserListIndex, 12, Buffer);
                                                //FreeMem(Buffer);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (n14 >= 1)
                                {
                                    sMsg = "";
                                }
                                else
                                {
                                    n14++;
                                }
                            }
                            if (HUtil32.TagCount(sMsg, '!') < 1)
                            {
                                break;
                            }
                        }
                        GateShare.SessionArray[UserData.nSocketIdx].sSocData = sMsg;
                    }
                    else
                    {
                        GateShare.SessionArray[UserData.nSocketIdx].sSocData = "";
                    }
                }
            }
            catch
            {
                if ((UserData.nSocketIdx >= 0) && (UserData.nSocketIdx < GateShare.GATEMAXSESSION))
                {
                    sData = "[" + GateShare.SessionArray[UserData.nSocketIdx].sRemoteAddr + "]";
                }
                GateShare.AddMainLogMsg("[Exception] ProcessUserPacket" + sData, 1);
            }
        }

        private void ProcessPacket(TSendUserData UserData)
        {
            string sData;
            string sSendBlock;
            TSessionInfo UserSession;
            if ((UserData.nSocketIdx >= 0) && (UserData.nSocketIdx < GateShare.GATEMAXSESSION))
            {
                UserSession = GateShare.SessionArray[UserData.nSocketIdx];
                if (UserSession.nSckHandle == UserData.nSocketHandle)
                {
                    nDeCodeMsgSize += UserData.sMsg.Length;
                    sData = UserSession.sSendData + UserData.sMsg;
                    while (sData != "")
                    {
                        if (sData.Length > GateShare.nClientSendBlockSize)
                        {
                            sSendBlock = sData.Substring(0 ,GateShare.nClientSendBlockSize);
                            sData = sData.Substring(GateShare.nClientSendBlockSize ,sData.Length - GateShare.nClientSendBlockSize);
                        }
                        else
                        {
                            sSendBlock = sData;
                            sData = "";
                        }
                        if (!UserSession.boSendAvailable)
                        {
                            if (HUtil32.GetTickCount() > UserSession.dwTimeOutTime)
                            {
                                UserSession.boSendAvailable = true;
                                UserSession.nCheckSendLength = 0;
                                GateShare.boSendHoldTimeOut = true;
                                GateShare.dwSendHoldTick = HUtil32.GetTickCount();
                            }
                        }
                        if (UserSession.boSendAvailable)
                        {
                            if (UserSession.nCheckSendLength >= GateShare.SENDCHECKSIZE)
                            {
                                if (!UserSession.boSendCheck)
                                {
                                    UserSession.boSendCheck = true;
                                    sSendBlock = "*" + sSendBlock;
                                }
                                if (UserSession.nCheckSendLength >= GateShare.SENDCHECKSIZEMAX)
                                {
                                    UserSession.boSendAvailable = false;
                                    UserSession.dwTimeOutTime = HUtil32.GetTickCount() + GateShare.dwClientCheckTimeOut;
                                }
                            }
                            if ((UserSession.Socket != null) && (UserSession.Socket.Connected))
                            {
                                nSendBlockSize += sSendBlock.Length;
                                UserSession.Socket.SendText(sSendBlock);
                            }
                            UserSession.nCheckSendLength += sSendBlock.Length;
                        }
                        else
                        {
                            sData = sSendBlock + sData;
                            break;
                        }
                    }
                    UserSession.sSendData = sData;
                }
            }
        }

        private void FilterSayMsg(ref string sMsg)
        {
            int nLen;
            string sReplaceText;
            string sFilterText;
            try
            {
                HUtil32.EnterCriticalSection(GateShare.CS_FilterMsg);
                for (var i = 0; i < GateShare.AbuseList.Count; i++)
                {
                    sFilterText = GateShare.AbuseList[i];
                    sReplaceText = "";
                    if ((sMsg.IndexOf(sFilterText) != -1))
                    {
                        for (nLen = 1; nLen <= sFilterText.Length; nLen++)
                        {
                            sReplaceText = sReplaceText + GateShare.sReplaceWord;
                        }
                        sMsg = sMsg.Replace(sFilterText, sReplaceText);
                    }
                }
            }
            finally
            {
                HUtil32.LeaveCriticalSection(GateShare.CS_FilterMsg);
            }
        }

        public void ClientSocketError(Object Sender, Socket Socket)
        {
            Socket.Close();
            boServerReady = false;
        }
        
        public void StartService()
        {
            try
            {
                GateShare.Initialization();
                GateShare.AddMainLogMsg("正在启动服务...", 2);
                GateShare.boServiceStart = true;
                GateShare.boGateReady = false;
                GateShare.boCheckServerFail = false;
                GateShare.boSendHoldTimeOut = false;
                GateShare.SessionCount = 0;
                LoadConfig();
                RestSessionArray();
                GateShare.dwProcessReviceMsgTimeLimit = 50;
                GateShare.dwProcessSendMsgTimeLimit = 50;
                boServerReady = false;
                dwReConnectServerTime = HUtil32.GetTickCount() - 25000;
                dwRefConsolMsgTick = HUtil32.GetTickCount();
                
                //ServerSocket.Active = false;
                //ServerSocket.Address = GateShare.GateAddr;
                //ServerSocket.Port = GateShare.GatePort;
                ServerSocket = new ISocketServer(20,ushort.MaxValue);
                ServerSocket.OnClientConnect += ServerSocketClientConnect;
                ServerSocket.OnClientDisconnect += ServerSocketClientDisconnect;
                ServerSocket.OnClientRead += ServerSocketClientRead;
                ServerSocket.OnClientError += ServerSocketClientError;
                ServerSocket.Init();
                ServerSocket.Start(GateShare.GateAddr, GateShare.GatePort);
                //ServerSocket.Active = true;
                
                //ClientSocket.Active = false;
                ClientSocket = new IClientScoket();
                ClientSocket.OnConnected += ClientSocketConnect;
                ClientSocket.OnDisconnected += ClientSocketDisconnect;
                ClientSocket.ReceivedDatagram += ClientSocketRead;
                ClientSocket.Address = GateShare.ServerAddr;
                ClientSocket.Port = GateShare.ServerPort;
                ClientSocket.Connect();
                //ClientSocket.Active = true;
                
                //SendTimer.Enabled = true;

                decodeTimer = new Timer(DecodeTimer, null, 0, 10);
                
                GateShare.AddMainLogMsg("服务已启动成功...", 2);
                GateShare.AddMainLogMsg("欢迎使用翎风系列游戏软件...",0);
                GateShare.AddMainLogMsg("网站:http://www.gameofmir.com",0);
                GateShare.AddMainLogMsg("论坛:http://bbs.gameofmir.com",0);
            }
            catch (Exception E)
            {
                GateShare.AddMainLogMsg(E.Message, 0);
            }
        }

        private void StopService()
        {
            GateShare.AddMainLogMsg("正在停止服务...", 2);
            GateShare.boServiceStart = false;
            GateShare.boGateReady = false;
            for (var nSockIdx = 0; nSockIdx < GateShare.GATEMAXSESSION; nSockIdx ++ )
            {
                if (GateShare.SessionArray[nSockIdx].Socket != null)
                {
                    GateShare.SessionArray[nSockIdx].Socket.Close();
                }
            }
            ServerSocket.Shutdown();
            ClientSocket.Disconnect();
            GateShare.AddMainLogMsg("服务停止成功...", 2);
        }

        private void LoadConfig()
        {
            GateShare.AddMainLogMsg("正在加载配置信息...", 3);
            if (GateShare.Conf != null)
            {
                 GateShare.TitleName = GateShare.Conf.ReadString(GateShare.GateClass, "Title", GateShare.TitleName);
                 GateShare.ServerAddr = GateShare.Conf.ReadString(GateShare.GateClass, "ServerAddr", GateShare.ServerAddr);
                 GateShare.ServerPort = GateShare.Conf.ReadInteger(GateShare.GateClass, "ServerPort", GateShare.ServerPort);
                 GateShare.GateAddr = GateShare.Conf.ReadString(GateShare.GateClass, "GateAddr", GateShare.GateAddr);
                 GateShare.GatePort = GateShare.Conf.ReadInteger(GateShare.GateClass, "GatePort", GateShare.GatePort);
                 GateShare.nShowLogLevel = GateShare.Conf.ReadInteger(GateShare.GateClass, "ShowLogLevel", GateShare.nShowLogLevel);
                 GateShare.boShowBite = GateShare.Conf.ReadBool(GateShare.GateClass, "ShowBite", GateShare.boShowBite);
                 GateShare.nMaxConnOfIPaddr = GateShare.Conf.ReadInteger(GateShare.GateClass, "MaxConnOfIPaddr", GateShare.nMaxConnOfIPaddr);
                 GateShare.BlockMethod = ((TBlockIPMethod)(GateShare.Conf.ReadInteger(GateShare.GateClass, "BlockMethod", ((int)GateShare.BlockMethod))));
                 GateShare.nMaxClientPacketSize = GateShare.Conf.ReadInteger(GateShare.GateClass, "MaxClientPacketSize", GateShare.nMaxClientPacketSize);
                 GateShare.nNomClientPacketSize = GateShare.Conf.ReadInteger(GateShare.GateClass, "NomClientPacketSize", GateShare.nNomClientPacketSize);
                 GateShare.nMaxClientMsgCount = GateShare.Conf.ReadInteger(GateShare.GateClass, "MaxClientMsgCount", GateShare.nMaxClientMsgCount);
                 GateShare.bokickOverPacketSize = GateShare.Conf.ReadBool(GateShare.GateClass, "kickOverPacket", GateShare.bokickOverPacketSize);
                 GateShare.dwCheckServerTimeOutTime = GateShare.Conf.ReadInteger<long>(GateShare.GateClass, "ServerCheckTimeOut", GateShare.dwCheckServerTimeOutTime);
                 GateShare.nClientSendBlockSize = GateShare.Conf.ReadInteger(GateShare.GateClass, "ClientSendBlockSize", GateShare.nClientSendBlockSize);
                 GateShare.dwClientTimeOutTime = GateShare.Conf.ReadInteger<long>(GateShare.GateClass, "ClientTimeOutTime", GateShare.dwClientTimeOutTime);
                 GateShare.dwSessionTimeOutTime = GateShare.Conf.ReadInteger<long>(GateShare.GateClass, "SessionTimeOutTime", GateShare.dwSessionTimeOutTime);
                 GateShare.nSayMsgMaxLen = GateShare.Conf.ReadInteger(GateShare.GateClass, "SayMsgMaxLen", GateShare.nSayMsgMaxLen);
                 GateShare.dwSayMsgTime = GateShare.Conf.ReadInteger<long>(GateShare.GateClass, "SayMsgTime", GateShare.dwSayMsgTime);
            }
            GateShare.AddMainLogMsg("配置信息加载完成...", 3);
            GateShare.LoadAbuseFile();
            GateShare.LoadBlockIPFile();
        }

        private void ShowMainLogMsg()
        {
            if ((HUtil32.GetTickCount() - dwShowMainLogTick) < 200)
            {
                return;
            }
            dwShowMainLogTick = HUtil32.GetTickCount();
            try
            {
                boShowLocked = true;
                try
                {
                    HUtil32.EnterCriticalSection(GateShare.CS_MainLog);
                    for (var i = 0; i < GateShare.MainLogMsgList.Count; i++)
                    {
                        TempLogList.Add(GateShare.MainLogMsgList[i]);
                    }
                    GateShare.MainLogMsgList.Clear();
                }
                finally
                {
                    HUtil32.LeaveCriticalSection(GateShare.CS_MainLog);
                }
                for (var i = 0; i < TempLogList.Count; i++)
                {
                    Console.WriteLine(TempLogList[i]);
                }
                TempLogList.Clear();
            }
            finally
            {
                boShowLocked = false;
            }
        }
        
        public void FormDestroy(Object Sender)
        {
            //GateShare.BlockIPList.SaveToFile(".\\BlockIPList.txt");
        }

        public void ShowLogMsg(bool boFlag)
        {
            // int nHeight;
            // switch(boFlag)
            // {
            //     case true:
            //         nHeight = Panel.Height;
            //         Panel.Height = 0;
            //         MemoLog.Height = nHeight;
            //         MemoLog.Top = Panel.Top;
            //         break;
            //     case false:
            //         nHeight = MemoLog.Height;
            //         MemoLog.Height = 0;
            //         Panel.Height = nHeight;
            //         break;
            // }
        }

        public void StartTimerTimer(System.Object Sender, System.EventArgs _e1)
        {
            if (GateShare.boStarted)
            {
                //StartTimer.Enabled = false;
                StopService();
                GateShare.boClose = true;
                //this.Close();
            }
            else
            {
                //MENU_VIEW_LOGMSGClick(Sender);
                GateShare.boStarted = true;
                //StartTimer.Enabled = false;
                StartService();
            }
        }

        public void TimerTimer(System.Object Sender, System.EventArgs _e1)
        {
            // if (ServerSocket.Active)
            // {
            //     StatusBar.Panels[0].Text = (ServerSocket.Port).ToString();
            //     POPMENU_PORT.Text = (ServerSocket.Port).ToString();
            //     if (GateShare.boSendHoldTimeOut)
            //     {
            //         StatusBar.Panels[2].Text = (GateShare.SessionCount).ToString() + "/#" + (ServerSocket.Socket.ActiveConnections).ToString();
            //         POPMENU_CONNCOUNT.Text = (GateShare.SessionCount).ToString() + "/#" + (ServerSocket.Socket.ActiveConnections).ToString();
            //     }
            //     else
            //     {
            //         StatusBar.Panels[2].Text = (GateShare.SessionCount).ToString() + "/" + (ServerSocket.Socket.ActiveConnections).ToString();
            //         POPMENU_CONNCOUNT.Text = (GateShare.SessionCount).ToString() + "/" + (ServerSocket.Socket.ActiveConnections).ToString();
            //     }
            // }
            // else
            // {
            //     StatusBar.Panels[0].Text = "????";
            //     StatusBar.Panels[2].Text = "????";
            //     POPMENU_CONNCOUNT.Text = "????";
            // }
        }

        private void RestSessionArray()
        {
            TSessionInfo tSession;
            for (var i = 0; i < GateShare.GATEMAXSESSION; i ++ )
            {
                if (GateShare.SessionArray[i] == null)
                {
                    GateShare.SessionArray[i] = new TSessionInfo();
                }
                tSession = GateShare.SessionArray[i];
                tSession.Socket = null;
                tSession.sSocData = "";
                tSession.sSendData = "";
                tSession.nUserListIndex = 0;
                tSession.nPacketIdx =  -1;
                tSession.nPacketErrCount = 0;
                tSession.boStartLogon = true;
                tSession.boSendLock = false;
                tSession.boOverNomSize = false;
                tSession.nOverNomSizeCount = 0;
                tSession.dwSendLatestTime = HUtil32.GetTickCount();
                tSession.boSendAvailable = true;
                tSession.boSendCheck = false;
                tSession.nCheckSendLength = 0;
                tSession.nReceiveLength = 0;
                tSession.dwReceiveTick = HUtil32.GetTickCount();
                tSession.nSckHandle =  -1;
                tSession.dwSayMsgTick = HUtil32.GetTickCount();
            }
        }

        private void ServerSocketClientConnect(object Sender,AsyncUserToken e)
        {
            ushort nSockIdx = (ushort)e.nIndex;
            TSessionInfo UserSession;
            string sRemoteAddress = e.RemoteIPaddr;
            if (GateShare.boGateReady)
            {
                try
                {
                    for (nSockIdx = 0; nSockIdx < GateShare.GATEMAXSESSION; nSockIdx++)
                    {
                        UserSession = GateShare.SessionArray[nSockIdx];
                        if (UserSession.Socket == null)
                        {
                            UserSession.Socket = e.Socket;
                            UserSession.sSocData = "";
                            UserSession.sSendData = "";
                            UserSession.nUserListIndex = 0;
                            UserSession.nPacketIdx = -1;
                            UserSession.nPacketErrCount = 0;
                            UserSession.boStartLogon = true;
                            UserSession.boSendLock = false;
                            UserSession.dwSendLatestTime = HUtil32.GetTickCount();
                            UserSession.boSendAvailable = true;
                            UserSession.boSendCheck = false;
                            UserSession.nCheckSendLength = 0;
                            UserSession.nReceiveLength = 0;
                            UserSession.dwReceiveTick = HUtil32.GetTickCount();
                            //UserSession.nSckHandle = Socket.SocketHandle;
                            UserSession.sRemoteAddr = sRemoteAddress;
                            UserSession.boOverNomSize = false;
                            UserSession.nOverNomSizeCount = 0;
                            UserSession.dwSayMsgTick = HUtil32.GetTickCount();
                            GateShare.SessionCount++;
                            break;
                        }
                    }
                }
                finally
                {
                }
                if (nSockIdx < GateShare.GATEMAXSESSION)
                {
                    SendServerMsg(Grobal2.GM_OPEN, nSockIdx, e.nIndex, 0, e.RemoteIPaddr.Length + 1, e.RemoteIPaddr);
                    //Socket.nIndex = nSockIdx;
                    GateShare.AddMainLogMsg("开始连接: " + sRemoteAddress, 5);
                }
                else
                {
                    //Socket.nIndex =  -1;
                    e.Socket.Close();
                    GateShare.AddMainLogMsg("禁止连接: " + sRemoteAddress, 1);
                }
            }
            else
            {
                //Socket.nIndex =  -1;
                e.Socket.Close();
                GateShare.AddMainLogMsg("禁止连接: " + sRemoteAddress, 1);
            }
        }

        private void ServerSocketClientDisconnect(object Sender, AsyncUserToken e)
        {
            TSessionInfo UserSession;
            string sRemoteAddr = e.RemoteIPaddr;
            int  nSockIndex = e.nIndex;
            if ((nSockIndex >= 0) && (nSockIndex < GateShare.GATEMAXSESSION))
            {
                UserSession = GateShare.SessionArray[nSockIndex];
                UserSession.Socket = null;
                UserSession.nSckHandle = -1;
                UserSession.sSocData = "";
                UserSession.sSendData = "";
                //Socket.nIndex =  -1;
                GateShare.SessionCount -= 1;
                if (GateShare.boGateReady)
                {
                    SendServerMsg(Grobal2.GM_CLOSE, 0, e.nIndex, 0, 0, null);
                    GateShare.AddMainLogMsg("断开连接: " + sRemoteAddr, 5);
                }
            }
        }

        private void ServerSocketClientError(object Sender, AsyncSocketErrorEventArgs e)
        {
            
        }

        private void ServerSocketClientRead(object Sender, AsyncUserToken token)
        {
            long dwProcessMsgTick = 0;
            long dwProcessMsgTime= 0;
            var nReviceLen= 0;
            var sReviceMsg = string.Empty;
            var sRemoteAddress= string.Empty;
            var nSocketIndex= token.nIndex;
            var nPos= 0;
            TSendUserData UserData = null;
            var nMsgCount= 0;
            TSessionInfo UserSession = null;
            try
            {
                dwProcessMsgTick = HUtil32.GetTickCount();
                sRemoteAddress = token.RemoteIPaddr;
                sReviceMsg = HUtil32.GetString(token.ReceiveBuffer, 0, token.BytesReceived);
                nReviceLen = token.BytesReceived;
                if (nSocketIndex is >= 0 and < GateShare.GATEMAXSESSION && (!string.IsNullOrEmpty(sReviceMsg)) && boServerReady)
                {
                    if (nReviceLen > GateShare.nNomClientPacketSize)
                    {
                        nMsgCount = HUtil32.TagCount(sReviceMsg, '!');
                        if ((nMsgCount > GateShare.nMaxClientMsgCount) || (nReviceLen > GateShare.nMaxClientPacketSize))
                        {
                            if (GateShare.bokickOverPacketSize)
                            {
                                switch (GateShare.BlockMethod)
                                {
                                    case TBlockIPMethod.mDisconnect:
                                        break;
                                    case TBlockIPMethod.mBlock:
                                        GateShare.TempBlockIPList.Add(sRemoteAddress);
                                        CloseConnect(sRemoteAddress);
                                        break;
                                    case TBlockIPMethod.mBlockList:
                                        GateShare.BlockIPList.Add(sRemoteAddress);
                                        CloseConnect(sRemoteAddress);
                                        break;
                                }
                                GateShare.AddMainLogMsg("踢除连接: IP(" + sRemoteAddress + "),信息数量(" + nMsgCount + "),数据包长度(" + nReviceLen + ")", 1);
                                token.Socket.Close();
                            }
                            return;
                        }
                    }
                    nReviceMsgSize += sReviceMsg.Length;
                    if (GateShare.boShowSckData)
                    {
                        GateShare.AddMainLogMsg(sReviceMsg, 0);
                    }
                    UserSession = GateShare.SessionArray[nSocketIndex];
                    if (UserSession.Socket == token.Socket)
                    {
                        nPos = sReviceMsg.IndexOf("*");
                        if (nPos > 0)
                        {
                            UserSession.boSendAvailable = true;
                            UserSession.boSendCheck = false;
                            UserSession.nCheckSendLength = 0;
                            UserSession.dwReceiveTick = HUtil32.GetTickCount();
                            sReviceMsg = sReviceMsg.Substring(1 - 1, nPos - 1) + sReviceMsg.Substring(nPos + 1 - 1, sReviceMsg.Length);
                        }
                        if ((sReviceMsg != "") && GateShare.boGateReady && !GateShare.boCheckServerFail)
                        {
                            UserData = new TSendUserData();
                            UserData.nSocketIdx = nSocketIndex;
                            //UserData.nSocketHandle = Socket.SocketHandle;
                            UserData.sMsg = sReviceMsg;
                            GateShare.ReviceMsgList.Add(UserData);
                        }
                    }
                }
                dwProcessMsgTime = HUtil32.GetTickCount() - dwProcessMsgTick;
                if (dwProcessMsgTime > dwProcessClientMsgTime)
                {
                    dwProcessClientMsgTime = dwProcessMsgTime;
                }
            }
            catch
            {
                GateShare.AddMainLogMsg("[Exception] ClientRead", 1);
            }
        }

        public void SendTimerTimer(System.Object Sender, System.EventArgs _e1)
        {
            TSessionInfo UserSession;
            if (( HUtil32.GetTickCount() - GateShare.dwSendHoldTick) > 3000)
            {
                GateShare.boSendHoldTimeOut = false;
            }
            if (GateShare.boGateReady && !GateShare.boCheckServerFail)
            {
                for (var i = 0; i < GateShare.GATEMAXSESSION; i ++ )
                {
                    UserSession = GateShare.SessionArray[i];
                    if (UserSession.Socket != null)
                    {
                        if (( HUtil32.GetTickCount() - UserSession.dwReceiveTick) > GateShare.dwSessionTimeOutTime)
                        {
                            UserSession.Socket.Close();
                            UserSession.Socket = null;
                            UserSession.nSckHandle =  -1;
                        }
                    }
                }
            }
            if (!GateShare.boGateReady)
            {
                //StatusBar.Panels[1].Text = "未连接";
                //StatusBar.Panels[3].Text = "????";
                //POPMENU_CHECKTICK.Text = "????";
                if ((( HUtil32.GetTickCount() - dwReConnectServerTime) > 1000) && GateShare.boServiceStart)
                {
                    dwReConnectServerTime = HUtil32.GetTickCount();
                    //ClientSocket.Active = false;
                    ClientSocket.Address = GateShare.ServerAddr;
                    ClientSocket.Port = GateShare.ServerPort;
                    ClientSocket.Connect();
                    //ClientSocket.Active = true;
                }
            }
            else
            {
                if (GateShare.boCheckServerFail)
                {
                    //StatusBar.Panels[1].Text = "超时";
                }
                else
                {
                    //StatusBar.Panels[1].Text = "已连接";
                    //LbLack.Text = (GateShare.n45AA84).ToString() + "/" + (GateShare.n45AA80).ToString();
                }
                GateShare.dwCheckServerTimeMin =  HUtil32.GetTickCount() - GateShare.dwCheckServerTick;
                if (GateShare.dwCheckServerTimeMax < GateShare.dwCheckServerTimeMin)
                {
                    GateShare.dwCheckServerTimeMax = GateShare.dwCheckServerTimeMin;
                }
               // StatusBar.Panels[3].Text = (GateShare.dwCheckServerTimeMin).ToString() + "/" + (GateShare.dwCheckServerTimeMax).ToString();
                //POPMENU_CHECKTICK.Text = (GateShare.dwCheckServerTimeMin).ToString() + "/" + (GateShare.dwCheckServerTimeMax).ToString();
            }
        }

        private void ClientSocketConnect(object sender, DSCClientConnectedEventArgs e)
        {
            GateShare.boGateReady = true;
            GateShare.dwCheckServerTick = HUtil32.GetTickCount();
            GateShare.dwCheckRecviceTick = HUtil32.GetTickCount();
            RestSessionArray();
            boServerReady = true;
            GateShare.dwCheckServerTimeMax = 0;
            GateShare.dwCheckServerTimeMax = 0;
        }

        private void ClientSocketDisconnect(object sender, DSCClientConnectedEventArgs e)
        {
            TSessionInfo UserSession;
            for (var i = 0; i < GateShare.GATEMAXSESSION; i ++ )
            {
                UserSession = GateShare.SessionArray[i];
                if (UserSession.Socket != null)
                {
                    UserSession.Socket.Close();
                    UserSession.Socket = null;
                    UserSession.nSckHandle =  -1;
                }
            }
            RestSessionArray();
            if (GateShare.SocketBuffer != null)
            {
               // FreeMem(GateShare.SocketBuffer);
            }
            GateShare.SocketBuffer = null;
            for (var i = 0; i < GateShare.List_45AA58.Count; i ++ )
            {
                
            }
            GateShare.List_45AA58.Clear();
            GateShare.boGateReady = false;
            boServerReady = false;
        }

        private void ClientSocketRead(object sender, DSCClientDataInEventArgs e)
        {
            long dwTime10;
            long dwTick14;
            int nMsgLen;
            try
            {
                Console.WriteLine("todo ClientSocketRead");
                dwTick14 = HUtil32.GetTickCount();
                nMsgLen = e.Buff.Length;
                ProcReceiveBuffer(e.Buff, nMsgLen);
                nBufferOfM2Size += nMsgLen;
                dwTime10 = HUtil32.GetTickCount() - dwTick14;
                if (dwProcessServerMsgTime < dwTime10)
                {
                    dwProcessServerMsgTime = dwTime10;
                }
            }
            catch (Exception E)
            {
                GateShare.AddMainLogMsg("[Exception] ClientSocketRead", 1);
            }
        }

        private void ProcReceiveBuffer(byte[] tBuffer, int nMsgLen)
        {
            int nLen;
            byte[] Buff;
            TMsgHeader pMsg;
            byte[] MsgBuff;
            byte[] TempBuff;
            try
            {
                //ReallocMem(GateShare.SocketBuffer, GateShare.nBuffLen + nMsgLen);
                GateShare.SocketBuffer = new byte[GateShare.nBuffLen + nMsgLen];
                //Move(tBuffer, GateShare.SocketBuffer[GateShare.nBuffLen], nMsgLen);
                Buffer.BlockCopy(tBuffer, 0, GateShare.SocketBuffer, 0, nMsgLen);
                //FreeMem(tBuffer);
                nLen = GateShare.nBuffLen + nMsgLen;
                Buff = GateShare.SocketBuffer;
                if (nLen >= 20)
                {
                    while (true)
                    {
                        pMsg = new TMsgHeader(Buff);
                        if (pMsg.dwCode == Grobal2.RUNGATECODE)
                        {
                            if ((Math.Abs(pMsg.nLength) + 20) > nLen)
                            {
                                break;
                            }
                            //MsgBuff = Ptr((long) Buff + 20);
                            MsgBuff = new byte[Buff.Length - 20];
                            Buffer.BlockCopy(Buff, 20, MsgBuff, 0, MsgBuff.Length);
                            switch (pMsg.wIdent)
                            {
                                case Grobal2.GM_CHECKSERVER:
                                    GateShare.boCheckServerFail = false;
                                    GateShare.dwCheckServerTick = HUtil32.GetTickCount();
                                    break;
                                case Grobal2.GM_SERVERUSERINDEX:
                                    if ((pMsg.wGSocketIdx < GateShare.GATEMAXSESSION) && (pMsg.nSocket ==
                                        GateShare.SessionArray[pMsg.wGSocketIdx].nSckHandle))
                                    {
                                        GateShare.SessionArray[pMsg.wGSocketIdx].nUserListIndex = pMsg.wUserListIndex;
                                    }
                                    break;
                                case Grobal2.GM_RECEIVE_OK:
                                    GateShare.dwCheckServerTimeMin = HUtil32.GetTickCount() - GateShare.dwCheckRecviceTick;
                                    if (GateShare.dwCheckServerTimeMin > GateShare.dwCheckServerTimeMax)
                                    {
                                        GateShare.dwCheckServerTimeMax = GateShare.dwCheckServerTimeMin;
                                    }
                                    GateShare.dwCheckRecviceTick = HUtil32.GetTickCount();
                                    SendServerMsg(Grobal2.GM_RECEIVE_OK, 0, 0, 0, 0, null);
                                    break;
                                case Grobal2.GM_DATA:
                                    ProcessMakeSocketStr(pMsg.nSocket, pMsg.wGSocketIdx, MsgBuff, pMsg.nLength);
                                    break;
                                case Grobal2.GM_TEST:
                                    break;
                            }
                            //Buff = Buff[20 + Math.Abs(pMsg.nLength)];
                            var tempBuff = new byte[20 + Math.Abs(pMsg.nLength)];
                            Buffer.BlockCopy(Buff, 20, tempBuff, 0, tempBuff.Length);
                            nLen = nLen - (Math.Abs(pMsg.nLength) + 20);
                        }
                        else
                        {
                            //Buff++;
                            nLen -= 1;
                        }
                        if (nLen < 20)
                        {
                            break;
                        }
                    }
                }
                if (nLen > 0)
                {
                    //GetMem(TempBuff, nLen);
                    TempBuff = new byte[nLen];
                    Buffer.BlockCopy(Buff, 0, TempBuff, 0, nLen);
                    //Move(Buff, TempBuff, nLen);
                    //FreeMem(GateShare.SocketBuffer);
                    GateShare.SocketBuffer = TempBuff;
                    GateShare.nBuffLen = nLen;
                }
                else
                {
                    //FreeMem(GateShare.SocketBuffer);
                    GateShare.SocketBuffer = null;
                    GateShare.nBuffLen = 0;
                }
            }
            catch (Exception E)
            {
                GateShare.AddMainLogMsg("[Exception] ProcReceiveBuffer", 1);
            }
        }

        private void ProcessMakeSocketStr(int nSocket, int nSocketIndex, byte[] buffer, int nMsgLen)
        {
            string sSendMsg;
            TDefaultMessage pDefMsg;
            TSendUserData UserData;
            try
            {
                sSendMsg = "";
                if (nMsgLen < 0)
                {
                    sSendMsg = "#" + buffer + "!";
                }
                else
                {
                    if ((nMsgLen >= 12))
                    {
                        pDefMsg = new TDefaultMessage(buffer);//((TDefaultMessage) (Buffer));
                        if (nMsgLen > 12)
                        {
                            var buffStr = new byte[buffer.Length - 12];
                            Buffer.BlockCopy(buffer, 12, buffStr, 0, buffStr.Length);
                            sSendMsg = "#" + EDcode.EncodeMessage(pDefMsg) + HUtil32.GetString(buffStr,0,buffStr.Length) + "!";
                            //sSendMsg = "#" + EDcode.EncodeMessage(pDefMsg) + ((Buffer[12] as string) as string) + "!";
                        }
                        else
                        {
                            sSendMsg = "#" + EDcode.EncodeMessage(pDefMsg) + "!";
                        }
                    }
                }
                if ((nSocketIndex >= 0) && (nSocketIndex < GateShare.GATEMAXSESSION) &&
                    (sSendMsg != ""))
                {
                    UserData = new TSendUserData();
                    UserData.nSocketIdx = nSocketIndex;
                    UserData.nSocketHandle = nSocket;
                    UserData.sMsg = sSendMsg;
                    GateShare.SendMsgList.Add(UserData);
                }
            }
            catch (Exception E)
            {
                GateShare.AddMainLogMsg("[Exception] ProcessMakeSocketStr", 1);
            }
        }

        private bool IsBlockIP(string sIPaddr)
        {
            bool result= false;
            string sBlockIPaddr;
            for (var i = 0; i < GateShare.TempBlockIPList.Count; i ++ )
            {
                sBlockIPaddr = GateShare.TempBlockIPList[i];
                if ((sIPaddr).ToLower().CompareTo((sBlockIPaddr).ToLower()) == 0)
                {
                    result = true;
                    break;
                }
            }
            for (var i = 0; i < GateShare.BlockIPList.Count; i ++ )
            {
                sBlockIPaddr = GateShare.BlockIPList[i];
                if (HUtil32.CompareLStr(sIPaddr, sBlockIPaddr, sBlockIPaddr.Length))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        private bool IsConnLimited(string sIPaddr)
        {
            bool result= false;
            int nCount= 0;
            // for (var i = 0; i < ServerSocket.Socket.ActiveConnections; i ++ )
            // {
            //     if ((sIPaddr).ToLower().CompareTo((ServerSocket.Connections[i].RemoteAddress).ToLower()) == 0)
            //     {
            //         nCount ++;
            //     }
            // }
            if (nCount > GateShare.nMaxConnOfIPaddr)
            {
                result = true;
            }
            return result;
        }

        private void CloseConnect(string sIPaddr)
        {
            var userSocket = ServerSocket.GetSocket(sIPaddr);
            userSocket?.Socket.Close();
        }

        private bool CheckDefMsg(TDefaultMessage DefMsg, TSessionInfo SessionInfo)
        {
            var result = true;
            switch(DefMsg.Ident)
            {
                case Grobal2.CM_WALK:
                case Grobal2.CM_RUN:
                    break;
                case Grobal2.CM_TURN:
                    break;
                case Grobal2.CM_HIT:
                case Grobal2.CM_HEAVYHIT:
                case Grobal2.CM_BIGHIT:
                case Grobal2.CM_POWERHIT:
                case Grobal2.CM_LONGHIT:
                case Grobal2.CM_WIDEHIT:
                case Grobal2.CM_FIREHIT:
                    break;
                case Grobal2.CM_SPELL:
                    break;
                case Grobal2.CM_DROPITEM:
                    break;
                case Grobal2.CM_PICKUP:
                    break;
            }
            return result;
        }

        private void CloseAllUser()
        {
            for (var nSockIdx = 0; nSockIdx < GateShare.GATEMAXSESSION; nSockIdx ++ )
            {
                if (GateShare.SessionArray[nSockIdx].Socket != null)
                {
                    GateShare.SessionArray[nSockIdx].Socket.Close();
                }
            }
        }
    } 
}
