#region Namespaces

using System.IO;
using CommandLineParser;

#endregion Namespaces

namespace ICCHeadshots
{
	[CommandLineDescription("Load Headshots for Iowa Code Camp to the Access Database")]
	public class CommandLineArguments
	{
		#region Public Fields

		[CommandLineArgument(CommandLineArgumentType.Required)]
		[CommandLineArgumentDescription("Folder to store the downloaded image files in")]
		public string imageFolder;

		[CommandLineArgument(CommandLineArgumentType.AtMostOnce)]
		[CommandLineArgumentDescription("Folder to store the resized image files in. Defaults to the <imageFolder>/resized")]
		public string resizedFolder;

		[CommandLineArgument(CommandLineArgumentType.Required)]
		[CommandLineArgumentDescription("Path to the Access database")]
		public string databasePath;

		[CommandLineArgument(CommandLineArgumentType.AtMostOnce, LongName = "download", ShortName = "l")]
		[CommandLineArgumentDescription("Download the images from the URLs listed in the Speakers table")]
		public bool download = false;

		[CommandLineArgument(CommandLineArgumentType.AtMostOnce, LongName = "resize", ShortName = "z")]
		[CommandLineArgumentDescription("Resize the images in the imageFolder")]
		public bool resize = false;

		[CommandLineArgument(CommandLineArgumentType.AtMostOnce)]
		[CommandLineArgumentDescription("Load the images to the Access database from the resized images")]
		public bool upload = false;

		[CommandLineArgument(CommandLineArgumentType.AtMostOnce, LongName = "maxWidth", ShortName = "w")]
		[CommandLineArgumentDescription("Max width of the resized images. Defaults to 90.")]
		public int maxWidth = 90;

		[CommandLineArgument(CommandLineArgumentType.AtMostOnce, LongName = "maxHeithg", ShortName = "h")]
		[CommandLineArgumentDescription("Max height of the resized images. Defaults to 117.")]
		public int mazHeight = 117;

		[CommandLineArgument(CommandLineArgumentType.AtMostOnce)]
		[CommandLineArgumentDescription("Create the SpeakerHeadshots table")]
		public bool makeHeadshotTable = false;

		[CommandLineArgument(CommandLineArgumentType.AtMostOnce)]
		[CommandLineArgumentDescription("Convert images in the imageFolder to bitmaps")]
		public bool convertToBitmap;

		//[CommandLineArgument(CommandLineArgumentType.AtMostOnce, LongName = "separateAccounts", ShortName = "sa")]
		//[CommandLineArgumentDescription("Output file for metadata match data")]

		#endregion Public Fields

		#region Public Methods

		public string Verify()
		{
			if (download || upload)
			{
				if (string.IsNullOrWhiteSpace(databasePath))
				{
					return "databasePath must be specified";
				}
			}

			if (resize || download)
			{
				if (string.IsNullOrWhiteSpace(imageFolder))
				{
					return "imageFolder must be specified.";
				}
			}

			if (resize || upload)
			{
				if (string.IsNullOrWhiteSpace(resizedFolder))
				{
					resizedFolder = Path.Combine(imageFolder, "resized");
				}
			}

			if (upload)
			{
				if (string.IsNullOrWhiteSpace(resizedFolder))
				{
					return "resizedFolder must be specified";
				}
			}

			return null;
		}

		#endregion Public Methods

	}

}