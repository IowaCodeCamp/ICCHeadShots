#region Namespaces

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

#endregion Namespaces

namespace CommandLineParser
{

	#region Enumerations

	/// <summary>
	/// Used to control parsing of command line arguments.
	/// </summary>
	[Flags]
	public enum CommandLineArgumentType
	{
		/// <summary>
		/// Indicates that this field is required. An error will be displayed
		/// if it is not present when parsing arguments.
		/// </summary>
		Required = 0x01,
		/// <summary>
		/// Only valid in conjunction with Multiple.
		/// Duplicate values will result in an error.
		/// </summary>
		Unique = 0x02,
		/// <summary>
		/// Inidicates that the argument may be specified more than once.
		/// Only valid if the argument is a collection
		/// </summary>
		Multiple = 0x04,

		/// <summary>
		/// The default type for non-collection arguments.
		/// The argument is not required, but an error will be reported if it is specified more than once.
		/// </summary>
		AtMostOnce = 0x00,

		/// <summary>
		/// For non-collection arguments, when the argument is specified more than
		/// once no error is reported and the value of the argument is the last
		/// value which occurs in the argument list.
		/// </summary>
		LastOccurenceWins = Multiple,

		/// <summary>
		/// The default type for collection arguments.
		/// The argument is permitted to occur multiple times, but duplicate 
		/// values will cause an error to be reported.
		/// </summary>
		MultipleUnique = Multiple | Unique,
	}

	#endregion Enumerations

	#region Attributes

	[AttributeUsage(AttributeTargets.Class)]
	public class CommandLineDescriptionAttribute : Attribute
	{
		private readonly string description;

		public CommandLineDescriptionAttribute(string description)
		{
			this.description = description;
		}

		public string Description
		{
			get { return description; }
		}
	}

	/// <summary>
	/// Allows control of command line parsing.
	/// Attach this attribute to instance fields of types used
	/// as the destination of command line argument parsing.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class CommandLineArgumentAttribute : Attribute
	{
		/// <summary>
		/// Allows control of command line parsing.
		/// </summary>
		/// <param name="type"> Specifies the error checking to be done on the argument. </param>
		public CommandLineArgumentAttribute(CommandLineArgumentType type)
		{
			this.type = type;
		}

		/// <summary>
		/// The error checking to be done on the argument.
		/// </summary>
		public CommandLineArgumentType Type
		{
			get { return type; }
		}

		/// <summary>
		/// Returns true if the argument did not have an explicit short name specified.
		/// </summary>
		public bool DefaultShortName
		{
			get { return null == shortName; }
		}

		/// <summary>
		/// The short name of the argument.
		/// </summary>
		public string ShortName
		{
			get { return shortName; }
			set { shortName = value; }
		}

		/// <summary>
		/// Returns true if the argument did not have an explicit long name specified.
		/// </summary>
		public bool DefaultLongName
		{
			get { return null == longName; }
		}

		/// <summary>
		/// The long name of the argument.
		/// </summary>
		public string LongName
		{
			get
			{
				Debug.Assert(!DefaultLongName);
				return longName;
			}
			set { longName = value; }
		}

		private string shortName;
		private string longName;
		private readonly CommandLineArgumentType type;
	}


