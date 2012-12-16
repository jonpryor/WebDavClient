using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Mono.Options;

using Cadenza.Net;

namespace Cadenza.Tools.WebDav {

	[AttributeUsage (AttributeTargets.Method, AllowMultiple=true)]
	class HelpAttribute : Attribute {

		public HelpAttribute (string helpText)
		{
			if (helpText == null)
				throw new ArgumentNullException ("helpText");
			HelpText = helpText;
		}

		public string HelpText {get; private set;}
	}

	class WebDavConnection
	{
		public WebDavMethodBuilder Builder {get; private set;}

		public WebDavConnection ()
		{
			Builder = new WebDavMethodBuilder ();
		}

		public static Action<WebDavConnection, string> GetCommand (string command)
		{
			Action<WebDavConnection, string> action;
			if (Commands.TryGetValue (command, out action))
				return action;
			return null;
		}

		static readonly Dictionary<string, Action<WebDavConnection, string>> Commands = new Dictionary<string, Action<WebDavConnection, string>>() {
			{ "help",     ShowHelp },
			{ "get",      Download },
			{ "exit",     Exit },
			{ "server",   Server },
			{ "ls",       ListPath },
			{ "verbose",  SetLogging },
		};

		[Help ("List all commands")]
		static void ShowHelp (WebDavConnection state, string args)
		{
			Console.WriteLine ("webdav commands--");
			foreach (var name in Commands.Keys.OrderBy (k => k)) {
				var d = Commands [name];
				var h = (HelpAttribute[]) d.Method.GetCustomAttributes (typeof (HelpAttribute), false);

				var prefix = "  " + name + ":";

				if (h == null || h.Length == 0) {
					Console.WriteLine (prefix);
					continue;
				}
				if (prefix.Length > 10) {
					Console.WriteLine (prefix);
					prefix = new string (' ', 10);
				} else {
					prefix = prefix.PadRight (10);
				}
				foreach (var a in h)
					foreach (var l in Mono.Options.StringCoda.WrappedLines (a.HelpText, 50, 48)) {
						Console.WriteLine ("{0}{1}", prefix, l);
						prefix = new string (' ', 12);
					}
			}
		}

		static void Exit (WebDavConnection state, string ignored)
		{
			Environment.Exit (0);
		}

		[Help ("SERVER")]
		[Help ("Open a connection to the specified WebDAV server.")]
		static void Server (WebDavConnection state, string server)
		{
			if (!string.IsNullOrEmpty (server))
				state.Builder.Server = new Uri (server);
			else
			    Console.WriteLine ("Server: {0}", state.Builder.Server);
		}

		[Help ("PATH")]
		[Help ("List files at PATH")]
		static void ListPath (WebDavConnection state, string args)
		{
			string[] v = Parse (args, 3);
			string p = v [0];
			int?   d = v [1] == null ? null : (int?) int.Parse (v [1]);
			string x = v [2];
			XElement r = null;
			if (x != null) {
				try {
					r = XElement.Parse (x);
				} catch (Exception e) {
					Console.Error.WriteLine ("Invalid XML in '{0}': {1}", x, e.Message);
					return;
				}
			}
			using (var t = state.Builder.CreateFileStatusMethodAsync (p, d, r)) {
				try {
					t.Wait ();
				} catch (Exception e) {
					Console.Error.WriteLine ("webdav: {0}", e);
					return;
				}
				if (t.IsFaulted) {
					Console.Error.WriteLine ("webdav: {0}", t.Exception.Flatten ());
					return;
				}
				foreach (var e in t.Result.GetResponses ()) {
					Console.WriteLine ("{0} {1,10} {2,-12} {3}",
							e.ResourceType == null ? " " : e.ResourceType == WebDavResourceType.Collection ? "d" : "-",
							e.ContentLength,
							e.CreationDate == null ? "" : e.CreationDate.Value.ToString ("MMM d HH:MM"),
							e.Href);
				}
			}
		}

		static string[] Parse (string source, int max)
		{
			string[] values = new string [max];
			int i = 0;
			foreach (string s in ArgumentSource.GetArguments (new StringReader (source))) {
				if (i < (values.Length-1))
					values [i++] = s;
				else
					values [i] = values [i] == null ? s : values [i] + " " + s;
			}
			return values;
		}

		[Help ("download contents: get SOURCE [target-file]")]
		static void Download (WebDavConnection state, string args)
		{
			string[] v = Parse (args, 2);
			string s = v [0];
			string d = v [1];

			Stream dest = d != null ? File.OpenWrite (d) : Console.OpenStandardOutput ();
			try {
				using (var t = state.Builder.CreateDownloadMethodAsync (s, dest)) {
					try {
						t.Wait ();
					} catch (Exception e) {
						Console.Error.WriteLine ("webdav: {0}", e);
						return;
					}
					if (t.IsFaulted) {
						Console.Error.WriteLine ("webdav: {0}", t.Exception.Flatten ());
						return;
					}
					Console.WriteLine ("StatusCode: {0}", t.Result.ResponseStatusCode);
				}
			} finally {
				if (d != null)
					dest.Close ();
			}
		}

		[Help ("Enable verbose logging")]
		static void SetLogging (WebDavConnection state, string args)
		{
			if (string.IsNullOrEmpty (args)) {
				Console.WriteLine (state.Builder.Log != null);
				return;
			}
			if (args == "1" || args == "true")
				state.Builder.Log = Console.Out;
			else
				state.Builder.Log = null;
		}
	}
}

