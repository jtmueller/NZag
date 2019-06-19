using System;
using System.Linq;
using Xunit;
using static NZag.Core.Tests.Helpers;
using ControlFlowBlock = NZag.Core.Graphs.Block<NZag.Core.Graphs.ControlFlowData>;
using ControlFlowGraph = NZag.Core.Graphs.Graph<NZag.Core.Graphs.ControlFlowData>;
using DataFlowAnalysis = NZag.Core.Graphs.DataFlowAnalysis;
using DataFlowBlock = NZag.Core.Graphs.Block<NZag.Core.Graphs.DataFlowBlockInfo>;

namespace NZag.Core.Tests
{
    public class AnalysisTests
    {
        [Fact]
        public void CZech_1AC8_ReachingDefinitions()
        {
            // 1ac9:  73 01 02 04             get_next_prop   local0 local1 -> local3
            // 1acd:  2d 16 01                store           g06 local0
            // 1ad0:  2d 17 02                store           g07 local1
            // 1ad3:  f9 28 02 21 04 03 0b 6b call_vn         884 local3 local2 s035
            // 1adb:  b0                      rtrue

            var expected =
                DefinitionsGraph(
                    DefinitionsBlock(Graphs.Entry,
                                     NoInDefs,
                                     NoOutDefs),
                    DefinitionsBlock(0,
                                     NoInDefs,
                                     Outs(0, 1, 2, 3, 4)),
                    DefinitionsBlock(1,
                                     Ins(0, 1, 2, 3, 4, 4, 5, 6, 7, 7, 7),
                                     Outs(0, 1, 2, 3, 4, 4, 5, 6, 7, 7, 7)),
                    DefinitionsBlock(2,
                                     Ins(0, 1, 2, 3, 4, 4, 5, 6, 7, 7, 7),
                                     Outs(0, 1, 2, 3, 4, 4, 5, 6, 7)),
                    DefinitionsBlock(3,
                                     Ins(0, 1, 2, 3, 4, 4, 5, 6, 7, 7, 7),
                                     Outs(0, 1, 2, 3, 4, 4, 5, 6, 7, 7, 7)),
                    DefinitionsBlock(4,
                                     Ins(0, 1, 2, 3, 4, 4, 5, 6, 7, 7, 7),
                                     Outs(0, 1, 2, 3, 4, 4, 5, 6, 7)),
                    DefinitionsBlock(5,
                                     Ins(0, 1, 2, 3, 4, 4, 5, 6, 7, 7, 7),
                                     Outs(0, 1, 2, 3, 4, 4, 5, 6, 7)),
                    DefinitionsBlock(6,
                                     Ins(0, 1, 2, 3, 4, 4, 5, 6, 7, 7, 7),
                                     Outs(0, 1, 2, 3, 4, 5, 6, 7, 7, 7)),
                    DefinitionsBlock(7,
                                     Ins(0, 1, 2, 3, 4, 4, 5, 6, 7, 7, 7),
                                     Outs(0, 1, 2, 3, 4, 4, 5, 6, 7, 7, 7, 8)),
                    DefinitionsBlock(Graphs.Exit,
                                     Ins(0, 1, 2, 3, 4, 4, 5, 6, 7, 7, 7, 8),
                                     Outs(0, 1, 2, 3, 4, 4, 5, 6, 7, 7, 7, 8)));
            Test(CZech, 0x1AC8, expected);
        }

        [Fact]
        public void Zork1_4E3B_ControlFlow()
        {
            // 4e3b:  b2 ...                  PRINT           "a "
            // 4e3e:  aa 01                   PRINT_OBJ       L00
            // 4e40:  b0                      RTRUE

            var expected =
                ControlFlowGraph(
                    ControlFlowBlock(Graphs.Entry, NoPred, Succ(0)),
                    ControlFlowBlock(0, Pred(Graphs.Entry), Succ(Graphs.Exit)),
                    ControlFlowBlock(Graphs.Exit, Pred(0), NoSucc));

            Test(Zork1, 0x4E38, expected);
        }

