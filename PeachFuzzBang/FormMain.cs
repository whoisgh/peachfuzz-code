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
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.ServiceProcess;

using Peach;
using Peach.Core;
using Peach.Core.Loggers;
using Peach.Core.Dom;
using Peach.Core.Publishers;
using Peach.Core.Agent;
using Peach.Core.MutationStrategies;
using System.Threading;

namespace PeachFuzzBang
{
	public partial class FormMain : Form
	{
		public Int32 IterationCount = 0;
		public Int32 FaultCount = 0;

		Thread thread = null;

		Peach.Core.Dom.Dom userSelectedDom = null;
		DataModel userSelectedDataModel = null;

		public FormMain()
		{
			InitializeComponent();

			List<MutationStrategy> strategies = new List<MutationStrategy>();
			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type t in a.GetExportedTypes())
				{
					if (!t.IsClass)
						continue;

					foreach (object attrib in t.GetCustomAttributes(true))
					{
						if (attrib is MutationStrategyAttribute)
						{
							//strategies.Add(((MutationStrategyAttribute)attrib).name);
							comboBoxFuzzingStrategy.Items.Add(((MutationStrategyAttribute)attrib).name);
						}
					}
				}
			}

			comboBoxAttachToServiceServices.Items.Clear();
			foreach (ServiceController srv in ServiceController.GetServices())
			{
				comboBoxAttachToServiceServices.Items.Add(srv.ServiceName);
			}

			textBoxAttachToProcessProcessName.Items.Clear();
			foreach (Process proc in Process.GetProcesses())
			{
				textBoxAttachToProcessProcessName.Items.Add(proc.ProcessName);
			}

			//tabControl.TabPages.Remove(tabPageGUI);
			tabControl.TabPages.Remove(tabPageFuzzing);
			//tabControl.TabPages.Remove(tabPageOutput);

			buttonStartFuzzing.Enabled = true;
			buttonSaveConfiguration.Enabled = false;
			buttonStopFuzzing.Enabled = false;

			if (Directory.Exists(@"C:\Program Files (x86)\Debugging Tools for Windows (x86)"))
				textBoxDebuggerPath.Text = @"C:\Program Files (x86)\Debugging Tools for Windows (x86)";
			if (Directory.Exists(@"C:\Program Files\Debugging Tools for Windows (x86)"))
				textBoxDebuggerPath.Text = @"C:\Program Files\Debugging Tools for Windows (x86)";
			if (Directory.Exists(@"C:\Program Files\Debugging Tools for Windows"))
				textBoxDebuggerPath.Text = @"C:\Program Files\Debugging Tools for Windows";

			comboBoxPitDataModel.SelectedIndexChanged += new EventHandler(comboBoxPitDataModel_SelectedIndexChanged);

