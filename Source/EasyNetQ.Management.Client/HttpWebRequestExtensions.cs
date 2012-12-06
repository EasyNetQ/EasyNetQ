using System.Net;

namespace EasyNetQ.Management.Client
{
    public static class HttpWebRequestExtensions
    {
        public static HttpWebResponse GetHttpResponse(this HttpWebRequest request)
        {
            HttpWebResponse response = null;

            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException exception)
            {
                if (exception.Status == WebExceptionStatus.ProtocolError)
                {
                    response = (HttpWebResponse) exception.Response;
                }
                else
                {
                    throw;
                }
            }

            return response;
        }
    }
}