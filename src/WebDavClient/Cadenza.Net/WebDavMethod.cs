using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Cadenza.Net {

	public abstract class WebDavMethod {

		internal WebRequest Request;
		internal Stream content;

		protected WebDavMethod (Stream content)
		{
			this.content = content;
		}

		internal Task UploadContentAsync ()
		{
			if (content == null)
				return Task.Factory.StartNew (() => {});

			Request.ContentLength   = content.Length;
			Request.ContentType     = "text/xml";

			return Task.Factory.FromAsync (Request.BeginGetRequestStream, UploadContent, null);
		}

		private void UploadContent (IAsyncResult result)
		{
			using (Stream response = Request.EndGetRequestStream (result))
				content.CopyTo (response);
			content.Close ();
		}

		internal Task GetResponseAsync ()
		{
			return Task.Factory.FromAsync (Request.BeginGetResponse, GetResponse, null);
		}

		private void GetResponse (IAsyncResult result)
		{
			using (var response = Request.EndGetResponse (result))
				using (var stream = response.GetResponseStream())
					OnResponse (stream);
		}

		protected abstract void OnResponse (Stream response);
	}
}

