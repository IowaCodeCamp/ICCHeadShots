#region Namespaces

using System;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;

#endregion Namespaces

namespace ICCHeadshots
{
	/// <summary>
	/// The base class for all database helpers.
	/// </summary>
	public class DatabaseHelperBase
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseHelperBase"/> class.
		/// </summary>
		/// <param name="databaseContext">The database context.</param>
		public DatabaseHelperBase(DatabaseContext databaseContext)
		{
			m_DatabaseContext = databaseContext;
			CommandTimeout = databaseContext.CommandTimeout;
		}

		#endregion Constructors

		#region Public Properties

		public int CommandTimeout
		{
			get { return m_CommandTimeout; }
			set { m_CommandTimeout = value; }
		}

		public DatabaseContext DatabaseContext
		{
			[DebuggerStepThrough]
			get { return m_DatabaseContext; }
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Gets the scalar value.
		/// </summary>
		/// <typeparam name="T1">The type of the expected return value.</typeparam>
		/// <typeparam name="T2">The type of the matching column value</typeparam>
		/// <param name="resultColumn">The result column.</param>
		/// <param name="targetTable">The target table.</param>
		/// <param name="matchingColumn">The matching column.</param>
		/// <param name="matchingColumnValue">The matching column value.</param>
		/// <returns></returns>
		public T1 GetScalarValue<T1, T2>(string resultColumn, string targetTable, string matchingColumn, T2 matchingColumnValue)
		{
			T1 returnValue = default(T1);
			object result = GetScalarValue(resultColumn, targetTable, matchingColumn, matchingColumnValue);
			if (result != null && result != DBNull.Value)
			{
				returnValue = (T1)result;
			}
			return returnValue;
		}

		/// <summary>
		/// Executes the query.
		/// </summary>
		/// <param name="sqlQuery">The SQL query.</param>
		/// <returns></returns>
		public bool ExecuteQuery(string sqlQuery)
		{
			return ExecuteQuery(sqlQuery, null);
		}

		/// <summary>
		/// Executes the query.
		/// </summary>
		/// <param name="sqlQuery">The SQL query.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		public bool ExecuteQuery(string sqlQuery, params OleDbParameter[] parameters)
		{
			var oleDbConnection = new OleDbConnection(m_DatabaseContext.ConnectionString);
			oleDbConnection.Open();

			var command = new OleDbCommand(sqlQuery, oleDbConnection);
			command.CommandType = CommandType.Text;
			command.CommandTimeout = m_CommandTimeout;

			try
			{
				if (parameters != null)
				{
					foreach (OleDbParameter parameter in parameters)
					{
						command.Parameters.Add(parameter);
					}
				}

				try
				{
					int recordsAffected = command.ExecuteNonQuery();
					return recordsAffected > 0 ? true : false;
				}
				catch (SqlException sqlException)
				{
					sqlException.Data.Add(SQL_COMMAND_TEXT_KEY, sqlQuery);
					sqlException.Data.Add(PARAMETERS_KEY, parameters);
					throw;
				}
			}
			finally
			{
				command.Parameters.Clear();
			}
		}


		/// <summary>
		/// Inserts the record using the supplied SQL.
		/// </summary>
		/// <param name="sqlInsert">The SQL insert.</param>
		/// <returns>The generated identity column value if an identity column.(SCOPE_IDENTITY)</returns>
		public int InsertRecord(string sqlInsert)
		{
			sqlInsert += " SET @IdentityKey = SCOPE_IDENTITY()";
			var oleDbConnection = new OleDbConnection(m_DatabaseContext.ConnectionString);
			oleDbConnection.Open();

			try
			{
				var command = new OleDbCommand(sqlInsert, oleDbConnection);
				command.CommandType = CommandType.Text;
				var identityKeyparameter = new OleDbParameter("@IdentityKey", SqlDbType.Int);
				identityKeyparameter.Direction = ParameterDirection.Output;
				command.Parameters.Add(identityKeyparameter);

				try
				{
					command.ExecuteNonQuery();
				}
				catch (SqlException sqlException)
				{
					sqlException.Data.Add(SQL_COMMAND_TEXT_KEY, sqlInsert);
					throw;
				}

				return identityKeyparameter.Value != null ? (int)identityKeyparameter.Value : 0;
			}
			finally
			{
				oleDbConnection.Close();
			}
		}

		/// <summary>
		/// Executes the query and return a single result.
		/// </summary>
		/// <param name="sqlQuery">The SQL query.</param>
		/// <returns></returns>
		public object ExecuteScalar(string sqlQuery)
		{
			return ExecuteScalar(sqlQuery, new OleDbParameter[] {});
		}

		/// <summary>
		/// Executes the query and return a single result.
		/// </summary>
		/// <param name="sqlQuery">The SQL query.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		public object ExecuteScalar(string sqlQuery, params OleDbParameter[] parameters)
		{
			using (var oleDbConnection = new OleDbConnection(m_DatabaseContext.ConnectionString))
			{
				oleDbConnection.Open();
				var command = new OleDbCommand(sqlQuery, oleDbConnection);
				command.CommandType = CommandType.Text;
				foreach (OleDbParameter parameter in parameters)
				{
					command.Parameters.Add(parameter);
				}

				try
				{
					object result = command.ExecuteScalar();
					return result;
				}
				catch (SqlException sqlException)
				{
					sqlException.Data.Add(SQL_COMMAND_TEXT_KEY, sqlQuery);
					sqlException.Data.Add(PARAMETERS_KEY, parameters);
					throw;
				}
			}
		}

		/// <summary>
		/// Gets the record by key.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="keyFieldName">Name of the key field.</param>
		/// <param name="keyFieldValue">The key field value.</param>
		/// <returns></returns>
		public IDataReader GetRecordByKey(Tables table, string keyFieldName, int keyFieldValue)
		{
			var sqlBuilder = new StringBuilder("SELECT *");
			sqlBuilder.Append(" FROM " + table);
			sqlBuilder.Append(" WHERE " + keyFieldName + " = @KeyFieldValue");

			// Connection should be closed by the returned IDataReader
			var oleDbConnection = new OleDbConnection(m_DatabaseContext.ConnectionString);
			oleDbConnection.Open();

			var command = new OleDbCommand(sqlBuilder.ToString(), oleDbConnection);
			command.CommandType = CommandType.Text;

			command.Parameters.Add(new OleDbParameter("@KeyFieldValue", SqlDbType.Int)).Value = keyFieldValue;

			try
			{
				IDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection);
				return reader;
			}
			catch (SqlException sqlException)
			{
				sqlException.Data.Add(SQL_COMMAND_TEXT_KEY, command.CommandText);
				throw;
			}
		}

