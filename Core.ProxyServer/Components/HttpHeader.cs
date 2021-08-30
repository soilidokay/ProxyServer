using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Core.ProxyServer.Components
{
    public class HttpHeader
    {
        public HttpHeader(string query) : this()
        {
            Query = query;
            if (IsValidQuery(query))
            {
                Convert(query);
            }
        }
        public HttpHeader()
        {
            Headers = new StringDictionary();
        }
        // private variables
        /// <summary>Holds the value of the HttpQuery property.</summary>
        //private string m_HttpQuery = "";
        public string Query { get; set; }
        /// <summary>Holds the value of the RequestedPath property.</summary>
        //private string m_RequestedPath = null;
        public string QueryPath { get; set; }
        /// <summary>Holds the value of the HeaderFields property.</summary>
        //private StringDictionary m_HeaderFields = null;
        public StringDictionary Headers { get; set; }
        /// <summary>Holds the value of the HttpVersion property.</summary>
        //private string m_HttpVersion = "";
        public string HttpVersion { get; set; }
        /// <summary>Holds the value of the HttpRequestType property.</summary>
        //private string m_HttpRequestType = "";
        public string Method { get; set; }
        /// <summary>Holds the POST data</summary>
        //private string m_HttpPost = null;
        public string Body { get; set; }

        ///<summary>Checks whether a specified string is a valid HTTP query string.</summary>
        ///<param name="Query">The query to check.</param>
        ///<returns>True if the specified string is a valid HTTP query, false otherwise.</returns>
        public static bool IsValidQuery(string Query)
        {
            int index = Query.IndexOf("\r\n\r\n");
            if (index == -1)
                return false;
            if (GetMethod(Query).ToUpper().Equals("POST"))
            {
                try
                {

                    int length = GetBodyLength(Query);
                    return Query.Length >= index + 6 + length;
                }
                catch
                {
                    //SendBadRequest();
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
        public static int GetBodyLength(string Query)
        {
            var match = Regex.Match(Query, @"(Content-Length)\s*:\s*\d+");
            var tmp = match.Value.Split(":")[1].Trim();
            return match == null ? 0 : int.Parse(tmp);
        }
        public static string GetMethod(string Query)
        {
            var Ret = Query.IndexOf(' ');
            return Ret > 0 && Ret < 10 ? Query.Substring(0, Ret) : "";
        }
        private HttpHeader Convert(string Query)
        {
            this.Query = Query;
            string[] Lines = Query.Replace("\r\n", "\n").Split('\n');
            int Cnt, Ret;
            //Extract requested URL
            if (Lines.Length > 0)
            {
                //Parse the Http Request Type
                Ret = Lines[0].IndexOf(' ');
                if (Ret > 0)
                {
                    Method = Lines[0].Substring(0, Ret);
                    Lines[0] = Lines[0].Substring(Ret).Trim();
                }
                //Parse the Http Version and the Requested Path
                Ret = Lines[0].LastIndexOf(' ');
                if (Ret > 0)
                {
                    HttpVersion = Lines[0].Substring(Ret).Trim();
                    QueryPath = Lines[0].Substring(0, Ret);
                }
                else
                {
                    QueryPath = Lines[0];
                }
                //Remove http:// if present
                if (QueryPath.Length >= 7 && QueryPath.Substring(0, 7).ToLower().Equals("http://"))
                {
                    Ret = QueryPath.IndexOf('/', 7);
                    if (Ret == -1)
                        QueryPath = "/";
                    else
                        QueryPath = QueryPath.Substring(Ret);
                }
            }
            for (Cnt = 1; Cnt < Lines.Length; Cnt++)
            {
                Ret = Lines[Cnt].IndexOf(":");
                if (Ret > 0 && Ret < Lines[Cnt].Length - 1)
                {
                    try
                    {
                        Headers.Add(Lines[Cnt].Substring(0, Ret), Lines[Cnt].Substring(Ret + 1).Trim());
                    }
                    catch { }
                }
            }

            if (Method.ToUpper().Equals("POST"))
            {
                Body = Query.Substring(Query.IndexOf("\r\n\r\n") + 4);
            }

            return this;
        }

        public static HttpHeader Parse(string Query)
        {
            HttpHeader httpHeader = new HttpHeader();
            return httpHeader.Convert(Query);
        }

    }
}