			richTextBoxIntroduction.LoadFile("Introduction.rtf");
		}

		void comboBoxPitDataModel_SelectedIndexChanged(object sender, EventArgs e)
		{
			userSelectedDataModel = userSelectedDom.dataModels[comboBoxPitDataModel.Text];
		}

		private void buttonStartFuzzing_Click(object sender, EventArgs e)
		{
			try
			{
				//tabControl.TabPages.Remove(tabPageGeneral);
				//tabControl.TabPages.Remove(tabPageDebugger);
				//tabControl.TabPages.Insert(0, tabPageOutput);
				tabControl.SelectedTab = tabPageOutput;
				buttonStartFuzzing.Enabled = false;
				buttonSaveConfiguration.Enabled = false;
				buttonStopFuzzing.Enabled = true;

				IterationCount = 0;
				FaultCount = 0;
				textBoxIterationCount.Text = IterationCount.ToString();
				textBoxFaultCount.Text = FaultCount.ToString();
				textBoxOutput.Text = "";

				Dom dom = new Dom();
				DataModel dataModel = null;

				// Data Set
				Data fileData = new Data();
				if (Directory.Exists(textBoxTemplateFiles.Text))
				{
					List<string> files = new List<string>();
					foreach (string fileName in Directory.GetFiles(textBoxTemplateFiles.Text))
						files.Add(fileName);

					fileData.DataType = DataType.Files;
					fileData.Files = files;
				}
				else if (File.Exists(textBoxTemplateFiles.Text))
				{
					fileData.DataType = DataType.File;
					fileData.FileName = textBoxTemplateFiles.Text;
				}
				else
				{
					MessageBox.Show("Error, Unable to locate file/folder called \"" + textBoxTemplateFiles.Text + "\".");
					return;
				}

				// DataModel
				if (userSelectedDataModel != null)
				{
					dataModel = ObjectCopier.Clone<DataModel>(dataModel);
					dataModel.dom = dom;
					dataModel.name = "TheDataModel";

					dom.dataModels.Add(dataModel.name, dataModel);
				}
				else
				{
					dataModel = new DataModel("TheDataModel");
					dataModel.Add(new Blob());
					dom.dataModels.Add(dataModel.name, dataModel);
				}

				// Publisher
				Dictionary<string, Variant> args = new Dictionary<string, Variant>();
				args["FileName"] = new Variant(textBoxFuzzedFile.Text);
				Peach.Core.Publishers.FilePublisher file = new Peach.Core.Publishers.FilePublisher(args);

				// StateModel
				StateModel stateModel = new StateModel();
				stateModel.name = "TheStateModel";

				State state = new State();
				state.name = "TheState";

				Peach.Core.Dom.Action actionOutput = new Peach.Core.Dom.Action();
				actionOutput.type = ActionType.Output;
				actionOutput.dataModel = dataModel;
				actionOutput.dataSet = new Peach.Core.Dom.DataSet();
				actionOutput.dataSet.Datas.Add(fileData);

				Peach.Core.Dom.Action actionClose = new Peach.Core.Dom.Action();
				actionClose.type = ActionType.Close;

				Peach.Core.Dom.Action actionCall = new Peach.Core.Dom.Action();
				actionCall.type = ActionType.Call;
				actionCall.publisher = "Peach.Agent";
				actionCall.method = "ScoobySnacks";

				state.actions.Add(actionOutput);
				state.actions.Add(actionClose);
				state.actions.Add(actionCall);

				stateModel.states.Add(state.name, state);
				stateModel.initialState = state;

				dom.stateModels.Add(stateModel.name, stateModel);

				// Agent
				Peach.Core.Dom.Agent agent = new Peach.Core.Dom.Agent();
				agent.name = "TheAgent";
				agent.url = "local://";

				Peach.Core.Dom.Monitor monitor = new Peach.Core.Dom.Monitor();
				monitor.cls = "WindowsDebugEngine";
				//monitor.cls = "WindowsDebugger";
				monitor.parameters["StartOnCall"] = new Variant("ScoobySnacks");
				monitor.parameters["WinDbgPath"] = new Variant(textBoxDebuggerPath.Text);

				if (radioButtonDebuggerStartProcess.Checked)
					monitor.parameters["CommandLine"] = new Variant(textBoxDebuggerCommandLine.Text);
				else if (radioButtonDebuggerAttachToProcess.Checked)
				{
					if (radioButtonAttachToProcessPID.Checked)
						monitor.parameters["ProcessName"] = new Variant(textBoxAttachToProcessPID.Text);
					else if (radioButtonAttachToProcessProcessName.Checked)
						monitor.parameters["ProcessName"] = new Variant(textBoxAttachToProcessProcessName.Text);
				}
				else if (radioButtonDebuggerAttachToService.Checked)
					monitor.parameters["Service"] = new Variant(comboBoxAttachToServiceServices.Text);
				else if (radioButtonDebuggerKernelDebugger.Checked)
					monitor.parameters["KernelConnectionString"] = new Variant(textBoxKernelConnectionString.Text);

				agent.monitors.Add(monitor);
				dom.agents.Add(agent.name, agent);

				// Mutation Strategy
				MutationStrategy strat = new RandomStrategy(new Dictionary<string, Variant>());
				if (comboBoxFuzzingStrategy.Text.ToLower().IndexOf("Squencial") > -1)
					strat = new Sequencial(new Dictionary<string, string>());

				// Test
				Test test = new Test();
				test.name = "TheTest";
				test.stateModel = stateModel;
				test.agents.Add(agent.name, agent);
				test.publishers.Add("FileWriter", file);
				test.strategy = strat;

				dom.tests.Add(test.name, test);

				// Run
				Run run = new Run();
				run.name = "DefaultRun";
				run.tests.Add(test.name, test);

				if (logger == null)
				{
					Dictionary<string, Variant> loggerArgs = new Dictionary<string, Variant>();
					loggerArgs["Path"] = new Variant(textBoxLogPath.Text);
					logger = new Peach.Core.Loggers.FileLogger(loggerArgs);
				}

				run.logger = logger;
				dom.runs.Add(run.name, run);

				// START FUZZING!!!!!
				thread = new Thread(new ParameterizedThreadStart(Run));
				thread.Start(dom);
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.ToString());
				throw ex;
			}
		}

		Logger logger = null;
		ConsoleWatcher consoleWatcher = null;

		public void Run(object obj)
		{
			try
			{
				if (consoleWatcher == null)
					consoleWatcher = new ConsoleWatcher(this);

				Dom dom = obj as Dom;
				RunConfiguration config = new RunConfiguration();
				Engine e = new Engine(consoleWatcher);

				config.pitFile = "PeachFuzzBang";

				if (!string.IsNullOrEmpty(textBoxIterations.Text))
				{
					try
					{
						int iter = int.Parse(textBoxIterations.Text);
						if (iter > 0)
						{
							config.range = true;
							config.rangeStart = 0;
							config.rangeStop = (uint)iter;
						}
					}
					catch
					{
					}
				}

				e.RunFinished += new Engine.RunFinishedEventHandler(Engine_RunFinished);
				e.RunError += new Engine.RunErrorEventHandler(Engine_RunError);

				e.startFuzzing(dom, config);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
				throw ex;
			}
		}

		void Engine_RunError(RunContext context, Exception e)
		{
			// TODO 
			//throw new NotImplementedException();
		}

		void Engine_RunFinished(RunContext context)
		{
			// TODO
			//throw new NotImplementedException();
		}

		private void buttonDebuggerPathBrowse_Click(object sender, EventArgs e)
		{
			OpenFileDialog browse = new OpenFileDialog();
			browse.DefaultExt = ".exe";
			browse.Title = "Browse to WinDbg";
			if (browse.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
				return;

			string fileName = browse.FileName;
			textBoxDebuggerPath.Text = fileName.Substring(0, fileName.LastIndexOf("\\"));

			buttonPitFileNameLoad_Click(null, null);
		}

		private void buttonPitFileNameLoad_Click(object sender, EventArgs e)
		{
			if (!System.IO.File.Exists(textBoxPitFileName.Text))
			{
				MessageBox.Show("Error, Pit file does not exist.");
				return;
			}

			var currentCursor = this.Cursor;
			this.Cursor = Cursors.WaitCursor;

			try
			{

				var pitParser = new Peach.Core.Analyzers.PitParser();
				userSelectedDom = pitParser.asParser(new Dictionary<string, string>(), textBoxPitFileName.Text);

				comboBoxPitDataModel.Items.Clear();
				foreach (var model in userSelectedDom.dataModels.Keys)
				{
					comboBoxPitDataModel.Items.Add(model);
				}

				if (userSelectedDom.dataModels.Count > 0)
					comboBoxPitDataModel.SelectedIndex = 0;

				label4.Enabled = true;
				comboBoxPitDataModel.Enabled = true;
			}
			catch (PeachException ex)
			{
				MessageBox.Show(ex.Message);
			}
			finally
			{
				this.Cursor = currentCursor;
			}
		}

		private void buttonPitFileNameBrowse_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.DefaultExt = ".xml";

			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
				return;

			textBoxPitFileName.Text = dialog.FileName;
		}

		public void StoppedFuzzing()
		{
			buttonStartFuzzing.Enabled = true;
			buttonSaveConfiguration.Enabled = false;
			buttonStopFuzzing.Enabled = false;

			//tabControl.SelectedTab = tabPageGeneral;
		}

		private void buttonStopFuzzing_Click(object sender, EventArgs e)
		{
			buttonStartFuzzing.Enabled = true;
			buttonSaveConfiguration.Enabled = false;
			buttonStopFuzzing.Enabled = false;

			tabControl.SelectedTab = tabPageGeneral;

			thread.Abort();
			thread = null;
		}

		private void buttonBrowseTemplates_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Title = "Select Template";
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
				return;

			textBoxTemplateFiles.Text = dialog.FileName;
		}

		private void buttonBrowseFuzzedFile_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Title = "Select Fuzzed File";
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
				return;

			textBoxFuzzedFile.Text = dialog.FileName;
		}

		private void buttonDebuggerCommandBrowse_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Title = "Select Executable";
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
				return;

			textBoxDebuggerCommandLine.Text = dialog.FileName;
		}

		private void buttonLogPathBrowse_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Title = "Select Logs Path";
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
				return;

			textBoxLogPath.Text = dialog.FileName;
		}
	}
}
