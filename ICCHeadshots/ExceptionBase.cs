#region Namespaces

using System;
using System.Diagnostics;

#endregion Namespaces

namespace ICCHeadshots
{
	/// <summary>
	/// Base class for exceptions
	/// </summary>
	public abstract class ExceptionBase : ApplicationException
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ExceptionBase"/> class.
		/// </summary>
		protected ExceptionBase(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ExceptionBase"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="innerException">The inner exception.</param>
		protected ExceptionBase(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		#endregion Constructors

		#region Public Properties

		public bool Logged
		{
			[DebuggerStepThrough]
			get { return m_Logged; }
			[DebuggerStepThrough]
			set { m_Logged = value; }
		}

		#endregion Public Properties

		#region Public Methods

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return Message + "\n" + InnerException.Message;
		}

		#endregion Public Methods

		#region Private Fields

		private bool m_Logged;

		#endregion Private Fields

		#region Private Methods

		#endregion Private Methods
	}
}