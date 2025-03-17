using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace chatClient.AiHandler
{
    public class AiHttpClientHandler : HttpClientHandler
    {
        private readonly string _uri;
        public AiHttpClientHandler(string uri)
        {
            _uri = uri;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            UriBuilder uriBuilder;
            Uri uri = new Uri(_uri);
            string host = uri.Host;

            uriBuilder = new UriBuilder(request.RequestUri)
            {
                // 这里是你要修改的 URL
                Scheme = uri.Scheme,
                Host = host,
                Path = uri.LocalPath,
            };
            request.RequestUri = uriBuilder.Uri;

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            return response;
        }

    }
}