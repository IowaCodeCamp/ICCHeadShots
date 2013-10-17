#region Namespaces

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using CommandLineParser;

#endregion Namespaces

namespace ICCHeadshots
{
	public static class Program
	{
		static void Main(string[] args)
		{
			var settings = new CommandLineArguments();
			if (!Utility.ParseCommandLineArguments(args, settings))
			{
				Usage();
				return;
			}

			string message = settings.Verify();
			if (!string.IsNullOrEmpty(message))
			{
				Usage(message);
				return;
			}

			var process = new Process(settings);

			if (settings.download)
				process.RetrieveImageFiles();

			if (settings.resize)
				process.ResizeFiles();
		}

		private static void Usage(string message = null)
		{
			if (!string.IsNullOrEmpty(message))
			{
				Console.Error.WriteLine(message);
			}

			Console.Error.WriteLine(Utility.CommandLineArgumentsUsage(typeof(CommandLineArguments)));
		}
	}

	public class Process
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Process"/> class.
		/// </summary>
		/// <param name="settings">The settings.</param>
		public Process(CommandLineArguments settings)
		{
			m_Settings = settings;

			OleDbConnectionStringBuilder connectionStringBuilder = new OleDbConnectionStringBuilder();
			connectionStringBuilder.DataSource = m_Settings.databasePath;
			connectionStringBuilder.Provider = "Microsoft.ACE.OLEDB.12.0";
			connectionStringBuilder.PersistSecurityInfo = false;
			m_ConnectionString = connectionStringBuilder.ToString();

			m_DatabaseContext = new DatabaseContext(m_ConnectionString, 30, 30);
			m_DatabaseHelperBase = new DatabaseHelperBase(m_DatabaseContext);
		}

		public void RetrieveImageFiles()
		{
			// Get the list of Speakers
			List<SpeakerHeadshot> headshots = GetHeadshots();

			foreach (var speakerHeadshot in headshots)
			{
				if (!string.IsNullOrWhiteSpace(speakerHeadshot.HeadshotUrl))
				{
					Uri uri = new Uri(speakerHeadshot.HeadshotUrl);
					string filename = System.IO.Path.GetFileName(uri.LocalPath);
					var extension = Path.GetExtension(filename);

					string outputFilename = GetFileName(speakerHeadshot.SpeakerName) + extension;
					speakerHeadshot.HeadshotFile = Path.Combine(m_Settings.imageFolder, outputFilename);
					RetrieveHeadshot(speakerHeadshot.HeadshotUrl, speakerHeadshot.HeadshotFile);

					UpdateHeadshotFilename(speakerHeadshot);
				}
			}
		}

		private void UpdateHeadshotFilename(SpeakerHeadshot speakerHeadshot)
		{
			m_DatabaseHelperBase.ExecuteQuery(
				"update Speakers set HeadShotFile = @headshotfile where SpeakerKey = @speakerkey", 
				new OleDbParameter("headshotfile", speakerHeadshot.HeadshotFile),
				new OleDbParameter("speakerkey", speakerHeadshot.SpeakerKey));
		}

		private List<SpeakerHeadshot> GetHeadshots()
		{
			var headshots = new List<SpeakerHeadshot>();

			using (DbDataReader dbDataReader = m_DatabaseHelperBase.GetRecords("select SpeakerKey, SpeakerName, HeadshotUrl from Speakers order by SpeakerKey", null))
			{
				while (dbDataReader.Read())
				{
					var speakerHeadshot = new SpeakerHeadshot();
					speakerHeadshot.SpeakerKey = (int) dbDataReader["SpeakerKey"];
					speakerHeadshot.SpeakerName = (string)dbDataReader["SpeakerName"];
					if (dbDataReader["HeadshotUrl"] != DBNull.Value)
						speakerHeadshot.HeadshotUrl = (string)dbDataReader["HeadshotUrl"];
					headshots.Add(speakerHeadshot);
				}
			}
			return headshots;
		}

		public void ResizeFiles()
		{
			string resizePath = Path.Combine(m_Settings.imageFolder, "resized");
			Directory.CreateDirectory(resizePath);
			string[] files = Directory.GetFiles(m_Settings.imageFolder, "*.*");
			foreach (var file in files)
			{
				string fileName = Path.GetFileName(file);

				Image image = Image.FromFile(file);
				Image resized = Resize(image, 90, 117);
				
				resized.Save(Path.Combine(resizePath, fileName));
			}
		}

		/// <summary>
		/// Gets the name of the file.
		/// </summary>
		/// <param name="speakerName">Name of the speaker.</param>
		/// <returns></returns>
		private string GetFileName(string speakerName)
		{
			var sb = new StringBuilder();
			foreach (char c in speakerName)
			{
				if (Char.IsLetterOrDigit(c) || c == '_' || c == '.')
				{
					sb.Append(c);
				}
			}

			return sb.ToString();
		}

		static void RetrieveHeadshot(string url, string outputFile)
		{
			WebClient Client = new WebClient ();
			Client.DownloadFile(url, outputFile);
		}


		/// <summary>
		/// Resizes the specified source.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="maxWidth">Width of the max.</param>
		/// <param name="maxHeight">Height of the max.</param>
		/// <returns></returns>
		static Image Resize(Image source, int maxWidth, int maxHeight)
		{
			var ratioX = (double)maxWidth / source.Width;
			var ratioY = (double)maxHeight / source.Height;
			var ratio = Math.Min(ratioX, ratioY);

			var newWidth = (int)(source.Width * ratio);
			var newHeight = (int)(source.Height * ratio);

			var newImage = new Bitmap(newWidth, newHeight);
			Graphics.FromImage(newImage).DrawImage(source, 0, 0, newWidth, newHeight);
			return newImage;
		}

		#region Private Fields

		private string m_ConnectionString;
		private readonly CommandLineArguments m_Settings;
		private DatabaseContext m_DatabaseContext;
		private DatabaseHelperBase m_DatabaseHelperBase;

		#endregion Private Fields

	}
}
