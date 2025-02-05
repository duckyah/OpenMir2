using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace SystemModule.Extensions
{
    /// <summary>
    /// 为System提供扩展。
    /// </summary>
    public static class SystemExtensions
    {
        #region 其他

        /// <summary>
        /// 安全性释放（不用判断对象是否为空）。不会抛出任何异常。
        /// </summary>
        /// <param name="dis"></param>
        /// <returns></returns>
        public static void SafeDispose(this IDisposable dis)
        {
            if (dis == default)
            {
                return;
            }
            try
            {
                dis.Dispose();
            }
            catch
            {
            }
        }

        #endregion 其他

        /// <summary>
        /// 获取自定义attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumObj"></param>
        /// <returns></returns>
        public static T GetAttribute<T>(this Enum enumObj) where T : Attribute
        {
            Type type = enumObj.GetType();
            Attribute attr = null;
            string enumName = Enum.GetName(type, enumObj);  //获取对应的枚举名
            FieldInfo field = type.GetField(enumName);
            attr = field.GetCustomAttribute(typeof(T), false);
            return (T)attr;
        }

        /// <summary>
        /// 格林尼治标准时间
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static string ToGMTString(this DateTime dt, string v)
        {
            return dt.ToString("r", CultureInfo.InvariantCulture);
        }

#if !NETCOREAPP3_1_OR_GREATER

        /// <summary>
        /// 清除所有成员
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            while (queue.TryDequeue(out _))
            {
            }
        }

#endif

        /// <summary>
        /// 清除所有成员
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="action"></param>
        public static void Clear<T>(this ConcurrentQueue<T> queue, Action<T> action)
        {
            while (queue.TryDequeue(out T t))
            {
                action?.Invoke(t);
            }
        }

        /// <summary>
        /// 获取字节中的指定Bit的值
        /// </summary>
        /// <param name="this">字节</param>
        /// <param name="index">Bit的索引值(0-7)</param>
        /// <returns></returns>
        public static int GetBit(this byte @this, short index)
        {
            byte x;
            switch (index)
            {
                case 0: { x = 0x01; } break;
                case 1: { x = 0x02; } break;
                case 2: { x = 0x04; } break;
                case 3: { x = 0x08; } break;
                case 4: { x = 0x10; } break;
                case 5: { x = 0x20; } break;
                case 6: { x = 0x40; } break;
                case 7: { x = 0x80; } break;
                default: { return 0; }
            }
            return (@this & x) == x ? 1 : 0;
        }

        /// <summary>
        /// 设置字节中的指定Bit的值
        /// </summary>
        /// <param name="this">字节</param>
        /// <param name="index">Bit的索引值(0-7)</param>
        /// <param name="bitvalue">Bit值(0,1)</param>
        /// <returns></returns>
        public static byte SetBit(this byte @this, short index, int bitvalue)
        {
            byte _byte = @this;
            if (bitvalue == 1)
            {
                switch (index)
                {
                    case 0: { return _byte |= 0x01; }
                    case 1: { return _byte |= 0x02; }
                    case 2: { return _byte |= 0x04; }
                    case 3: { return _byte |= 0x08; }
                    case 4: { return _byte |= 0x10; }
                    case 5: { return _byte |= 0x20; }
                    case 6: { return _byte |= 0x40; }
                    case 7: { return _byte |= 0x80; }
                    default: { return _byte; }
                }
            }
            else
            {
                switch (index)
                {
                    case 0: { return _byte &= 0xFE; }
                    case 1: { return _byte &= 0xFD; }
                    case 2: { return _byte &= 0xFB; }
                    case 3: { return _byte &= 0xF7; }
                    case 4: { return _byte &= 0xEF; }
                    case 5: { return _byte &= 0xDF; }
                    case 6: { return _byte &= 0xBF; }
                    case 7: { return _byte &= 0x7F; }
                    default: { return _byte; }
                }
            }
        }
    }
}