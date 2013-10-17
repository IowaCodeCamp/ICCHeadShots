#region Namespaces

using System;

#endregion Namespaces

namespace ICCHeadshots
{
	/// <summary>
	/// Holds information on an error related to database operations
	/// </summary>
	public class DatabaseException : ExceptionBase
	{
		#region Constructors

		public DatabaseException()
			: base("Auto Generated Exception for Testing")
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="innerException">The inner exception.</param>
		public DatabaseException(string message, Exception innerException)
			: base(message, innerException)
		{
			m_IncludesMessageDetails = false;
		}


		#endregion Constructors

		#region Public Properties

		#endregion Public Properties

		#region Public Methods

		#endregion Public Methods

		#region Private Fields

		private bool m_IncludesMessageDetails;

		#endregion Private Fields
	}
}