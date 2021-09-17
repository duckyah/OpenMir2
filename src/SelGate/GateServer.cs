using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using SystemModule;
using SystemModule.Sockets;

namespace SelGate
{
    public class GateServer
    {
        private readonly ISocketServer ServerSocket;
        private string sProcMsg = String.Empty;
        private long dwSendKeepAliveTick = 0;
        private ArrayList StringList318 = null;
        private long dwDecodeMsgTime = 0;
        private readonly GateClient gateClient;
        private Timer decodeTimer;


        public GateServer(GateClient gateClient)
        {
            ServerSocket = new ISocketServer(ushort.MaxValue, 1024);
            ServerSocket.OnClientConnect += ServerSocketClientConnect;
            ServerSocket.OnClientDisconnect += ServerSocketClientDisconnect;
            ServerSocket.OnClientRead += ServerSocketClientRead;
            ServerSocket.Init();
            this.gateClient = gateClient;
            StringList318 = new ArrayList();
        }

        public void Start()
        {
            //网关对外开放的端口号，此端口标准为 7200，此端口可根据自己的要求进行修改。
            ServerSocket.Start(GateShare.GateAddr, GateShare.GatePort);
            //SendTimer.Enabled = true;
            decodeTimer = new Timer(DecodeTimer, null, 0, 1);
        }

        private void ServerSocketClientConnect(object sender, AsyncUserToken e)
        {
            TUserSession UserSession;
            string sLocalIPaddr = string.Empty;
            int nSockIndex;
            TSockaddr IPaddr;
            var sRemoteIPaddr = e.RemoteIPaddr;
            if (GateShare.g_boDynamicIPDisMode)
            {
                sLocalIPaddr = sRemoteIPaddr;
            }
            else
            {
                sLocalIPaddr = sRemoteIPaddr;
            }
            if (IsBlockIP(sRemoteIPaddr))
            {
                GateShare.MainOutMessage("过滤连接: " + sRemoteIPaddr, 1);
                e.Socket.Close();
                return;
            }
            if (IsConnLimited(sRemoteIPaddr))
            {
                switch (GateShare.BlockMethod)
                {
                    case TBlockIPMethod.mDisconnect:
                        e.Socket.Close();
                        break;
                    case TBlockIPMethod.mBlock:
                        IPaddr = new TSockaddr();
                        IPaddr.nIPaddr = HUtil32.IpToInt(sRemoteIPaddr);
                        GateShare.TempBlockIPList.Add(IPaddr);
                        CloseConnect(sRemoteIPaddr);
                        break;
                    case TBlockIPMethod.mBlockList:
                        IPaddr = new TSockaddr();
                        IPaddr.nIPaddr = HUtil32.IpToInt(sRemoteIPaddr);
                        GateShare.BlockIPList.Add(IPaddr);
                        CloseConnect(sRemoteIPaddr);
                        break;
                }
                GateShare.MainOutMessage("端口攻击: " + sRemoteIPaddr, 1);
                return;
            }
            if (GateShare.boGateReady)
            {
                for (nSockIndex = 0; nSockIndex < GateShare.GATEMAXSESSION; nSockIndex++)
                {
                    UserSession = GateShare.g_SessionArray[nSockIndex];
                    if (UserSession.Socket == null)
                    {
                        UserSession.Socket = e.Socket;
                        UserSession.sRemoteIPaddr = sRemoteIPaddr;
                        UserSession.nSendMsgLen = 0;
                        UserSession.bo0C = false;
                        UserSession.dw10Tick = HUtil32.GetTickCount();
                        UserSession.dwConnctCheckTick = HUtil32.GetTickCount();
                        UserSession.boSendAvailable = true;
                        UserSession.boSendCheck = false;
                        UserSession.nCheckSendLength = 0;
                        UserSession.n20 = 0;
                        UserSession.dwUserTimeOutTick = HUtil32.GetTickCount();
                        UserSession.SocketHandle = (int)e.Socket.Handle;
                        UserSession.sIP = sRemoteIPaddr;
                        UserSession.MsgList.Clear();
                        //Socket.nIndex = nSockIndex;
                        GateShare._sessionMap.TryAdd(e.ConnectionId, nSockIndex);
                        GateShare.nSessionCount++;
                        break;
                    }
                }
                if (nSockIndex >= 0)
                {
                    gateClient.ClientSocket.SendText("%O" + (int)e.Socket.Handle + "/" + sRemoteIPaddr + "/" + sLocalIPaddr + "$");
                    GateShare.MainOutMessage("Connect: " + sRemoteIPaddr, 5);
                }
                else
                {
                    e.Socket.Close();
                    GateShare.MainOutMessage("Kick Off: " + sRemoteIPaddr, 1);
                }
            }
            else
            {
                e.Socket.Close();
                GateShare.MainOutMessage("Kick Off: " + sRemoteIPaddr, 1);
            }
        }

