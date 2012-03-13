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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peach.Core;
using Peach.Core.Agent;
using System.Windows.Forms;

namespace PeachFuzzBang
{
		public class ConsoleWatcher : Watcher
		{
			FormMain _form = null;
			public ConsoleWatcher(FormMain form)
			{
				_form = form;
			}

			protected override void RunContext_Debug(DebugLevel level, RunContext context, string from, string msg)
			{
			}

			protected override void Engine_Fault(RunContext context, uint currentIteration, Dictionary<string, Variant> stateModelData, Dictionary<AgentClient, Hashtable> faultData)
			{
				_form.textBoxOutput.Invoke(new DeligateAppendToText(AppendToText),
					new object[] { _form.textBoxOutput, "\r\n**** FAULT DETECTED AND LOGGED! ****\r\n" });

				_form.progressBarOuputFuzzing.Invoke(new DeligateFaultIncrement(FaultIncrement),
					new object[] { });

			}

			protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
			{
			}

			public delegate void DeligateIncrement(ProgressBar cltr);
			public void Increment(ProgressBar ctrl)
			{
				ctrl.Increment(1);
				_form.IterationCount++;
				int count = _form.IterationCount;
				_form.textBoxIterationCount.Text = count.ToString();
			}

			public delegate void DeligateFaultIncrement();
			public void FaultIncrement()
			{
				_form.FaultCount++;
				int count = _form.FaultCount;
				_form.textBoxFaultCount.Text = count.ToString();
			}

			public delegate void DeligateStopFuzzing();
			public void StopFuzzing()
			{
				_form.StoppedFuzzing();
			}

			public delegate void DeligateSetMax(ProgressBar cltr, int max);
			public void SetMax(ProgressBar ctrl, int max)
			{
				ctrl.Maximum = max;
			}

			protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
			{
				_form.progressBarOuputFuzzing.Invoke(new DeligateIncrement(Increment),
					new object[] { _form.progressBarOuputFuzzing });
				
				if (totalIterations == null)
				{
					_form.textBoxOutput.Invoke(new DeligateAppendToText(AppendToText),
						new object[] { _form.textBoxOutput, string.Format("\r\n[{0},-,-] Performing iteration\r\n", currentIteration) });
				}
				else
				{
					if(_form.progressBarOuputFuzzing.Maximum != (int)totalIterations)
						_form.progressBarOuputFuzzing.Invoke(new DeligateSetMax(SetMax),
							new object[] { _form.progressBarOuputFuzzing, (int)totalIterations });

					_form.textBoxOutput.Invoke(new DeligateAppendToText(AppendToText),
						new object[] { _form.textBoxOutput, string.Format("\r\n[{0},{1},?] Performing iteration\r\n", currentIteration, totalIterations) });
				}
			}

			protected override void Engine_TestError(RunContext context, Exception e)
			{
				_form.textBoxOutput.Invoke(new DeligateAppendToText(AppendToText),
					new object[] { _form.textBoxOutput, "\r\n[!] Test '" + context.test.name + "' error: " + e.Message + "\r\n" });
			}

			protected override void Engine_TestFinished(RunContext context)
			{
				_form.textBoxOutput.Invoke(new DeligateAppendToText(AppendToText),
					new object[] { _form.textBoxOutput, "\r\n[*] Test '" + context.test.name + "' finished.\r\n" });
			}

			protected override void Engine_TestStarting(RunContext context)
			{
				_form.textBoxOutput.Invoke(new DeligateAppendToText(AppendToText),
					new object[] { _form.textBoxOutput, "[*] Test '" + context.test.name + "' starting.\r\n" });
			}

			protected override void Engine_RunError(RunContext context, Exception e)
			{
				_form.textBoxOutput.Invoke(new DeligateAppendToText(AppendToText),
					new object[] { _form.textBoxOutput, "\r\n[!] Run '" + context.run.name + "' error: " + e.Message + "\r\n" });
			}

			protected override void Engine_RunFinished(RunContext context)
			{
				_form.textBoxOutput.Invoke(new DeligateAppendToText(AppendToText),
					new object[] { _form.textBoxOutput, "[*] Run '" + context.run.name + "' finished.\r\n" });

				_form.textBoxOutput.Invoke(new DeligateStopFuzzing(StopFuzzing),
					new object[] { });
			}

			public delegate void DeligateAppendToText(TextBox cltr, String text);
			public void AppendToText(TextBox ctrl, string text)
			{
				ctrl.Text += text;
				ctrl.SelectionStart = ctrl.Text.Length;
				ctrl.ScrollToCaret();
				ctrl.Refresh();
			}

			protected override void Engine_RunStarting(RunContext context)
			{
				_form.textBoxOutput.Invoke(new DeligateAppendToText(AppendToText), 
					new object[] { _form.textBoxOutput, "[*] Run '" + context.run.name + "' starting.\r\n"});
			}
		}
}