        [Fact]
        public void Zork1_4E42_ControlFlow()
        {
            // 4e45:  a0 4c cb                JZ              G3c [TRUE] 4e51
            // 4e48:  e7 7f 64 00             RANDOM          #64 -> -(SP)
            // 4e4c:  63 01 00 c1             JG              L00,(SP)+ [TRUE] RTRUE
            // 4e50:  b1                      RFALSE
            // 4e51:  e7 3f 01 2c 00          RANDOM          #012c -> -(SP)
            // 4e56:  63 01 00 c1             JG              L00,(SP)+ [TRUE] RTRUE
            // 4e5a:  b1                      RFALSE

            var expected =
                ControlFlowGraph(
                    ControlFlowBlock(Graphs.Entry, NoPred, Succ(0)),
                    ControlFlowBlock(0, Pred(Graphs.Entry), Succ(1, 6)),
                    ControlFlowBlock(1, Pred(0), Succ(2, 3)),
                    ControlFlowBlock(2, Pred(1), Succ(4)),
                    ControlFlowBlock(3, Pred(1), Succ(4)),
                    ControlFlowBlock(4, Pred(2, 3), Succ(Graphs.Exit, 5)),
                    ControlFlowBlock(5, Pred(4), Succ(Graphs.Exit)),
                    ControlFlowBlock(6, Pred(0), Succ(7, 8)),
                    ControlFlowBlock(7, Pred(6), Succ(9)),
                    ControlFlowBlock(8, Pred(6), Succ(9)),
                    ControlFlowBlock(9, Pred(7, 8), Succ(Graphs.Exit, 10)),
                    ControlFlowBlock(10, Pred(9), Succ(Graphs.Exit)),
                    ControlFlowBlock(Graphs.Exit, Pred(4, 5, 9, 10), NoSucc));

            Test(Zork1, 0x4E42, expected);
        }

        [Fact]
        public void Zork1_4E42_ReachingDefinitions()
        {
            // 4e45:  a0 4c cb                JZ              G3c [TRUE] 4e51
            // 4e48:  e7 7f 64 00             RANDOM          #64 -> -(SP)
            // 4e4c:  63 01 00 c1             JG              L00,(SP)+ [TRUE] RTRUE
            // 4e50:  b1                      RFALSE
            // 4e51:  e7 3f 01 2c 00          RANDOM          #012c -> -(SP)
            // 4e56:  63 01 00 c1             JG              L00,(SP)+ [TRUE] RTRUE
            // 4e5a:  b1                      RFALSE

            var expected =
                DefinitionsGraph(
                    DefinitionsBlock(Graphs.Entry,
                                     NoInDefs,
                                     NoOutDefs),
                    DefinitionsBlock(0,
                                     NoInDefs,
                                     Outs(0, 1)),
                    DefinitionsBlock(1,
                                     Ins(0, 1),
                                     Outs(0, 1)),
                    DefinitionsBlock(2,
                                     Ins(0, 1),
                                     Outs(0, 1)),
                    DefinitionsBlock(3,
                                     Ins(0, 1),
                                     Outs(0, 1)),
                    DefinitionsBlock(4,
                                     Ins(0, 1),
                                     Outs(0, 1, 2)),
                    DefinitionsBlock(5,
                                     Ins(0, 1, 2),
                                     Outs(0, 1, 2)),
                    DefinitionsBlock(6,
                                     Ins(0, 1),
                                     Outs(0, 1)),
                    DefinitionsBlock(7,
                                     Ins(0, 1),
                                     Outs(0, 1)),
                    DefinitionsBlock(8,
                                     Ins(0, 1),
                                     Outs(0, 1)),
                    DefinitionsBlock(9,
                                     Ins(0, 1),
                                     Outs(0, 1, 3)),
                    DefinitionsBlock(10,
                                     Ins(0, 1, 3),
                                     Outs(0, 1, 3)),
                    DefinitionsBlock(Graphs.Exit,
                                     Ins(0, 1, 2, 3),
                                     Outs(0, 1, 2, 3)));

            Test(Zork1, 0x4E42, expected);
        }

        private Action<ControlFlowGraph> ControlFlowGraph(params Action<ControlFlowBlock>[] actions)
        {
            return (ControlFlowGraph g) =>
            {
                Assert.Equal(actions.Length, g.Blocks.Length);

                for (int i = 0; i < actions.Length; i++)
                {
                    actions[i](g.Blocks[i]);
                }
            };
        }

        private Action<ControlFlowBlock> ControlFlowBlock(int id, params Action<ControlFlowBlock>[] actions)
        {
            return (ControlFlowBlock b) =>
            {
                Assert.Equal(id, b.ID);

                foreach (var action in actions)
                    action(b);
            };
        }

