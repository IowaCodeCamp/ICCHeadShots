using System;

namespace ICCHeadshots
{
	/// <summary>
	/// All database table names involved in the SFDC sync process.
	/// </summary>
	public enum Tables
	{
		Sessions,
		Speakers,
		SpeakerHeadshots,
	}

	public static class TableConverter
	{
		public static string ToString(Tables table)
		{
			return Enum.GetName(typeof(Tables), table);
		}

		public static Tables ToEnum(string table)
		{
			return (Tables)Enum.Parse(typeof(Tables), table);
		}
	}
}
