using SimpleMVVM;
using System;

namespace NZag.Profiling
{
    public class RoutineViewModel : ViewModelBase
    {
        public int Address { get; private set; }
        public TimeSpan InitialCompileTime { get; private set; }
        public int ILByteSize { get; private set; }

        public bool IsOptimized { get; private set; }
        public TimeSpan OptimizedCompileTime { get; private set; }
        public int OptimizedILByteSize { get; private set; }

        public int InvocationCount { get; private set; }

        public int LocalCount { get; private set; }
        public int InstructionCount { get; private set; }

        public RoutineViewModel(int address, TimeSpan compileTime, int ilByteSize, int localCount, int instructionCount)
        {
            Address = address;
            InitialCompileTime = compileTime;
            LocalCount = localCount;
            InstructionCount = instructionCount;
            ILByteSize = ilByteSize;
        }

        public void IncrementInvocationCount() => InvocationCount += 1;

        public void Recompiled(TimeSpan compileTime, int ilByteSize)
        {
            IsOptimized = true;
            OptimizedCompileTime = compileTime;
            OptimizedILByteSize = ilByteSize;
        }

        public double OptimizedILByteSizePercentage => (OptimizedILByteSize / (double)ILByteSize) * 100;

        public string OptimizedILByteSizeDisplay => String.Format("{0:#,0} ({1:0.00}%)", OptimizedILByteSize, OptimizedILByteSizePercentage);
    }
}