        private void ServerSocketClientDisconnect(object Sender, AsyncUserToken e)
        {
            TUserSession UserSession;
            int nSockIndex = 0;
            string sRemoteIPaddr = string.Empty;
            TSockaddr IPaddr = null;
            long nIPaddr = HUtil32.IpToInt(sRemoteIPaddr);
            for (var i = 0; i < GateShare.CurrIPaddrList.Count; i++)
            {
                IPaddr = GateShare.CurrIPaddrList[i];
                if (IPaddr.nIPaddr == nIPaddr)
                {
                    IPaddr.nCount -= 1;
                    if (IPaddr.nCount <= 0)
                    {
                        IPaddr = null;
                        GateShare.CurrIPaddrList.RemoveAt(i);
                    }
                    break;
                }
            }
            if ((nSockIndex >= 0) && (nSockIndex < GateShare.GATEMAXSESSION))
            {
                UserSession = GateShare.g_SessionArray[nSockIndex];
                UserSession.Socket = null;
                UserSession.sRemoteIPaddr = "";
                UserSession.SocketHandle = -1;
                UserSession.MsgList.Clear();
                GateShare.nSessionCount -= 1;
                if (GateShare.boGateReady)
                {
                    gateClient.ClientSocket.SendText("%X" + (int)e.Socket.Handle + "$");
                    GateShare.MainOutMessage("DisConnect: " + sRemoteIPaddr, 5);
                }
            }
        }

        public void ServerSocketClientError(Object Sender, Socket Socket)
        {

        }

        private void ServerSocketClientRead(object Sender, AsyncUserToken e)
        {
            TUserSession UserSession;
            int nSockIndex = 0;
            string s10;
            string s1C;
            int nPos;
            int nMsgLen;
            if ((nSockIndex >= 0) && (nSockIndex < GateShare.GATEMAXSESSION))
            {
                UserSession = GateShare.g_SessionArray[nSockIndex];
                var nReviceLen = e.BytesReceived;
                var data = new byte[nReviceLen];
                Buffer.BlockCopy(e.ReceiveBuffer, e.Offset, data, 0, nReviceLen);
                var sReviceMsg = HUtil32.GetString(data, 0, data.Length);
                if ((sReviceMsg != "") && GateShare.boServerReady)
                {
                    nPos = sReviceMsg.IndexOf("*", StringComparison.OrdinalIgnoreCase);
                    if (nPos > 0)
                    {
                        UserSession.boSendAvailable = true;
                        UserSession.boSendCheck = false;
                        UserSession.nCheckSendLength = 0;
                        s10 = sReviceMsg.Substring(0, nPos - 1);
                        s1C = sReviceMsg.Substring(nPos + 1 - 1, sReviceMsg.Length - nPos);
                        sReviceMsg = s10 + s1C;
                    }
                    nMsgLen = sReviceMsg.Length;
                    if ((sReviceMsg != "") && (GateShare.boGateReady) && (!GateShare.boKeepAliveTimcOut))
                    {
                        UserSession.dwConnctCheckTick = HUtil32.GetTickCount();
                        if ((HUtil32.GetTickCount() - UserSession.dwUserTimeOutTick) < 1000)
                        {
                            UserSession.n20 += nMsgLen;
                        }
                        else
                        {
                            UserSession.n20 = nMsgLen;
                        }
                        gateClient.ClientSocket.SendText("%A" + (int)e.Socket.Handle + "/" + sReviceMsg + "$");
                    }
                }
            }
        }

