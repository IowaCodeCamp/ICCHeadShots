#region Namespaces

using System.Data.SqlClient;

#endregion namespaces

namespace ICCHeadshots
{
	/// <summary>
	/// The database context.
	/// </summary>
	public class DatabaseContext
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseContext"/> class.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="commandTimeout">The command timeout.</param>
		/// <param name="longOperationCommandTimeout">The long operation command timeout.</param>
		public DatabaseContext(string connectionString, int commandTimeout, int longOperationCommandTimeout)
		{
			m_ConnectionString = connectionString;
			m_CommandTimeout = commandTimeout;
			m_LongOperationCommandTimeout = longOperationCommandTimeout;
		}

		#endregion Constructors

		#region Public Properties

		/// <summary>
		/// Gets or sets the command timeout.
		/// </summary>
		/// <value>The command timeout.</value>
		public int CommandTimeout
		{
			get { return m_CommandTimeout; }
			set { m_CommandTimeout = value; }
		}

		/// <summary>
		/// Gets or sets the connection string.
		/// </summary>
		/// <value>The connection string.</value>
		public string ConnectionString
		{
			get { return m_ConnectionString; }
			set { m_ConnectionString = value; }
		}

		/// <summary>
		/// Gets or sets the command timeout for a long operation
		/// </summary>
		/// <value>The command timeout.</value>
		public int LongOperationCommandTimeout
		{
			get { return m_LongOperationCommandTimeout; }
			set { m_LongOperationCommandTimeout = value; }
		}

		#endregion Public Properties

		#region Private Fields

		private string m_ConnectionString;
		private int m_CommandTimeout;
		private int m_LongOperationCommandTimeout;

		#endregion Private Fields
	}
}
