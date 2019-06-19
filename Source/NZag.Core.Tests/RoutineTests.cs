using System;
using Xunit;
using static NZag.Core.Tests.Helpers;

namespace NZag.Core.Tests
{
    public class RoutineTests
    {
        [Fact]
        public void Zork1_4E3B()
        {
            // 4e3b:  b2 ...                  PRINT           "a "
            // 4e3e:  aa 01                   PRINT_OBJ       L00
            // 4e40:  b0                      RTRUE

            Test(Zork1, 0x4E38, new ushort[] { 0 },
                Instruction(0x4E3B, Opcode("print"), Text("a ")),
                Instruction(0x4E3E, Opcode("print_obj"), Operands(LocalVarOp(0))),
                Instruction(0x4E40, Opcode("rtrue"), NoOperands));
        }

        [Fact]
        public void Zork1_4E42()
        {
            // 4e45:  a0 4c cb                JZ              G3c [TRUE] 4e51
            // 4e48:  e7 7f 64 00             RANDOM          #64 -> -(SP)
            // 4e4c:  63 01 00 c1             JG              L00,(SP)+ [TRUE] RTRUE
            // 4e50:  b1                      RFALSE
            // 4e51:  e7 3f 01 2c 00          RANDOM          #012c -> -(SP)
            // 4e56:  63 01 00 c1             JG              L00,(SP)+ [TRUE] RTRUE
            // 4e5a:  b1                      RFALSE

            Test(Zork1, 0x4E42, new ushort[] { 0 },
                Instruction(0x4E45, Opcode("jz"), Operands(GlobalVarOp(0x3C)), OffsetBranch(true, 11)),
                Instruction(0x4E48, Opcode("random"), Operands(SmallConst(0x64)), Store(StackVar)),
                Instruction(0x4E4C, Opcode("jg"), Operands(LocalVarOp(0), StackVarOp), RTrueBranch(true)),
                Instruction(0x4E50, Opcode("rfalse"), NoOperands),
                Instruction(0x4E51, Opcode("random"), Operands(LargeConst(0x12C)), Store(StackVar)),
                Instruction(0x4E56, Opcode("jg"), Operands(LocalVarOp(0), StackVarOp), RTrueBranch(true)),
                Instruction(0x4E5A, Opcode("rfalse"), NoOperands));
        }

        private void Test(string gameName, int address, Span<ushort> locals, params Action<Instruction>[] instructions)
        {
            var memory = GameMemory(gameName);
            var reader = new RoutineReader(memory);
            var routine = reader.ReadRoutine(address);

            Assert.Equal(address, routine.Address);
            Assert.True(locals.SequenceEqual(routine.Locals), "Locals don't match");

            Assert.Equal(instructions.Length, routine.Instructions.Length);
            for (int i = 0; i < instructions.Length; i++)
                instructions[i](routine.Instructions[i]);
        }
    }
}
