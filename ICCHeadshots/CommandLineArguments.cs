#region Namespaces

using CommandLineParser;

#endregion Namespaces

namespace ICCHeadshots
{
	[CommandLineDescription("Load Headshots for Iowa Code Camp to the Access Database")]
	public class CommandLineArguments
	{
		#region Public Fields

		[CommandLineArgument(CommandLineArgumentType.Required)]
		[CommandLineArgumentDescription("Folder to store the image files in")]
		public string imageFolder;

		[CommandLineArgument(CommandLineArgumentType.Required)]
		[CommandLineArgumentDescription("Path to the Access database")]
		public string databasePath;

		[CommandLineArgument(CommandLineArgumentType.AtMostOnce, LongName = "download", ShortName = "l")]
		[CommandLineArgumentDescription("Path to the Access database")]
		public bool download = false;

		[CommandLineArgument(CommandLineArgumentType.AtMostOnce)]
		[CommandLineArgumentDescription("Path to the Access database")]
		public bool resize = false;

		//[CommandLineArgument(CommandLineArgumentType.AtMostOnce, LongName = "separateAccounts", ShortName = "sa")]
		//[CommandLineArgumentDescription("Output file for metadata match data")]

		#endregion Public Fields

		public string Verify()
		{
			return null;
		}

	}

}