        private void DecodeTimer(object obj)
        {
            string sProcessMsg = string.Empty;
            string sSocketMsg = string.Empty;
            string sSocketHandle = string.Empty;
            int nSocketIndex;
            int nMsgCount;
            int nSendRetCode;
            int nSocketHandle;
            long dwDecodeTick;
            long dwDecodeTime;
            string sRemoteIPaddr;
            TUserSession UserSession;
            TSockaddr IPaddr;
            //ShowMainLogMsg();
            if (GateShare.boDecodeLock || (!GateShare.boGateReady))
            {
                return;
            }
            try
            {
                dwDecodeTick = HUtil32.GetTickCount();
                GateShare.boDecodeLock = true;
                sProcessMsg = "";
                while (true)
                {
                    if (GateShare.ClientSockeMsgList.Count <= 0)
                    {
                        break;
                    }
                    sProcessMsg = sProcMsg + GateShare.ClientSockeMsgList[0];
                    sProcMsg = "";
                    GateShare.ClientSockeMsgList.RemoveAt(0);
                    while (true)
                    {
                        if (HUtil32.TagCount(sProcessMsg, '$') < 1)
                        {
                            break;
                        }
                        sProcessMsg = HUtil32.ArrestStringEx(sProcessMsg, "%", "$", ref sSocketMsg);
                        if (sSocketMsg == "")
                        {
                            break;
                        }
                        if (sSocketMsg[0] == '+')
                        {
                            if (sSocketMsg[1] == '-')
                            {
                                CloseSocket(HUtil32.Str_ToInt(sSocketMsg.Substring(2, sSocketMsg.Length - 2), 0));
                                continue;
                            }
                            else
                            {
                                GateShare.dwKeepAliveTick = HUtil32.GetTickCount();
                                GateShare.boKeepAliveTimcOut = false;
                                continue;
                            }
                        }
                        sSocketMsg = HUtil32.GetValidStr3(sSocketMsg, ref sSocketHandle, new string[] { "/" });
                        nSocketHandle = HUtil32.Str_ToInt(sSocketHandle, -1);
                        if (nSocketHandle < 0)
                        {
                            continue;
                        }
                        for (nSocketIndex = 0; nSocketIndex < GateShare.GATEMAXSESSION; nSocketIndex++)
                        {
                            if (GateShare.g_SessionArray[nSocketIndex].SocketHandle == nSocketHandle)
                            {
                                GateShare.g_SessionArray[nSocketIndex].MsgList.Add(sSocketMsg);
                                break;
                            }
                        }
                    }
                }
                if (sProcessMsg != "")
                {
                    sProcMsg = sProcessMsg;
                }
                GateShare.nSendMsgCount = 0;
                GateShare.n456A2C = 0;
                StringList318.Clear();
                for (nSocketIndex = 0; nSocketIndex < GateShare.GATEMAXSESSION; nSocketIndex++)
                {
                    if (GateShare.g_SessionArray[nSocketIndex].SocketHandle <= -1)// 踢除超时无数据传输连接
                    {
                        continue;
                    }
                    if ((HUtil32.GetTickCount() - GateShare.g_SessionArray[nSocketIndex].dwConnctCheckTick) > GateShare.dwKeepConnectTimeOut)
                    {
                        sRemoteIPaddr = GateShare.g_SessionArray[nSocketIndex].sRemoteIPaddr;
                        switch (GateShare.BlockMethod)
                        {
                            case TBlockIPMethod.mDisconnect:
                                GateShare.g_SessionArray[nSocketIndex].Socket.Close();
                                break;
                            case TBlockIPMethod.mBlock:
                                IPaddr = new TSockaddr();
                                IPaddr.nIPaddr = HUtil32.IpToInt((sRemoteIPaddr as string));
                                GateShare.TempBlockIPList.Add(IPaddr);
                                CloseConnect(sRemoteIPaddr);
                                break;
                            case TBlockIPMethod.mBlockList:
                                IPaddr = new TSockaddr();
                                IPaddr.nIPaddr = HUtil32.IpToInt((sRemoteIPaddr as string));
                                GateShare.BlockIPList.Add(IPaddr);
                                CloseConnect(sRemoteIPaddr);
                                break;
                        }
                        GateShare.MainOutMessage("端口空连接攻击: " + sRemoteIPaddr, 1);
                        continue;
                    }
                    while (true)
                    {
                        if (GateShare.g_SessionArray[nSocketIndex].MsgList.Count <= 0)
                        {
                            break;
                        }
                        UserSession = GateShare.g_SessionArray[nSocketIndex];
                        nSendRetCode = SendUserMsg(UserSession, UserSession.MsgList[0]);
                        if ((nSendRetCode >= 0))
                        {
                            if (nSendRetCode == 1)
                            {
                                UserSession.dwConnctCheckTick = HUtil32.GetTickCount();
                                UserSession.MsgList.RemoveAt(0);
                                continue;
                            }
                            if (UserSession.MsgList.Count > 100)
                            {
                                nMsgCount = 0;
                                while (nMsgCount != 51)
                                {
                                    UserSession.MsgList.RemoveAt(0);
                                    nMsgCount++;
                                }
                            }
                            GateShare.n456A2C += UserSession.MsgList.Count;
                            GateShare.MainOutMessage(UserSession.sIP + " : " + UserSession.MsgList.Count, 5);
                            GateShare.nSendMsgCount++;
                        }
                        else
                        {
                            UserSession.SocketHandle = -1;
                            UserSession.Socket = null;
                            UserSession.MsgList.Clear();
                        }
                    }
                }
                if ((HUtil32.GetTickCount() - dwSendKeepAliveTick) > 2 * 1000)
                {
                    dwSendKeepAliveTick = HUtil32.GetTickCount();
                    if (GateShare.boGateReady)
                    {
                        gateClient.ClientSocket.SendText("%--$");
                    }
                }
                if ((HUtil32.GetTickCount() - GateShare.dwKeepAliveTick) > 10 * 1000)
                {
                    GateShare.boKeepAliveTimcOut = true;
                    //ClientSocket.Close();
                }
            }
            finally
            {
                GateShare.boDecodeLock = false;
            }
            dwDecodeTime = HUtil32.GetTickCount() - dwDecodeTick;
            if (dwDecodeMsgTime < dwDecodeTime)
            {
                dwDecodeMsgTime = dwDecodeTime;
            }
            if (dwDecodeMsgTime > 50)
            {
                dwDecodeMsgTime -= 50;
            }
        }

