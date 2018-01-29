using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;


namespace Cadenza.Net
{
    public class HttpRequestBuilder
    {
        public String User { get; set; }
        public String Password { get; set; }

        public bool ForceSSL { get; set; } = true;

        /// <summary>
        /// Cookies used for shiboleth or other "session based" auth
        /// </summary>
        public CookieContainer cookies { get; set; } = null;
        /// <summary>
        /// Bearer token is used for oauth and similar
        /// </summary>
        public String bearerToken { get; set; } = null;

        /// <summary>
        /// Set the proxy to use:
        /// - System: user system proxy
        /// - Direct: no proxy (direct connection)
        /// - other: use the url as proxy (i.e. http://proxy:6138 )
        /// only http proxy are supported
        /// </summary>
        public String proxy { get; set; } = "System";
        public String proxyUser { get; set; }
        public String proxyPassword { get; set; }

        /// <summary>
        /// This will create a web request object for a given uri with the builder parameter
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public HttpWebRequest Build(String uri)
        {
            return Build(new Uri(uri));
        }
        /// <summary>
        /// This will create a web request object for a given uri with the builder parameter
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public HttpWebRequest Build(Uri uri)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)System.Net.HttpWebRequest.Create(uri);

            // If you want to disable SSL certificate validation

            if (ForceSSL)
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback +=
                delegate (object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors sslError)
                {
                    return true;
                };
            }

            if (proxy == "Direct")
            {
                // no proxy
            }
            else if (proxy == "System" || String.IsNullOrEmpty(proxy))
            {
                // use system (IE) web proxy
                httpWebRequest.Proxy = WebRequest.GetSystemWebProxy();
            }
            else
            {
                // custom proxy
                WebProxy wproxy = new WebProxy(proxy, true);
                if (!String.IsNullOrEmpty(proxyUser))
                    wproxy.Credentials = new NetworkCredential(proxyUser, proxyPassword);
                httpWebRequest.Proxy = wproxy;
            }

            // The server may use authentication
            if(cookies != null)
            {
                httpWebRequest.CookieContainer = cookies;
            } else if(!String.IsNullOrEmpty(bearerToken))
            {
                httpWebRequest.Headers.Add("Authorization", "Bearer " + bearerToken);
            }
            else if (!String.IsNullOrEmpty(User) && !String.IsNullOrEmpty(Password))
            {
                NetworkCredential networkCredential;
                networkCredential = new NetworkCredential(User, Password);
                httpWebRequest.Credentials = networkCredential;
                // Send authentication along with first request.
                httpWebRequest.PreAuthenticate = true;
            }

            // our user agent
            httpWebRequest.UserAgent = "Mozilla/5.0 (epiK) mirall/2.4.0 (build 1234)";
            return httpWebRequest;
        }
    }
}
