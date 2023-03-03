// Area of Interest Cleaner command line utility.
namespace SeverityCleaner
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Threading;

	using ClearScada.Client;
	using CommandLine;

	public static class Program
	{
		private const string CopyrightMessage = "Copyright (c) 2022 Schneider Electric. All Rights Reserved.\n" +
										"See the license terms and ReadMe files in the source code folder.\n" +
										"For further information please search\n" +
										"https://community.exchange.se.com/t5/Geo-SCADA-Knowledge-Base/Resource-Center-Home/ba-p/279133\n" +
										"\n" +
										"This program includes the library 'Command Line Parser'\n" +
										"To view its copyright notice please use command argument -l";

		private const string ClpCopyrightMessage = "*******************************************************************************\n" +
										"Copyright message for Command Line Parser library software only:\n" +
										"Copyright (c) 2005 - 2012 Giacomo Stelluti Scala\n" +
										"http://commandline.codeplex.com\n" +
										"\n" +
										"Permission is hereby granted, free of charge, to any person obtaining a copy of\n" +
										"this software and associated documentation files (the \"Software\"), to deal in\n" +
										"the Software without restriction, including without limitation the rights to\n" +
										"use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies\n" +
										"of the Software, and to permit persons to whom the Software is furnished to do\n" +
										"so, subject to the following conditions:\n" +
										"The above copyright notice and this permission notice shall be included in all\n" +
										"copies or substantial portions of the Software.\n" +
										"THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\n" +
										"IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\n" +
										"FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE\n" +
										"AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER\n" +
										"LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,\n" +
										"OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE\n" +
										"SOFTWARE.\n" +
										"*******************************************************************************";

		public static void Main(string[] args)
		{
			Console.WriteLine("** Geo SCADA Alarm Severity Cleaner Program **");
			Console.WriteLine("Use argument -? or --help for options.");

			Parser.Default.ParseArguments<Options>(args)
				.WithParsed(RunOptions)
				.WithNotParsed(HandleParseError);
		}

		private static void RunOptions(Options options)
		{
			if (options.Help)
			{
				Console.WriteLine(Options.GetUsage);
				Console.WriteLine(CopyrightMessage);
				Environment.Exit(0);
			}

			if (options.CommandLineHelp)
			{
				Console.WriteLine(ClpCopyrightMessage);
				Environment.Exit(0);
			}

			// consume Options type properties
			if (options.Verbose)
			{
				Console.WriteLine("Verbose mode selected.");
			}

			Process(options);

			if (options.Wait)
			{
				Console.WriteLine("Press a key to continue:");
				Console.ReadKey(true);
			}
		}

		private static void HandleParseError(IEnumerable<Error> errs)
		{
			Console.WriteLine(Options.GetUsage);
			Console.WriteLine("Unrecognised command line parameters.");
		}

		// The principal function which gets and loops through AoI
		private static void Process(Options options)
		{
			var username = options.UserName;
			if (string.IsNullOrEmpty(username))
			{
				// If run with no username, it is likely that it was run from Explorer
				// So force Wait option so that results are displayed
				options.Wait = true;

				// Also, if change option is set, verify that user wishes to proceed
				if (options.Change && !WarnUserBeforeProceeding())
				{
					Environment.Exit(0);
				}

				username = GetUsername();
			}

			var password = options.Password;
			if (string.IsNullOrEmpty(password))
			{
				password = GetPassword();
			}

			using (var connection = new ClearScada.Client.Simple.Connection("SeverityCleaner"))
			{
				// Replacing this version-specific connection code:
				// EDIT YOUR CONNECTION SETTINGS ACCORDING TO GEO SCADA VERSION
				//var node = new ServerNode(ConnectionType.Standard, options.NodeName, options.Port);	// Up to v83
				//var node = new ServerNode(options.NodeName, options.Port);								// From v84 onwards
				//ClearScada.Client.Advanced.IServer AdvConnection; // Used for query interface
				//try
				//{
				//	connection.Connect(node);
				//	//AdvConnection = node.Connect("SeverityCleaner");									// Up to v80
				//	//AdvConnection = node.Connect("SeverityCleaner", false);							// From v81 to v84
				//	var conSettings = new ClientConnectionSettings();									// From v85 onwards
				//	conSettings.IsLimited = false;														// From v85 onwards
				//	conSettings.IsVirtualized = false;													// From v85 onwards
				//	AdvConnection = node.Connect("SeverityCleaner", conSettings);                       // From v85 onwards
				//}
				// With this: version-independent code
#pragma warning disable 612, 618
				ClearScada.Client.Advanced.IServer AdvConnection;
				try
				{
					var node = new ServerNode(ConnectionType.Standard, "127.0.0.1", 5481);
					AdvConnection = node.Connect("SeverityCleaner");
				}
#pragma warning restore 612, 618
				catch (CommunicationsException)
				{
					Console.WriteLine("Unable to communicate with Geo SCADA server.");
					return;
				}

				if (!connection.IsConnected)
				{
					Console.WriteLine("Failed Connection to Geo SCADA.");
				}

				if (options.Verbose)
				{
					Console.WriteLine("Connected to database.");
				}

				using (var spassword = new System.Security.SecureString())
				{
					foreach (var c in password)
					{
						spassword.AppendChar(c);
					}

					try
					{
						connection.LogOn(username, spassword);
						AdvConnection.LogOn(username, spassword);
					}
					catch (AccessDeniedException)
					{
						Console.WriteLine("Access denied, please check username and password. Check CAPS LOCK is off.");
						return;
					}
					catch (PasswordExpiredException)
					{
						Console.WriteLine("Password is expired. Please reset with ViewX or WebX.");
						return;
					}

					if (options.Verbose)
					{
						Console.WriteLine("Logged In.");
					}
				}

				// Timers
				var start = DateTime.Now;

				if (options.Verbose)
				{
					Console.WriteLine("Reading Severities.");
				}
				var severities = QueryDatabaseForSeverities(AdvConnection, options);
				// We add a zero severity because that is not an error
				if (!severities.ContainsKey(0))
				{
					severities.Add(0, "None");
				}

				// Check mapping arguments
				// Option to remap requires both From and To parameters, fall back to do unmapped if either 
				if ((options.RemapFrom == 0 || options.RemapTo == 0) && (options.RemapFrom != 0 || options.RemapTo != 0))
				{
					Console.WriteLine("Only one of -from or -to parameters entered, need both to remap severities.");
					return;
				}
				if (options.RemapFrom > 0 && options.RemapTo > 0)
				{
					if (!severities.ContainsKey(options.RemapTo))
					{
						Console.WriteLine($"Cannot change severity to {options.RemapTo} as it is not configured.");
						return;
					}
				}


				// Read Schema to find alarm severity information
				// By table - get the name and a boolean indicating this is an Aggregate table
				var severityTables = QueryDatabaseForSeverityTables(AdvConnection);
				Console.WriteLine("Total database tables: " + severityTables.Count.ToString());

				// By field - get the field name (table.field) and table name
				var severityFields = QueryDatabaseForSeverityFields(AdvConnection, options);
				Console.WriteLine("Total database fields: " + severityFields.Count.ToString());

				// Eliminate tables with no rows
				var trimmedTables = new Dictionary<string, bool>();
				foreach (var tablename in severityTables.Keys)
				{
					// Check there are severity fields in this table by reading the field list
					foreach( var thistablename in severityFields.Values)
					{
						if (thistablename == tablename)
						{
							// At least one field in this table,
							// Now check if a table has rows then add to our trimmed table list
							if (CountRowsInTable(AdvConnection, tablename) != 0)
							{
								if (options.Verbose)
								{
									Console.WriteLine("Table with content: " + tablename);
								}
								trimmedTables.Add(tablename, severityTables[tablename]);
							}
							break;
						}
					}
				}
				Console.WriteLine($"Tables with severity content: {trimmedTables.Count}");

				// Eliminate fields in tables with no rows
				var trimmedFields = new Dictionary<string, string>();
				foreach (var fieldname in severityFields.Keys)
				{
					foreach (var tablename in trimmedTables.Keys)
					{
						if (fieldname.StartsWith(tablename + "."))
						{
							if (options.Verbose)
							{
								Console.WriteLine("Field in table with severity content: " + fieldname);
							}
							trimmedFields.Add(fieldname, severityFields[fieldname]);
						}
					}
				}
				Console.WriteLine($"Fields in tables with content: {trimmedFields.Count}");

				// Read all severity values into a List of Entries with Table, Row, and list of Field, value and Aggregate field name vs Severity
				Console.WriteLine("Reading database severity values...");
				var severityConfiguration = QueryDatabaseForSeverityColumnData(AdvConnection, trimmedTables, trimmedFields, options);
				Console.WriteLine($"Total database rows with severities: {severityConfiguration.Count}");

				// Remap Severities - the convention default is to step to the next lower severity
				// Some may be template-controlled, so multiple objects may be dependent on a setting, only set these once

				if (options.Change)
				{
					Console.WriteLine("Modifying severities...");
				}
				else
				{
					Console.WriteLine("Checking for writable severities...");
				}

				int remapCount;
				if (options.RemapFrom > 0 && options.RemapTo > 0)
				{
					Console.WriteLine($"Change severity from {options.RemapFrom} to {options.RemapTo}.");
					remapCount = RemapSeverities(AdvConnection, severityConfiguration, severities, options);
				}
				else
				{
					// Look for invalid severities
					var unmappedSeverities = SearchConfigurationForUnmapped(AdvConnection, severityConfiguration, severities, options);
					Console.WriteLine($"Total unmapped severities: {unmappedSeverities.Count}");

					Console.WriteLine("Finding unmapped severities");
					remapCount = RemapSeverities(AdvConnection, unmappedSeverities, severities, options);
				}
				Console.WriteLine($"Count of Modified severities: {remapCount}");

				var end = DateTime.Now;
				Console.WriteLine("Total Duration: " + end.Subtract(start).TotalSeconds.ToString(CultureInfo.CurrentCulture) + " seconds.");

				Console.WriteLine("Complete.");
			}
		}

		// Read the Severities in the database
		private static Dictionary<int, string> QueryDatabaseForSeverities(ClearScada.Client.Advanced.IServer AdvConnection, Options options)
		{
			var Severities = new Dictionary<int, string>();

			string sql = "SELECT Priority, Description FROM CSeverity";
			ClearScada.Client.Advanced.IQuery serverQuery = AdvConnection.PrepareQuery(sql, new ClearScada.Client.Advanced.QueryParseParameters());
			ClearScada.Client.Advanced.QueryResult queryResult = serverQuery.ExecuteSync(new ClearScada.Client.Advanced.QueryExecuteParameters());

			if (queryResult.Status == ClearScada.Client.Advanced.QueryStatus.NoDataFound)
			{
				Console.WriteLine("No Severities found in the database.");
			}
			else
			if (queryResult.Status == ClearScada.Client.Advanced.QueryStatus.Succeeded)
			{
				if (queryResult.Rows.Count > 0)
				{
					// Found
					IEnumerator<ClearScada.Client.Advanced.QueryRow> e = queryResult.Rows.GetEnumerator();
					while (e.MoveNext())
					{
						Severities.Add((Int32)e.Current.Data[0], (string)e.Current.Data[1]);
						Console.WriteLine($"{((Int32)e.Current.Data[0])}, {(string)e.Current.Data[1]}");
					}
				}
			}
			serverQuery.Dispose();

			return Severities;
		}

		// Read the Severity Fields in the database, returns a list of table.field and parenttable names
		private static Dictionary<string, string> QueryDatabaseForSeverityFields(ClearScada.Client.Advanced.IServer AdvConnection, Options options)
		{
			var Fields = new Dictionary<string, string>();
			// Pick up redirection with % at end of Severity. Pick up alarm reprioritisation redir.
			string sql = "select FieldName, Table from dbfielddef where " +
				"( (FieldName like '%Severity%') OR (FieldName = 'CDBAlarmActionPriority.NewPriority') ) " + 
				" and Type in (1,12) and Size = 1 and IsWritable = True and StorageType = 0";
			ClearScada.Client.Advanced.IQuery serverQuery = AdvConnection.PrepareQuery(sql, new ClearScada.Client.Advanced.QueryParseParameters());
			ClearScada.Client.Advanced.QueryResult queryResult = serverQuery.ExecuteSync(new ClearScada.Client.Advanced.QueryExecuteParameters());

			if (queryResult.Status == ClearScada.Client.Advanced.QueryStatus.NoDataFound)
			{
				Console.WriteLine("No Severity fields found in the database.");
			}
			else
			if (queryResult.Status == ClearScada.Client.Advanced.QueryStatus.Succeeded)
			{
				if (queryResult.Rows.Count > 0)
				{
					// Found
					IEnumerator<ClearScada.Client.Advanced.QueryRow> e = queryResult.Rows.GetEnumerator();
					while (e.MoveNext())
					{
						Fields.Add((string)e.Current.Data[0], (string)e.Current.Data[1]);
						//if (options.Verbose)
						//{
						//	Console.WriteLine($"{((string)e.Current.Data[0])}, {(string)e.Current.Data[1]}");
						//}
					}
				}
			}
			serverQuery.Dispose();

			return Fields;
		}

		// Find tables list to determine if a table is a creatable class (has FullName property) or an aggregate (has AggrName property)
		private static Dictionary<string, bool> QueryDatabaseForSeverityTables(ClearScada.Client.Advanced.IServer AdvConnection)
		{
			var Tables = new Dictionary<string, bool>(); // Boolean indicates this is an aggregate table

			string sql = "select distinct Table,Name from dbfielddef where (Name = 'FullName' or Name = 'AggrName')";
			ClearScada.Client.Advanced.IQuery serverQuery = AdvConnection.PrepareQuery(sql, new ClearScada.Client.Advanced.QueryParseParameters());
			ClearScada.Client.Advanced.QueryResult queryResult = serverQuery.ExecuteSync(new ClearScada.Client.Advanced.QueryExecuteParameters());

			if (queryResult.Status == ClearScada.Client.Advanced.QueryStatus.NoDataFound)
			{
				Console.WriteLine("No tables found in the database.");
			}
			else
			if (queryResult.Status == ClearScada.Client.Advanced.QueryStatus.Succeeded)
			{
				if (queryResult.Rows.Count > 0)
				{
					// Found
					IEnumerator<ClearScada.Client.Advanced.QueryRow> e = queryResult.Rows.GetEnumerator();
					while (e.MoveNext())
					{
						Tables.Add((string)e.Current.Data[0], (string)e.Current.Data[1] == "AggrName");
						// Console.WriteLine(((string)e.Current.Data[0]) + ", " + (string)e.Current.Data[1]);
					}
				}
			}
			serverQuery.Dispose();

			return Tables;
		}

		// Count tables rows to help trim the data queries later
		private static int CountRowsInTable(ClearScada.Client.Advanced.IServer AdvConnection, string TableName)
		{
			int rows = 0;
			string sql = "select with templates count(0) from " + TableName;
			ClearScada.Client.Advanced.IQuery serverQuery = AdvConnection.PrepareQuery(sql, new ClearScada.Client.Advanced.QueryParseParameters());
			ClearScada.Client.Advanced.QueryResult queryResult = serverQuery.ExecuteSync(new ClearScada.Client.Advanced.QueryExecuteParameters());

			if (queryResult.Status == ClearScada.Client.Advanced.QueryStatus.Succeeded)
			{
				if (queryResult.Rows.Count > 0)
				{
					// Found some rows
					IEnumerator<ClearScada.Client.Advanced.QueryRow> e = queryResult.Rows.GetEnumerator();
					while (e.MoveNext())
					{
						rows = (int)e.Current.Data[0];
						break;
					}
				}
			}
			serverQuery.Dispose();

			return rows;
		}

		// Read all current objects and their severity configuration
		private static List<SeverityEntry> QueryDatabaseForSeverityColumnData(ClearScada.Client.Advanced.IServer AdvConnection,
			Dictionary<string, bool> Tables, Dictionary<string, string> Fields, Options options)
		{
			var SeverityData = new List<SeverityEntry>();

			foreach (var tablename in Tables.Keys)
			{
				if (options.Verbose)
				{
					Console.WriteLine("Read from Table: " + tablename);
				}
				// Get a list of fields
				string sql = "";
				foreach (var fieldname in Fields.Keys)
				{
					if (fieldname.StartsWith(tablename + "."))
					{
						sql += fieldname.Substring(tablename.Length + 1) + ",";
					}
				}
				// Remove trailing comma
				sql = sql.Substring(0, sql.Length - 1);
				// Build SQL
				// If aggregate class
				if (Tables[tablename])
				{
					sql = "Select With Templates Id, AggrName, " + sql + " From " + tablename;
				}
				else
				{
					sql = "Select With Templates Id, FullName, " + sql + " From " + tablename;
				}

				ClearScada.Client.Advanced.IQuery serverQuery = AdvConnection.PrepareQuery(sql, new ClearScada.Client.Advanced.QueryParseParameters());
				ClearScada.Client.Advanced.QueryResult queryResult = serverQuery.ExecuteSync(new ClearScada.Client.Advanced.QueryExecuteParameters());

				if (queryResult.Status == ClearScada.Client.Advanced.QueryStatus.Succeeded)
				{
					if (queryResult.Rows.Count > 0)
					{
						// Found database rows with severities
						IEnumerator<ClearScada.Client.Advanced.QueryRow> e = queryResult.Rows.GetEnumerator();
						while (e.MoveNext())
						{
							// Create an entry and set properties
							var entry = new SeverityEntry();
							entry.TableName = tablename;
							entry.RowNumber = (int)e.Current.Data[0];
							entry.FieldsValues = new Dictionary<String, int>();
							// If not aggregate class
							if (!Tables[tablename])
							{
								entry.FullName = (string)e.Current.Data[1];
							}
							//if (options.Verbose)
							//{
							//	Console.Write($"{tablename}, {((int)e.Current.Data[0])}");
							//}
							// Add field values
							int i = 2;
							foreach (var fieldname in Fields.Keys)
							{
								if (fieldname.StartsWith(tablename + "."))
								{
									// Field names we add here are (i) field name only if this is a creatable class, or
									// (ii) aggregate instance name _._ field name, if this is an aggregate table
									var thisfieldname = fieldname.Substring(tablename.Length + 1);
									// If aggregate class
									if (Tables[tablename])
									{
										var aggregateinstance = (string)e.Current.Data[1];
										thisfieldname = aggregateinstance + "." + thisfieldname;
									}
									entry.FieldsValues.Add(thisfieldname, (int)e.Current.Data[i]);
									//if (options.Verbose)
									//{
									//	Console.Write($", {thisfieldname} = {(int)e.Current.Data[i]}");
									//}
									i += 1;
								}
							}
							// Store in a large list
							SeverityData.Add(entry);
							//if (options.Verbose)
							//{
							//	Console.WriteLine("");
							//}
						}
					}
				}
				serverQuery.Dispose();
			}
			return SeverityData;
		}

		private static List<SeverityEntry> SearchConfigurationForUnmapped(ClearScada.Client.Advanced.IServer AdvConnection,
			List<SeverityEntry> currentConfig, Dictionary<int, string> severities, Options options)
		{
			var unmapped = new List<SeverityEntry>();
			foreach (var entry in currentConfig)
			{
				foreach (string fieldname in entry.FieldsValues.Keys)
				{
					bool found = false;
					foreach (int severity in severities.Keys)
					{
						if (severity == entry.FieldsValues[fieldname])
						{
							found = true;
							break;
						}
					}
					if (!found)
					{
						var unmappedEntry = new SeverityEntry();
						unmappedEntry.TableName = entry.TableName;
						unmappedEntry.RowNumber = entry.RowNumber;
						unmappedEntry.FullName = entry.FullName;
						unmappedEntry.FieldsValues = new Dictionary<string, int>();
						unmappedEntry.FieldsValues.Add(fieldname, entry.FieldsValues[fieldname]);
						unmapped.Add(unmappedEntry);
						if (options.Verbose)
						{
							// Find object name
							unmappedEntry.FullName = (string)AdvConnection.GetProperty(new ObjectId((int)entry.RowNumber), "FullName");
							Console.WriteLine($"Unmapped severity: {entry.TableName}, row={unmappedEntry.RowNumber}, {unmappedEntry.FullName}, {fieldname}, {entry.FieldsValues[fieldname]}");
						}
					}
				}
			}
			return unmapped;
		}

		// Remap Severities - the convention default is to step to the next lower severity
		// Some may be template-controlled, so multiple objects may be dependent on a setting, only set these once
		private static int RemapSeverities(ClearScada.Client.Advanced.IServer AdvConnection,
			List < SeverityEntry > currentConfig, Dictionary<int, string> severities, Options options)
		{
			int RemapCount = 0;
			int ErrorCount = 0;

			foreach (var entry in currentConfig)
			{
				foreach (string fieldname in entry.FieldsValues.Keys)
				{
					int foundseverity = 0;
					// If we are remapping invalid severities
					if (options.RemapFrom == 0 || options.RemapTo == 0)
					{
						// Compare against the list of severities in descending order
						foreach (int severity in severities.Keys.Reverse())
						{
							// Ignore the zero severity and look for the first which is smaller than the unmapped value
							if (severity != 0 && severity < entry.FieldsValues[fieldname])
							{
								foundseverity = severity;
								break;
							}
						}
					}
					else
					{
						// Remapping from old to new
						foundseverity = options.RemapTo;
						if (entry.FieldsValues[fieldname] != options.RemapFrom)
						{
							// Do not remap
							continue;
						}
					}
					// Find objects needing to be altered
					if (foundseverity > 0)
					{
						// Set severity

						// Find object name, if not already found
						if ((options.Verbose || !options.Quiet || !options.Change) && entry.FullName == "")
						{
							entry.FullName = (string)AdvConnection.GetProperty(new ObjectId((int)entry.RowNumber), "FullName");
						}

						// Check we still need to do this change, in case we changed a template before
						var severityValue = AdvConnection.GetProperty(new ObjectId((int)entry.RowNumber), fieldname);
						int currentSeverity;
						try
						{
							//currentSeverity = (int)severityValue;
							currentSeverity = int.Parse(severityValue.ToString());
						}
						catch
						{
							Console.WriteLine($"No change for {entry.RowNumber} '{entry.FullName}' Table {entry.TableName} Field {fieldname} " +
											  $"incorrect field value type {entry.FieldsValues[fieldname]}.");
							currentSeverity = 0;
							continue;
						}
						if (currentSeverity != entry.FieldsValues[fieldname])
						{
							if (options.Verbose)
							{
								Console.WriteLine($"No change for {entry.RowNumber} '{entry.FullName}' Table {entry.TableName} Field {fieldname} " +
												  $"was {entry.FieldsValues[fieldname]} " +
												  $"is now {foundseverity} ({severities[foundseverity]}).");
							}
							continue;
						}
						// What we are about to do
						if ( options.Verbose)
						{
							Console.WriteLine($"Change {entry.RowNumber} '{entry.FullName}' Table {entry.TableName} Field {fieldname} " +
											  $"from {entry.FieldsValues[fieldname]} " + 
											  $"to {foundseverity} ({severities[foundseverity]}).");

						}
						// Make the change
						if (AdvConnection.IsPropertyWritable(new ObjectId((int)entry.RowNumber), fieldname))
						{
							// Here would be a good time to wait a few mSec if reducing system loading
							if (options.TimeDelayMS > 0)
							{
								Thread.Sleep(options.TimeDelayMS); // This will block this thread, not the server
							}


							if (options.Change)
							{
								try
								{
									AdvConnection.SetProperty(new ObjectId((int)entry.RowNumber), fieldname, foundseverity);

									RemapCount++;
									entry.Changed = true;
									// Log changes if verbose or the quiet parameter is clear
									if (options.Verbose || !options.Quiet)
									{
										Console.WriteLine($"Changed {entry.RowNumber} '{entry.FullName}' Table {entry.TableName} Field {fieldname} " +
															$"from {entry.FieldsValues[fieldname]} " +
															$"to {foundseverity} ({severities[foundseverity]}).");
									}
								}
								catch (Exception ex)
								{
									Console.WriteLine($"*** Property write error for {entry.RowNumber} '{entry.FullName}' Table {entry.TableName} " +
													  $"Field {fieldname}, {ex.Message}.");
									ErrorCount++;
								}
							}
							else
							{
								// Output the intent if verbose mode, or not quiet mode, or not set to change
								if (options.Verbose || !options.Quiet || !options.Change)
								{
									Console.WriteLine($"Use -c option to change: {entry.RowNumber} '{entry.FullName}' Table {entry.TableName} Field {fieldname} " +
													$"from {entry.FieldsValues[fieldname]} " +
													$"to {foundseverity} ({severities[foundseverity]}).");
								}
							}
						}
						else
						{
							if (options.Verbose)
							{
								// Cannot write property - likely to be template override
								Console.WriteLine($"Property not writable for {entry.RowNumber} '{entry.FullName}' Table {entry.TableName} Field {fieldname}.");
							}
						}
					}
				}
			}
			if (ErrorCount > 0)
			{
				Console.WriteLine($"*** Error Count: {ErrorCount}");
			}
			return RemapCount;
		}

		// Get username interactively with no echo to screen
		private static string GetUsername()
		{
			Console.Write("Enter Geo SCADA User name: ");
			var username = Console.ReadLine();
			return username;
		}

		// Get password interactively with no echo to screen
		private static string GetPassword()
		{
			Console.Write("Enter Geo SCADA Password: ");
			var password = SE.App.Console.ReadPassword();
			Console.WriteLine(string.Empty);
			return password;
		}

		// Ask whether to proceed
		private static bool WarnUserBeforeProceeding()
		{
			Console.WriteLine("\nStarting Severity Cleaner with default options.\n" +
								"Please run from the Command Line to specify options.\n" +
								"THIS PROGRAM CAN MAKE CHANGES TO YOUR DATABASE.\n");
			Console.Write("Type Y and Enter to continue or press Enter to exit this utility: ");
			var confirm = Console.ReadLine();
			if (confirm.ToUpper(CultureInfo.InvariantCulture) == "Y")
			{
				return true;
			}

			return false;
		}
	}
	class SeverityEntry
	{
		public string TableName = "";
		public long RowNumber = 0;
		public Dictionary <String, int> FieldsValues;
		public bool Changed = false;
		public string FullName = "";
	}
}