using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Cadenza.Net {

	public class WebDavPropertyFindMethod : WebDavMethod {

		public XDocument Response {get; private set;}
		public Uri Uri {get; private set;}

		IDictionary<string, string> requestHeaders;
		public override IDictionary<string, string>  RequestHeaders {
			get {return requestHeaders;}
		}

		internal WebDavPropertyFindMethod (Uri uri, Stream content, int depth)
			: base (content)
		{
			Uri             = uri;
			requestHeaders  = new Dictionary<string, string> () {
				{ "Depth", depth == -1 ? "infinity" : depth.ToString () },
			};
		}

		protected override void OnResponse (Stream response)
		{
			Response = XDocument.Load (response);
		}

		public IEnumerable<WebDavResponse> GetResponses ()
		{
			return Response.Elements (WebDavNames.Multistatus)
				.Elements (WebDavNames.Response)
				.Select (r => new WebDavResponse (r));
		}
	}
}

