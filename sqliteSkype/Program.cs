using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Data.Sqlite;

namespace sqliteSkype
{
	class Author 
	{
		public string name;
		public int total_msg_count = 0;
		int[] month_msg_count = new int[12 * 10];
		int[] day_msg_count = new int[365 * 10];
		int day = 0;
		int month = 0;
		DateTime first_time;
		DateTime last_time;

		public Author (string nm, DateTime dt)
		{
			first_time = dt;
			name = nm;
		}

		public void AddMsg(DateTime dt)
		{
			total_msg_count++;
			if (dt.DayOfYear != last_time.DayOfYear)
				day++; //wrong
			day_msg_count[day]++;
			if (dt.Month != last_time.Month)
				month++; //also wrong
			month_msg_count [month]++;
			last_time = dt;
		}
	}

	class Party  //this class used to collect statistics on the chat
	{
		List<Author> list = new List<Author>();
		public string party_name;

		public Party(string chatname)
		{
			party_name = chatname;
		}

		public void TakeMsg(string author_name, DateTime dt) {
			var instance = (from author in list where author.name == author_name select author).SingleOrDefault();
			if (instance == null) {
				list.Add (new Author (author_name, dt));
				instance = (from author in list where author.name == author_name select author).SingleOrDefault();
			} 
			instance.AddMsg (dt);
		}

		public List<string> GetAuthorNamesByMsgCount ()
		{
			List<Author> SortedList = list.OrderByDescending(o=>o.total_msg_count).ToList();
			List<String> ret = new List<string>();
			foreach (var author in SortedList)
				ret.Add(author.name + " " + author.total_msg_count);
			return ret;
		}
	}

	class ChatManager 
	{
		StreamWriter writer;
		Party party;
		DateTime prevdt;
		public string chatname;
		string curfile; 
		int messages = 0;

		public ChatManager (DateTime dt, string chat)
		{
			chatname = chat;
			prevdt = dt;
			party = new Party (chatname);
			System.IO.Directory.CreateDirectory (chatname);
			curfile = chatname + System.IO.Path.DirectorySeparatorChar + dt.ToString ("d") + ".html";
			writer = new StreamWriter (new FileStream (curfile, FileMode.Create));
			writer.Write ("<!DOCTYPE html>\n<html><head><meta charset=\"UTF-8\"><style>\n" +
			"td  {\n    font-family: monospace;\n    font-size: 100%;\n    vertical-align: top;\n    padding-bottom: 7px;\n}\n" +
			"#nik {color:#3498db;width:15%;text-align:right;}\n#time {color:#95a5a6;width:3%}\n#msg {width:82%;padding-left:15px}\n" +
			"#qhead {font-size: 200%;width:3%;color:#95a5a6;font-family:cursive;border-right:solid;border-right-width:1px;}\n" +
			"#quote {padding-bottom:0px;padding-left:5px}\n#qname {color:#95a5a6;padding-bottom:0px;padding-left:5px}\n" + 
			"</style></head>\n<body>\n<div style=\"background-color:gray;color:white;padding:1px 10px;width:100%;font-family:monospace;position:absolute;top:0;left:0;\">\n" +
			"  <h2>" + chatname + "</h2>\n  <p>" + dt.ToString ("d") + "</p>\n</div> \n<br><br><br><br><br>\n<div style=\"width: 65%;margin-left: auto;margin-right: auto;\">" +
			"<table valign=\"top\" style=\"width:100%;\">\n");
			writer.Flush ();
		}

		private void StartNextDay (DateTime dt)
		{
			if (messages > 0) {
				writer.Write ("</table>\n</div>\n</body>\n</html>\n");
				writer.Flush ();
				writer.Close ();
			} else {
				writer.Close ();
				File.Delete (curfile); //delete file without messages
			}
			curfile = chatname + System.IO.Path.DirectorySeparatorChar + dt.ToString ("d") + ".html";
			writer = new StreamWriter (new FileStream (curfile, FileMode.Create));
			writer.Write ("<!DOCTYPE html>\n<html><head><meta charset=\"UTF-8\"><style>\n" +
				"td  {\n    font-family: monospace;\n    font-size: 100%;\n    vertical-align: top;\n    padding-bottom: 7px;\n}\n" +
				"#nik {color:#3498db;width:15%;text-align:right;}\n#time {color:#95a5a6;width:3%}\n#msg {width:82%;padding-left:15px}\n" +
				"#qhead {font-size: 200%;width:3%;color:#95a5a6;font-family:cursive;border-right:solid;border-right-width:1px;}\n" +
				"#quote {padding-bottom:0px;padding-left:5px}\n#qname {color:#95a5a6;padding-bottom:0px;padding-left:5px}\n" + 
				"</style></head>\n<body>\n<div style=\"background-color:gray;color:white;padding:1px 10px;width:100%;font-family:monospace;position:absolute;top:0;left:0;\">\n" +
				"  <h2>" + chatname + "</h2>\n  <p>" + dt.ToString ("d") + "</p>\n</div> \n<br><br><br><br><br>\n<div style=\"width: 65%;margin-left: auto;margin-right: auto;\">" +
				"<table valign=\"top\" style=\"width:100%;\">\n");
			writer.Flush ();
		}