	/// <summary>
	/// Indicates that this argument is the default argument.
	/// '/' or '-' prefix. Only the argument value is specified - no name.
	/// Example: A default file argument for the program del.exe: 
	///		del.exe c:\temp\testfile.txt
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class DefaultCommandLineArgumentAttribute : CommandLineArgumentAttribute
	{
		/// <summary>
		/// Indicates that this argument is the default argument.
		/// </summary>
		/// <param name="type"> Specifies the error checking to be done on the argument. </param>
		public DefaultCommandLineArgumentAttribute(CommandLineArgumentType type)
			: base(type)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class CommandLineArgumentDescriptionAttribute : Attribute
	{
		public CommandLineArgumentDescriptionAttribute(string description)
		{
			m_description = description;
		}

		public string Description
		{
			get { return m_description; }
		}

		private readonly string m_description;
	}

	#endregion Attributes

	/// <summary>
	/// Parser for command line arguments.
	///
	/// The parser specification is infered from the instance fields of the object
	/// specified as the destination of the parse.
	/// Valid argument types are: int, uint, string, bool, enums
	/// Also argument types of Array of the above types are also valid.
	/// 
	/// Error checking options can be controlled by adding a CommandLineArgumentAttribute
	/// to the instance fields of the destination object.
	///
	/// At most one field may be marked with the DefaultCommandLineArgumentAttribute
	/// indicating that arguments without a '-' or '/' prefix will be parsed as that argument.
	///
	/// If not specified then the parser will infer default options for parsing each
	/// instance field. The default long name of the argument is the field name. The
	/// default short name is the first character of the long name. Long names and explicitly
	/// specified short names must be unique. Default short names will be used provided that
	/// the default short name does not conflict with a long name or an explicitly
	/// specified short name.
	///
	/// Arguments which are array types are collection arguments. Collection
	/// arguments can be specified multiple times.
	/// 
	/// </summary>
	/// <remarks>
	/// <p>To use this library, define a class whose fields represent the data that your 
	///	application wants to receive from arguments on the command line. Then call 
	///	Utilities.Utility.ParseCommandLineArguments() to fill the object with the data 
	///	from the command line. Each field in the class defines a command line argument. 
	///	The type of the field is used to validate the data read from the command line. 
	///	The name of the field defines the name of the command line option.</p>
	/// 
	///	<p>The parser can handle fields of the following types:</p>
	/// <list type="bullet">
	///	<item>string</item>
	///	<item>int</item>
	///	<item>uint</item>
	///	<item>bool</item>
	///	<item>enum</item>
	///	<item>array of the above type</item>
	/// </list>
	/// 
	///	<p>For example, suppose you want to read in the argument list for wc (word count). 
	///	wc takes three optional boolean arguments: -l, -w, and -c and a list of files.</p>
	/// <p>
	///	You could parse these arguments using the following code:
	/// </p>
	/// <code>
	///	class MyProgramArguments
	///	{
	///		public bool lines;
	///		public bool words;
	///		public bool chars;
	///		public string[] files;
	///	}
	///
	///	class MyProgram
	///	{
	///		static void Main(string[] args)
	///		{
	///			MyProgramArguments parsedArgs = new MyProgramArguments();
	///			if (!Utilities.Utility.ParseCommandLineArguments(args, parsedArgs)) 
	///			{
	///				// error encountered in arguments. Display usage message
	///				System.Console.Write(Utilities.Utility.CommandLineArgumentsUsage(typeof(MyProgramArguments)));
	///			}
	///			else
	///			{
	///				// insert application code here
	///			}
	///		}
	///	}
	/// </code>
	/// 
	/// <p>
	///	So you could call this aplication with the following command line to count 
	///	lines in the foo and bar files:
	/// </p>
	/// <p>
	/// <code><example>
	///		myprog.exe /lines /files:foo /files:bar
	/// </example></code>
	/// </p>
	///	<p>The program will display the following usage message when bad command line 
	///	arguments are used:</p>
	/// <p>
	/// <example>
	///		myprog.exe -x
	/// </example></p>
	/// <code>
	///	Unrecognized command line argument '-x' <p></p>
	///		/lines[+|-]                         short form /l <p></p>
	///		/words[+|-]                         short form /w <p></p>
	///		/chars[+|-]                         short form /c <p></p>
	///		/files:string                       short form /f <p></p>
	///		@filename                           Read response file for more options <p></p>
	/// </code>
	///	<p>That was pretty easy. However, you realy want to omit the "/files:" for the 
	///	list of files. The details of field parsing can be controled using custom 
	///	attributes. The attributes which control parsing behaviour are:</p>
	/// <p></p>
	/// 
	///	<p>CommandLineArgumentAttribute </p>
	///		<p>- controls short name, long name, required, allow duplicates</p>
	///	<p>DefaultCommandLineArgumentAttribute </p>
	///		<p>- allows omition of the "/name".</p>
	///		<p>- This attribute is allowed on only one field in the argument class.</p>
	///
	///	<p>So for the myprog.exe program we want this:</p>
	/// <code>
	///	class MyProgramArguments
	///	{
	///		public bool lines;
	///		public bool words;
	///		public bool chars;
	///		[Utilities.Utility.DefaultCommandLineArgument]
	///		public string[] files;
	///	}
	///
	///	class MyProgram
	///	{
	///		static void Main(string[] args)
	///		{
	///			MyProgramArguments parsedArgs = new MyProgramArguments();
	///			if (!Utilities.Utility.ParseCommandLineArguments(args, parsedArgs)) 
	///			{
	///				// error encountered in arguments. Display usage message
	///				System.Console.Write(Utilities.Utility.CommandLineArgumentsUsage(typeof(MyProgramArguments)));
	///			}
	///			else
	///			{
	///				// insert application code here
	///			}
	///		}
	///	}
	/// </code>
	/// 
	///	<p>So now we have the command line we want:</p>
	/// <p><code><example>
	///		myprog.exe /lines foo bar
	/// </example></code></p>
	///	<p>This will set lines to true and will set files to an array containing the 
	///	strings "foo" and "bar".</p>
	///
	/// <code>
	///	The new usage message becomes:
	///
	///		myprog.exe -x
	///
	///	Unrecognized command line argument '-x'
	///		/lines[+|-]                         short form /l
	///		/words[+|-]                         short form /w
	///		/chars[+|-]                         short form /c
	///		@filename                            Read response file for more options
	///		files
	/// </code>
	///	
	/// <p>If you don't want to display the results to the Console you can also provide
	///	a delegate which will be called when the parser reports errors during parsing.</p>
	/// 
	///	<p>Cheers,</p>
	///	<p>Peter Hallam</p>
	///	<p>C# Compiler Developer</p>
	///	<p>Microsoft Corp.</p>
	/// </remarks>
	public class CommandLineArgumentParser
	{
		#region Constructors

		/// <summary>
		/// Creates a new command line argument parser.
		/// </summary>
		/// <param name="argumentSpecification"> The type of object to  parse. </param>
		/// <param name="reporter"> The destination for parse errors. </param>
		public CommandLineArgumentParser(Type argumentSpecification, ErrorReporter reporter)
		{
			this.reporter = reporter;
			arguments = new ArrayList();
			argumentMap = new Hashtable();

			Attribute classDescriptionAttribute =
				Attribute.GetCustomAttribute(argumentSpecification, typeof(CommandLineDescriptionAttribute));
			if (classDescriptionAttribute != null)
			{
				description = ((CommandLineDescriptionAttribute) classDescriptionAttribute).Description;
			}


			foreach(FieldInfo field in argumentSpecification.GetFields())
			{
				if (!field.IsStatic && !field.IsInitOnly && !field.IsLiteral)
				{
					CommandLineArgumentAttribute attribute = GetAttribute(field);
					Argument newArgument;
					if (attribute is DefaultCommandLineArgumentAttribute)
					{
						Debug.Assert(defaultArgument == null);
						newArgument = new Argument(attribute, field, reporter);
						defaultArgument = newArgument;
					}
					else
					{
						newArgument = new Argument(attribute, field, reporter);
						arguments.Add(newArgument);
					}

					CommandLineArgumentDescriptionAttribute descriptionAttribute = GetOptionDescriptionAttribute(field);
					if (descriptionAttribute != null)
					{
						newArgument.Description = descriptionAttribute.Description;
					}
				}
			}

			// add explicit names to map
			foreach(Argument argument in arguments)
			{
				Debug.Assert(!argumentMap.ContainsKey(argument.LongName));
				argumentMap[argument.LongName] = argument;
				if (argument.ExplicitShortName && argument.ShortName != null && argument.ShortName.Length > 0)
				{
					Debug.Assert(!argumentMap.ContainsKey(argument.ShortName));
					argumentMap[argument.ShortName] = argument;
				}
			}

			// add implicit names which don't collide to map
			foreach(Argument argument in arguments)
			{
				if (!argument.ExplicitShortName && argument.ShortName != null && argument.ShortName.Length > 0)
				{
					if (!argumentMap.ContainsKey(argument.ShortName))
						argumentMap[argument.ShortName] = argument;
				}
			}
		}

		#endregion Constructors

		#region Public Properties

		/// <summary>
		/// A user friendly program description string without the command line argument syntax.
		/// </summary>
		public string Description
		{
			get
			{
				return this.description;
			}
		}

		/// <summary>
		/// A user firendly usage string describing the command line argument syntax.
		/// </summary>
		public string Usage
		{
			get
			{
				StringBuilder builder = new StringBuilder();
				if (description.Length > 0)
					builder.Append(description + Utility.NewLine);

				int oldLength;
				foreach(Argument arg in arguments)
				{
					oldLength = builder.Length;

					builder.Append("    /");
					builder.Append(arg.LongName);
					Type valueType = arg.ValueType;
					if (valueType == typeof(int))
					{
						builder.Append(":<int>");
					}
					else if (valueType == typeof(uint))
					{
						builder.Append(":<uint>");
					}
					else if (valueType == typeof(bool))
					{
						builder.Append("[+|-]");
					}
					else if (valueType == typeof(string))
					{
						builder.Append(":<string>");
					}
					else if (valueType == typeof(short))
					{
						builder.Append(":<short>");
					}
					else
					{
						Debug.Assert(valueType.IsEnum);

						builder.Append(":{");
						bool first = true;
						foreach(FieldInfo field in valueType.GetFields())
						{
							if (field.IsStatic)
							{
								if (first)
									first = false;
								else
									builder.Append('|');
								builder.Append(field.Name);
							}
						}

						builder.Append('}');
					}

					if (arg.ShortName != arg.LongName && argumentMap[arg.ShortName] == arg)
					{
						builder.Append(' ', IndentLength(builder.Length - oldLength));
						builder.Append("short form /");
						builder.Append(arg.ShortName);
					}

					if (arg.Description.Length > 0)
						builder.Append(" - " + arg.Description);

					builder.Append(Utility.NewLine);
				}

				oldLength = builder.Length;
				builder.Append("    @<file>");
				builder.Append(' ', IndentLength(builder.Length - oldLength));
				builder.Append("Read response file for more options");
				builder.Append(Utility.NewLine);

				if (defaultArgument != null)
				{
					oldLength = builder.Length;
					builder.Append("    <");
					builder.Append(defaultArgument.LongName);
					builder.Append(">");
					if (defaultArgument.Description.Length > 0)
					{
						builder.Append(' ', IndentLength(builder.Length - oldLength));
						builder.Append(defaultArgument.Description);
					}
					builder.Append(Utility.NewLine);
				}

				return builder.ToString();
			}
		}

		#endregion Public Properties

		#region Public Methods

		/// <summary>
		/// Parses an argument list.
		/// </summary>
		/// <param name="args"> The arguments to parse. </param>
		/// <param name="destination"> The destination of the parsed arguments. </param>
		/// <returns> true if no parse errors were encountered. </returns>
		public bool Parse(string[] args, object destination)
		{
			bool hadError = ParseArgumentList(args, destination);

			// check for missing required arguments
			foreach(Argument arg in arguments)
			{
				hadError |= arg.Finish(destination);
			}
			if (defaultArgument != null)
			{
				hadError |= defaultArgument.Finish(destination);
			}

			return !hadError;
		}

		#endregion Public Methods

		#region Private Methods

		private static CommandLineArgumentAttribute GetAttribute(ICustomAttributeProvider field)
		{
			object[] attributes = field.GetCustomAttributes(typeof(CommandLineArgumentAttribute), false);
			if (attributes.Length == 1)
				return (CommandLineArgumentAttribute) attributes[0];

			Debug.Assert(attributes.Length == 0);
			return null;
		}

		private static CommandLineArgumentDescriptionAttribute GetOptionDescriptionAttribute(ICustomAttributeProvider field)
		{
			object[] attributes = field.GetCustomAttributes(typeof(CommandLineArgumentDescriptionAttribute), false);
			if (attributes.Length == 1)
				return (CommandLineArgumentDescriptionAttribute) attributes[0];
			else
				return null;
		}

		private void ReportUnrecognizedArgument(string argument)
		{
			reporter(string.Format("Unrecognized command line argument '{0}'", argument));
		}

		/// <summary>
		/// Parses an argument list into an object
		/// </summary>
		/// <param name="args"></param>
		/// <param name="destination"></param>
		/// <returns> true if an error occurred </returns>
		private bool ParseArgumentList(string[] args, object destination)
		{
			bool hadError = false;
			if (args != null)
			{
				foreach(string argument in args)
				{
					if (argument.Length > 0)
					{
						switch(argument[0])
						{
							case '-':
							case '/':
								int endIndex = argument.IndexOfAny(new char[] {':', '+', '-'}, 1);
								string option = argument.Substring(1, endIndex == -1 ? argument.Length - 1 : endIndex - 1);
								string optionArgument;
								if (option.Length + 1 == argument.Length)
								{
									optionArgument = null;
								}
								else if (argument.Length > 1 + option.Length && argument[1 + option.Length] == ':')
								{
									optionArgument = argument.Substring(option.Length + 2);
								}
								else
								{
									optionArgument = argument.Substring(option.Length + 1);
								}

								Argument arg = (Argument) argumentMap[option];
								if (arg == null)
								{
									ReportUnrecognizedArgument(argument);
									hadError = true;
								}
								else
								{
									hadError |= !arg.SetValue(optionArgument, destination);
								}
								break;

							case '@':
								string[] nestedArguments;
								hadError |= LexFileArguments(argument.Substring(1), out nestedArguments);
								hadError |= ParseArgumentList(nestedArguments, destination);
								break;

							default:
								if (defaultArgument != null)
								{
									hadError |= !defaultArgument.SetValue(argument, destination);
								}
								else
								{
									ReportUnrecognizedArgument(argument);
									hadError = true;
								}
								break;
						}
					}
				}
			}

			return hadError;
		}


		private static int IndentLength(int lineLength)
		{
			return Math.Max(4, 40 - lineLength);
		}

		private bool LexFileArguments(string fileName, out string[] parsedArguments)
		{
			string args;

			try
			{
				using(FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
				{
					args = (new StreamReader(file)).ReadToEnd();
				}
			}
			catch(Exception e)
			{
				reporter(string.Format("Error: Can't open command line argument file '{0}' : '{1}'", fileName, e.Message));
				parsedArguments = null;
				return false;
			}

			bool hadError = false;
			ArrayList argArray = new ArrayList();
			StringBuilder currentArg = new StringBuilder();
			bool inQuotes = false;
			int index = 0;

			// while (index < args.Length)
			try
			{
				while(true)
				{
					// skip whitespace
					while(char.IsWhiteSpace(args[index]))
					{
						index += 1;
					}

					// # - comment to end of line
					if (args[index] == '#')
					{
						index += 1;
						while(args[index] != '\n')
						{
							index += 1;
						}
						continue;
					}

					// do one argument
					do
					{
						// if (args[index] == '\\')
						if ((args[index] == '/') || (args[index] == '-'))
						{
							char argStart = args[index];

							int cSlashes = 1;
							index += 1;

							//while (index == args.Length && args[index] == '\\')
							while (index == args.Length && args[index] == argStart)
							{
								cSlashes += 1;
							}

							if (index == args.Length || args[index] != '"')
							{
								//currentArg.Append('\\', cSlashes);
								currentArg.Append(argStart, cSlashes);
							}
							else
							{
								//currentArg.Append('\\', (cSlashes >> 1));
								currentArg.Append(argStart, (cSlashes >> 1));
								if (0 != (cSlashes & 1))
								{
									currentArg.Append('"');
								}
								else
								{
									inQuotes = !inQuotes;
								}
							}
						}
						else
						{
							if (args[index] == '"')
							{
								inQuotes = !inQuotes;
								index += 1;
							}
							else
							{
								currentArg.Append(args[index]);
								index += 1;
							}
						}

					} while(!char.IsWhiteSpace(args[index]) || inQuotes);

					argArray.Add(currentArg.ToString());
					currentArg.Length = 0;

				}
			}
			catch(IndexOutOfRangeException)
			{
				// got EOF 
				if (inQuotes)
				{
					reporter(string.Format("Error: Unbalanced '\"' in command line argument file '{0}'", fileName));
					hadError = true;
				}
				else if (currentArg.Length > 0)
				{
					// valid argument can be terminated by EOF
					argArray.Add(currentArg.ToString());
				}
			}

			parsedArguments = (string[]) argArray.ToArray(typeof(string));
			return hadError;
		}

		private static string LongName(CommandLineArgumentAttribute attribute, FieldInfo field)
		{
			return (attribute == null || attribute.DefaultLongName) ? field.Name : attribute.LongName;
		}

		private static string ShortName(CommandLineArgumentAttribute attribute, FieldInfo field)
		{
			return !ExplicitShortName(attribute) ? LongName(attribute, field).Substring(0, 1) : attribute.ShortName;
		}

		private static bool ExplicitShortName(CommandLineArgumentAttribute attribute)
		{
			return (attribute != null && !attribute.DefaultShortName);
		}

		private static Type ElementType(FieldInfo field)
		{
			if (IsCollectionType(field.FieldType))
				return field.FieldType.GetElementType();
			else
				return null;
		}

		private static CommandLineArgumentType Flags(CommandLineArgumentAttribute attribute, FieldInfo field)
		{
			if (attribute != null)
				return attribute.Type;
			else if (IsCollectionType(field.FieldType))
				return CommandLineArgumentType.MultipleUnique;
			else
				return CommandLineArgumentType.AtMostOnce;
		}

		private static bool IsCollectionType(Type type)
		{
			return type.IsArray;
		}

		private static bool IsValidElementType(Type type)
		{
			return type != null && (
			                       	type == typeof(int) ||
			                       	type == typeof(uint) ||
			                       	type == typeof(short) ||
			                       	type == typeof(string) ||
			                       	type == typeof(bool) ||
			                       	type.IsEnum);
		}

		#endregion Private Methods

		#region Private Types

		private class Argument
		{
			public Argument(CommandLineArgumentAttribute attribute, FieldInfo field, ErrorReporter reporter)
			{
				longName = CommandLineArgumentParser.LongName(attribute, field);
				explicitShortName = CommandLineArgumentParser.ExplicitShortName(attribute);
				shortName = CommandLineArgumentParser.ShortName(attribute, field);
				elementType = ElementType(field);
				flags = Flags(attribute, field);
				this.field = field;
				seenValue = false;
				this.reporter = reporter;
				isDefault = attribute != null && attribute is DefaultCommandLineArgumentAttribute;

				if (IsCollection)
				{
					collectionValues = new ArrayList();
				}

				Debug.Assert(longName != null && longName.Length > 0);
				Debug.Assert(!IsCollection || AllowMultiple, "Collection arguments must have allow multiple");
				Debug.Assert(!Unique || IsCollection, "Unique only applicable to collection arguments");
				Debug.Assert(IsValidElementType(Type) ||
				             IsCollectionType(Type));
				Debug.Assert((IsCollection && IsValidElementType(elementType)) ||
				             (!IsCollection && elementType == null));
			}

			public bool Finish(object destination)
			{
				if (IsCollection)
				{
					field.SetValue(destination, collectionValues.ToArray(elementType));
				}

				return ReportMissingRequiredArgument();
			}

			private bool ReportMissingRequiredArgument()
			{
				if (IsRequired && !SeenValue)
				{
					if (IsDefault)
						reporter(string.Format("Missing required argument '<{0}>'.", LongName));
					else
						reporter(string.Format("Missing required argument '/{0}'.", LongName));
					return true;
				}
				return false;
			}

			private void ReportDuplicateArgumentValue(string value)
			{
				reporter(string.Format("Duplicate '{0}' argument '{1}'", LongName, value));
			}

			public bool SetValue(string value, object destination)
			{
				if (SeenValue && !AllowMultiple)
				{
					reporter(string.Format("Duplicate '{0}' argument", LongName));
					return false;
				}
				seenValue = true;

				object newValue;
				if (!ParseValue(ValueType, value, out newValue))
					return false;
				if (IsCollection)
				{
					if (Unique && collectionValues.Contains(newValue))
					{
						ReportDuplicateArgumentValue(value);
						return false;
					}
					else
					{
						collectionValues.Add(newValue);
					}
				}
				else
				{
					field.SetValue(destination, newValue);
				}

				return true;
			}

			public Type ValueType
			{
				get { return IsCollection ? elementType : Type; }
			}

			private void ReportBadArgumentValue(string value)
			{
				reporter(string.Format("'{0}' is not a valid value for the '{1}' command line option", value, LongName));
			}

			private bool ParseValue(Type type, string stringData, out object value)
			{
				// null is only valid for bool variables
				// empty string is never valid
				if ((stringData != null || type == typeof(bool)) && (stringData == null || stringData.Length > 0))
				{
					try
					{
						if (type == typeof(string))
						{
							value = stringData;
							return true;
						}
						else if (type == typeof(bool))
						{
							if (stringData == null || stringData == "+")
							{
								value = true;
								return true;
							}
							else if (stringData == "-")
							{
								value = false;
								return true;
							}
						}
						else if (type == typeof(int))
						{
							value = int.Parse(stringData);
							return true;
						}
						else if (type == typeof(uint))
						{
							value = int.Parse(stringData);
							return true;
						}
						else if (type == typeof(short))
						{
							value = short.Parse(stringData);
							return true;
						}
						else
						{
							Debug.Assert(type.IsEnum);
							value = Enum.Parse(type, stringData, true);
							return true;
						}
					}
					catch(Exception)
					{
						// catch parse errors
					}
				}

				ReportBadArgumentValue(stringData);
				value = null;
				return false;
			}

			public string LongName
			{
				get { return longName; }
			}

			public bool ExplicitShortName
			{
				get { return explicitShortName; }
			}

			public string ShortName
			{
				get { return shortName; }
			}

			public bool IsRequired
			{
				get { return 0 != (flags & CommandLineArgumentType.Required); }
			}

			public bool SeenValue
			{
				get { return seenValue; }
			}

			public bool AllowMultiple
			{
				get { return 0 != (flags & CommandLineArgumentType.Multiple); }
			}

			public bool Unique
			{
				get { return 0 != (flags & CommandLineArgumentType.Unique); }
			}

			public Type Type
			{
				get { return field.FieldType; }
			}

			public bool IsCollection
			{
				get { return IsCollectionType(Type); }
			}

			public bool IsDefault
			{
				get { return isDefault; }
			}

			public string Description
			{
				get { return description; }
				set { description = value; }
			}

			private readonly string longName;
			private readonly string shortName;
			private readonly bool explicitShortName;
			private readonly FieldInfo field;
			private readonly Type elementType;
			private readonly CommandLineArgumentType flags;
			private readonly ArrayList collectionValues;
			private readonly ErrorReporter reporter;
			private readonly bool isDefault;

			private bool seenValue;
			private string description = string.Empty;
		}

		#endregion Private Types

		#region Private Fields

		private readonly ArrayList arguments;
		private readonly Hashtable argumentMap;
		private readonly Argument defaultArgument;
		private readonly ErrorReporter reporter;
		private readonly string description = string.Empty;

		#endregion Private Fields
	}
}