using System.Collections.Generic;
using System.ComponentModel;

namespace HttpKit
{
    /// <summary>
    /// 
    /// </summary>
    public class Global
    {
        public const string HTML_HEADER = @"<html><body><FONT FACE=""courier"">";
        public const string HTML_FOOTER = @"</FONT></body></html>";

        /// <summary>
        /// 
        /// </summary>
        public enum HttpVersion
        {
            [Description("1.0")]
            Http1 = 0,
            [Description("1.1")]
            Http11 = 1
        }

        /// <summary>
        /// Taken from: http://annevankesteren.nl/2007/10/http-methods
        /// </summary>
        public static readonly List<string> HTTP_METHODS = new List<string>() { 
            "GET", "POST", "CONNECT", "OPTIONS", "HEAD", "PUT", "DELETE", 
            "TRACE","PROPFIND", "PROPPATCH", "MKCOL", "COPY", "MOVE", 
            "LOCK", "UNLOCK", "VERSION-CONTROL", "REPORT", "CHECKOUT", 
            "CHECKIN", "UNCHECKOUT", "MKWORKSPACE", "UPDATE", "LABEL", 
            "MERGE", "BASELINE-CONTROL", "MKACTIVITY", "ORDERPATCH", 
            "ACL", "PATCH", "SEARCH"};
    }
}