        private int SendUserMsg(TUserSession UserSession, string sSendMsg)
        {
            int result;
            result = -1;
            if (UserSession.Socket != null)
            {
                if (!UserSession.bo0C)
                {
                    if (!UserSession.boSendAvailable && (HUtil32.GetTickCount() > UserSession.dwSendLockTimeOut))
                    {
                        UserSession.boSendAvailable = true;
                        UserSession.nCheckSendLength = 0;
                        GateShare.boSendHoldTimeOut = true;
                        GateShare.dwSendHoldTick = HUtil32.GetTickCount();
                    }
                    if (UserSession.boSendAvailable)
                    {
                        if (UserSession.nCheckSendLength >= 250)
                        {
                            if (!UserSession.boSendCheck)
                            {
                                UserSession.boSendCheck = true;
                                sSendMsg = "*" + sSendMsg;
                            }
                            if (UserSession.nCheckSendLength >= 512)
                            {
                                UserSession.boSendAvailable = false;
                                UserSession.dwSendLockTimeOut = HUtil32.GetTickCount() + 3 * 1000;
                            }
                        }
                        UserSession.Socket.SendText(sSendMsg);
                        UserSession.nSendMsgLen += sSendMsg.Length;
                        UserSession.nCheckSendLength += sSendMsg.Length;
                        result = 1;
                    }
                    else
                    {
                        result = 0;
                    }
                }
                else
                {
                    result = 0;
                }
            }
            return result;
        }

