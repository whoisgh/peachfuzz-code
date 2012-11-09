﻿
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using NLog;

namespace Peach.Core.Debuggers.WindowsSystem
{
	/// <summary>
	/// Callback to handle an A/V exception
	/// </summary>
	/// <param name="?"></param>
	public delegate void HandleAccessViolation(UnsafeMethods.DEBUG_EVENT e);

	/// <summary>
	/// Callback to handle a breakpoint
	/// </summary>
	/// <param name="?"></param>
	public delegate void HandleBreakpoint(UnsafeMethods.DEBUG_EVENT e);

	/// <summary>
	/// Callback to handle a breakpoint
	/// </summary>
	/// <param name="?"></param>
	public delegate void HandleLoadDll(UnsafeMethods.DEBUG_EVENT e, string moduleName);

	/// <summary>
	/// Called every second to check if we should continue debugging
	/// process.
	/// </summary>
	/// <returns></returns>
	public delegate bool ContinueDebugging();

	/// <summary>
	/// A lightweight Windows debugger written using the 
	/// system debugger APIs.
	/// </summary>
	/// <remarks>
	/// This debugger does not support symbols or other usefull 
	/// things.  When a crash is located using the system debugger
	/// it should be reproduced using the Windows Debug Engine to
	/// gather more information.
	/// </remarks>
	public class SystemDebugger
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		#region Constants

		public const uint DEBUG_ONLY_THIS_PROCESS = 0x00000002;
		public const uint DEBUG_PROCESS = 0x00000001;
		public const uint INFINITE = 0;
		public const uint DBG_CONTINUE = 0x00010002;
		public const uint DBG_EXCEPTION_NOT_HANDLED = 0x80010001;

		public const uint STATUS_GUARD_PAGE_VIOLATION = 0x80000001;
		public const uint STATUS_DATATYPE_MISALIGNMENT = 0x80000002;
		public const uint STATUS_BREAKPOINT = 0x80000003;
		public const uint STATUS_SINGLE_STEP = 0x80000004;
		public const uint STATUS_LONGJUMP = 0x80000026;
		public const uint STATUS_UNWIND_CONSOLIDATE = 0x80000029;
		public const uint STATUS_ACCESS_VIOLATION = 0xC0000005;
		public const uint STATUS_IN_PAGE_ERROR = 0xC0000006;
		public const uint STATUS_INVALID_HANDLE = 0xC0000008;
		public const uint STATUS_INVALID_PARAMETER = 0xC000000D;
		public const uint STATUS_NO_MEMORY = 0xC0000017;
		public const uint STATUS_ILLEGAL_INSTRUCTION = 0xC000001D;
		public const uint STATUS_NONCONTINUABLE_EXCEPTION = 0xC0000025;
		public const uint STATUS_INVALID_DISPOSITION = 0xC0000026;
		public const uint STATUS_ARRAY_BOUNDS_EXCEEDED = 0xC000008C;
		public const uint STATUS_FLOAT_DENORMAL_OPERAND = 0xC000008D;
		public const uint STATUS_FLOAT_DIVIDE_BY_ZERO = 0xC000008E;
		public const uint STATUS_FLOAT_INEXACT_RESULT = 0xC000008F;
		public const uint STATUS_FLOAT_INVALID_OPERATION = 0xC0000090;
		public const uint STATUS_FLOAT_OVERFLOW = 0xC0000091;
		public const uint STATUS_FLOAT_STACK_CHECK = 0xC0000092;
		public const uint STATUS_FLOAT_UNDERFLOW = 0xC0000093;
		public const uint STATUS_INTEGER_DIVIDE_BY_ZERO = 0xC0000094;
		public const uint STATUS_INTEGER_OVERFLOW = 0xC0000095;
		public const uint STATUS_PRIVILEGED_INSTRUCTION = 0xC0000096;
		public const uint STATUS_STACK_OVERFLOW = 0xC00000FD;
		public const uint STATUS_DLL_NOT_FOUND = 0xC0000135;
		public const uint STATUS_ORDINAL_NOT_FOUND = 0xC0000138;
		public const uint STATUS_ENTRYPOINT_NOT_FOUND = 0xC0000139;
		public const uint STATUS_CONTROL_C_EXIT = 0xC000013A;
		public const uint STATUS_DLL_INIT_FAILED = 0xC0000142;
		public const uint STATUS_FLOAT_MULTIPLE_FAULTS = 0xC00002B4;
		public const uint STATUS_FLOAT_MULTIPLE_TRAPS = 0xC00002B5;
		public const uint STATUS_REG_NAT_CONSUMPTION = 0xC00002C9;
		public const uint STATUS_STACK_BUFFER_OVERRUN = 0xC0000409;
		public const uint STATUS_INVALID_CRUNTIME_PARAMETER = 0xC0000417;
		public const uint STATUS_POSSIBLE_DEADLOCK = 0xC0000194;

