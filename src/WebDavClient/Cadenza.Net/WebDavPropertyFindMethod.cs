using System;
using System.Collections.Generic;
using System.IO;
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

		public IEnumerable<WebDavEntry> GetEntries ()
		{
			string path = Uri.LocalPath;
			foreach (var r in Response.Elements (WebDavNames.Multistatus).Elements (WebDavNames.Response)) {
				// Console.WriteLine ("WebDAV PROPFIND node: {0}", r);
				var href = r.Element (WebDavNames.Href);
				string filepath = Uri.UnescapeDataString (href.Value);
				if (filepath.StartsWith (path))
					filepath = filepath.Substring (path.Length);
				if (filepath.Length == 0)
					continue;
				var type = filepath.EndsWith ("/") ? WebDavEntryType.Directory : WebDavEntryType.File;
				int endDir = filepath.LastIndexOf ('/');
				if (type == WebDavEntryType.Directory)
					endDir = filepath.LastIndexOf ("/", endDir - 1);
				endDir++;
				yield return new WebDavEntry {
					Directory   = filepath.Substring (0, endDir),
					Name        = filepath.Substring (endDir),
					Path        = filepath,
					Type        = type,
				};
			}
		}
	}
}

