using System;
using System.Globalization;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.Threading;

namespace ConEmu.Inside
{
	/// <summary>
	///     <para>Hosts ConEmu in a native window (the WinForms <c>ConEmuControl</c> of conemu-inside, without WinForms):</para>
	///     <para>the console emulator runs as a child of <see cref="ParentWindow" /> and follows its size.</para>
	/// </summary>
	public sealed unsafe class ConEmuHost : IDisposable
	{
		private int? _nLastExitCode;

		[CanBeNull]
		private ConEmuSession _running;

		/// <param name="parentWindow">The native window (HWND) in which ConEmu runs.</param>
		public ConEmuHost(IntPtr parentWindow)
		{
			if (parentWindow == IntPtr.Zero)
				throw new ArgumentNullException(nameof(parentWindow));
			ParentWindow = parentWindow;
		}

		/// <summary>The native window in which ConEmu runs.</summary>
		public IntPtr ParentWindow { get; }

		/// <summary>Whether the status bar of ConEmu is visible.</summary>
		public bool IsStatusbarVisible { get; set; } = true;

		/// <summary>The exit code of the last console process, if any.</summary>
		public int? LastExitCode
		{
			get
			{
				ConEmuSession running = _running;
				if ((running != null) && running.IsConsoleProcessExited)
					return running.GetConsoleProcessExitCode();
				return _nLastExitCode;
			}
		}

		/// <summary>The running session, if any.</summary>
		[CanBeNull]
		public ConEmuSession RunningSession => _running;

		public States State => _running != null ? (_running.IsConsoleProcessExited ? States.ConsoleEmulatorEmpty : States.ConsoleEmulatorWithConsoleProcess) : (_nLastExitCode.HasValue ? States.Recycled : States.Unused);

		public event EventHandler StateChanged;

		/// <summary>Starts a new console emulator session with <paramref name="startinfo" />, in the style and font given.</summary>
		[NotNull]
		public ConEmuSession Start([NotNull] ConEmuStartInfo startinfo, [NotNull] JoinableTaskFactory joinableTaskFactory,
			[NotNull] string conEmuStyle, string conEmuFontName, string conEmuFontSize)
		{
			if (startinfo == null)
				throw new ArgumentNullException(nameof(startinfo));

			SetConsoleFontSize();
			SetConsoleFontName();
			SetConsoleStyle();

			_running?.CloseConsoleEmulator();
			if (_running != null)
				throw new InvalidOperationException("Cannot start a new console process because another console emulator session has failed to close in due time.");

			var session = new ConEmuSession(startinfo, new ConEmuSession.HostContext((void*)ParentWindow, IsStatusbarVisible), joinableTaskFactory);
			_running = session;
			StateChanged?.Invoke(this, EventArgs.Empty);

			session.WaitForConsoleEmulatorCloseAsync().ContinueWith(scheduler: TaskScheduler.FromCurrentSynchronizationContext(), continuationAction: task =>
			{
				try
				{
					_nLastExitCode = _running?.GetConsoleProcessExitCode();
				}
				catch (Exception)
				{
					// NOP
				}

				_running = null;
				StateChanged?.Invoke(this, EventArgs.Empty);
			}).Forget();

			return session;

			void SetConsoleFontName()
			{
				var startInfoBaseConfiguration = startinfo.BaseConfiguration;
				if (!string.IsNullOrWhiteSpace(conEmuFontName))
				{
					var nodeFontName = startInfoBaseConfiguration.SelectSingleNode("/key/key/key/value[@name='FontName']");
					if (nodeFontName?.Attributes != null)
						nodeFontName.Attributes["data"].Value = conEmuFontName;
				}

				startinfo.BaseConfiguration = startInfoBaseConfiguration;
			}

			void SetConsoleFontSize()
			{
				var startInfoBaseConfiguration = startinfo.BaseConfiguration;
				if (!string.IsNullOrWhiteSpace(conEmuFontSize) && int.TryParse(conEmuFontSize, out var fontSize))
				{
					var nodeFontSize = startInfoBaseConfiguration.SelectSingleNode("/key/key/key/value[@name='FontSize']");
					if (nodeFontSize?.Attributes != null)
						nodeFontSize.Attributes["data"].Value = fontSize.ToString("X8", CultureInfo.InvariantCulture);
				}

				startinfo.BaseConfiguration = startInfoBaseConfiguration;
			}

			void SetConsoleStyle()
			{
				if (conEmuStyle != "Default")
					startinfo.ConsoleProcessExtraArgs = " -new_console:P:\"" + conEmuStyle + "\"";
			}
		}

		public void Dispose()
		{
			if (_running != null)
			{
				try
				{
					_running.CloseConsoleEmulator();
				}
				catch (Exception)
				{
					// Nothing to do
				}
			}
		}
	}
}