		public const uint DBG_CONTROL_C = 0x40010005;
		public const uint EXCEPTION_ACCESS_VIOLATION = STATUS_ACCESS_VIOLATION;
		public const uint EXCEPTION_DATATYPE_MISALIGNMENT = STATUS_DATATYPE_MISALIGNMENT;
		public const uint EXCEPTION_BREAKPOINT = STATUS_BREAKPOINT;
		public const uint EXCEPTION_SINGLE_STEP = STATUS_SINGLE_STEP;
		public const uint EXCEPTION_ARRAY_BOUNDS_EXCEEDED = STATUS_ARRAY_BOUNDS_EXCEEDED;
		public const uint EXCEPTION_FLT_DENORMAL_OPERAND = STATUS_FLOAT_DENORMAL_OPERAND;
		public const uint EXCEPTION_FLT_DIVIDE_BY_ZERO = STATUS_FLOAT_DIVIDE_BY_ZERO;
		public const uint EXCEPTION_FLT_INEXACT_RESULT = STATUS_FLOAT_INEXACT_RESULT;
		public const uint EXCEPTION_FLT_INVALID_OPERATION = STATUS_FLOAT_INVALID_OPERATION;
		public const uint EXCEPTION_FLT_OVERFLOW = STATUS_FLOAT_OVERFLOW;
		public const uint EXCEPTION_FLT_STACK_CHECK = STATUS_FLOAT_STACK_CHECK;
		public const uint EXCEPTION_FLT_UNDERFLOW = STATUS_FLOAT_UNDERFLOW;
		public const uint EXCEPTION_INT_DIVIDE_BY_ZERO = STATUS_INTEGER_DIVIDE_BY_ZERO;
		public const uint EXCEPTION_INT_OVERFLOW = STATUS_INTEGER_OVERFLOW;
		public const uint EXCEPTION_PRIV_INSTRUCTION = STATUS_PRIVILEGED_INSTRUCTION;
		public const uint EXCEPTION_IN_PAGE_ERROR = STATUS_IN_PAGE_ERROR;
		public const uint EXCEPTION_ILLEGAL_INSTRUCTION = STATUS_ILLEGAL_INSTRUCTION;
		public const uint EXCEPTION_NONCONTINUABLE_EXCEPTION = STATUS_NONCONTINUABLE_EXCEPTION;
		public const uint EXCEPTION_STACK_OVERFLOW = STATUS_STACK_OVERFLOW;
		public const uint EXCEPTION_INVALID_DISPOSITION = STATUS_INVALID_DISPOSITION;
		public const uint EXCEPTION_GUARD_PAGE = STATUS_GUARD_PAGE_VIOLATION;
		public const uint EXCEPTION_INVALID_HANDLE = STATUS_INVALID_HANDLE;
		public const uint EXCEPTION_POSSIBLE_DEADLOCK = STATUS_POSSIBLE_DEADLOCK;

		#endregion

		public HandleAccessViolation HandleAccessViolation = null;
		public HandleBreakpoint HandleBreakPoint = null;
		public HandleLoadDll HandleLoadDll = null;
		public ContinueDebugging ContinueDebugging = () => { return true; };

		public static SystemDebugger CreateProcess(string command)
		{
			// CreateProcess
			UnsafeMethods.STARTUPINFO startUpInfo = new UnsafeMethods.STARTUPINFO();
			UnsafeMethods.PROCESS_INFORMATION processInformation = new UnsafeMethods.PROCESS_INFORMATION();

			if (!UnsafeMethods.CreateProcess(
					null,			// lpApplicationName 
					command,		// lpCommandLine 
					0,				// lpProcessAttributes 
					0,				// lpThreadAttributes 
					false,			// bInheritHandles 
					1,				// dwCreationFlags, DEBUG_PROCESS
					IntPtr.Zero,	// lpEnvironment 
					null,			// lpCurrentDirectory 
					ref startUpInfo, // lpStartupInfo 
					out processInformation)) // lpProcessInformation 
				throw new Exception("Failed to create new process and attach debugger.");

			UnsafeMethods.CloseHandle(processInformation.hProcess);
			UnsafeMethods.CloseHandle(processInformation.hThread);
			UnsafeMethods.DebugSetProcessKillOnExit(true);

			return new SystemDebugger(startUpInfo, processInformation);
		}

		public static SystemDebugger AttachToProcess(int dwProcessId)
		{
			// DebugActiveProcess
			if (!UnsafeMethods.DebugActiveProcess((uint)dwProcessId))
				throw new Exception("Can't attach to process " + dwProcessId + ".");

			UnsafeMethods.DebugSetProcessKillOnExit(true);

			return new SystemDebugger(dwProcessId);
		}

