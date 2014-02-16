using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using woanware;

namespace HttpKit
{
    /// <summary>
    /// 
    /// </summary>
    public class Request
    {
        public List<Header> Headers { get; private set; }
        public List<byte> Body { get; set; }
        public Global.HttpVersion HttpVersion { get; private set; }
        public string Url { get; private set; }
        public string Method { get; private set; }
        public bool WriteParsedDataToFile { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Request()
        {
            Headers = new List<Header>();
            Body = new List<byte>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public bool Parse(string line, Stream stream, LineReader lineReader)
        {
            ParseFirstLine(line);

            do
            {
                //string temp = Utility.ReadLine(stream);
                string temp = lineReader.ReadLine();

                // Check to see if we have reached the end of the 
                // headers, if so then we process the body etc
                if (temp.Length == 0)
                {
                    long contentLength = this.ContentLength;
                    if (contentLength == -1)
                    {
                        return true;
                    }

                    if (stream.Position + contentLength > stream.Length)
                    {
                        // Invalid Body data e.g. is greater length than rest of stream
                        return true;
                    }

                    // Read in the request body
                    byte[] body = new byte[contentLength];
                    int ret = stream.Read(body, 0, (int)contentLength);
                    this.Body = body.ToList();

                    return true;
                }

                if (temp.Contains("Host: www.luscombemaye.co.uk"))
                {

                }

                // Still identified a header so add to list
                Header header = new Header(temp);
                this.Headers.Add(header);
            }
            while (stream.Position < stream.Length);

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        private void ParseFirstLine(string data)
        {
            int index1 = data.IndexOf(" ");
            Method = data.Substring(0, index1);
            int index2 = data.IndexOf(" HTTP/1.1");
            if (index2 > -1)
            {
                Url = data.Substring(index1 + 1, data.Length - (index1 + 1) - 9);
                HttpVersion = Global.HttpVersion.Http11;
            }
            else
            {
                int index3 = data.IndexOf(" HTTP/1.0");
                Url = data.Substring(index1 + 1, data.Length - (index1 + 1) - 9);
                HttpVersion = Global.HttpVersion.Http1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetHeadersAsString()
        {
            StringBuilder output = new StringBuilder();
            foreach (Header header in this.Headers)
            {
                output.AppendLine(header.ToString());
            }

            return output.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetBodyAsString()
        {
            return string.Join(string.Empty, this.Body);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            output.AppendFormat("{0} {1} HTTP/{2}{3}", this.Method, this.Url, this.HttpVersion.GetEnumDescription(), Environment.NewLine);
            output.AppendLine(GetHeadersAsString());

            if (this.Body.Count > 0)
            {
                output.AppendLine(string.Empty);
                output.AppendLine(GetBodyAsString());
            }

            return output.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="overwrite"></param>
        public void WriteToFile(string filePath, bool overwrite)
        {
            FileMode fileMode = FileMode.OpenOrCreate;
            if (overwrite == true)
            {
                fileMode = FileMode.Create;
            }

            using (FileStream fs = new FileStream(filePath, fileMode, FileAccess.Write))
            {
                byte[] request = System.Text.Encoding.ASCII.GetBytes(this.ToString());
                fs.Write(request, 0, request.Length);
            }
        }

        #region Properties
        /// <summary>
        /// Locates the "Content-Length" HTTP header and parses
        /// to a long. Returns -1 if the header does not exist
        /// </summary>
        public long ContentLength
        {
            get
            {
                return Utility.ReturnHeaderValueAsLong(Headers, "content-length");
            }
        }

        /// <summary>
        /// </summary>
        public string Host
        {
            get
            {
                return Utility.ReturnHeaderValueAsString(Headers, "host");
            }
        }
        #endregion
    }
}
