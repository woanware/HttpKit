using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using woanware;

namespace HttpKit
{
    /// <summary>
    /// 
    /// </summary>
    public class Parser
    {
        #region Member Variables
        public long MaxResponseSize { get; set; }
        private List<Message> messages;
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public Parser(){}
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        public void Parse(Stream data, string inputfile)
        {
            Stream stream = data;

            using (LineReader lr = new LineReader(stream, 4096, Encoding.Default))
            {
                messages = new List<Message>();
                do
                {
                    string line = lr.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    if (IsRequest(line) == false)
                    {
                        continue;
                    }

                    Message message = new Message();

                    Request request = new Request();
                    if (request.Parse(line, stream, lr) == false)
                    {
                        continue;
                    }

                    message.Request = request;

                    line = lr.ReadLine();
                    if (line != null)
                    {
                        if (line.StartsWith("HTTP/1.") == true)
                        {
                            Response response = new Response();
                            if (response.Parse(line, stream, lr, inputfile) == true)
                            {
                                message.Response = response;
                            }
                        }
                    }

                    messages.Add(message);
                }
                while (stream.Position < stream.Length);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="overwrite"></param>
        public void WriteToFile(FileStream fileStream)
        {
            using (NonClosingStreamWrapper ncsw = new NonClosingStreamWrapper(fileStream))
            {
                foreach (Message message in this.Messages)
                {
                    // Write the request to the file
                    byte[] request = System.Text.Encoding.ASCII.GetBytes(message.Request.ToString());
                    ncsw.Write(request, 0, request.Length);

                    if (message.Response.StatusCode == 0)
                    {
                        continue;
                    }

                    // Write the response to the file
                    string temp = message.Response.GetHeadersAsString(true);
                    temp += Environment.NewLine;
                    byte[] headers = System.Text.Encoding.ASCII.GetBytes(temp);
                    ncsw.Write(headers, 0, headers.Length);

                    if (message.Response.TempFileSize == 0)
                    {
                        continue;
                    }

                    using (FileStream read = System.IO.File.OpenRead(message.Response.TempFile))
                    using (BinaryReader reader = new BinaryReader(read))
                    {
                        byte[] buffer = new Byte[4096];
                        int bytesRead;

                        // While the read method returns bytes
                        // keep writing them to the output stream
                        while ((bytesRead = reader.Read(buffer, 0, 4096)) > 0)
                        {
                            ncsw.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileStream"></param>
        public void WriteToHtmlFile(FileStream fileStream)
        {
            using (NonClosingStreamWrapper ncsw = new NonClosingStreamWrapper(fileStream))
            {
                Utility.WriteToFileStream(ncsw, Global.HTML_HEADER);

                foreach (Message message in this.Messages)
                {
                    Utility.WriteToFileStream(ncsw, "<font color=\"#006600\" size=\"2\">");

                    string temp = message.Request.ToString();
                    string tempHtml = HttpUtility.HtmlEncode(temp);
                    tempHtml = tempHtml.Replace("\r\n", "<br>");
                    tempHtml += @"</font><br>";

                    // Write the request to the file
                    Utility.WriteToFileStream(ncsw, tempHtml);

                    if (message.Response.StatusCode == 0)
                    {
                        continue;
                    }

                    // Write the response to the file
                    Utility.WriteToFileStream(ncsw, "<font color=\"#FF0000\" size=\"2\">");
                    temp = message.Response.GetHeadersAsString(true);
                    temp += Environment.NewLine;
                    temp = HttpUtility.HtmlEncode(temp);
                    temp = temp.Replace("\r\n", "<br>");

                    Utility.WriteToFileStream(ncsw, temp);

                    if (message.Response.TempFileSize == 0)
                    {
                        continue;
                    }

                    using (FileStream read = System.IO.File.OpenRead(message.Response.TempFile))
                    {
                        byte[] buffer;
                        FileInfo fi = new FileInfo(message.Response.TempFile);
                        if (fi.Length > 5242880)
                        {
                            buffer = new Byte[5242880];
                            int ret = read.Read(buffer, 0, 5242880);
                        }
                        else
                        {
                            buffer = new Byte[fi.Length];
                            int ret = read.Read(buffer, 0, (int)fi.Length);
                        }

                        string sanitised = System.Text.Encoding.ASCII.GetString(buffer);
                        sanitised = HttpUtility.HtmlEncode(sanitised);
                        sanitised = sanitised.Replace("\r\n", "<br>");
                        sanitised += "</font>";
                        
                        Utility.WriteToFileStream(ncsw, sanitised);
                    }
                }

                Utility.WriteToFileStream(ncsw, Global.HTML_HEADER);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsRequest(string line)
        {
            foreach (string method in Global.HTTP_METHODS)
            {
                if (line.StartsWith(method + " /") == true)
                {
                    return true;
                }
            }

            foreach (string method in Global.HTTP_METHODS)
            {
                if (line.StartsWith(method + " http") == true)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Message> Messages
        {
            get
            {
                return this.messages.AsEnumerable<Message>();
            }
        }
    }
}