		public int dwProcessId = 0;
		public bool processExit = false;
		public bool verbose = false;
		UnsafeMethods.STARTUPINFO _startUpInfo;
		UnsafeMethods.PROCESS_INFORMATION _processInformation;
		public ManualResetEvent processStarted = new ManualResetEvent(false);

		public Dictionary<ulong, byte> _breakpointOrigionalInstructions = new Dictionary<ulong, byte>();
		public Dictionary<string, IntPtr> _moduleBaseAddresses = new Dictionary<string, IntPtr>();

		protected SystemDebugger(int dwProcessId)
		{
			this.dwProcessId = dwProcessId;
		}

		protected SystemDebugger(UnsafeMethods.STARTUPINFO startUpInfo, UnsafeMethods.PROCESS_INFORMATION processInformation)
		{
			_startUpInfo = startUpInfo;
			_processInformation = processInformation;
			dwProcessId = processInformation.dwProcessId;
		}

		public bool Verbose
		{
			get { return verbose; }
			set { verbose = value; }
		}

		public void MainLoop()
		{
			UnsafeMethods.DEBUG_EVENT debug_event;
			processStarted.Set();

			while (!processExit && ContinueDebugging())
			{
				if (!UnsafeMethods.WaitForDebugEvent(out debug_event, 100))
					continue;

				uint dwContinueStatus = ProcessDebugEvent(ref debug_event);

				if (!UnsafeMethods.ContinueDebugEvent(debug_event.dwProcessId,
									debug_event.dwThreadId, dwContinueStatus))
					throw new Exception("ContinueDebugEvent failed");
			}
		}

		public void Close()
		{
			processStarted.Close();
		}

		protected uint ProcessDebugEvent(ref UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			uint dwContinueStatus = DBG_EXCEPTION_NOT_HANDLED;

			switch (DebugEv.dwDebugEventCode)
			{
				case UnsafeMethods.DebugEventType.EXCEPTION_DEBUG_EVENT:
					// Process the exception code. When handling 
					// exceptions, remember to set the continuation 
					// status parameter (dwContinueStatus). This value 
					// is used by the ContinueDebugEvent function. 
					logger.Trace("EXCEPTION_DEBUG_EVENT");
					dwContinueStatus = OnExceptionDebugEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.CREATE_THREAD_DEBUG_EVENT:
					// As needed, examine or change the thread's registers 
					// with the GetThreadContext and SetThreadContext functions; 
					// and suspend and resume thread execution with the 
					// SuspendThread and ResumeThread functions. 

					logger.Trace("CREATE_THREAD_DEBUG_EVENT");
					dwContinueStatus = OnCreateThreadDebugEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.CREATE_PROCESS_DEBUG_EVENT:
					// As needed, examine or change the registers of the
					// process's initial thread with the GetThreadContext and
					// SetThreadContext functions; read from and write to the
					// process's virtual memory with the ReadProcessMemory and
					// WriteProcessMemory functions; and suspend and resume
					// thread execution with the SuspendThread and ResumeThread
					// functions. Be sure to close the handle to the process image
					// file with CloseHandle.

					logger.Trace("CREATE_PROCESS_DEBUG_EVENT");
					dwContinueStatus = OnCreateProcessDebugEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.EXIT_THREAD_DEBUG_EVENT:
					// Display the thread's exit code. 

					logger.Trace("EXIT_PROCESS_DEBUG_EVENT");
					dwContinueStatus = OnExitThreadDebugEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.EXIT_PROCESS_DEBUG_EVENT:
					// Display the process's exit code. 

					logger.Trace("EXIT_PROCESS_DEBUG_EVENT");
					dwContinueStatus = OnExitProcessDebugEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.LOAD_DLL_DEBUG_EVENT:
					// Read the debugging information included in the newly 
					// loaded DLL. Be sure to close the handle to the loaded DLL 
					// with CloseHandle.

					logger.Trace("LOAD_DLL_DEBUG_EVENT");
					dwContinueStatus = OnLoadDllDebugEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.UNLOAD_DLL_DEBUG_EVENT:
					// Display a message that the DLL has been unloaded. 

					logger.Trace("UNLOAD_DLL_DEBUG_EVENT");
					dwContinueStatus = OnUnloadDllDebugEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.OUTPUT_DEBUG_STRING_EVENT:
					// Display the output debugging string. 

					logger.Trace("OUTPUT_DEBUG_STRING_EVENT");
					dwContinueStatus = OnOutputDebugStringEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.RIP_EVENT:
					logger.Trace("RIP_EVENT");
					dwContinueStatus = OnRipEvent(DebugEv);
					break;

				default:
					logger.Trace("UNKNOWN DEBUG EVENT");
					break;
			}

			return dwContinueStatus;
		}

