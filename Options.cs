namespace SeverityCleaner
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using CommandLine;

	public class Options
	{
		// Converted from a method with this decorator on recommendation from StyleCop. [HelpOption]
		public static string GetUsage
		{
			get
			{
				StringBuilder usage = new StringBuilder();
				usage.AppendLine("This program will remove unused alarm severities from the Geo SCADA database.\n");
				usage.AppendLine("To remap specific severities use the optional -from and -to parameters.\n");
				usage.AppendLine("Usage: SeverityCleaner -u MyUserName\n");
                usage.AppendLine("All parameters:");
				usage.AppendLine("-n or --node     <node>  Geo SCADA server node name/address (default localhost)");
				usage.AppendLine("-p or --port     <port>  Geo SCADA server port number (default 5481)");
				usage.AppendLine("-u or --user     <user>  Geo SCADA User name (omit to enter interactively)");
				usage.AppendLine("-a or --password <pass>  Geo SCADA Password (omit to enter interactively)");
				usage.AppendLine("-d or --delay    <delay> Delay milliseconds between server requests (default 1)");
				usage.AppendLine("-f or --from     <num>   Optional: look to change all with this severity");
				usage.AppendLine("-t or --to       <num>   Required with -f only: change found items to this severity");
				usage.AppendLine("Options:");
				usage.AppendLine("-c or --change           Make database changes.");
				usage.AppendLine("                 THE DATABASE WILL NOT BE CHANGED UNLESS THIS OPTION IS USED");
				usage.AppendLine("-q or --quiet            Silence the output of severity changes made.");
				usage.AppendLine("-v or --verbose          Output all details during execution.");
				usage.AppendLine("-w or --wait             Pause for a key press after completion.");
				usage.AppendLine("-? or --help             Print this text and copyright messages.");

				return usage.ToString();
			}
		}

		[Option('n', "node", Default = "localhost", HelpText = "Geo SCADA server node name or address (default localhost).")]
		public string NodeName { get; set; }

		[Option('p', "port", Default = 5481, HelpText = "Geo SCADA server port number (default 5481).")]
		public int Port { get; set; }

		[Option('u', "user", Default = "", HelpText = "Geo SCADA Username.")]
		public string UserName { get; set; }

		[Option('a', "password", Default = "", HelpText = "Geo SCADA Password (omit this to enter interactively).")]
		public string Password { get; set; }

		[Option('d', "delay", Default = 1, HelpText = "Delay milliseconds between each server request (default 1).")]
		public int TimeDelayMS { get; set; }

		[Option('f', "from", Default = 0, HelpText = "Remap from this severity (default unmapped).")]
		public int RemapFrom { get; set; }

		[Option('t', "to", Default = 0, HelpText = "Remap to this severity. Use with -f (default unmapped).")]
		public int RemapTo { get; set; }

		[Option('c', "change", Default = false, HelpText = "Make database changes.")]
		public bool Change { get; set; }

		[Option('q', "quiet", Default = false, HelpText = "Do not display details of severities changed.")]
		public bool Quiet { get; set; }

		[Option('v', "verbose", Default = false, HelpText = "Display details during execution.")]
		public bool Verbose { get; set; }

		[Option('w', "wait", Default = false, HelpText = "Pause for a key press after completion.")]
		public bool Wait { get; set; }

		[Option('?', "help", Default = false, HelpText = "Display help and copyright.")]
		public bool Help { get; set; }

		[Option('l', "clp", Default = false, HelpText = "Display CLP copyright.")]
		public bool CommandLineHelp { get; set; }
	}
}
