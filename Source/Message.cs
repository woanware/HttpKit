using System;
using System.Collections.Generic;
using System.IO;

namespace HttpKit
{
    /// <summary>
    /// 
    /// </summary>
    public class Message
    {
        public Request Request { get; set; }
        public Response Response { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Message()
        {
            Request = new Request();
            Response = new Response();
        }

        /// <summary>
        /// Writes the contents of the HTTP request and response to a file
        /// </summary>
        public void WriteToFile(string filePath, bool overwrite)
        {
            FileMode fileMode = FileMode.OpenOrCreate;
            if (overwrite == true)
            {
                fileMode = FileMode.Create;
            }

            using (FileStream fs = new FileStream(filePath, fileMode, FileAccess.Write))
            {
                // Write the request to the file
                byte[] request = System.Text.Encoding.ASCII.GetBytes(this.ToString());
                fs.Write(request, 0, request.Length);

                // Add a new line between the request and response
                request = System.Text.Encoding.ASCII.GetBytes(Environment.NewLine);
                fs.Write(request, 0, request.Length);

                // Write the response to the file
                string temp = this.Response.GetHeadersAsString(true);
                temp += Environment.NewLine;
                byte[] headers = System.Text.Encoding.ASCII.GetBytes(temp);
                fs.Write(headers, 0, headers.Length);

                using (FileStream read = System.IO.File.OpenRead(this.Response.TempFile))
                using (BinaryReader reader = new BinaryReader(read))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    // create a buffer to hold the bytes 
                    byte[] buffer = new Byte[4096];
                    int bytesRead;

                    // while the read method returns bytes
                    // keep writing them to the output stream
                    while ((bytesRead = reader.Read(buffer, 0, 4096)) > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void WriteToHtmlFile(string filePath)
        {

        }
    }
}
