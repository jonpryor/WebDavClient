using System;
using System.Collections.Generic;
using System.Linq;

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
			{ "exit",     Exit },
			{ "server",   Server },
			{ "ls",       ListPath },
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
		static void ListPath (WebDavConnection state, string path)
		{
			var t = string.IsNullOrEmpty (path)
				? state.Builder.CreatePropertyFindMethodAsync ()
				: state.Builder.CreatePropertyFindMethodAsync (path);
			if (t.IsFaulted) {
				Console.Error.WriteLine ("webdav: {0}", t.Exception.Flatten ());
				return;
			}
			Console.WriteLine ("Response: {0}", t.Result.Response);
			foreach (var e in t.Result.GetEntries ()) {
				Console.WriteLine ("Name={0} Directory={1} Path={2} Result={3}", e.Name, e.Directory, e.Path, e.Response);
			}
		}
	}
}

