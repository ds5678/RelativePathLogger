using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.IO;
using System.Text;

namespace RelativePathLogger
{
	internal static class Program
	{
		private const string Child = "|---";
		private const string NotChild = "|   ";
		static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				Console.WriteLine("This program takes exactly one argument: a directory to log all subcontents.");
				Console.ReadKey();
				return;
			}
			string rootFolderPath = args[0];
			if (string.IsNullOrWhiteSpace(rootFolderPath))
			{
				Console.WriteLine("Directory path cannot be empty");
				Console.ReadKey();
				return;
			}
			bool deleteDirectory;
			if (Directory.Exists(rootFolderPath))
			{
				deleteDirectory = false;
			}
			else if (File.Exists(rootFolderPath))
			{
				// Assume the file is a compressed archive and try to extract it to a temporary directory
				string? directoryName = Path.GetFileName(rootFolderPath);
				if (string.IsNullOrWhiteSpace(directoryName))
				{
					directoryName = Guid.NewGuid().ToString();
				}
				string tempDirectory = Path.Join("temp", directoryName);
				Directory.CreateDirectory(tempDirectory);
				try
				{
					using var reader = ReaderFactory.Open(rootFolderPath);
					reader.WriteAllToDirectory(tempDirectory, new ExtractionOptions()
					{
						ExtractFullPath = true,
						Overwrite = true
					});
				}
				catch (Exception e)
				{
					Console.WriteLine($"Failed to extract archive: {e}");
					Console.ReadKey();
					return;
				}

				deleteDirectory = true;
				rootFolderPath = tempDirectory;
			}
			else
			{
				Console.WriteLine("Invalid directory");
				Console.ReadKey();
				return;
			}
			string logFilePath = "Output.txt";
			File.Create(logFilePath).Close();
			try
			{
				ProcessDirectory(logFilePath, new DirectoryInfo(rootFolderPath), -1);
				Console.WriteLine("Done");
			}
			catch(Exception e)
			{
				Console.WriteLine(e);
			}
			finally
			{
				if (deleteDirectory)
				{
					try
					{
						Directory.Delete(rootFolderPath, true);
					}
					catch (Exception e)
					{
						Console.WriteLine($"Failed to delete temporary directory: {e}");
					}
				}
			}

			Console.ReadKey();
		}

		private static void ProcessDirectory(string logFilePath, DirectoryInfo directoryInfo, int depth)
		{
			Log(logFilePath, GetString(directoryInfo, depth));

			foreach (FileInfo file in directoryInfo.GetFiles())
			{
				Log(logFilePath, GetString(file, depth + 1));
			}
			foreach (DirectoryInfo childDirectory in directoryInfo.GetDirectories())
			{
				ProcessDirectory(logFilePath, childDirectory, depth + 1);
			}
		}

		private static void Log(string logFilePath, string text)
		{
			Console.Write(text);
			File.AppendAllText(logFilePath, text);
		}

		private static string GetString(DirectoryInfo directoryInfo, int depth)
		{
			if (depth < 0) return directoryInfo.Name + Environment.NewLine;

			StringBuilder builder = new();

			if (depth == 0)
			{
				builder.Append(Child);
			}	
			else
			{
				builder.AppendMultiple(NotChild, depth);
				builder.Append(Child);
			}

			builder.Append(directoryInfo.Name);
			builder.Append(Environment.NewLine);

			return builder.ToString();
		}

		private static string GetString(FileInfo fileInfo, int depth)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(depth);
			StringBuilder builder = new();

			if (depth == 0)
			{
				builder.Append(Child);
			}
			else
			{
				builder.AppendMultiple(NotChild, depth);
				builder.Append(Child);
			}

			builder.Append(fileInfo.Name);
			builder.Append("  -  ");
			builder.Append(fileInfo.Length);
			builder.Append(" bytes");
			builder.Append(Environment.NewLine);

			return builder.ToString();
		}
	}

	internal static class Extensions
	{
		internal static void AppendMultiple(this StringBuilder builder, string message, int count)
		{
			for (int i = 0; i < count; i++)
				builder.Append(message);
		}
	}
}
