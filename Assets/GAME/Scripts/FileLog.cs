using System;
using System.Collections.Generic;
using System.IO;

public static class FileLog
{
	private static readonly object fileLock = new object();

	static FileLog()
	{
		var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
		_ = Directory.CreateDirectory(desktopPath);
		logPath = Path.Combine(desktopPath, "unity.txt");
	}

	public static string logPath;
	public static char indentChar = '\t';
	public static int indentLevel = 0;

	static List<string> buffer = new List<string>();

	static string IndentString()
	{
		return new string(indentChar, indentLevel);
	}

	public static void ChangeIndent(int delta)
	{
		lock (fileLock)
		{
			indentLevel = Math.Max(0, indentLevel + delta);
		}
	}

	public static void LogBuffered(string str)
	{
		lock (fileLock)
		{
			buffer.Add(IndentString() + str);
		}
	}

	public static void LogBuffered(List<string> strings)
	{
		lock (fileLock)
		{
			buffer.AddRange(strings);
		}
	}

	public static List<string> GetBuffer(bool clear)
	{
		lock (fileLock)
		{
			var result = buffer;
			if (clear)
				buffer = new List<string>();
			return result;
		}
	}

	public static void SetBuffer(List<string> buffer)
	{
		lock (fileLock)
		{
			FileLog.buffer = buffer;
		}
	}

	public static void FlushBuffer()
	{
		lock (fileLock)
		{
			if (buffer.Count > 0)
			{
				using (var writer = File.AppendText(logPath))
				{
					foreach (var str in buffer)
						writer.WriteLine(str);
				}
				buffer.Clear();
			}
		}
	}

	public static void Log(string str)
	{
		lock (fileLock)
		{
			using var writer = File.AppendText(logPath);
			writer.WriteLine(IndentString() + str);
		}
	}

	public static void Reset()
	{
		lock (fileLock)
		{
			var path = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{Path.DirectorySeparatorChar}unity.txt";
			File.WriteAllText(path, "");
		}
	}
}
