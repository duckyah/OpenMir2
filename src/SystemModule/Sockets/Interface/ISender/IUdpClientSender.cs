using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using SystemModule.Sockets.Exceptions;

namespace SystemModule.Sockets.Interface.ISender
{
    /// <summary>
    /// 具有Udp终结点的发送
    /// </summary>
    public interface IUdpClientSender : ISender
    {
        /// <summary>
        /// 同步组合发送数据。
        /// <para>内部已经封装Ssl和发送长度检测，即：调用完成即表示数据全部发送完毕。</para>
        /// <para>该发送会经过适配器封装，具体封装内容由适配器决定。</para>
        /// </summary>
        /// <param name="endPoint">远程终结点</param>
        /// <param name="transferBytes">组合数据</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        void Send(EndPoint endPoint, IList<ArraySegment<byte>> transferBytes);

        /// <summary>
        /// 异步组合发送数据。
        /// <para>在<see cref="ITcpClient"/>时，如果使用独立线程发送，则不会触发异常。</para>
        /// <para>在<see cref="ITcpClientBase"/>时，相当于<see cref="Socket.BeginSend(byte[], int, int, SocketFlags, out SocketError, AsyncCallback, object)"/>。</para>
        /// <para>该发送会经过适配器封装，具体封装内容由适配器决定。</para>
        /// </summary>
        /// <param name="endPoint">远程终结点</param>
        /// <param name="transferBytes">组合数据</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        Task SendAsync(EndPoint endPoint, IList<ArraySegment<byte>> transferBytes);

        /// <summary>
        /// 同步组合发送数据。
        /// <para>内部已经封装Ssl和发送长度检测，即：调用完成即表示数据全部发送完毕。</para>
        /// <para>该发送会经过适配器封装，具体封装内容由适配器决定。</para>
        /// </summary>
        /// <param name="endPoint">远程终结点</param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        void Send(EndPoint endPoint, byte[] buffer, int offset, int length);

        /// <summary>
        /// 异步组合发送数据。
        /// <para>在<see cref="ITcpClient"/>时，如果使用独立线程发送，则不会触发异常。</para>
        /// <para>在<see cref="ITcpClientBase"/>时，相当于<see cref="Socket.BeginSend(byte[], int, int, SocketFlags, out SocketError, AsyncCallback, object)"/>。</para>
        /// <para>该发送会经过适配器封装，具体封装内容由适配器决定。</para>
        /// </summary>
        /// <param name="endPoint">远程终结点</param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        Task SendAsync(EndPoint endPoint, byte[] buffer, int offset, int length);
    }
}