using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Cadenza.Net {

	public class WebDavPropertyFindMethod : WebDavMethod {

		public XDocument Response {get; private set;}
		public Uri Uri {get; private set;}

		internal WebDavPropertyFindMethod (Uri uri, Stream content)
			: base (content)
		{
			Uri = uri;
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

