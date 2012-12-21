using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Cadenza.Net {

	public class WebDavResponse {

		public WebDavResponse (XElement response)
		{
			if (response == null)
				throw new ArgumentNullException ("response");

			Element = response;

			Href    = response.GetElementPathValue (WebDavNames.Href);
			Hrefs   = response.Elements (WebDavNames.Href)
				.Select (h => h.Value)
				.Skip (1);
			if (Hrefs.Any ()) {
				Status              = response.GetElementPathValue (WebDavNames.Status);
				PropertyStatuses    = new WebDavPropertyStatus [0];
			}
			else {
				PropertyStatuses = response.Elements (WebDavNames.Propstat)
					.Select (propstat => new WebDavPropertyStatus (propstat));
			}

			Error = response.GetElementPath (WebDavNames.Error);
			if (Error != null)
				Error = Error.Elements ().FirstOrDefault ();

			ResponseDescription = response.GetElementPathValue (WebDavNames.ResponseDescription);
			Location            = response.GetElementPathValue (WebDavNames.Location, WebDavNames.Href);
		}

		public XElement                             Element             {get; private set;}
		public string                               Href                {get; private set;}
		public IEnumerable<string>                  Hrefs               {get; private set;}
		public string                               Status              {get; private set;}
		public HttpStatusCode?                      StatusCode          {get {return GetStatusCode (Status);}}
		public IEnumerable<WebDavPropertyStatus>    PropertyStatuses    {get; private set;}
		public XElement                             Error               {get; private set;}
		public string                               ResponseDescription {get; private set;}
		public string                               Location            {get; private set;}

		static readonly Regex ExtractStatusCode = new Regex (@"^HTTP/[^ ]+ (\d+) ");

		internal static HttpStatusCode? GetStatusCode (string status)
		{
			var m = ExtractStatusCode.Match (status);
			if (!m.Success)
				return null;
			return (HttpStatusCode) int.Parse (m.Groups [1].Value);
		}

		// Optional helpers
		public DateTime? CreationDate {
			get {
				var date = GetProps (WebDavNames.CreationDate).FirstOrDefault ();
				if (date == null || string.IsNullOrEmpty (date.Value))
					return null;
				return DateTime.Parse (date.Value);
			}
		}

		IEnumerable<XElement> GetProps (XName name)
		{
			return PropertyStatuses.Where (ps => ps.StatusCode == HttpStatusCode.OK)
				.SelectMany (ps => ps.Properties.Where (p => p.Name == name));
		}

		public WebDavResourceType? ResourceType {
			get {
				int c = 0;
				foreach (var rt in GetProps (WebDavNames.ResourceType)) {
					c++;
					if (rt.Element (WebDavNames.Collection) != null)
						return WebDavResourceType.Collection;
				}
				if (c == 0)
					return null;
				return WebDavResourceType.Default;
			}
		}

		public long? ContentLength {
			get {
				var cl = GetProps (WebDavNames.GetContentLength).FirstOrDefault ();
				if (cl == null || string.IsNullOrEmpty (cl.Value))
					return null;
				return long.Parse (cl.Value);
			}
		}
	}
}

