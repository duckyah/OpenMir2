using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace SystemModule
{
    public class HUtil32
    {
        public static int Sequence()
        {
            var bytes = Guid.NewGuid().ToByteArray();
            var sequence = BitConverter.ToInt32(bytes, 0);
            while (sequence < 0)
            {
                bytes = Guid.NewGuid().ToByteArray();
                sequence = BitConverter.ToInt32(bytes, 0);
                if (sequence > 0) break;
            }

            return sequence;
        }


        public static int GetTickCount()
        {
            return System.Environment.TickCount;
        }


        public static int MakeLong(int lowPart, int highPart)
        {
            return lowPart | (short) highPart << 16;
        }

        public static int MakeLong(double lowPart, double highPart)
        {
            return (int) lowPart | ((int) highPart << 16);
        }

        public static int MakeLong(ushort lowPart, int highPart)
        {
            return lowPart | (short) highPart << 16;
        }

        public static int MakeLong(short lowPart, int highPart)
        {
            return (ushort) lowPart | ((short) highPart << 16);
        }

        public static int MakeLong(short lowPart, short highPart)
        {
            return (ushort) lowPart | (highPart << 16);
        }

        public static int MakeLong(short lowPart, ushort highPart)
        {
            return (ushort) lowPart | ((short) highPart << 16);
        }

        //public static ushort MakeWord(byte bLow, byte bHigh)
        //{
        //    return (ushort)(bLow | (bHigh << 8));
        //}

        public static short MakeWord(int bLow, int bHigh)
        {
            return (short) (bLow | (bHigh << 8));
        }

        public static short HiWord(int dword)
        {
            return (short) (dword >> 16);
        }

        public static short LoWord(int dword)
        {
            return (short) dword;
        }

        public static byte HiByte(short W)
        {
            return (byte) (W >> 8);
        }

        public static byte HiByte(int W)
        {
            return (byte) (W >> 8);
        }

        public static byte LoByte(short W)
        {
            return (byte) W;
        }

        public static byte LoByte(int W)
        {
            return (byte) W;
        }

        public static int Round(object r)
        {
            return (int) Math.Round(Convert.ToDouble(r) + 0.5, 1, MidpointRounding.AwayFromZero);
        }
        
        /// <summary>
        /// �ж���ֵ�Ƿ��ڷ�Χ֮��
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool RangeInDefined(int values, int min, int max)
        {
            return Math.Max(min, values) == Math.Min(values, max);
        }

        /// <summary>
        /// �ж���ֵ�Ƿ��ڷ�Χ֮��
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool RangeInDefined(long values, int min, int max)
        {
            return Math.Max(min, values) == Math.Min(values, max);
        }

        public static void EnterCriticalSection(object obj)
        {
        }

        public static void LeaveCriticalSection(object obj)
        {
        }

        public static unsafe string StrPas(byte* Buff)
        {
            var nLen = 0;
            var pb = Buff;
            while (*pb++ != 0) nLen++;

            var ret = new string('\0', nLen);
            var sb = new StringBuilder(ret);
            pb = Buff;
            for (var i = 0; i < nLen; i++) sb[i] = (char) *pb++;

            return sb.ToString();
        }

        public static unsafe byte[] StringToByteAry(string str)
        {
            var nLen = StringToBytePtr(str, null, 0);
            var ret = new byte[nLen];
            fixed (byte* pb = ret)
            {
                StringToBytePtr(str, pb, 0);
            }

            return ret;
        }

        public static unsafe int StringToBytePtr(string str, byte* retby, int StartIndex)
        {
            var bDecode = false;
            if (string.IsNullOrEmpty(str)) return 0;
            for (var i = 0; i < str.Length; i++)
                if ((ushort) str[i] >> 8 != 0)
                {
                    bDecode = true;
                    break;
                }

            var nLen = 0;
            if (bDecode)
                nLen = Encoding.GetEncoding("gb2312").GetByteCount(str);
            else
                nLen = str.Length;
            if (retby == null)
                return nLen;

            if (bDecode)
            {
                var by = Encoding.GetEncoding("gb2312").GetBytes(str);
                var pb = retby + StartIndex;
                for (var i = 0; i < by.Length; i++)
                    *pb++ = by[i];
            }
            else
            {
                var pb = retby + StartIndex;
                for (var i = 0; i < str.Length; i++) *pb++ = (byte) str[i];
            }

            return nLen;
        }

        public static string CaptureString(string source, ref string rdstr)
        {
            string result;
            int st;
            int et;
            int c;
            int len;
            int i;
            if (source == "")
            {
                rdstr = "";
                result = "";
                return result;
            }

            c = 1;
            // et := 0;
            len = source.Length;
            while (source[c] == ' ')
                if (c < len)
                    c++;
                else
                    break;
            if (source[c] == '\"' && c < len)
            {
                st = c + 1;
                et = len;
                for (i = c + 1; i <= len; i++)
                    if (source[i] == '\"')
                    {
                        et = i - 1;
                        break;
                    }
            }
            else
            {
                st = c;
                et = len;
                for (i = c; i <= len; i++)
                    if (source[i] == ' ')
                    {
                        et = i - 1;
                        break;
                    }
            }

            rdstr = source.Substring(st - 1, et - st + 1);
            if (len >= et + 2)
                result = source.Substring(et + 2 - 1, len - (et + 1));
            else
                result = "";
            return result;
        }

        public static string RemoveSpace(string str)
        {
            string result;
            int i;
            result = "";
            for (i = 1; i <= str.Length; i++)
                if (str[i] != ' ')
                    result = result + str[i];
            return result;
        }

        public static int Str_ToInt(string Str, int def)
        {
            var result = def;
            int.TryParse(Str, out result);
            return result;

            //int result = def;
            //if (Str != "")
            //{
            //    if (((((short)Str[1]) >= (short)'0') && (((short)Str[1]) <= (short)'9')) || (Str[1] == '+') || (Str[1] == '-'))
            //    {
            //        try
            //        {
            //            result = Convert.ToInt32(Str);
            //        }
            //        catch
            //        {
            //        }
            //    }
            //}
            //return result;
        }

        public static DateTime Str_ToDate(string Str)
        {
            DateTime result;
            if (Str.Trim() == "")
                result = DateTime.Today;
            else
                result = Convert.ToDateTime(Str);
            return result;
        }

        public static DateTime Str_ToTime(string Str)
        {
            DateTime result;
            if (Str.Trim() == "")
                result = DateTime.Now;
            else
                result = Convert.ToDateTime(Str);
            return result;
        }

        public static double Str_ToFloat(string str)
        {
            double result;
            if (str != "")
                try
                {
                    result = Convert.ToSingle(str);
                    return result;
                }
                catch
                {
                }

            result = 0;
            return result;
        }

        public static string ExtractFileNameOnly(string fname)
        {
            string result;
            int extpos;
            string ext;
            string fn;
            ext = Path.GetExtension(fname);
            fn = Path.GetFileName(fname);
            if (ext != "")
            {
                extpos = fn.IndexOf(ext);
                result = fn.Substring(1 - 1, extpos - 1);
            }
            else
            {
                result = fn;
            }

            return result;
        }

        public static string GetValidStr3(string Str, ref string Dest, char Divider)
        {
            var Ary = Str.Split('/'); //���ز������յ�ֵ
            if (Ary.Length > 0)
                Dest = Ary[0]; //Ŀ����Ϊ��һ��
            else
                Dest = "";
            if (Ary.Length > 1)
                return Ary[1]; //���صڶ���
            else
                return "";
        }

        public static string GetValidStr3(string Str, ref string Dest, char[] DividerAry)
        {
            var Div = new char[DividerAry.Length];
            int i;
            for (i = 0; i < DividerAry.Length; i++) Div[i] = DividerAry[i];

            var Ary = Str.Split(Div, 2, StringSplitOptions.RemoveEmptyEntries); //���ز������յ�ֵ
            if (Ary.Length > 0)
                Dest = Ary[0]; //Ŀ����Ϊ��һ��
            else
                Dest = "";
            if (Ary.Length > 1)
                return Ary[1]; //���صڶ���
            else
                return "";
        }

        public static string GetValidStr3(string Str, ref string Dest, string[] DividerAry)
        {
            var Div = new char[DividerAry.Length];
            for (var i = 0; i < DividerAry.Length; i++) Div[i] = DividerAry[i][0];
            var Ary = Str.Split(Div, 2, StringSplitOptions.RemoveEmptyEntries); //���ز������յ�ֵ
            Dest = Ary.Length > 0 ? Ary[0] : "";
            return Ary.Length > 1 ? Ary[1] : "";
        }
        
        public static string GetValidStr3(string Str, ref string Dest, string DividerAry)
        {
            var div = new char[DividerAry.Length];
            for (var i = 0; i < DividerAry.Length; i++) div[i] = DividerAry[i];
            var Ary = Str.Split(div, 2, StringSplitOptions.RemoveEmptyEntries); //���ز������յ�ֵ
            Dest = Ary.Length > 0 ? Ary[0] : "";
            return Ary.Length > 1 ? Ary[1] : "";
        }

        public static string GetValidStrCap(string Str, ref string Dest, string[] Divider)
        {
            string result;
            Str = Str.TrimStart();
            if (Str != "")
            {
                if (Str[0] == '\"')
                    result = CaptureString(Str, ref Dest);
                else
                    result = GetValidStr3(Str, ref Dest, Divider);
            }
            else
            {
                result = "";
                Dest = "";
            }

            return result;
        }

        public static string IntToStr2(int n)
        {
            string result;
            if (n < 10)
                result = '0' + n.ToString();
            else
                result = n.ToString();
            return result;
        }

        public static string IntToStrFill(int num, int len, char fill)
        {
            string result;
            int i;
            string str;
            result = "";
            str = num.ToString();
            for (i = 1; i <= len - str.Length; i++) result = result + fill;
            result = result + str;
            return result;
        }

        public static bool IsInB(string Src, int Pos, string Targ)
        {
            bool result;
            int TLen;
            int I;
            result = false;
            TLen = Targ.Length;
            if (Src.Length < Pos + TLen) return result;
            for (I = 0; I < TLen; I++)
                if (char.ToUpper(Src[Pos + I]) != char.ToUpper(Targ[I + 1]))
                    return result;
            result = true;
            return result;
        }

        public static bool IsStringNumber(string str)
        {
            var result = true;
            for (var i = 0; i <= str.Length - 1; i++)
                if ((byte) str[i] < (byte) '0' || (byte) str[i] > (byte) '9')
                {
                    result = false;
                    break;
                }

            return result;
        }

        /// <summary>
        /// ��ȡ�ַ��� �� ArrestStringEx('[1234]','[',']',str)    str=1234
        /// </summary>
        /// <param name="Source">Դ�ַ���</param>
        /// <param name="SearchAfter">��Ҫƥ��ķ���</param>
        /// <param name="ArrestBefore">��Ҫƥ��ķ���</param>
        /// <param name="ArrestStr">��ȡ֮��Ľ��</param>
        /// <returns></returns>
        public static string ArrestStringEx(string Source, string SearchAfter, string ArrestBefore,
            ref string ArrestStr)
        {
            var result = string.Empty;
            int srclen;
            bool GoodData;
            int n;
            ArrestStr = string.Empty;
            if (Source == "")
            {
                result = "";
                return result;
            }

            try
            {
                srclen = Source.Length;
                GoodData = false;
                if (srclen >= 2)
                {
                    if (Source[0].ToString() == SearchAfter)
                    {
                        Source = Source.Substring(1, srclen - 1);
                        srclen = Source.Length;
                        GoodData = true;
                    }
                    else
                    {
                        n = Source.IndexOf(SearchAfter) + 1;
                        if (n > 0)
                        {
                            Source = Source.Substring(n, srclen - n);
                            srclen = Source.Length;
                            GoodData = true;
                        }
                    }
                }

                if (GoodData)
                {
                    n = Source.IndexOf(ArrestBefore) + 1;
                    if (n > 0)
                    {
                        ArrestStr = Source.Substring(0, n - 1);
                        result = Source.Substring(n, srclen - n);
                    }
                    else
                    {
                        result = SearchAfter + Source;
                    }
                }
                else
                {
                    for (var I = 1; I <= srclen; I++)
                        if (Source[I - 1].ToString() == SearchAfter)
                        {
                            result = Source.Substring(I - 1, srclen - I + 1);
                            break;
                        }
                }
            }
            catch
            {
                ArrestStr = "";
                result = "";
            }

            return result;
        }

        public static string ArrestStringEx(string Source, char SearchAfter, char ArrestBefore, ref string ArrestStr)
        {
            var result = string.Empty;
            int srclen;
            bool GoodData;
            int n;
            ArrestStr = string.Empty;
            if (Source == "")
            {
                result = "";
                return result;
            }

            try
            {
                srclen = Source.Length;
                GoodData = false;
                if (srclen >= 2)
                {
                    if (Source[0].ToString() == SearchAfter.ToString())
                    {
                        Source = Source.Substring(1, srclen - 1);
                        srclen = Source.Length;
                        GoodData = true;
                    }
                    else
                    {
                        n = Source.IndexOf(SearchAfter) + 1;
                        if (n > 0)
                        {
                            Source = Source.Substring(n, srclen - n);
                            srclen = Source.Length;
                            GoodData = true;
                        }
                    }
                }

                if (GoodData)
                {
                    n = Source.IndexOf(ArrestBefore) + 1;
                    if (n > 0)
                    {
                        ArrestStr = Source.Substring(0, n - 1);
                        result = Source.Substring(n, srclen - n);
                    }
                    else
                    {
                        result = SearchAfter + Source;
                    }
                }
                else
                {
                    for (var I = 1; I <= srclen; I++)
                        if (Source[I - 1].ToString() == SearchAfter.ToString())
                        {
                            result = Source.Substring(I - 1, srclen - I + 1);
                            break;
                        }
                }
            }
            catch
            {
                ArrestStr = "";
                result = "";
            }

            return result;
        }

        public static string SkipStr(string Src, char[] Skips)
        {
            string result;
            int I;
            int Len;
            int C;
            bool NowSkip;
            Len = Src.Length;
            // Count := sizeof(Skips) div sizeof (Char);
            for (I = 1; I <= Len; I++)
            {
                NowSkip = false;
                for (C = Skips.GetLowerBound(0); C <= Skips.GetUpperBound(0); C++)
                    if (Src[I] == Skips[C])
                    {
                        NowSkip = true;
                        break;
                    }

                if (!NowSkip) break;
            }

            result = Src.Substring(I - 1, Len - I + 1);
            return result;
        }

        public static string CombineDirFile(string SrcDir, string TargName)
        {
            string result;
            if (SrcDir == "" || TargName == "")
            {
                result = SrcDir + TargName;
                return result;
            }

            if (SrcDir[SrcDir.Length] == '\\')
                result = SrcDir + TargName;
            else
                result = SrcDir + '\\' + TargName;
            return result;
        }

        public static bool CompareLStr(string src, string targ, int compn)
        {
            var result = false;
            if (compn <= 0) return result;
            if (src.Length < compn) return result;
            if (targ.Length < compn) return result;
            result = true;
            for (var i = 0; i <= compn - 1; i++)
            {
                if (char.ToUpper(src[i]) == char.ToUpper(targ[i])) continue;
                result = false;
                break;
            }
            return result;
        }

        private static bool IsEnglish(char Ch)
        {
            var result = false;
            if (Ch >= 'A' && Ch <= 'Z' || Ch >= 'a' && Ch <= 'z') result = true;
            return result;
        }

        public static bool IsEngNumeric(char Ch)
        {
            var result = false;
            if (IsEnglish(Ch) || Ch >= '0' && Ch <= '9') result = true;
            return result;
        }

        public static bool IsEnglishStr(string sEngStr)
        {
            var result = false;
            for (var i = 1; i <= sEngStr.Length; i++)
            {
                result = IsEnglish(sEngStr[i]);
                if (result) break;
            }

            return result;
        }

        public static bool IsFloatNumeric(string str)
        {
            bool result;
            if (str.Trim() == "")
            {
                result = false;
                return result;
            }

            try
            {
                Convert.ToSingle(str);
                result = true;
            }
            catch
            {
                result = false;
            }

            return result;
        }

        public static string ReplaceChar(string src, char srcchr, char repchr)
        {
            string result;
            int i;
            int len;
            if (src != "")
            {
                len = src.Length;
                var sb = new StringBuilder();
                for (i = 0; i < len; i++)
                    if (src[i] == srcchr)
                        sb.Append(repchr);
            }

            result = src;
            return result;
        }

        public static bool IsUniformStr(string src, char ch)
        {
            bool result;
            int i;
            int len;
            result = true;
            if (src != "")
            {
                len = src.Length;
                for (i = 0; i < len; i++)
                    if (src[i] == ch)
                    {
                        result = false;
                        break;
                    }
            }

            return result;
        }

        public static string _StrPas(string dest)
        {
            string result;
            int i;
            result = "";
            for (i = 0; i < dest.Length; i++)
                if (dest[i] != (char) 0)
                    result = result + dest[i];
                else
                    break;
            return result;
        }


        public static string CutHalfCode(string Str)
        {
            string result;
            int pos;
            int Len;
            result = "";
            pos = 1;
            Len = Str.Length;
            while (true)
            {
                if (pos > Len) break;
                if (Str[pos] > '')
                {
                    if (pos + 1 <= Len && Str[pos + 1] > '')
                    {
                        result = result + Str[pos] + Str[pos + 1];
                        pos++;
                    }
                }
                else
                {
                    result = result + Str[pos];
                }

                pos++;
            }

            return result;
        }

        public static int TagCount(string source, char tag)
        {
            var result = 0;
            var tcount = 0;
            for (var i = 0; i <= source.Length - 1; i++)
                if (source[i] == tag)
                    tcount++;
            result = tcount;
            return result;
        }

        // "xxxxxx" => xxxxxx
        public static string TakeOffTag(string src, char tag, ref string rstr)
        {
            string result;
            int n2;
            n2 = src.Substring(2 - 1, src.Length).IndexOf(tag);
            rstr = src.Substring(2 - 1, n2 - 1);
            result = src.Substring(n2 + 2 - 1, src.Length - n2);
            return result;
        }

        // *
        public static string CatchString(string source, char cap, ref string catched)
        {
            string result;
            int n;
            result = "";
            catched = "";
            if (source == "") return result;
            if (source.Length < 2)
            {
                result = source;
                return result;
            }

            if (source[1] == cap)
            {
                if (source[2] == cap)
                    // ##abc#
                    source = source.Substring(2 - 1, source.Length);
                if (TagCount(source, cap) >= 2)
                    result = TakeOffTag(source, cap, ref catched);
                else
                    result = source;
            }
            else
            {
                if (TagCount(source, cap) >= 2)
                {
                    n = source.IndexOf(cap);
                    source = source.Substring(n - 1, source.Length);
                    result = TakeOffTag(source, cap, ref catched);
                }
                else
                {
                    result = source;
                }
            }

            return result;
        }

        // GetValidStr3�� �޸� �ĺ��ڰ� �������� ���ð�� ó�� �ȵ�
        // �ĺ��ڰ� ���� ���, nil ����..
        public static string DivString(string source, char cap, ref string sel)
        {
            string result;
            int n;
            if (source == "")
            {
                sel = "";
                result = "";
                return result;
            }

            n = source.IndexOf(cap);
            if (n > 0)
            {
                sel = source.Substring(1 - 1, n - 1);
                result = source.Substring(n + 1 - 1, source.Length);
            }
            else
            {
                sel = source;
                result = "";
            }

            return result;
        }

        public static string DivTailString(string source, char cap, ref string sel)
        {
            string result;
            int i;
            int n;
            if (source == "")
            {
                sel = "";
                result = "";
                return result;
            }

            n = 0;
            for (i = source.Length; i >= 1; i--)
                if (source[i] == cap)
                {
                    n = i;
                    break;
                }

            if (n > 0)
            {
                sel = source.Substring(n + 1 - 1, source.Length);
                result = source.Substring(1 - 1, n - 1);
            }
            else
            {
                sel = "";
                result = source;
            }

            return result;
        }

        public static int SPos(string substr, string str)
        {
            int result;
            int i;
            int j;
            int len;
            int slen;
            bool flag;
            result = -1;
            len = str.Length;
            slen = substr.Length;
            for (i = 0; i <= len - slen; i++)
            {
                flag = true;
                for (j = 1; j <= slen; j++)
                    if ((byte) str[i + j] >= 0xB0)
                    {
                        if (j < slen && i + j < len)
                        {
                            if (substr[j] != str[i + j])
                            {
                                flag = false;
                                break;
                            }

                            if (substr[j + 1] != str[i + j + 1])
                            {
                                flag = false;
                                break;
                            }
                        }
                        else
                        {
                            flag = false;
                        }
                    }
                    else if (substr[j] != str[i + j])
                    {
                        flag = false;
                        break;
                    }

                if (flag)
                {
                    result = i + 1;
                    break;
                }
            }

            return result;
        }

        public static string BoolToStr(bool boo)
        {
            string result;
            if (boo)
                result = "TRUE";
            else
                result = "FALSE";
            return result;
        }

        public static int _MIN(int n1, int n2)
        {
            int result;
            if (n1 < n2)
                result = n1;
            else
                result = n2;
            return result;
        }

        public static int _MAX(int n1, int n2)
        {
            int result;
            if (n1 > n2)
                result = n1;
            else
                result = n2;
            return result;
        }

        public static bool IsIPaddr(string IP)
        {
            bool result;
            var Node = new int[3 + 1];
            result = false;
            //tIP = IP;
            //tLen = tIP.Length;
            //tPos = tIP.IndexOf('.');
            //tNode = tIP.Substring(1, tPos - 1);
            //tIP = tIP.Substring(tPos + 1, tLen - tPos);
            //
            //if (!TryStrToInt(tNode, Node[0]))
            //{
            //    return result;
            //}
            //tLen = tIP.Length;
            //tPos = tIP.IndexOf('.');
            //tNode = tIP.Substring(1, tPos - 1);
            //tIP = tIP.Substring(tPos + 1, tLen - tPos);
            //
            //if (!TryStrToInt(tNode, Node[1]))
            //{
            //    return result;
            //}
            //tLen = tIP.Length;
            //tPos = tIP.IndexOf('.');
            //tNode = tIP.Substring(1, tPos - 1);
            //tIP = tIP.Substring(tPos + 1, tLen - tPos);
            //
            //if (!TryStrToInt(tNode, Node[2]))
            //{
            //    return result;
            //}
            //
            //if (!TryStrToInt(tIP, Node[3]))
            //{
            //    return result;
            //}
            //for (tLen = Node.GetLowerBound(0); tLen <= Node.GetUpperBound(0); tLen ++ )
            //{
            //    if ((Node[tLen] < 0) || (Node[tLen] > 255))
            //    {
            //        return result;
            //    }
            //}
            result = true;
            return result;
        }

        public static string BoolToCStr(bool b)
        {
            string result;
            if (b)
                result = "��";
            else
                result = "��";
            return result;
        }

        public static string BoolToIntStr(bool b)
        {
            string result;
            if (b)
                result = "1";
            else
                result = "0";
            return result;
        }

        public static int CalcFileCRC(string FileName)
        {
            return 0;
        }

        public static byte[] GetBytes(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public static byte[] GetBytes(int str)
        {
            return Encoding.ASCII.GetBytes(str.ToString());
        }

        public static int GetDayCount(DateTime MaxDate, DateTime MinDate)
        {
            int result;
            int YearMax;
            int MonthMax;
            int DayMax;
            int YearMin;
            int MonthMin;
            int DayMin;
            result = 0;
            if (MaxDate < MinDate) return result;
            YearMax = MaxDate.Year;
            MonthMax = MaxDate.Month;
            DayMax = MaxDate.Day;
            YearMin = MinDate.Year;
            MonthMin = MinDate.Month;
            DayMin = MinDate.Day;
            YearMax -= YearMin;
            YearMin = 0;
            result = YearMax * 12 * 30 + MonthMax * 30 + DayMax - (YearMin * 12 * 30 + MonthMin * 30 + DayMin);
            return result;
        }

        // Damian - These 2 functions are same, so use either
        public static int GetCodeMsgSize(double X)
        {
            int result;
            if (Convert.ToInt32(X) < X)
                result = Convert.ToInt32(X) + 1;
            else
                result = Convert.ToInt32(X);
            return result;
        }


        public static unsafe void IntPtrToIntPtr(IntPtr Src, int SrcIndex, IntPtr Dest, int DestIndex, int nLen)
        {
            var pSrc = (byte*) Src + SrcIndex;
            var pDest = (byte*) Dest + DestIndex;
            if (pDest > pSrc)
            {
                pDest = pDest + (nLen - 1);
                pSrc = pSrc + (nLen - 1);
                for (var i = 0; i < nLen; i++)
                    *pDest-- = *pSrc--;
            }
            else
            {
                for (var i = 0; i < nLen; i++)
                    *pDest++ = *pSrc++;
            }
        }

        public static unsafe byte[] BytePtrToByteArray(byte* pb, int pbSize)
        {
            var ret = new byte[pbSize];
            for (var i = 0; i < pbSize; i++) ret[i] = pb[i];
            return ret;
        }

        public static unsafe int ByteArrayToBytePtr(byte* pb, int pbSize, byte[] ByteAry)
        {
            int nLen;
            if (ByteAry.Length > pbSize)
            {
                for (var i = 0; i < pbSize; i++)
                    pb[i] = ByteAry[i];

                nLen = pbSize;
            }
            else
            {
                for (var i = 0; i < ByteAry.Length; i++) pb[i] = ByteAry[i];

                nLen = ByteAry.Length;
            }

            return nLen;
        }

        public static unsafe ushort[] ushortPrtToushortArray(ushort* pb, int pbSize)
        {
            var ret = new ushort[pbSize];
            for (var i = 0; i < pbSize; i++) ret[i] = pb[i];
            return ret;
        }

        public static unsafe int UShortArrayToUShortPtr(ushort[] pb, int pbSize, ushort[] ByteAry)
        {
            var nLen = 0;
            for (var i = 0; i < pbSize; i++)
            {
                pb[i] = ByteAry[i];
                nLen = pbSize;
            }

            return nLen;
        }

        public static unsafe int UShortArrayToUShortPtr(ushort* pb, int pbSize, ushort[] ByteAry)
        {
            int nLen;
            if (ByteAry.Length > pbSize)
            {
                for (var i = 0; i < pbSize; i++)
                    pb[i] = ByteAry[i];

                nLen = pbSize;
            }
            else
            {
                for (var i = 0; i < ByteAry.Length; i++) pb[i] = ByteAry[i];

                nLen = ByteAry.Length;
            }

            return nLen;
        }


        /// <summary>
        /// SByteתstring
        /// </summary>
        /// <param name="by"></param>
        /// <param name="StartIndex"></param>
        /// <param name="Len"></param>
        /// <returns></returns>
        public static unsafe string SBytePtrToString(sbyte* by, int StartIndex, int Len)
        {
            try
            {
                return BytePtrToString((byte*) by, StartIndex, Len);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// SByteתstring
        /// </summary>
        /// <param name="by"></param>
        /// <param name="StartIndex"></param>
        /// <param name="Len"></param>
        /// <returns></returns>
        public static unsafe string SBytePtrToString(byte* by, int StartIndex, int Len)
        {
            try
            {
                return BytePtrToString(by, StartIndex, Len);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// SByteתString
        /// </summary>
        /// <param name="by"></param>
        /// <param name="Len"></param>
        /// <returns></returns>
        public static unsafe string SBytePtrToString(sbyte* by, int Len)
        {
            try
            {
                return Marshal.PtrToStringAnsi((IntPtr) by, Len);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static unsafe int StringToSBytePtr(string str, sbyte* retby, int StartIndex)
        {
            var nLen = StringToBytePtr(str, null, 0);
            if (retby == null)
                return nLen;

            return StringToBytePtr(str, (byte*) retby, StartIndex);
        }

        public static unsafe string IntPtrToString(IntPtr by, int StartIndex, int Len)
        {
            return BytePtrToString((byte*) by, StartIndex, Len);
        }

        public static unsafe int StringToIntPtr(string str, IntPtr retby, int StartIndex)
        {
            return StringToBytePtr(str, (byte*) retby, StartIndex);
        }

        public static unsafe string IntPtrPlusLenToString(IntPtr by, int StartIndex)
        {
            var pb = (byte*) by + StartIndex;
            var nLen = *(int*) pb;

            var ret = new string('\0', nLen);
            var sb = new StringBuilder(ret);

            pb += sizeof(int);
            for (var i = 0; i < nLen; i++)
                sb[i] = (char) pb[i];

            return sb.ToString();
        }

        public static unsafe int StringToIntPtrPlusLen(string str, IntPtr retby, int StartIndex)
        {
            var nLen = StringToBytePtr(str, null, 0);
            if (retby == (IntPtr) 0)
                return nLen + sizeof(int);

            var pb = (byte*) retby + StartIndex;
            *(int*) pb = nLen;
            pb += sizeof(int);
            StringToBytePtr(str, pb, 0);

            return nLen + sizeof(int);
        }

        public static unsafe string BytePtrToString(byte* by, int StartIndex, int Len)
        {
            var ret = new string('\0', Len);
            var sb = new StringBuilder(ret);

            by += StartIndex;
            for (var i = 0; i < Len; i++) sb[i] = (char) *@by++;

            return sb.ToString();
        }

        /// <summary>
        /// �ַ���תByte�ֽ�����
        /// </summary>
        /// <param name="str"></param>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static unsafe byte[] StringToByteAry(string str, out int strLength)
        {
            strLength = StringToBytePtr(str, null, 0);
            var ret = new byte[strLength + 1];
            fixed (byte* pb = ret)
            {
                StringToBytePtr(str, pb, 1);
            }

            return ret;
        }

        /// <summary>
        /// �����л�
        /// </summary>
        /// <param name="rawdatas"></param>
        /// <param name="retType"></param>
        /// <returns></returns>
        public static object rawDeserialize(byte[] rawdatas, Type retType)
        {
            var retobj = Activator.CreateInstance(retType);
            var rawsize = Marshal.SizeOf(retType);
            if (rawsize > rawdatas.Length)
                return retobj;
            var buffer = Marshal.AllocHGlobal(rawsize);
            try
            {
                Marshal.Copy(rawdatas, 0, buffer, rawsize);
                retobj = Marshal.PtrToStructure(buffer, retType);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return retobj;
        }
    }
}