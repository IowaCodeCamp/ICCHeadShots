#region Namespaces

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using CommandLineParser;
using ICCHeadshots.Speakers_and_SessionsDataSetTableAdapters;

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

			if (settings.upload)
				process.UploadHeadshotsToDatabase();

			if (settings.makeHeadshotTable)
				process.CreateHeadshotTable();

			if (settings.convertToBitmap)
				process.ConvertToBitmap();

			Console.WriteLine("Processing Completed. Press Enter to terminate.");
			Console.ReadLine();
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

			var connectionStringBuilder = new OleDbConnectionStringBuilder();
			connectionStringBuilder.DataSource = m_Settings.databasePath;
			connectionStringBuilder.Provider = "Microsoft.ACE.OLEDB.12.0";
			connectionStringBuilder.PersistSecurityInfo = false;
			m_ConnectionString = connectionStringBuilder.ToString();

			m_DatabaseContext = new DatabaseContext(m_ConnectionString, 30, 30);
			m_DatabaseHelperBase = new DatabaseHelperBase(m_DatabaseContext);
		}

		public void RetrieveImageFiles()
		{
			Directory.CreateDirectory(m_Settings.imageFolder);

			// Get the list of Speakers
			List<SpeakerHeadshot> headshots = GetHeadshotsFromDatabase();

			foreach (var speakerHeadshot in headshots)
			{
				if (!string.IsNullOrWhiteSpace(speakerHeadshot.HeadshotUrl))
				{
					var uri = new Uri(speakerHeadshot.HeadshotUrl);
					string filename = System.IO.Path.GetFileName(uri.LocalPath);
					var extension = Path.GetExtension(filename);

					string outputFilename = GetFileName(speakerHeadshot.SpeakerName) + extension;
					speakerHeadshot.HeadshotFile = outputFilename;
					try
					{
						RetrieveHeadshot(speakerHeadshot.HeadshotUrl, Path.Combine(m_Settings.imageFolder, speakerHeadshot.HeadshotFile));

						UpdateHeadshotFilename(speakerHeadshot);
					}
					catch (Exception)
					{
						Console.WriteLine("Could not retrieve image for {0}", speakerHeadshot.SpeakerName);
					}
				}
			}
		}

		private List<SpeakerHeadshot> GetHeadshotsFromDatabase()
		{
			var headshots = new List<SpeakerHeadshot>();

			using (DbDataReader dbDataReader = m_DatabaseHelperBase.GetRecords("select SpeakerKey, SpeakerName, HeadshotUrl, HeadshotFile from Speakers order by SpeakerKey", null))
			{
				while (dbDataReader.Read())
				{
					var speakerHeadshot = new SpeakerHeadshot();
					speakerHeadshot.SpeakerKey = (int) dbDataReader["SpeakerKey"];
					speakerHeadshot.SpeakerName = (string)dbDataReader["SpeakerName"];
					if (dbDataReader["HeadshotUrl"] != DBNull.Value)
						speakerHeadshot.HeadshotUrl = (string)dbDataReader["HeadshotUrl"];
					if (dbDataReader["HeadshotFile"] != DBNull.Value)
						speakerHeadshot.HeadshotFile = (string)dbDataReader["HeadshotFile"];

					headshots.Add(speakerHeadshot);
				}
			}
			return headshots;
		}

		/// <summary>
		/// Resizes the files.
		/// </summary>
		public void ResizeFiles()
		{
			Directory.CreateDirectory(m_Settings.resizedFolder);
			string[] files = Directory.GetFiles(m_Settings.imageFolder, "*.*");
			foreach (var file in files)
			{
				try
				{
					if (m_Settings.convertToBitmap)
					{
						string fileName = Path.GetFileNameWithoutExtension(file);
						fileName = fileName + ".bmp";

						Image image = Image.FromFile(file);
						Image resized = Resize(image, m_Settings.maxWidth, m_Settings.mazHeight);

						using (var ms = new MemoryStream())
						{
							resized.Save(ms, ImageFormat.Bmp);
							Image bitmap = Image.FromStream(ms);
							bitmap.Save(Path.Combine(m_Settings.resizedFolder, fileName));
						}
					}
					else
					{
						Image image = Image.FromFile(file);
						Image resized = Resize(image, m_Settings.maxWidth, m_Settings.mazHeight);

						string fileName = Path.GetFileName(file);
						resized.Save(Path.Combine(m_Settings.resizedFolder, fileName));
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Error processing file: {0}", Path.GetFileName(file));
				}
			}
		}

		/// <summary>
		/// Converts files in a folder to Bitmaps
		/// </summary>
		public void ConvertToBitmap()
		{
			string[] files = Directory.GetFiles(m_Settings.imageFolder, "*.*");
			foreach (var file in files)
			{
				string fileName = Path.GetFileNameWithoutExtension(file);
				string extension = Path.GetExtension(file);

				if (extension == ".bmp")
					continue;

				fileName = fileName + ".bmp";

				Image image = Image.FromFile(file);
				using (var ms = new MemoryStream())
				{
					image.Save(ms, ImageFormat.Bmp);
					Image bitmap = Image.FromStream(ms);
					bitmap.Save(Path.Combine(m_Settings.imageFolder, fileName));
				}
			}
		}
		
		public void UploadHeadshotsToDatabase()
		{
			List<SpeakerHeadshot> headshotsFromDatabase = GetHeadshotsFromDatabase();
			foreach (var speakerHeadshot in headshotsFromDatabase)
			{
				string filePath = Path.Combine(m_Settings.resizedFolder, speakerHeadshot.HeadshotFile);
				Image image = Image.FromFile(filePath);

				UpdateHeadshot(speakerHeadshot, image);
			}
		}

		public void CreateHeadshotTable()
		{
			string sql = "Create TABLE SpeakerHeadshots2 (SpeakerKey int, Headshot longbinary)";
			m_DatabaseHelperBase.ExecuteQuery(sql);
		}

		private void UpdateHeadshot(SpeakerHeadshot speakerHeadshot, Image image)
		{
			byte[] imageBytes;
			using(var ms = new MemoryStream())
			{
				image.Save(ms, ImageFormat.Bmp);
				imageBytes = ms.ToArray();
			}

			string sql = "select speakerkey from SpeakerHeadShots where speakerkey = @speakerkey";
			using(DbDataReader reader = m_DatabaseHelperBase.GetRecords(sql, new OleDbParameter("speakerkey", speakerHeadshot.SpeakerKey)))
			{
				var imageDataParameter = new OleDbParameter("headshot", OleDbType.LongVarBinary, imageBytes.Length);
				imageDataParameter.Value = imageBytes;
				var speakerKeyParameter = new OleDbParameter("speakerkey", speakerHeadshot.SpeakerKey);

				if (reader.HasRows)
				{
					sql = "update SpeakerHeadShots2 set HeadShot = @headshot where SpeakerKey = @speakerkey";
					m_DatabaseHelperBase.ExecuteQuery(
						sql,
						imageDataParameter,
						speakerKeyParameter);
				}
				else
				{
					var table = new SpeakerHeadshotsTableAdapter();
					table.GetData();
					table.Insert(speakerHeadshot.SpeakerKey, imageBytes, 0);

					//sql = "insert INTO SpeakerHeadShots (SpeakerKey, Headshot) Values(@speakerkey, @headshot)";
					//m_DatabaseHelperBase.ExecuteQuery(
					//    sql,
					//    imageDataParameter,
					//    speakerKeyParameter);
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


		/// <summary>
		/// Gets the name of the file.
		/// </summary>
		/// <param name="speakerName">Name of the speaker.</param>
		/// <returns></returns>
		private static string GetFileName(string speakerName)
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
			using (var client = new WebClient())
			{
				client.DownloadFile(url, outputFile);
			}
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
