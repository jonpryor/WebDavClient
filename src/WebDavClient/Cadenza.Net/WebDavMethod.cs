using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Cadenza.Net {

	public abstract class WebDavMethod {

		internal WebRequest Request;
		internal Stream content;
		internal WebDavMethodBuilder Builder;

		public virtual IDictionary<string, string>  RequestHeaders {
			get {
				return null;
			}
		}

		public WebHeaderCollection                  ResponseHeaders             {get; private set;}
		public long                                 ResponseContentLength       {get; private set;}
		public string                               ResponseContentType         {get; private set;}
		public HttpStatusCode                       ResponseStatusCode          {get; private set;}
		public string                               ResponseStatusDescription   {get; private set;}

		private string contentType;

		protected WebDavMethod (Stream content = null, string contentType = null)
		{
			this.content = content;
			this.contentType = contentType;
		}

		internal Task UploadContentAsync ()
		{
			if (content == null)
				return Task.Factory.StartNew (() => {});

			if (content.CanSeek)
				Request.ContentLength   = content.Length;
			if (contentType != null)
				Request.ContentType     = contentType;

			if (Builder.Log != null) {
				foreach (string key in Request.Headers.Keys)
					Builder.Log.WriteLine ("{0}: {1}", key, Request.Headers [key]);
			}
			
			return Task.Factory.FromAsync (Request.BeginGetRequestStream, UploadContent, null);
		}

		private void UploadContent (IAsyncResult result)
		{
			if (Builder.Log != null) {
				Builder.Log.WriteLine ();
				var r = new StreamReader (content);
				Builder.Log.WriteLine (r.ReadToEnd ());
				content.Position = 0;
			}

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
			using (var response = (HttpWebResponse) Request.EndGetResponse (result)) {
				ResponseHeaders = new WebHeaderCollection () {
					response.Headers,
				};
				ResponseContentLength       = response.ContentLength;
				ResponseContentType         = response.ContentType;
				ResponseStatusCode          = response.StatusCode;
				ResponseStatusDescription   = response.StatusDescription;
				using (var stream = response.GetResponseStream()) {
					var t = stream;
					if (Builder.Log != null) {
						foreach (var key in ResponseHeaders.AllKeys)
							Console.WriteLine ("{0}: {1}", key, ResponseHeaders [key]);
						Console.WriteLine ();
						t = new MemoryStream ();
						stream.CopyTo (t);
						t.Position = 0;
						Builder.Log.WriteLine (new StreamReader (t).ReadToEnd ());
						t.Position = 0;
					}
					OnResponse (t);
				}
			}
		}

		protected abstract void OnResponse (Stream response);
	}
}

