using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using woanware;

namespace HttpKit
{
    /// <summary>
    /// 
    /// </summary>
    public class Response
    {
        public List<Header> Headers { get; set; }
        public List<byte> Body { get; set; }
        public string File { get; set; }
        public string TempFile { get; private set; }
        public long TempFileSize { get; private set; }
        public HttpKit.Global.HttpVersion HttpVersion { get; private set; }
        public string StatusDesc { get; private set; }
        public short StatusCode { get; private set; }
        public bool HasBeenGzipped { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public Response()
        {
            Headers = new List<Header>();
            Body = new List<byte>();
            TempFile = Utility.GetTempFile();
            StatusCode = 0;
            File = string.Empty;
            StatusDesc = string.Empty;
        }

        /// <summary>
        /// Extract out the response code etc
        /// </summary>
        /// <param name="data"></param>
        private void ParseFirstLine(string data)
        {
            //Get HTTP version
            if (data.StartsWith("HTTP/1.0") == true)
            {
                HttpVersion = Global.HttpVersion.Http1;
            }
            else if (data.StartsWith("HTTP/1.1") == true)
            {
                HttpVersion = Global.HttpVersion.Http11;
            }
            else
            {
                throw new Exception("Invalid HTTP version: " + data);
            }

            string temp = string.Empty;
            string statusCode = string.Empty;
            string statusDesc = string.Empty;

            try
            {
                // Get past the HTTP/1.0 or HTTP/1.1 (e.g. 8 chars), to then get the status code and desc
                temp = data.Substring(9);
                statusCode = temp.Substring(0, 3);
                statusDesc = temp.Substring(4);
            }
            catch (Exception){}

            this.StatusDesc = statusDesc;
            short tempStatusCode = 0;
            if (short.TryParse(statusCode, out tempStatusCode) == true)
            {
                this.StatusCode = tempStatusCode;
            }
        }

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        /// <param name="stream"></param>
        /// <param name="lineReader"></param>
        /// <returns></returns>
        public bool Parse(string line, 
                          Stream stream, 
                          LineReader lineReader, string inputfile)
        {
            ParseFirstLine(line);

            do
            {
                //string temp = Utility.ReadLine(stream);
                string temp = lineReader.ReadLine();

                if (temp == null)
                {
                    return true;
                }

                // Check to see if we have reached the end of the 
                // headers, if so then we process the body etc
                if (temp.Length == 0)
                {
                    if (DoesHttpMethodIncludeBody() == false)
                    {
                        return true;
                    }

                    if (this.IsChunked == true)
                    {
                        if (ParseChunkedResponse(stream, lineReader, inputfile) == false)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (ParseResponse(stream) == false)
                        {
                            return false;
                        }
                    }

                    if (this.IsGzipped == true)
                    {
                        if (PerformGzipDecompression(inputfile) == false)
                        {
                            // TODO raise error?
                        }
                    }

                    // Response complete
                    return true;
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
        /// <returns></returns>
        public string GetHeadersAsString(bool includeStatus)
        {
            StringBuilder output = new StringBuilder();
            if (includeStatus == true)
            {
                output.AppendFormat("HTTP/{0} {1} {2}{3}", 
                                    this.HttpVersion.GetEnumDescription(), 
                                    this.StatusCode, 
                                    this.StatusDesc, 
                                    Environment.NewLine);
            }

            foreach (Header header in this.Headers)
            {
                bool outputHeader = true;
                if (this.HasBeenGzipped == true)
                {
                    if (header.Name == ("content-encoding").ToLower())
                    {
                        if (header.Value.Contains("gzip") == true)
                        {
                            outputHeader = false;
                        }
                    }
                }

                if (outputHeader == true)
                {
                    output.AppendLine(header.ToString());
                }
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
                string temp = GetHeadersAsString(true);
                temp += Environment.NewLine;
                byte[] headers = System.Text.Encoding.ASCII.GetBytes(temp);
                fs.Write(headers, 0, headers.Length);

                using (FileStream read = System.IO.File.OpenRead(this.TempFile))
                {
                    BinaryReader reader = new BinaryReader(read);
                    BinaryWriter writer = new BinaryWriter(fs);

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
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine(GetHeadersAsString(true));

            if (this.TempFileSize > 0)
            {
                byte[] temp = System.IO.File.ReadAllBytes(this.TempFile);
                temp = woanware.Text.ReplaceNulls(temp);

                string data = ASCIIEncoding.ASCII.GetString(temp);
                output.AppendLine(string.Empty);
                output.AppendLine(data);
            }

            return string.Empty;
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private bool ParseResponse(Stream stream)
        {
            long bytesWritten = 0;
            int bufferSize = 4096;
            try
            {
                long contentLength = ContentLength;
                using (FileStream writeStream = System.IO.File.OpenWrite(this.TempFile))
                {
                    byte[] buffer = new byte[bufferSize]; 
                    do
                    {
                        if (contentLength != -1)
                        {
                            if ((contentLength - bytesWritten) < bufferSize)
                            {
                                bufferSize = (int)(contentLength - bytesWritten);
                                buffer = new byte[bufferSize];
                            }
                        }

                        // Read the data from the response in chunks
                        int ret = stream.Read(buffer, 0, bufferSize);
                        // Write the response data to the temp file
                        writeStream.Write(buffer, 0, ret);

                        bytesWritten += ret;

                        if (ret != bufferSize)
                        {
                            // Reached the end of the stream
                            return true;
                        }

                        if (bytesWritten == contentLength)
                        {
                            // Reached the end of the content, now need to back the stream position

                            return true;
                        }
                    }
                    while (stream.Position < stream.Length);
                }
            }
            finally
            {
                this.TempFileSize = bytesWritten;
            }
            
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="lineReader"></param>
        /// <returns></returns>
        private bool ParseChunkedResponse(Stream stream, 
                                          LineReader lineReader, 
                                          string inputfile)
        {
            using (FileStream writeStream = System.IO.File.OpenWrite(this.TempFile))
            {
                long bytesWritten = 0;
                try
                {
                    do
                    {
                        string temp = lineReader.ReadLine();
                        if (temp == null)
                        {
                            return true;
                        }

                        if (temp.Length == 0)
                        {
                            temp = lineReader.ReadLine();

                            if (temp == null)
                            {
                                return true;
                            }

                            if (temp.Length == 0)
                            {
                                return true;
                            }
                        }

                        // Some chunked encoding has a semi-colon after
                        // the size of the chunk, so lets just remove it
                        temp = temp.Replace(";", string.Empty);

                        // Now try to parse out an INT from the string representation of a hex number
                        int chunkSize = 0;
                        if (int.TryParse(temp,
                                         NumberStyles.HexNumber,
                                         CultureInfo.InvariantCulture,
                                         out chunkSize) == false)
                        {
                            // TODO raise error?
                            return true;
                        }

                        if (chunkSize == 0)
                        {
                            // A zero signifies that we have reached the end of the chunked sections so lets exit
                            return true;
                        }

                        //if (stream.Position + chunkSize > stream.Length)
                        //{
                        //    // TODO raise error? Invalid chunk data e.g. is greater length than rest of stream
                        //    return false;
                        //}

                        byte[] chunk = new byte[chunkSize];
                        int ret = lineReader.Read(chunk, 0, chunkSize);
                        bytesWritten += ret;
                        if (ret != chunkSize)
                        {
                            // TODO raise error? e.g. the amount of data read should be equal to the amount in the chunk size?
                            OutputDebug("GZIP", inputfile, string.Empty);
                            return false;
                        }

                        writeStream.Write(chunk, 0, ret);
                    }
                    while (stream.Position < stream.Length);
                }
                finally
                {
                    this.TempFileSize = bytesWritten;
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool PerformGzipDecompression(string inputfile)
        {
            string decompressTempFile = Utility.GetTempFile();
            try
            {
                using (FileStream fileRead = System.IO.File.OpenRead(this.TempFile))
                using (FileStream fileWrite = System.IO.File.OpenWrite(decompressTempFile))
                using (GZipStream gzipStream = new GZipStream(fileRead, CompressionMode.Decompress))
                {
                    const int size = 4096;
                    byte[] buffer = new byte[size];
                    using (MemoryStream memory = new MemoryStream())
                    {
                        int count = 0;
                        do
                        {
                            count = gzipStream.Read(buffer, 0, size);
                            if (count > 0)
                            {
                                fileWrite.Write(buffer, 0, count);
                            }
                        }
                        while (count > 0);
                    }
                }

                IO.DeleteFile(this.TempFile);
                this.TempFile = decompressTempFile;
                HasBeenGzipped = true;
                
                return true;
            }
            catch (Exception ex)
            {
                OutputDebug("GZIP", inputfile, ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool DoesHttpMethodIncludeBody()
        {
            switch (this.StatusCode)
            {
                case 204: // No Content
                case 304: // Not Modified
                    return false;
                default:
                    return true;
            }
        }

        #region Properties
        // <summary>
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
        public string ContentType
        {
            get
            {
                return Utility.ReturnHeaderValueAsString(Headers, "content-type");
            }
        }

        /// <summary>
        /// </summary>
        public string ContentDisposition
        {
            get
            {
                return Utility.ReturnHeaderValueAsString(Headers, "content-disposition");
            }
        }

        /// <summary>
        /// </summary>
        public string ContentEncoding
        {
            get
            {
                return Utility.ReturnHeaderValueAsString(Headers, "content-encoding");
            }
        }

        /// <summary>
        /// </summary>
        public string ContentRange
        {
            get
            {
                return Utility.ReturnHeaderValueAsString(Headers, "content-range");
            }
        }

        /// <summary>
        /// </summary>
        public string TransferEncoding
        {
            get
            {
                return Utility.ReturnHeaderValueAsString(Headers, "transfer-encoding");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsChunked
        {
            get
            {
                if (Utility.ReturnHeaderValueAsString(Headers, "transfer-encoding").ToLower() == "chunked")
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasContentLength
        {
            get
            {
                return Utility.HasHeader(this.Headers, "content-length");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsGzipped
        {
            get
            {
                if (Utility.ReturnHeaderValueAsString(Headers, "content-encoding").ToLower().Contains("gzip") == true)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string GetContentDispositionFileName
        {
            get
            {
                string contentDisposition = this.ContentDisposition;
                int ret = contentDisposition.IndexOf("filename");
                if (ret == -1)
                {
                    return string.Empty;
                }

                string temp = contentDisposition.Substring(ret + "filename".Length);
                ret = temp.IndexOf("=");
                if (ret == -1)
                {
                    return string.Empty;
                }

                return temp.Substring(ret + 1);
            }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="error"></param>
        private void OutputDebug(string reason, string inputfile, string error)
        {
            string temp = "Reason: " + reason + Environment.NewLine;
            temp += "Input: " + inputfile + Environment.NewLine;
            temp += this.GetHeadersAsString(true);
            temp += Environment.NewLine + this.TempFile;
            temp += Environment.NewLine + error;

            this.Log().Error(temp);      
        }
    }
}
