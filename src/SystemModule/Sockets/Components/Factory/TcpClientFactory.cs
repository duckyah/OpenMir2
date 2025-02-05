using System;
using System.Collections.Generic;
using System.Threading;
using SystemModule.Core.Common;
using SystemModule.Core.Config;
using SystemModule.Core.Run.Timers;
using SystemModule.Extensions;
using SystemModule.Sockets.Components.TCP;
using SystemModule.Sockets.Extensions;
using SystemModule.Sockets.Interface;

namespace SystemModule.Sockets.Components.Factory
{
    /// <summary>
    /// 适用于Tcp客户端的连接工厂。
    /// </summary>
    /// <typeparam name="TClient"></typeparam>
    public class TcpClientFactory<TClient> : ClientFactory<TClient> where TClient : ITcpClient, new()
    {
        private readonly TClient m_mainClient = new TClient();

        private readonly SingleTimer m_singleTimer;

        private bool first = true;

        /// <summary>
        /// 适用于Tcp客户端的连接工厂。
        /// </summary>
        public TcpClientFactory()
        {
            m_singleTimer = new SingleTimer(1000, () =>
            {
                List<TClient> list = new List<TClient>();
                foreach (TClient item in CreatedClients)
                {
                    if (!IsAlive(item))
                    {
                        list.Add(item);
                    }
                }

                foreach (TClient item in list)
                {
                    DisposeClient(item);
                }

                if (IsAlive(MainClient))
                {
                    if (CreatedClients.Count < MinCount)
                    {
                        try
                        {
                            CreateTransferClient();
                        }
                        catch
                        {
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 连接超时设定
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <inheritdoc/>
        public override TClient MainClient { get => m_mainClient; }

        /// <summary>
        /// 获取传输的客户端配置
        /// </summary>
        public Func<TouchSocketConfig> OnGetTransferConfig { get; set; }

        /// <inheritdoc/>
        public override Result CheckStatus(bool tryInit = true)
        {
            lock (m_singleTimer)
            {
                try
                {
                    if (!IsAlive(m_mainClient))
                    {
                        if (!tryInit)
                        {
                            return Result.UnknownFail;
                        }
                        if (first)
                        {
                            OnMainClientSetuping();
                            MainClient.Setup(MainConfig);
                            first = false;
                        }
                        MainClient.Close();
                        MainClient.Connect((int)ConnectTimeout.TotalMilliseconds);
                    }
                    return Result.Success;
                }
                catch (Exception ex)
                {
                    return new Result(ex);
                }
            }
        }

        /// <summary>
        /// 在主客户端加载配置之前
        /// </summary>
        protected virtual void OnMainClientSetuping()
        {

        }

        /// <inheritdoc/>
        public override void DisposeClient(TClient client)
        {
            client.TryShutdown();
            client.SafeDispose();
            CreatedClients.Remove(client);
        }

        /// <summary>
        /// 获取可以使用的客户端数量。
        /// <para>
        /// 注意：该值不一定是<see cref="ClientFactory{TClient}.FreeClients"/>的长度，当已创建数量小于设定的最大值时，也会累加未创建的值。
        /// </para>
        /// </summary>
        /// <returns></returns>
        public override int GetAvailableCount()
        {
            return Math.Max(0, MaxCount - CreatedClients.Count) + FreeClients.Count;
        }

        /// <summary>
        /// 获取一个空闲的连接对象，如果等待超出设定的时间，则会创建新的连接。
        /// </summary>
        /// <param name="waitTime">指定毫秒数</param>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="Exception"></exception>
        public TClient GetTransferClient(int waitTime)
        {
            return GetTransferClient(TimeSpan.FromMilliseconds(waitTime));
        }

        /// <summary>
        /// 获取一个空闲的连接对象，如果等待超出1秒的时间，则会创建新的连接。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="Exception"></exception>
        public TClient GetTransferClient()
        {
            return GetTransferClient(TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// 获取一个空闲的连接对象，如果等待超出设定的时间，则会创建新的连接。
        /// </summary>
        /// <param name="waitTime"></param>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="Exception"></exception>
        public override TClient GetTransferClient(TimeSpan waitTime)
        {
            while (FreeClients.TryDequeue(out TClient client))
            {
                if (IsAlive(client))
                {
                    return client;
                }
                else
                {
                    DisposeClient(client);
                }
            }

            if (CreatedClients.Count > MaxCount)
            {
                if (SpinWait.SpinUntil(Wait, waitTime))
                {
                    return GetTransferClient(waitTime);
                }
            }

            TClient clientRes = CreateTransferClient();
            return clientRes;
        }

        /// <inheritdoc/>
        public override bool IsAlive(TClient client)
        {
            return client.Online;
        }

        /// <summary>
        /// 归还使用完的连接。
        /// <para>
        /// 首先内部会判定存活状态，如果不再活动状态，会直接调用<see cref="DisposeClient(TClient)"/>。
        /// 其次会计算是否可以进入缓存队列，如果队列数量超出<see cref="ClientFactory{TClient}.MaxCount"/>，也会直接调用<see cref="DisposeClient(TClient)"/>
        /// </para>
        /// </summary>
        /// <param name="client"></param>
        public override void ReleaseTransferClient(TClient client)
        {
            if ((object)client == (object)MainClient)
            {
                return;
            }
            if (!IsAlive(client))
            {
                DisposeClient(client);
                return;
            }
            if (FreeClients.Count < MaxCount)
            {
                FreeClients.Enqueue(client);
            }
            else
            {
                DisposeClient(client);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            m_singleTimer.SafeDispose();
            base.Dispose(disposing);
        }

        /// <inheritdoc/>
        protected override TouchSocketConfig GetTransferConfig()
        {
            return OnGetTransferConfig?.Invoke();
        }

        private TClient CreateTransferClient()
        {
            TClient client = new TClient();
            client.Setup(GetTransferConfig());
            client.Connect((int)ConnectTimeout.TotalMilliseconds);
            CreatedClients.Add(client);
            return client;
        }

        private bool Wait()
        {
            if (FreeClients.Count > 0)
            {
                return true;
            }
            return false;
        }
    }

    /// <summary>
    ///  适用于基于<see cref="TcpClient"/>的连接工厂。
    /// </summary>
    public class TcpClientFactory : TcpClientFactory<TcpClient>
    {
    }
}