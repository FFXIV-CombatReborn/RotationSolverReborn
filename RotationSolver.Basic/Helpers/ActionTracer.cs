using ECommons.DalamudServices;
using ECommons.Logging;
using System.Text;

namespace RotationSolver.Basic.Helpers;

/// <summary>
/// Debug-only tracer that logs every action evaluation made by the rotation solver.
/// Disabled by default; gated by <c>Service.Config.EnableActionTracer</c>. When enabled, writes
/// one line per Try/Reject/Accept event to a per-session file under the plugin config directory,
/// with an optional mirror to Dalamud's plugin log.
/// </summary>
internal static class ActionTracer
{
	private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

	private const int MaxRetainedFiles = 10;

	private static StreamWriter? _writer;
	private static readonly object _lock = new();
	private static long _frameCounter;
	private static string? _traceDirectory;
	private static string? _currentFilePath;
	private static readonly StringBuilder _currentFrame = new();

	internal static string? CurrentFilePath => _currentFilePath;

	internal static string? TraceDirectory => _traceDirectory;

	internal static string? LastFrameSummary { get; private set; }

	internal static bool Enabled;

	internal static bool MirrorToPluginLog;

	internal static void Init()
	{
		try
		{
			_traceDirectory = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "Traces");
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"[ActionTracer] Init failed to resolve trace directory: {ex.Message}");
			_traceDirectory = null;
		}
	}

	internal static void Shutdown()
	{
		lock (_lock)
		{
			try
			{
				_writer?.Flush();
				_writer?.Dispose();
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"[ActionTracer] Shutdown error: {ex.Message}");
			}
			finally
			{
				_writer = null;
				_currentFilePath = null;
			}
		}
	}

	internal static void ClearTrace()
	{
		string? dir;
		lock (_lock)
		{
			dir = _traceDirectory;
			try
			{
				_writer?.Flush();
				_writer?.Dispose();
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"[ActionTracer] ClearTrace dispose error: {ex.Message}");
			}
			finally
			{
				_writer = null;
				_currentFilePath = null;
				_currentFrame.Clear();
				LastFrameSummary = null;
				_frameCounter = 0;
			}
		}

		if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;

		foreach (string path in Directory.EnumerateFiles(dir, "actiontrace_*.log"))
		{
			try
			{
				File.Delete(path);
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"[ActionTracer] ClearTrace delete failed for '{path}': {ex.Message}");
			}
		}
	}

	internal static bool HasAnyTraceFiles()
	{
		if (string.IsNullOrEmpty(_traceDirectory) || !Directory.Exists(_traceDirectory)) return false;
		try
		{
			foreach (string _ in Directory.EnumerateFiles(_traceDirectory, "actiontrace_*.log"))
			{
				return true;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	internal static void BeginFrame()
	{
		if (!Enabled) return;
		long n = Interlocked.Increment(ref _frameCounter);
		_currentFrame.Clear();
		Write($"---- frame {n} ----");
	}

	internal static void EndFrame(IAction? chosen)
	{
		if (!Enabled) return;
		Write($"=> chose {Format(chosen)}");
		LastFrameSummary = _currentFrame.ToString();
	}

	internal static void Try(IBaseAction a)
	{
		if (!Enabled) return;
		Write($"TRY    {Format(a)}");
	}

	internal static bool Reject(IBaseAction a, string reason)
	{
		if (!Enabled) return false;
		Write($"REJECT {Format(a)} {reason}");
		return false;
	}

	internal static void Accept(IBaseAction a)
	{
		if (!Enabled) return;
		Write($"ACCEPT {Format(a)}");
	}

	private static string Format(IAction? a) => a == null ? "<null>" : $"{a.Name}({a.ID})";

	private static void Write(string body)
	{
		string line = $"{DateTime.Now:HH:mm:ss.fff} {body}";
		_currentFrame.AppendLine(line);

		lock (_lock)
		{
			try
			{
				EnsureWriter();
				_writer?.WriteLine(line);

				if (_writer != null && _writer.BaseStream.Length >= MaxFileSizeBytes)
				{
					RolloverWriter();
				}
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"[ActionTracer] write failed: {ex.Message}");
			}
		}

		if (MirrorToPluginLog)
		{
			PluginLog.Debug($"[ActionTracer] {line}");
		}
	}

	private static void RolloverWriter()
	{
		try
		{
			_writer?.WriteLine($"# rollover: file reached {MaxFileSizeBytes / (1024 * 1024)} MB, continuing in next file");
			_writer?.Flush();
			_writer?.Dispose();
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"[ActionTracer] rollover dispose error: {ex.Message}");
		}
		finally
		{
			_writer = null;
			_currentFilePath = null;
		}
	}

	private static void EnsureWriter()
	{
		if (_writer != null || _traceDirectory == null) return;

		Directory.CreateDirectory(_traceDirectory);
		// Millisecond precision in the filename avoids collisions when rollover happens twice in one second.
		string path = Path.Combine(_traceDirectory, $"actiontrace_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log");
		_writer = new StreamWriter(path, append: false) { AutoFlush = true };
		_writer.WriteLine($"# RotationSolverReborn action trace started {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} (max size {MaxFileSizeBytes / (1024 * 1024)} MB before rollover)");
		_currentFilePath = path;

		PruneOldFiles();
	}

	private static void PruneOldFiles()
	{
		if (string.IsNullOrEmpty(_traceDirectory)) return;
		try
		{
			FileInfo[] files = new DirectoryInfo(_traceDirectory).GetFiles("actiontrace_*.log");
			int excess = files.Length - MaxRetainedFiles;
			if (excess <= 0) return;

			Array.Sort(files, static (a, b) => a.CreationTimeUtc.CompareTo(b.CreationTimeUtc));

			for (int i = 0; i < excess; i++)
			{
				try
				{
					files[i].Delete();
				}
				catch (Exception ex)
				{
					PluginLog.Warning($"[ActionTracer] prune failed for '{files[i].FullName}': {ex.Message}");
				}
			}
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"[ActionTracer] prune enumeration failed: {ex.Message}");
		}
	}
}
