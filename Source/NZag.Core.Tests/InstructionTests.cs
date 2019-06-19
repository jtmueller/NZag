using System;
using Xunit;
using static NZag.Core.Tests.Helpers;

namespace NZag.Core.Tests
{
    public class InstructionTests
    {
        [Fact]
        public void Zork1_4E3B()
        {
            // 4e3b: b2 ...  PRINT  "a "
            Test(Zork1, 0x4E3B,
                 Opcode("print"),
                 Text("a "));
        }

        [Fact]
        public void Zork1_4E3E()
        {
            // 4e3e: aa 01  PRINT_OBJ  L00
            Test(Zork1, 0x4E3E,
                 Opcode("print_obj"),
                 Operands(LocalVarOp(0)));
        }

        [Fact]
        public void Zork1_4E40()
        {
            // 4e40: b0  RTRUE
            Test(Zork1, 0x4E40,
                 Opcode("rtrue"),
                 NoOperands);
        }

        [Fact]
        public void Zork1_4E45()
        {
            // 4e45: a0 4c cb  JZ  G3c [TRUE] 4e51
            Test(Zork1, 0x4E45,
                 Opcode("jz"),
                 Operands(GlobalVarOp(0x3C)),
                 OffsetBranch(true, 11));
        }

        [Fact]
        public void Zork1_4E48()
        {
            // 4e48: e7 7f 64 00  RANDOM  #64 -> -(SP)
            Test(Zork1, 0x4E48,
                 Opcode("random"),
                 Operands(SmallConst(0x64)),
                 Store(StackVar));
        }

        [Fact]
        public void Zork1_4E4C()
        {
            // 4e4c: 63 01 00 c1  JG  L00,(SP)+ [TRUE] RTRUE
            Test(Zork1, 0x4E4C,
                 Opcode("jg"),
                 Operands(LocalVarOp(0), StackVarOp),
                 RTrueBranch(true));
        }

        [Fact]
        public void Zork1_4E50()
        {
            // 4e50: b1  RFALSE
            Test(Zork1, 0x4E50,
                 Opcode("rfalse"),
                 NoOperands);
        }

        private void Test(string gameName, int address, params Action<Instruction>[] validators)
        {
            var memory = GameMemory(gameName);
            var reader = new InstructionReader(memory);
            var inst = reader.ReadInstruction(address);

            ValidateInstruction(inst, address, validators);
        }
    }
}
