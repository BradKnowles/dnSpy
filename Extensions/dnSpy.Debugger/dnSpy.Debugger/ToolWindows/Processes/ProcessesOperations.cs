﻿/*
    Copyright (C) 2014-2017 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Dialogs.AttachToProcess;

namespace dnSpy.Debugger.ToolWindows.Processes {
	abstract class ProcessesOperations {
		public abstract bool CanCopy { get; }
		public abstract void Copy();
		public abstract bool CanSelectAll { get; }
		public abstract void SelectAll();
		public abstract bool CanContinueProcess { get; }
		public abstract void ContinueProcess();
		public abstract bool CanBreakProcess { get; }
		public abstract void BreakProcess();
		public abstract bool CanStepIntoProcess { get; }
		public abstract void StepIntoProcess();
		public abstract bool CanStepOverProcess { get; }
		public abstract void StepOverProcess();
		public abstract bool CanStepOutProcess { get; }
		public abstract void StepOutProcess();
		public abstract bool CanDetachProcess { get; }
		public abstract void DetachProcess();
		public abstract bool CanTerminateProcess { get; }
		public abstract void TerminateProcess();
		public abstract bool CanAttachToProcess { get; }
		public abstract void AttachToProcess();
		public abstract bool CanToggleDetachWhenDebuggingStopped { get; }
		public abstract void ToggleDetachWhenDebuggingStopped();
		public abstract bool DetachWhenDebuggingStopped { get; set; }
		public abstract bool CanToggleUseHexadecimal { get; }
		public abstract void ToggleUseHexadecimal();
		public abstract bool UseHexadecimal { get; set; }
		public abstract bool CanSetCurrentProcess { get; }
		public abstract void SetCurrentProcess();
	}

	[Export(typeof(ProcessesOperations))]
	sealed class ProcessesOperationsImpl : ProcessesOperations {
		readonly IProcessesVM processesVM;
		readonly DebuggerSettings debuggerSettings;
		readonly Lazy<DbgManager> dbgManager;
		readonly Lazy<ShowAttachToProcessDialog> showAttachToProcessDialog;

		ObservableCollection<ProcessVM> AllItems => processesVM.AllItems;
		ObservableCollection<ProcessVM> SelectedItems => processesVM.SelectedItems;
		//TODO: This should be view order
		IEnumerable<ProcessVM> SortedSelectedItems => SelectedItems.OrderBy(a => a.Order);

		[ImportingConstructor]
		ProcessesOperationsImpl(IProcessesVM processesVM, DebuggerSettings debuggerSettings, Lazy<DbgManager> dbgManager, Lazy<ShowAttachToProcessDialog> showAttachToProcessDialog) {
			this.processesVM = processesVM;
			this.debuggerSettings = debuggerSettings;
			this.dbgManager = dbgManager;
			this.showAttachToProcessDialog = showAttachToProcessDialog;
		}

		public override bool CanCopy => SelectedItems.Count != 0;
		public override void Copy() {
			var output = new StringBuilderTextColorOutput();
			foreach (var vm in SortedSelectedItems) {
				var formatter = vm.Context.Formatter;
				formatter.WriteImage(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteName(output, vm.Process);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteId(output, vm.Process);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteTitle(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteState(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteDebugging(output, vm.Process);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WritePath(output, vm.Process);
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0) {
				try {
					Clipboard.SetText(s);
				}
				catch (ExternalException) { }
			}
		}

		public override bool CanSelectAll => SelectedItems.Count != AllItems.Count;
		public override void SelectAll() {
			SelectedItems.Clear();
			foreach (var vm in AllItems)
				SelectedItems.Add(vm);
		}

		public override bool CanContinueProcess => SelectedItems.Any(a => a.Process.State == DbgProcessState.Paused);
		public override void ContinueProcess() {
			foreach (var vm in SelectedItems)
				vm.Process.Run();
		}

		public override bool CanBreakProcess => SelectedItems.Any(a => a.Process.State == DbgProcessState.Running);
		public override void BreakProcess() {
			foreach (var vm in SelectedItems)
				vm.Process.Break();
		}

		public override bool CanStepIntoProcess => dbgManager.Value.CurrentProcess.Current != null;
		public override void StepIntoProcess() {
			//TODO: Use current process, not selected process
		}

		public override bool CanStepOverProcess => dbgManager.Value.CurrentProcess.Current != null;
		public override void StepOverProcess() {
			//TODO: Use current process, not selected process
		}

		public override bool CanStepOutProcess => dbgManager.Value.CurrentProcess.Current != null;
		public override void StepOutProcess() {
			//TODO: Use current process, not selected process
		}

		public override bool CanDetachProcess => SelectedItems.Count != 0;
		public override void DetachProcess() {
			foreach (var vm in SelectedItems)
				vm.Process.Detach();
		}

		public override bool CanTerminateProcess => SelectedItems.Count != 0;
		public override void TerminateProcess() {
			foreach (var vm in SelectedItems)
				vm.Process.Terminate();
		}

		public override bool CanAttachToProcess => true;
		public override void AttachToProcess() => showAttachToProcessDialog.Value.Attach();

		public override bool CanToggleDetachWhenDebuggingStopped => SelectedItems.Count != 0;
		public override void ToggleDetachWhenDebuggingStopped() => DetachWhenDebuggingStopped = !DetachWhenDebuggingStopped;
		public override bool DetachWhenDebuggingStopped {
			get => SelectedItems.Count != 0 && !SelectedItems.Any(a => !a.Process.ShouldDetach);
			set {
				foreach (var vm in SelectedItems)
					vm.Process.ShouldDetach = value;
			}
		}

		public override bool CanToggleUseHexadecimal => true;
		public override void ToggleUseHexadecimal() => UseHexadecimal = !UseHexadecimal;
		public override bool UseHexadecimal {
			get => debuggerSettings.UseHexadecimal;
			set => debuggerSettings.UseHexadecimal = value;
		}

		public override bool CanSetCurrentProcess => SelectedItems.Count == 1 && SelectedItems[0].Process.State == DbgProcessState.Paused;
		public override void SetCurrentProcess() {
			if (!CanSetCurrentProcess)
				return;
			SelectedItems[0].SelectProcess();
		}
	}
}