        private bool IsBlockIP(string sIPaddr)
        {
            bool result = false;
            TSockaddr IPaddr;
            long nIPaddr = HUtil32.IpToInt(sIPaddr);
            for (var i = 0; i < GateShare.TempBlockIPList.Count; i++)
            {
                IPaddr = GateShare.TempBlockIPList[i];
                if (IPaddr.nIPaddr == nIPaddr)
                {
                    result = true;
                    return result;
                }
            }
            for (var i = 0; i < GateShare.BlockIPList.Count; i++)
            {
                IPaddr = GateShare.BlockIPList[i];
                if (IPaddr.nIPaddr == nIPaddr)
                {
                    result = true;
                    return result;
                }
            }
            return result;
        }

        private bool IsConnLimited(string sIPaddr)
        {
            TSockaddr IPaddr;
            var result = false;
            var boDenyConnect = false;
            long nIPaddr = HUtil32.IpToInt(sIPaddr);
            for (var i = 0; i < GateShare.CurrIPaddrList.Count; i++)
            {
                IPaddr = GateShare.CurrIPaddrList[i];
                if (IPaddr.nIPaddr == nIPaddr)
                {
                    IPaddr.nCount++;
                    if (HUtil32.GetTickCount() - IPaddr.dwIPCountTick1 < 1000)
                    {
                        IPaddr.nIPCount1++;
                        if (IPaddr.nIPCount1 >= GateShare.nIPCountLimit1)
                        {
                            boDenyConnect = true;
                        }
                    }
                    else
                    {
                        IPaddr.dwIPCountTick1 = HUtil32.GetTickCount();
                        IPaddr.nIPCount1 = 0;
                    }
                    if (HUtil32.GetTickCount() - IPaddr.dwIPCountTick2 < 3000)
                    {
                        IPaddr.nIPCount2++;
                        if (IPaddr.nIPCount2 >= GateShare.nIPCountLimit2)
                        {
                            boDenyConnect = true;
                        }
                    }
                    else
                    {
                        IPaddr.dwIPCountTick2 = HUtil32.GetTickCount();
                        IPaddr.nIPCount2 = 0;
                    }
                    if (IPaddr.nCount > GateShare.nMaxConnOfIPaddr)
                    {
                        boDenyConnect = true;
                    }
                    result = boDenyConnect;
                    return result;
                }
            }
            IPaddr = new TSockaddr();
            IPaddr.nIPaddr = nIPaddr;
            IPaddr.nCount = 1;
            GateShare.CurrIPaddrList.Add(IPaddr);
            return result;
        }

        private void CloseConnect(string sIPaddr)
        {
            int i;
            bool boCheck;
            if (ServerSocket.Active)
            {
                while (true)
                {
                    //boCheck = false;
                    //for (i = 0; i < ServerSocket.Socket.ActiveConnections; i++)
                    //{
                    //    if (sIPaddr == ServerSocket.Socket.Connections[i].RemoteAddress)
                    //    {
                    //        ServerSocket.Socket.Connections[i].Close();
                    //        boCheck = true;
                    //        break;
                    //    }
                    //}
                    //if (!boCheck)
                    //{
                    //    break;
                    //}
                }
            }
        }

        private void CloseSocket(int nSocketHandle)
        {
            TUserSession UserSession;
            for (var nIndex = 0; nIndex < GateShare.GATEMAXSESSION; nIndex++)
            {
                UserSession = GateShare.g_SessionArray[nIndex];
                if ((UserSession.Socket != null) && (UserSession.SocketHandle == nSocketHandle))
                {
                    UserSession.Socket.Close();
                    break;
                }
            }
        }
    }
}