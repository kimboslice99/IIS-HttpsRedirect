using System;
using System.Web;
using NetTools;
using System.Collections.Specialized;
using System.Net;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq;

namespace HttpsRedirect
{
    public class HttpsRedirect : IHttpModule
    {
        public void Dispose() { }
        public void Init(HttpApplication app)
        {
            app.BeginRequest += new EventHandler(Redirect);
        }

        public void Redirect(Object source, EventArgs e)
        {
            HttpApplication app = (HttpApplication)source;
            HttpRequest request = app.Request;
            HttpResponse response = app.Response;
            NameValueCollection variables = request.ServerVariables;

            IPAddress remote_addr = IPAddress.Parse(variables["REMOTE_ADDR"]);

            if (variables["HTTPS"] == "off" && !IsLocal(remote_addr))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[HttpsRedirect]: IP is not local URL={variables["HTTP_HOST"] + variables["REQUEST_URI"]}");
#endif
                response.Redirect($"https://{variables["HTTP_HOST"]}{variables["REQUEST_URI"]}");
            }
        }

        /// <summary>
        /// Determines if an ip address is local
        /// </summary>
        /// <param name="ip"></param>
        /// <returns>true if local</returns>
        internal bool IsLocal(IPAddress ip)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[HttpsRedirect]: parsing {ip.ToString()}");
#endif
            if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[HttpsRedirect]: IP is IPv4");
#endif
                var ranges = new List<string>
                {
                    "0.0.0.0/8",
                    "192.168.0.0/16",
                    "127.0.0.0/8",
                    "10.0.0.0/8",
                    "172.16.0.0/12",
                };
                return ranges.Select(range => IPAddressRange.Parse(range))
                             .Any(parsedRange => parsedRange != null && parsedRange.Contains(ip));
            }
            else if (ip.AddressFamily == AddressFamily.InterNetworkV6) // IPv6
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[HttpsRedirect]: IP is IPv6");
#endif
                // Check for link-local and site-local IPv6 addresses
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
                    return true;

                var ranges = new List<string>
                {
                    "::1/128",
                    "fc00::/7"
                };

                return ranges.Select(range => IPAddressRange.Parse(range))
                             .Any(parsedRange => parsedRange != null && parsedRange.Contains(ip));
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[HttpsRedirect]: IP was unable to be parsed");
#endif
            return false; // Not IPv4 or IPv6
        }
    }
}
