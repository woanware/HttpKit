using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HttpKit
{
    /// <summary>
    /// 
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Generic method for returning a headers value
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        public static string ReturnHeaderValueAsString(List<Header> headers, 
                                                       string header)
        {
            var temp = from h in headers where h.Name.ToLower() == header.ToLower() select h;
            if (temp.Any() == false)
            {
                return string.Empty;
            }

            return temp.First().Value;
        }

        /// <summary>
        /// Generic method for returning a headers value
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        public static long ReturnHeaderValueAsLong(List<Header> headers, 
                                                   string header)
        {
            var temp = from h in headers where h.Name.ToLower() == header.ToLower() select h;
            if (temp.Any() == false)
            {
                return -1;
            }

            long ret = -1;
            if (long.TryParse(temp.First().Value, out ret) == false)
            {
                return -1;
            }

            return ret;
        }

        /// <summary>
        /// Generic method for returning a headers value
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        public static bool HasHeader(List<Header> headers,
                                     string header)
        {
            var temp = from h in headers where h.Name.ToLower() == header.ToLower() select h;
            return temp.Any();
        }

        /// <summary>
        /// Reads one line from the byte[] in data and returns the line as a string.
        /// A line is defined by a number of char's followed by \r\n
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dataIndex"></param>
        /// <returns>The string (wihtout CRLF) if all is OK, otherwise null (for example if there is no CRLF)</returns>
        //public static string ReadLine(Stream stream)
        //{
        //    //int maxStringLength = 16384;
        //    //  \r = 0x0d = carriage return
        //    //  \n = 0x0a = line feed
        //    StringBuilder line = new StringBuilder();
        //    //int indexOffset = 0;

        //    bool newline = false;
        //    while (newline == false)
        //    {
        //        //if (dataIndex + indexOffset >= data.Length || indexOffset >= maxStringLength)
        //        //    return null;
        //        //else
        //        {
        //            byte b = (byte)stream.ReadByte();
        //            if (b == 0x0d)
        //            {
        //                newline = true;
        //            }
        //            else if (b == 0x0a)
        //            {
        //                newline = true;
        //            }

        //            line.Append((char)b);

        //            if (line.Length > 100000)
        //            {

        //            }
        //            //indexOffset++;
        //        }
        //    }
        //    //dataIndex += indexOffset;
        //    return line.ToString();
        //}

        /// <summary>
        /// Reads one line from the byte[] in data and returns the line as a string.
        /// A line is defined by a number of char's followed by \r\n
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dataIndex"></param>
        /// <returns>The string (wihtout CRLF) if all is OK, otherwise null (for example if there is no CRLF)</returns>
        public static string ReadLineold(Stream stream)
        {
            //int maxStringLength = 16384;
            //  \r = 0x0d = carriage return
            //  \n = 0x0a = line feed
            StringBuilder line = new StringBuilder();
            bool carrigeReturnReceived = false;
            bool lineFeedReceived = false;
            //int indexOffset = 0;
            while (!carrigeReturnReceived || !lineFeedReceived)
            {
                //if (dataIndex + indexOffset >= data.Length || indexOffset >= maxStringLength)
                //    return null;
                //else
                {
                    byte b = (byte)stream.ReadByte();
                    if (b == 0x0d)
                        carrigeReturnReceived = true;
                    else if (carrigeReturnReceived && b == 0x0a)
                        lineFeedReceived = true;
                    else
                    {
                        line.Append((char)b);
                        carrigeReturnReceived = false;
                        lineFeedReceived = false;
                    }
                    //indexOffset++;
                }
            }
            //dataIndex += indexOffset;
            return line.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="data"></param>
        public static void WriteToFileStream(Stream fs, string data)
        {
            byte[] temp = ASCIIEncoding.ASCII.GetBytes(data);
            fs.Write(temp, 0, temp.Length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="data"></param>
        public static void WriteToFileStream(Stream fs, byte[] data)
        {
            fs.Write(data, 0, data.Length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string GetTempFile()
        {
            return Path.Combine(Path.GetTempPath(), System.Guid.NewGuid().ToString() + ".tmp");
        }
    }
}