		private string MakeNiceQ (string body)
		{
			string[] authors = body.Split (new string[] {"author=\""}, StringSplitOptions.None);
			string[] dispnames = body.Split (new string[] {"authorname=\""}, StringSplitOptions.None);
			string[] timestamps = body.Split (new string[] {"timestamp=\""}, StringSplitOptions.None);
			string[] qbodys = body.Split (new string[] {"</legacyquote>"}, StringSplitOptions.None);
			List<string> quotes = new List<string> ();
			for (int i = 1; i < authors.Length; i++) {
				var author = authors[i].Substring(0, authors [i].IndexOf('"'));
				var dispname = dispnames[i].Substring(0, dispnames [i].IndexOf('"'));
				var dt = MainClass.TimeStampToDateTime(long.Parse(timestamps[i].Substring(0, timestamps [i].IndexOf('"'))));
				var qbody = qbodys [i * 2 - 1].Substring (0, qbodys [i * 2 - 1].IndexOf ("<legacyquote>"));
				quotes.Add("<table style=\"width:80%\"><tr><td rowspan=\"2\" id=\"qhead\">“</td><td id=\"quote\">" + qbody + 
					"</td></tr><tr><td id=\"qname\" title=\"" + author + " " +  dt.ToString("d") + "\"> — " + dispname + " " + dt.ToString("t") + "</td></tr></table>\n");
			}

			string ret = "";
			foreach (var q in quotes)
				ret += q;
			ret += body.Substring (body.LastIndexOf ("</quote>") + 8); //8 is lenght of "</quote>"
			return ret;
		}

		public void AddMsg (DateTime dt, string author, string dispname, string body)
		{
			if (body == "")
				return;
			if (dt.DayOfYear != prevdt.DayOfYear) {
				StartNextDay (dt);
				prevdt = dt;
			}
			if(body.Contains("</quote>"))
				body = MakeNiceQ(body);
			writer.Write ("  <tr>\n    <td id=\"nik\" title=\"" + author + "\">" + dispname + "</td>\n    <td id=\"msg\">" + body + "</td>\n    <td id=\"time\" title=\"" +
				dt.ToString("d") + "\">" + dt.ToString ("t") + "</td>\n  </tr>\n");
			writer.Flush ();
			messages++;
			party.TakeMsg (author, dt);
		}

		public void Close ()
		{
			if (messages > 0) {
				writer.Write ("</table>\n</div>\n</body>\n</html>\n мосткус пидор");
				writer.Flush ();
				writer.Close ();
			} else {
				writer.Close ();
				File.Delete (curfile); //delete file without messages
			}
		}

		public void PrintSats ()
		{
			List<string> stat = party.GetAuthorNamesByMsgCount ();
			File.WriteAllLines (chatname + System.IO.Path.DirectorySeparatorChar + "stat.txt", stat);
		}
	}

	class MainClass
	{
		private static DateTime TimeStampToDateTime (object unix)
		{
			return TimeStampToDateTime (long.Parse (unix.ToString ()));
		}

		public static DateTime TimeStampToDateTime (long unix)
		{
			var dt = new DateTime (1970, 1, 1, 10, 0, 0, 0, System.DateTimeKind.Local); //ten to vladivostok time
			long day = 24 * 60 * 60;
			dt = dt.AddDays (unix / day);    
			dt = dt.AddSeconds (unix % day); //AddSeconds dont change the days counter, at least in mono
			return dt;
		}

		public static void SubMain (string maindb)
		{
			IDbConnection connection = (IDbConnection)new SqliteConnection ("Data Source=" + maindb + ";Version=3;");
			connection.Open ();
			var command = (IDbCommand)new SqliteCommand ("SELECT chatname, author, timestamp, from_dispname, body_xml, type FROM Messages ORDER BY timestamp", (SqliteConnection)connection);
			var reader = command.ExecuteReader ();

			List<ChatManager> manager = new List<ChatManager>();
			while (reader.Read ()) {
				var dt = TimeStampToDateTime (reader ["timestamp"]);
				var chat_instance = (from chat in manager where chat.chatname == reader ["chatname"].ToString () select chat).SingleOrDefault();
				if (chat_instance == null) {
					manager.Add (new ChatManager (dt, reader ["chatname"].ToString ()));
					chat_instance = (from chat in manager where chat.chatname == reader ["chatname"].ToString () select chat).SingleOrDefault ();
				} 
				chat_instance.AddMsg (dt, reader ["author"].ToString (), reader ["from_dispname"].ToString (), reader ["body_xml"].ToString ());
			}
			foreach (var chat in manager) {
				chat.Close ();
				chat.PrintSats ();
			}
			connection.Close ();
		}

		public static void Main (string[] args)
		{
			if (File.Exists ("main.db"))
				SubMain ("main.db");
			else {
				string[] maindb = Directory.GetFiles (System.Environment.GetFolderPath (Environment.SpecialFolder.UserProfile) + "/.Skype", "main.db", SearchOption.AllDirectories);
				foreach (var db in maindb)
					SubMain (db);
			}
		}
	}
}
