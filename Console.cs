namespace SE.App
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// http://stackoverflow.com/questions/3404421/password-masking-console-application
	/// Adds some nice help to the console. 
	/// </summary>
	public static class Console
	{
		/// <summary>
		/// Like System.Console.ReadLine(), only with a mask.
		/// </summary>
		/// <param name="mask">a <c>char</c> representing your choice of console mask</param>
		/// <returns>the string the user typed in </returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.Write(System.String)", Justification = "Non localised project")]
		public static string ReadPassword(char mask)
		{
			const char Enter = (char)13, Backsp = (char)8, Ctrlbacksp = (char)127;
			int[] filtered = { 0, 27, 9, 10 /*, 32 space, allowed in passwords */ };

			var pass = new Stack<char>();
			char chr;

			while ((chr = System.Console.ReadKey(true).KeyChar) != Enter)
			{
				switch (chr)
				{
				case Backsp:
					if (pass.Count > 0)
					{
						System.Console.Write("\b \b");
						pass.Pop();
					}

					break;
				case Ctrlbacksp:
					while (pass.Count > 0)
					{
						System.Console.Write("\b \b");
						pass.Pop();
					}

					break;
				default:
					if (filtered.Count(x => chr == x) <= 0)
					{
						pass.Push(chr);
						System.Console.Write(mask);
					}

					break;
				}
			}

			System.Console.WriteLine();

			return new string(pass.Reverse().ToArray());
		}

		/// <summary>
		/// Like System.Console.ReadLine(), only with a mask.
		/// </summary>
		/// <returns>the string the user typed in </returns>
		public static string ReadPassword()
		{
			return ReadPassword('*');
		}
	}
}