		/// <summary>
		/// Gets the records.
		/// </summary>
		/// <param name="sqlQuery">The SQL query which is parameterised.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		public DbDataReader GetRecords(string sqlQuery, params OleDbParameter[] parameters)
		{
			var oleDbConnection = new OleDbConnection(m_DatabaseContext.ConnectionString);
			oleDbConnection.Open();
			var command = new OleDbCommand(sqlQuery, oleDbConnection);
			command.CommandType = CommandType.Text;

			if (parameters != null)
			{
				foreach (OleDbParameter parameter in parameters)
				{
					command.Parameters.Add(parameter);
				}
			}

			try
			{
				DbDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection);
				return reader;
			}
			catch (SqlException sqlException)
			{
				sqlException.Data.Add(SQL_COMMAND_TEXT_KEY, sqlQuery);
				sqlException.Data.Add(PARAMETERS_KEY, parameters);
				throw;
			}
		}

		public string BuildInParameters(
			string paramName,
			string[] values,
			SqlDbType dataType,
			out OleDbParameter[] parameterCollection)
		{
			var paramNames = new string[values.Length];
			for (int index = 0; index < values.Length; index++)
			{
				paramNames[index] = "@" + paramName + index;
			}

			parameterCollection = new OleDbParameter[values.Length];

			string inClause = string.Join(",", paramNames);
			for (int index = 0; index < paramNames.Length; index++)
			{
				parameterCollection[index] = new OleDbParameter(paramNames[index], dataType);
				parameterCollection[index].Value = values[index];
			}

			return inClause;
		}

		#endregion Public Methods

		#region Private Fields

		private const string PARAMETERS_KEY = "parameters";
		private const string SQL_COMMAND_TEXT_KEY = "sql";

		private int m_CommandTimeout = 30;
		private readonly DatabaseContext m_DatabaseContext;

		#endregion Private Fields

		#region Private Methods

		/// <summary>
		/// Gets the scalar value.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="resultColumn">The result column.</param>
		/// <param name="targetTable">The target table.</param>
		/// <param name="matchingColumn">The matching column.</param>
		/// <param name="matchingColumnValue">The matching column value.</param>
		/// <returns></returns>
		private object GetScalarValue<T>(string resultColumn, string targetTable, string matchingColumn, T matchingColumnValue)
		{
			var sqlQuery = new StringBuilder("SELECT ");
			sqlQuery.Append(resultColumn);
			sqlQuery.Append(" FROM ");
			sqlQuery.Append(targetTable);
			sqlQuery.Append(" WHERE ");
			sqlQuery.Append(matchingColumn);
			sqlQuery.Append(" = @MatchingColumnValue");

			var parameterMatchingColumnValue = new OleDbParameter("@MatchingColumnValue", matchingColumnValue);
			object result;
			try
			{
				result = ExecuteScalar(sqlQuery.ToString(), parameterMatchingColumnValue);
			}
			catch (SqlException sqlException)
			{
				throw new DatabaseException(string.Format("A failure occurred reading a value from table {0}", targetTable), sqlException);
			}

			return result;
		}

		#endregion Private Methods
	}
}