        private Action<ControlFlowBlock> Pred(params int[] ids)
        {
            return (ControlFlowBlock b) =>
            {
                Assert.True(ids.AsSpan().SequenceEqual(b.Predecessors), "ID's did not match predecessors");
            };
        }

        private Action<ControlFlowBlock> Succ(params int[] ids)
        {
            return (ControlFlowBlock b) =>
            {
                Assert.True(ids.AsSpan().SequenceEqual(b.Successors), "ID's did not match successors");
            };
        }

        private readonly Action<ControlFlowBlock> NoPred =
            (ControlFlowBlock b) => Assert.Empty(b.Predecessors);

        private readonly Action<ControlFlowBlock> NoSucc =
            (ControlFlowBlock b) => Assert.Empty(b.Successors);

        private Action<DataFlowAnalysis> DefinitionsGraph(params Action<DataFlowAnalysis, DataFlowBlock>[] actions)
        {
            return (DataFlowAnalysis dfa) =>
            {
                Assert.Equal(actions.Length, dfa.Graph.Blocks.Length);

                for (int i = 0; i < actions.Length; i++)
                {
                    actions[i](dfa, dfa.Graph.Blocks[i]);
                }
            };
        }

        private Action<DataFlowAnalysis, DataFlowBlock> DefinitionsBlock(int id, params Action<DataFlowAnalysis, DataFlowBlock>[] actions)
        {
            return (DataFlowAnalysis dfa, DataFlowBlock b) =>
            {
                Assert.Equal(id, b.ID);

                foreach (var action in actions)
                    action(dfa, b);
            };
        }

        private Action<DataFlowAnalysis, DataFlowBlock> Ins(params int[] ids)
        {
            return (DataFlowAnalysis dfa, DataFlowBlock b) =>
            {
                var orderedIns = b.Data.Ins.AllSet
                    .Select(d => dfa.Definitions[d])
                    .OrderBy(d => d.Temp)
                    .ToArray();

                Assert.Equal(ids.Length, orderedIns.Length);

                for (int i = 0; i < ids.Length; i++)
                    Assert.Equal(ids[i], orderedIns[i].Temp);
            };
        }

        private readonly Action<DataFlowAnalysis, DataFlowBlock> NoInDefs =
            (DataFlowAnalysis dfa, DataFlowBlock b) => Assert.Empty(b.Data.Ins.AllSet);

        private Action<DataFlowAnalysis, DataFlowBlock> Outs(params int[] ids)
        {
            return (DataFlowAnalysis dfa, DataFlowBlock b) =>
            {
                var orderedOuts = b.Data.Outs.AllSet
                    .Select(d => dfa.Definitions[d])
                    .OrderBy(d => d.Temp)
                    .ToArray();

                Assert.Equal(ids.Length, orderedOuts.Length);

                for (int i = 0; i < ids.Length; i++)
                    Assert.Equal(ids[i], orderedOuts[i].Temp);
            };
        }

        private readonly Action<DataFlowAnalysis, DataFlowBlock> NoOutDefs =
            (DataFlowAnalysis dfa, DataFlowBlock b) => Assert.Empty(b.Data.Outs.AllSet);

        private void Test(string gameName, int address, Action<ControlFlowGraph> expected)
        {
            var memory = GameMemory(gameName);
            var reader = new RoutineReader(memory);
            var routine = reader.ReadRoutine(address);

            Assert.Equal(address, routine.Address);

            var binder = new RoutineBinder(memory, debugging: false);
            var tree = binder.BindRoutine(routine);
            var optimized = Optimization.Optimize(tree);
            var graph = Graphs.BuildControlFlowGraph(optimized);

            expected(graph);
        }

        private void Test(string gameName, int address, Action<DataFlowAnalysis> expected)
        {
            var memory = GameMemory(gameName);
            var reader = new RoutineReader(memory);
            var routine = reader.ReadRoutine(address);

            Assert.Equal(address, routine.Address);

            var binder = new RoutineBinder(memory, debugging: false);
            var tree = binder.BindRoutine(routine);
            var optimized = Optimization.Optimize(tree);
            var graph = Graphs.BuildControlFlowGraph(optimized);
            var dfa = Graphs.AnalyzeDataFlow(graph);

            expected(dfa);
        }
    }
}
