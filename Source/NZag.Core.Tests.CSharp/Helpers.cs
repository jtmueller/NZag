using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace NZag.Core.Tests
{
    internal static class Helpers
    {
        public const string Zork1 = "zork1.z3";
        public const string CZech = "czech.z5";
        public const string Advent = "Advent.z5";
        public const string Count = "COUNT.Z5";

        public static readonly Variable StackVar = Variable.StackVariable;
        public static readonly Operand StackVarOp = Operand.NewVariableOperand(Helpers.StackVar);

        public static Memory GameMemory(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(asm.GetName().Name + ".Resources." + name);
            return Memory.CreateFrom(stream);
        }

        public static Memory CreateMemory(byte version, Span<byte> bytes)
        {
            uint size = (uint)(0x40 + bytes.Length);
            int multiplier = version switch
            {
                var v when v >= 1 && v <= 3 => 2,
                var v when v >= 4 && v <= 5 => 4,
                var v when v >= 6 && v <= 8 => 8,
                _ => throw new InvalidOperationException($"Unsupported version: {version}")
            };

            ushort packedSize = (ushort)(size / multiplier);
            if (size % multiplier > 0)
            {
                packedSize++;
            }

            using var stream = new MemoryStream((int)size);
            // write version
            stream.Seek(0L, SeekOrigin.Begin);
            stream.WriteByte(version);
            // write size
            stream.Seek(0x1A, SeekOrigin.Begin);
            stream.WriteByte((byte)(packedSize >> 8));
            stream.WriteByte((byte)(packedSize & 0xff));
            // write data
            stream.Seek(0x40, SeekOrigin.Begin);
            stream.Write(bytes);

            return Memory.CreateFrom(stream);
        }

        public static Memory CreateMemory(byte version, int dataSize)
            => Helpers.CreateMemory(version, new byte[dataSize]);

        public static Action<Instruction> Instruction(int address, params Action<Instruction>[] validators)
        {
            return (Instruction i) =>
            {
                Helpers.ValidateInstruction(i, address, validators);
            };
        }

        public static void ValidateInstruction(Instruction i, int address, params Action<Instruction>[] validators)
        {
            Assert.Equal(address, i.Address);
            foreach (var validator in validators)
            {
                validator(i);
            }
        }

        public static Action<Instruction> Opcode(string name)
        {
            return (Instruction i) =>
            {
                Assert.Equal(name, i.Opcode.Name);
            };
        }

        public static Action<Instruction> Text(string value)
        {
            return (Instruction i) =>
            {
                Assert.Equal(value, i.Text.Value);
            };
        }

        public static Operand SmallConst(byte value) => Operand.NewSmallConstantOperand(value);

        public static Operand LargeConst(ushort value) => Operand.NewLargeConstantOperand(value);

        public static Variable LocalVar(byte index) => Variable.NewLocalVariable(index);

        public static Operand LocalVarOp(byte index) => Operand.NewVariableOperand(Helpers.LocalVar(index));

        public static Variable GlobalVar(byte index) => Variable.NewGlobalVariable(index);

        public static Operand GlobalVarOp(byte index) => Operand.NewVariableOperand(Helpers.GlobalVar(index));

        public static Action<Instruction> Operands(params Operand[] ops)
        {
            return (Instruction i) =>
            {
                Assert.Equal(ops.Length, i.Operands.Length);
                for (int j = 0; j < ops.Length; j++)
                {
                    Assert.Equal(ops[j], i.Operands[j]);
                }
            };
        }

        public static Action<Instruction> RTrueBranch(bool condition)
        {
            return delegate (Instruction i)
            {
                Assert.True(i.Branch.Value.IsRTrueBranch);
                Assert.Equal(condition, i.Branch.Value.rtb);
            };
        }

        public static Action<Instruction> RFalseBranch(bool condition)
        {
            return delegate (Instruction i)
            {
                Assert.True(i.Branch.Value.IsRFalseBranch);
                Assert.Equal(condition, i.Branch.Value.rtb);
            };
        }

        public static Action<Instruction> OffsetBranch(bool condition, short offset)
        {
            return delegate (Instruction i)
            {
                Assert.True(i.Branch.Value.IsOffsetBranch);
                Assert.Equal(condition, i.Branch.Value.ofb);
                Assert.Equal(offset, i.Branch.Value.ofs);
            };
        }

        public static Action<Instruction> Store(Variable var)
        {
            return (Instruction i) =>
            {
                Assert.Equal(var, i.StoreVariable.Value);
            };
        }

        public static void TestBinder(string gameName, int address, string expected, bool debugging = false)
        {
            var memory = Helpers.GameMemory(gameName);
            var reader = new RoutineReader(memory);
            var r = reader.ReadRoutine(address);
            Assert.Equal(address, r.Address);
            var binder = new RoutineBinder(memory, debugging);
            var tree = binder.BindRoutine(r);
            var optimized = Optimization.Optimize(tree);
            var builder = new StringBuilder();
            var dumper = new BoundNodeDumper(builder);
            dumper.Dump(optimized);

            Assert.True(expected.AsSpan().Trim().SequenceEqual(builder.ToString()));
        }

        public static readonly Action<Instruction> NoOperands = (Instruction i) =>
        {
            Assert.Empty(i.Operands);
        };
    }
}
