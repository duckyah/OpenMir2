using System;
using System.Threading;
using System.Threading.Tasks;
using SystemModule.ByteManager;
using SystemModule.Core.Common;
using SystemModule.Core.Run.Action;
using SystemModule.Core.Run.WaitPool;
using SystemModule.Extensions;
using SystemModule.Sockets.Enum;
using SystemModule.Sockets.Exceptions;
using SystemModule.Sockets.Interface;
using SystemModule.Sockets.Interface.ISender;
using SystemModule.Sockets.SocketEventArgs;

namespace SystemModule.Sockets.WaitingClient
{
    internal class WaitingClient<TClient> : DisposableObject, IWaitingClient<TClient> where TClient : IClient, IDefaultSender, ISender
    {
        private readonly WaitData<ResponsedData> m_waitData;

        private volatile bool breaked;

        public WaitingClient(TClient client, WaitingOptions waitingOptions)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            m_waitData = new WaitData<ResponsedData>();
            WaitingOptions = waitingOptions;
        }

        public bool CanSend
        {
            get
            {
                if (Client is ITcpClientBase tcpClient)
                {
                    return tcpClient.Online;
                }
                else if (Client is IUdpSession)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public TClient Client { get; private set; }

        public WaitingOptions WaitingOptions { get; set; }

        /// <summary>
        /// 发送字节流
        /// </summary>
        /// <param name="buffer">数据缓存区</param>
        /// <param name="offset">偏移</param>
        /// <param name="length">长度</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="token">取消令箭</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        /// <returns>返回的数据</returns>
        public ResponsedData SendThenResponse(byte[] buffer, int offset, int length, int timeout = 1000 * 5, CancellationToken token = default)
        {
            lock (this)
            {
                try
                {
                    breaked = false;
                    m_waitData.Reset();
                    if (WaitingOptions.BreakTrigger && Client is ITcpClientBase tcpClient)
                    {
                        tcpClient.Disconnected += OnDisconnected;
                    }

                    if (WaitingOptions.AdapterFilter == AdapterFilter.AllAdapter || WaitingOptions.AdapterFilter == AdapterFilter.WaitAdapter)
                    {
                        Client.OnHandleReceivedData += OnHandleReceivedData;
                    }
                    else
                    {
                        Client.OnHandleRawBuffer += OnHandleRawBuffer;
                    }

                    if (WaitingOptions.RemoteIPHost != null && Client is IUdpSession session)
                    {
                        if (WaitingOptions.AdapterFilter == AdapterFilter.AllAdapter || WaitingOptions.AdapterFilter == AdapterFilter.SendAdapter)
                        {
                            session.Send(WaitingOptions.RemoteIPHost.EndPoint, buffer, offset, length);
                        }
                        else
                        {
                            session.DefaultSend(WaitingOptions.RemoteIPHost.EndPoint, buffer, offset, length);
                        }
                    }
                    else
                    {
                        if (WaitingOptions.AdapterFilter == AdapterFilter.AllAdapter || WaitingOptions.AdapterFilter == AdapterFilter.SendAdapter)
                        {
                            Client.Send(buffer, offset, length);
                        }
                        else
                        {
                            Client.DefaultSend(buffer, offset, length);
                        }
                    }

                    m_waitData.SetCancellationToken(token);
                    switch (m_waitData.Wait(timeout))
                    {
                        case WaitDataStatus.SetRunning:
                            return m_waitData.WaitResult;

                        case WaitDataStatus.Overtime:
                            throw new TimeoutException();
                        case WaitDataStatus.Canceled:
                            {
                                if (WaitingOptions.ThrowBreakException && breaked)
                                {
                                    throw new Exception("等待已终止。可能是客户端已掉线，或者被注销。");
                                }
                                return default;
                            }
                        case WaitDataStatus.Default:
                        case WaitDataStatus.Disposed:
                        default:
                            throw new Exception(TouchSocketStatus.UnknownError.GetDescription());
                    }
                }
                finally
                {
                    if (WaitingOptions.BreakTrigger && Client is ITcpClientBase tcpClient)
                    {
                        tcpClient.Disconnected -= OnDisconnected;
                    }

                    if (WaitingOptions.AdapterFilter == AdapterFilter.AllAdapter || WaitingOptions.AdapterFilter == AdapterFilter.WaitAdapter)
                    {
                        Client.OnHandleReceivedData -= OnHandleReceivedData;
                    }
                    else
                    {
                        Client.OnHandleRawBuffer -= OnHandleRawBuffer;
                    }
                }
            }
        }

        /// <summary>
        /// 发送字节流
        /// </summary>
        /// <param name="buffer">数据缓存区</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="token">取消令箭</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        /// <returns>返回的数据</returns>
        public ResponsedData SendThenResponse(byte[] buffer, int timeout = 1000 * 5, CancellationToken token = default)
        {
            return SendThenResponse(buffer, 0, buffer.Length, timeout, token);
        }

        /// <summary>
        /// 发送流中的有效数据
        /// </summary>
        /// <param name="byteBlock">数据块载体</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="token">取消令箭</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        /// <returns>返回的数据</returns>
        public ResponsedData SendThenResponse(ByteBlock byteBlock, int timeout = 1000 * 5, CancellationToken token = default)
        {
            return SendThenResponse(byteBlock.Buffer, 0, byteBlock.Len, timeout, token);
        }

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="buffer">数据缓存区</param>
        /// <param name="offset">偏移</param>
        /// <param name="length">长度</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="token">取消令箭</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        /// <returns>返回的数据</returns>
        public Task<ResponsedData> SendThenResponseAsync(byte[] buffer, int offset, int length, int timeout = 1000 * 5, CancellationToken token = default)
        {
            return EasyTask.Run(() =>
            {
                return SendThenResponse(buffer, offset, length, timeout, token);
            });
        }

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="buffer">数据缓存区</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="token">取消令箭</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        /// <returns>返回的数据</returns>
        public Task<ResponsedData> SendThenResponseAsync(byte[] buffer, int timeout = 1000 * 5, CancellationToken token = default)
        {
            return EasyTask.Run(() =>
            {
                return SendThenResponse(buffer, timeout, token);
            });
        }

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="byteBlock">数据块载体</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="token">取消令箭</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        /// <returns>返回的数据</returns>
        public Task<ResponsedData> SendThenResponseAsync(ByteBlock byteBlock, int timeout = 1000 * 5, CancellationToken token = default)
        {
            return EasyTask.Run(() =>
            {
                return SendThenResponse(byteBlock, timeout, token);
            });
        }

        /// <summary>
        /// 发送字节流
        /// </summary>
        /// <param name="buffer">数据缓存区</param>
        /// <param name="offset">偏移</param>
        /// <param name="length">长度</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="token">取消令箭</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        /// <returns>返回的数据</returns>
        public byte[] SendThenReturn(byte[] buffer, int offset, int length, int timeout = 1000 * 5, CancellationToken token = default)
        {
            return SendThenResponse(buffer, offset, length, timeout, token).Data;
        }

        /// <summary>
        /// 发送字节流
        /// </summary>
        /// <param name="buffer">数据缓存区</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="token">取消令箭</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        /// <returns>返回的数据</returns>
        public byte[] SendThenReturn(byte[] buffer, int timeout = 1000 * 5, CancellationToken token = default)
        {
            return SendThenReturn(buffer, 0, buffer.Length, timeout, token);
        }

        /// <summary>
        /// 发送流中的有效数据
        /// </summary>
        /// <param name="byteBlock">数据块载体</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="token">取消令箭</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        /// <returns>返回的数据</returns>
        public byte[] SendThenReturn(ByteBlock byteBlock, int timeout = 1000 * 5, CancellationToken token = default)
        {
            return SendThenReturn(byteBlock.Buffer, 0, byteBlock.Len, timeout, token);
        }

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="buffer">数据缓存区</param>
        /// <param name="offset">偏移</param>
        /// <param name="length">长度</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="token">取消令箭</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        /// <returns>返回的数据</returns>
        public Task<byte[]> SendThenReturnAsync(byte[] buffer, int offset, int length, int timeout = 1000 * 5, CancellationToken token = default)
        {
            return EasyTask.Run(() =>
            {
                return SendThenReturn(buffer, offset, length, timeout, token);
            });
        }

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="buffer">数据缓存区</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="token">取消令箭</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        /// <returns>返回的数据</returns>
        public Task<byte[]> SendThenReturnAsync(byte[] buffer, int timeout = 1000 * 5, CancellationToken token = default)
        {
            return EasyTask.Run(() =>
            {
                return SendThenReturn(buffer, timeout, token);
            });
        }

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="byteBlock">数据块载体</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="token">取消令箭</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        /// <returns>返回的数据</returns>
        public Task<byte[]> SendThenReturnAsync(ByteBlock byteBlock, int timeout = 1000 * 5, CancellationToken token = default)
        {
            return EasyTask.Run(() =>
            {
                return SendThenReturn(byteBlock, timeout, token);
            });
        }

        protected override void Dispose(bool disposing)
        {
            Client = default;
            m_waitData.SafeDispose();
            base.Dispose(disposing);
        }

        private void OnDisconnected(ITcpClientBase client, DisconnectEventArgs e)
        {
            breaked = true;
            m_waitData.Cancel();
        }

        private bool OnHandleRawBuffer(ByteBlock byteBlock)
        {
            ResponsedData responsedData = new ResponsedData(byteBlock.ToArray(), null);
            return !m_waitData.Set(responsedData);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="byteBlock"></param>
        /// <param name="requestInfo"></param>
        private bool OnHandleReceivedData(ByteBlock byteBlock, IRequestInfo requestInfo)
        {
            ResponsedData responsedData;
            if (byteBlock != null)
            {
                responsedData = new ResponsedData(byteBlock.ToArray(), requestInfo);
            }
            else
            {
                responsedData = new ResponsedData(null, requestInfo);
            }
            return !m_waitData.Set(responsedData);
        }
    }
}