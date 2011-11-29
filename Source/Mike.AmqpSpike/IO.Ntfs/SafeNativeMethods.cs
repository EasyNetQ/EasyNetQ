using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Mike.AmqpSpike.IO.Ntfs
{
    /// <summary>
	/// Safe native methods.
	/// </summary>
	internal static class SafeNativeMethods
	{
		#region Constants and flags

		public const int MaxPath = 256;
		private const string LongPathPrefix = @"\\?\";
		public const char StreamSeparator = ':';
		public const int DefaultBufferSize = 0x1000;

		private const int ErrorFileNotFound = 2;

		// "Characters whose integer representations are in the range from 1 through 31, 
		// except for alternate streams where these characters are allowed"
		// http://msdn.microsoft.com/en-us/library/aa365247(v=VS.85).aspx
		private static readonly char[] InvalidStreamNameChars = Path.GetInvalidFileNameChars().Where(c => c < 1 || c > 31).ToArray();

		[Flags]
		public enum NativeFileFlags : uint
		{
			WriteThrough		= 0x80000000,
			Overlapped			= 0x40000000,
			NoBuffering			= 0x20000000,
			RandomAccess		= 0x10000000,
			SequentialScan		= 0x8000000,
			DeleteOnClose		= 0x4000000,
			BackupSemantics		= 0x2000000,
			PosixSemantics		= 0x1000000,
			OpenReparsePoint	= 0x200000,
			OpenNoRecall		= 0x100000
		}

		[Flags]
		public enum NativeFileAccess : uint
		{
			GenericRead		= 0x80000000,
			GenericWrite	= 0x40000000
		}

		#endregion

		#region P/Invoke Structures

		[StructLayout(LayoutKind.Sequential)]
		private struct LargeInteger
		{
			public readonly int Low;
			public readonly int High;

			public long ToInt64()
			{
				return (this.High * 0x100000000) + this.Low;
			}

			/*
			public static LargeInteger FromInt64(long value)
			{
				return new LargeInteger
				{
					Low = (int)(value & 0x11111111),
					High = (int)((value / 0x100000000) & 0x11111111)
				};
			}
			*/
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct Win32StreamId
		{
			public readonly int StreamId;
			public readonly int StreamAttributes;
			public LargeInteger Size;
			public readonly int StreamNameSize;
		}

/*
		[StructLayout(LayoutKind.Sequential)]
		private struct FileInformationByHandle
		{
			public int dwFileAttributes;
			public LargeInteger ftCreationTime;
			public LargeInteger ftLastAccessTime;
			public LargeInteger ftLastWriteTime;
			public int dwVolumeSerialNumber;
			public LargeInteger FileSize;
			public int nNumberOfLinks;
			public LargeInteger FileIndex;
		}
*/

		#endregion

		#region P/Invoke Methods

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
		private static extern int FormatMessage(
			int dwFlags, 
			IntPtr lpSource, 
			int dwMessageId, 
			int dwLanguageId, 
			StringBuilder lpBuffer, 
			int nSize, 
			IntPtr vaListArguments);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern int GetFileAttributes(string fileName);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetFileSizeEx(SafeFileHandle handle, out LargeInteger size);

		[DllImport("kernel32.dll")]
		private static extern int GetFileType(SafeFileHandle handle);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern SafeFileHandle CreateFile(
			string name,
			NativeFileAccess access,
			FileShare share,
			IntPtr security,
			FileMode mode,
			NativeFileFlags flags,
			IntPtr template);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteFile(string name);

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool BackupRead(
			SafeFileHandle hFile,
			ref Win32StreamId pBuffer,
			int numberOfBytesToRead,
			out int numberOfBytesRead,
			[MarshalAs(UnmanagedType.Bool)] bool abort,
			[MarshalAs(UnmanagedType.Bool)] bool processSecurity,
			ref IntPtr context);

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool BackupRead(
			SafeFileHandle hFile,
			SafeHGlobalHandle pBuffer,
			int numberOfBytesToRead,
			out int numberOfBytesRead,
			[MarshalAs(UnmanagedType.Bool)] bool abort,
			[MarshalAs(UnmanagedType.Bool)] bool processSecurity,
			ref IntPtr context);

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool BackupSeek(
			SafeFileHandle hFile,
			int bytesToSeekLow,
			int bytesToSeekHigh,
			out int bytesSeekedLow,
			out int bytesSeekedHigh,
			ref IntPtr context);

		#endregion

		#region Utility Structures

		public struct Win32StreamInfo
		{
			public FileStreamType StreamType;
			public FileStreamAttributes StreamAttributes;
			public long StreamSize;
			public string StreamName;
		}

		#endregion

		#region Utility Methods

		private static int MakeHRFromErrorCode(int errorCode)
		{
			return (-2147024896 | errorCode);
		}

		private static string GetErrorMessage(int errorCode)
		{
			var lpBuffer = new StringBuilder(0x200);
			if (0 != FormatMessage(0x3200, IntPtr.Zero, errorCode, 0, lpBuffer, lpBuffer.Capacity, IntPtr.Zero))
			{
				return lpBuffer.ToString();
			}

			return string.Format(Resources.Error_UnknownError, errorCode);
		}

		private static void ThrowIOError(int errorCode, string path)
		{
			switch (errorCode)
			{
				case 0:
				{
					break;
				}
				case 2: // File not found
				{
					if (string.IsNullOrEmpty(path)) throw new FileNotFoundException();
					throw new FileNotFoundException(null, path);
				}
				case 3: // Directory not found
				{
					if (string.IsNullOrEmpty(path)) throw new DirectoryNotFoundException();
					throw new DirectoryNotFoundException(string.Format(Resources.Culture, Resources.Error_DirectoryNotFound, path));
				}
				case 5: // Access denied
				{
					if (string.IsNullOrEmpty(path)) throw new UnauthorizedAccessException();
					throw new UnauthorizedAccessException(string.Format(Resources.Error_AccessDenied_Path, path));
				}
				case 15: // Drive not found
				{
					if (string.IsNullOrEmpty(path)) throw new DriveNotFoundException();
					throw new DriveNotFoundException(string.Format(Resources.Culture, Resources.Error_DriveNotFound, path));
				}
				case 32: // Sharing violation
				{
					if (string.IsNullOrEmpty(path)) throw new IOException(GetErrorMessage(errorCode), MakeHRFromErrorCode(errorCode));
					throw new IOException(string.Format(Resources.Culture, Resources.Error_SharingViolation, path), MakeHRFromErrorCode(errorCode));
				}
				case 80: // File already exists
				{
					if (!string.IsNullOrEmpty(path))
					{
						throw new IOException(string.Format(Resources.Culture, Resources.Error_FileAlreadyExists, path), MakeHRFromErrorCode(errorCode));
					}
					break;
				}
				case 87: // Invalid parameter
				{
					throw new IOException(GetErrorMessage(errorCode), MakeHRFromErrorCode(errorCode));
				}
				case 183: // File or directory already exists
				{
					if (!string.IsNullOrEmpty(path))
					{
						throw new IOException(string.Format(Resources.Culture, Resources.Error_AlreadyExists, path), MakeHRFromErrorCode(errorCode));
					}
					break;
				}
				case 206: // Path too long
				{
					throw new PathTooLongException();
				}
				case 995: // Operation cancelled
				{
					throw new OperationCanceledException();
				}
				default:
				{
					Marshal.ThrowExceptionForHR(MakeHRFromErrorCode(errorCode));
					break;
				}
			}
		}

		public static void ThrowLastIOError(string path)
		{
			int errorCode = Marshal.GetLastWin32Error();
			if (0 != errorCode)
			{
				int hr = Marshal.GetHRForLastWin32Error();
				if (0 <= hr) throw new Win32Exception(errorCode);
				ThrowIOError(errorCode, path);
			}
		}

		public static NativeFileAccess ToNative(this FileAccess access)
		{
			NativeFileAccess result = 0;
			if (FileAccess.Read == (FileAccess.Read & access)) result |= NativeFileAccess.GenericRead;
			if (FileAccess.Write == (FileAccess.Write & access)) result |= NativeFileAccess.GenericWrite;
			return result;
		}

		public static string BuildStreamPath(string filePath, string streamName)
		{
			string result = filePath;
			if (!string.IsNullOrEmpty(filePath))
			{
				if (1 == result.Length) result = ".\\" + result;
				result += StreamSeparator + streamName + StreamSeparator + "$DATA";
				if (MaxPath <= result.Length) result = LongPathPrefix + result;
			}
			return result;
		}

		public static void ValidateStreamName(string streamName)
		{
			if (!string.IsNullOrEmpty(streamName) && -1 != streamName.IndexOfAny(InvalidStreamNameChars))
			{
				throw new ArgumentException(Resources.Error_InvalidFileChars);
			}
		}

		public static int SafeGetFileAttributes(string name)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

			int result = GetFileAttributes(name);
			if (-1 == result)
			{
				int errorCode = Marshal.GetLastWin32Error();
				if (ErrorFileNotFound != errorCode) ThrowLastIOError(name);
			}

			return result;
		}

		public static bool SafeDeleteFile(string name)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

			bool result = DeleteFile(name);
			if (!result)
			{
				int errorCode = Marshal.GetLastWin32Error();
				if (ErrorFileNotFound != errorCode) ThrowLastIOError(name);
			}

			return result;
		}

		public static SafeFileHandle SafeCreateFile(string path, NativeFileAccess access, FileShare share, IntPtr security, FileMode mode, NativeFileFlags flags, IntPtr template)
		{
			SafeFileHandle result = CreateFile(path, access, share, security, mode, flags, template);
			if (!result.IsInvalid && 1 != GetFileType(result))
			{
				result.Dispose();
				throw new NotSupportedException(string.Format(Resources.Culture,
					Resources.Error_NonFile, path));
			}

			return result;
		}

		private static long GetFileSize(string path, SafeFileHandle handle)
		{
			long result = 0L;
			if (null != handle && !handle.IsInvalid)
			{
				LargeInteger value;
				if (GetFileSizeEx(handle, out value))
				{
					result = value.ToInt64();
				}
				else
				{
					ThrowLastIOError(path);
				}
			}

			return result;
		}

		public static long GetFileSize(string path)
		{
			long result = 0L;
			if (!string.IsNullOrEmpty(path))
			{
				using (SafeFileHandle handle = SafeCreateFile(path, NativeFileAccess.GenericRead, FileShare.Read, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero))
				{
					result = GetFileSize(path, handle);
				}
			}

			return result;
		}

		public static IList<Win32StreamInfo> ListStreams(string filePath)
		{
			if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException("filePath");
			if (-1 != filePath.IndexOfAny(Path.GetInvalidPathChars())) throw new ArgumentException(Resources.Error_InvalidFileChars, "filePath");

			var result = new List<Win32StreamInfo>();

			using (SafeFileHandle hFile = SafeCreateFile(filePath, NativeFileAccess.GenericRead, FileShare.Read, IntPtr.Zero, FileMode.Open, NativeFileFlags.BackupSemantics, IntPtr.Zero))
			using (var hName = new StreamName())
			{
				if (!hFile.IsInvalid)
				{
					var streamId = new Win32StreamId();
					int dwStreamHeaderSize = Marshal.SizeOf(streamId);
					bool finished = false;
					IntPtr context = IntPtr.Zero;
					int bytesRead;
					string name;

					try
					{
						while (!finished)
						{
							// Read the next stream header:
							if (!BackupRead(hFile, ref streamId, dwStreamHeaderSize, out bytesRead, false, false, ref context))
							{
								finished = true;
							}
							else if (dwStreamHeaderSize != bytesRead)
							{
								finished = true;
							}
							else
							{
								// Read the stream name:
								if (0 >= streamId.StreamNameSize)
								{
									name = null;
								}
								else
								{
									hName.EnsureCapacity(streamId.StreamNameSize);
									if (!BackupRead(hFile, hName.MemoryBlock, streamId.StreamNameSize, out bytesRead, false, false, ref context))
									{
										name = null;
										finished = true;
									}
									else
									{
										// Unicode chars are 2 bytes:
										name = hName.ReadStreamName(bytesRead >> 1);
									}
								}

								// Add the stream info to the result:
								if (!string.IsNullOrEmpty(name))
								{
									result.Add(new Win32StreamInfo
									{
										StreamType = (FileStreamType)streamId.StreamId,
										StreamAttributes = (FileStreamAttributes)streamId.StreamAttributes,
										StreamSize = streamId.Size.ToInt64(),
										StreamName = name
									});
								}

								// Skip the contents of the stream:
								int bytesSeekedLow, bytesSeekedHigh;
								if (!finished && !BackupSeek(hFile, streamId.Size.Low, streamId.Size.High, out bytesSeekedLow, out bytesSeekedHigh, ref context))
								{
									finished = true;
								}
							}
						}
					}
					finally
					{
						// Abort the backup:
						BackupRead(hFile, hName.MemoryBlock, 0, out bytesRead, true, false, ref context);
					}
				}
			}

			return result;
		}

		#endregion
	}
}
