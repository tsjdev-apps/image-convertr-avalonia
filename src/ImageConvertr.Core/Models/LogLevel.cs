namespace ImageConvertr.Core.Models;

/// <summary>
/// Specifies the severity level of a log message.
/// </summary>
/// <remarks>Use this enumeration to indicate the type of information being logged, such as informational
/// messages, successful operations, or error conditions. The log level can be used to filter or categorize log output
/// in logging frameworks.</remarks>
public enum LogLevel
{
	/// <summary>
	/// Indicates an informational progress update.
	/// </summary>
	Info,

	/// <summary>
	/// Indicates that an image was converted successfully.
	/// </summary>
	Success,

	/// <summary>
	/// Indicates that an image or operation failed.
	/// </summary>
	Error
}
