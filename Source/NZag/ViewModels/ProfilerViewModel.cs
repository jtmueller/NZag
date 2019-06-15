using NZag.Core;
using NZag.Extensions;
using NZag.Profiling;
using SimpleMVVM;
using SimpleMVVM.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Controls;

namespace NZag.ViewModels
{
    [Export]
    public class ProfilerViewModel : ViewModelBase<UserControl>, IProfiler
    {
        private readonly SortedList<int, RoutineViewModel> _routineList;

        private readonly BulkObservableCollection<RoutineViewModel> _routines;
        private bool _refreshingData;
        private readonly object _gate = new object();

        [ImportingConstructor]
        private ProfilerViewModel()
            : base("Views/ProfilerView")
        {
            _routineList = new SortedList<int, RoutineViewModel>();
            _routines = new BulkObservableCollection<RoutineViewModel>();
            Routines = _routines.AsReadOnly();
        }

        public void RoutineCompiled(Routine routine, TimeSpan compileTime, int ilByteSize, bool optimized)
        {
            int address = routine.Address;

            if (!_routineList.TryGetValue(address, out var routineData))
            {
                Debug.Assert(!optimized);
                routineData = new RoutineViewModel(address, compileTime, ilByteSize, routine.Locals.Length, routine.Instructions.Length);
                lock (_gate)
                {
                    _routineList.Add(address, routineData);
                }
            }
            else
            {
                Debug.Assert(optimized);
                routineData.Recompiled(compileTime, ilByteSize);
            }
        }

        public void EnterRoutine(Routine routine)
        {
            int address = routine.Address;
            var routineData = _routineList[address];
            routineData.IncrementInvocationCount();

            if (!_refreshingData)
            {
                View.PostAction(RefreshData);
                _refreshingData = true;
            }
        }

        public void ExitRoutine(Routine routine)
        {
        }

        private void RefreshData()
        {
            lock (_gate)
            {
                var routinesCopy = _routines;

                routinesCopy.BeginBulkOperation();
                try
                {
                    routinesCopy.Clear();
                    foreach (var pair in _routineList)
                    {
                        routinesCopy.Add(pair.Value);
                    }
                }
                finally
                {
                    routinesCopy.EndBulkOperation();
                }

                _refreshingData = false;
            }
        }

        public ReadOnlyBulkObservableCollection<RoutineViewModel> Routines { get; }
    }
}