		private uint OnRipEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			return DBG_CONTINUE;
		}

		private uint OnOutputDebugStringEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			return DBG_CONTINUE;
		}

		private uint OnUnloadDllDebugEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			return DBG_CONTINUE;
		}

		private uint OnLoadDllDebugEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			var LoadDll = DebugEv.u.LoadDll;
			UnsafeMethods.CloseHandle(LoadDll.hFile);
			return DBG_CONTINUE;
		}

		private uint OnExitThreadDebugEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			return DBG_CONTINUE;
		}

		private uint OnCreateProcessDebugEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			var CreateProcessInfo = DebugEv.u.CreateProcessInfo;
			UnsafeMethods.CloseHandle(CreateProcessInfo.hFile);
			UnsafeMethods.CloseHandle(CreateProcessInfo.hProcess);
			UnsafeMethods.CloseHandle(CreateProcessInfo.hThread);
			return DBG_CONTINUE;
		}

		private uint OnCreateThreadDebugEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			var CreateThread = DebugEv.u.CreateThread;
			UnsafeMethods.CloseHandle(CreateThread.hThread);
			return DBG_CONTINUE;
		}

		private uint OnExitProcessDebugEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			if (dwProcessId == DebugEv.dwProcessId)
			{
				processExit = true;
			}

			return DBG_CONTINUE;
		}

		private uint OnExceptionDebugEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			// Filter for target process id.  It is possible to get a 2nd
			// chance exception for a process that we stopped wanting
			// to monitor after processing a 1st chance exception. Or anytime
			// the ContinueDebugging callback returns false before processExit is true.

			var Exception = DebugEv.u.Exception;

			switch (Exception.ExceptionRecord.ExceptionCode)
			{
				case EXCEPTION_ACCESS_VIOLATION:
					// First chance: Pass this on to the system. 
					// Last chance: Display an appropriate error. 

					logger.Trace("EXCEPTION_ACCESS_VIOLATION");
					if (DebugEv.dwProcessId == this.dwProcessId && HandleAccessViolation != null)
						HandleAccessViolation(DebugEv);

					break;

				case EXCEPTION_BREAKPOINT:
					logger.Trace("EXCEPTION_BREAKPOINT");
					// From: http://stackoverflow.com/questions/3799294/im-having-problems-with-waitfordebugevent-exception-debug-event
					// If launch a process and expect to debug it using the Windows API calls,
					// you should know that Windows will send one EXCEPTION_BREAKPOINT (INT3)
					// when it first loads. You must DEBUG_CONTINUE this first breakpoint
					// exception... if you DBG_EXCEPTION_NOT_HANDLED you will get the popup
					// message box: The application failed to initialize properly (0x80000003).
					return DBG_CONTINUE;

				case EXCEPTION_DATATYPE_MISALIGNMENT:
					logger.Trace("EXCEPTION_DATATYPE_MISALIGNMENT");
					// First chance: Pass this on to the system. 
					// Last chance: Display an appropriate error. 
					break;

				case EXCEPTION_SINGLE_STEP:
					logger.Trace("EXCEPTION_SINGLE_STEP");
					// First chance: Update the display of the 
					// current instruction and register values. 
					break;

				case DBG_CONTROL_C:
					logger.Trace("DBG_CONTROL_C");
					// First chance: Pass this on to the system. 
					// Last chance: Display an appropriate error. 
					break;

				default:
					logger.Trace("UNKNOWN");
					// Handle other exceptions. 
					break;
			}

			return DBG_EXCEPTION_NOT_HANDLED;
		}

		string GetFileNameFromHandle(IntPtr hFile)
		{
			StringBuilder pszFilename = new StringBuilder(256);
			IntPtr hFileMap;

			// Get the file size.
			uint dwFileSizeHi = 0;
			uint dwFileSizeLo = UnsafeMethods.GetFileSize(hFile, ref dwFileSizeHi);

			if (dwFileSizeLo == 0 && dwFileSizeHi == 0)
			{
				return null;
			}

			// Create a file mapping object.
			hFileMap = UnsafeMethods.CreateFileMapping(hFile,
				IntPtr.Zero,
				UnsafeMethods.FileMapProtection.PageReadonly,
				0,
				1,
				null);

			if (hFileMap != IntPtr.Zero)
			{
				// Create a file mapping to get the file name.
				IntPtr pMem = UnsafeMethods.MapViewOfFile(hFileMap, UnsafeMethods.FileMapAccess.FileMapRead, 0, 0, 1);

				if (pMem != IntPtr.Zero)
				{
					uint maxSize = 256;
					uint size = UnsafeMethods.GetMappedFileName(
						UnsafeMethods.GetCurrentProcess(),
						pMem, ref pszFilename, maxSize);

					UnsafeMethods.UnmapViewOfFile(pMem);
				}

				UnsafeMethods.CloseHandle(hFileMap);
			}

			return pszFilename.ToString();
		}

	}
}

// end
