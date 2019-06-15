﻿namespace NZag.Core

open System
open System.Text
open NZag.Utilities

type OpcodeKind =
    | TwoOp  = 0
    | OneOp  = 1
    | ZeroOp = 2
    | VarOp  = 3
    | Ext    = 4

[<Flags>]
type OpcodeFlags =
    | None         = 0x00
    | Store        = 0x01
    | Branch       = 0x02
    | Text         = 0x04
    | Call         = 0x10
    | DoubleVar    = 0x20
    | FirstOpByRef = 0x40
    | Return       = 0x80

[<Struct>]
type Opcode internal (kind: OpcodeKind, number: byte, version: byte, name: string, flags: OpcodeFlags) =
    static let formatB = StringBuilder.appendFormat "%s (%A:%02x)"

    member x.Kind = kind
    member x.Number = number
    member x.Version = version
    member x.Name = name
    member x.HasStoreVariable = (flags &&& OpcodeFlags.Store) = OpcodeFlags.Store
    member x.HasBranch = (flags &&& OpcodeFlags.Branch) = OpcodeFlags.Branch
    member x.HasText = (flags &&& OpcodeFlags.Text) = OpcodeFlags.Text
    member x.IsCall = (flags &&& OpcodeFlags.Call) = OpcodeFlags.Call
    member x.IsDoubleVar = (flags &&& OpcodeFlags.DoubleVar) = OpcodeFlags.DoubleVar
    member x.IsFirstOpByRef = (flags &&& OpcodeFlags.FirstOpByRef) = OpcodeFlags.FirstOpByRef
    member x.IsReturn = (flags &&& OpcodeFlags.Return) = OpcodeFlags.Return
    member x.IsJump = kind = OpcodeKind.OneOp && number = 0x0cuy
    member x.IsQuit = kind = OpcodeKind.ZeroOp && number = 0x0auy

    override x.ToString() =
        sprintf "%s (%A:%02x)" name kind number

    member x.WriteToBuilder(builder: StringBuilder) =
        formatB builder name kind number

type OpcodeTable internal (version: byte) =

    let opcodes = Array.zeroCreate (5 * 32)

    let getIndex kind number =
        ((int kind) * 32) + int number

    member internal x.Add(kind, number, name, flags) =
        let index = getIndex kind number
        opcodes.[index] <- new Opcode(kind, number, version, name, flags)

    member x.Item
        with get(kind, number) =
            let index = getIndex kind number
            opcodes.[index]

    member x.Version = version

module OpcodeTables =

    let private opcodeTables = Array.init 8 (fun i -> new OpcodeTable(byte (i + 1)))

    let private add kind number name flags fromVersion toVersion =
        for v = fromVersion to toVersion do
            opcodeTables.[v - 1].Add(kind, number, name, flags)

    [<Literal>]
    let private None = OpcodeFlags.None
    [<Literal>]
    let private Branch = OpcodeFlags.Branch
    [<Literal>]
    let private Store = OpcodeFlags.Store
    [<Literal>]
    let private Call = OpcodeFlags.Call
    [<Literal>]
    let private FirstOpByRef = OpcodeFlags.FirstOpByRef
    [<Literal>]
    let private DoubleVar = OpcodeFlags.DoubleVar
    [<Literal>]
    let private Return = OpcodeFlags.Return
    [<Literal>]
    let private Text = OpcodeFlags.Text

    [<Literal>]
    let private MinVer = 1
    [<Literal>]
    let private MaxVer = 8

    let inline private twoOp number = add OpcodeKind.TwoOp (byte number)
    let inline private oneOp number = add OpcodeKind.OneOp (byte number)
    let inline private zeroOp number = add OpcodeKind.ZeroOp (byte number)
    let inline private varOp number = add OpcodeKind.VarOp (byte number)
    let inline private ext number = add OpcodeKind.Ext (byte number)

    do
        twoOp  0x01 "je"               Branch                        MinVer MaxVer
        twoOp  0x02 "jl"               Branch                        MinVer MaxVer
        twoOp  0x03 "jg"               Branch                        MinVer MaxVer
        twoOp  0x04 "dec_chk"         (Branch ||| FirstOpByRef)      MinVer MaxVer
        twoOp  0x05 "inc_chk"         (Branch ||| FirstOpByRef)      MinVer MaxVer
        twoOp  0x06 "jin"              Branch                        MinVer MaxVer
        twoOp  0x07 "test"             Branch                        MinVer MaxVer
        twoOp  0x08 "or"               Store                         MinVer MaxVer
        twoOp  0x09 "and"              Store                         MinVer MaxVer
        twoOp  0x0A "test_attr"        Branch                        MinVer MaxVer
        twoOp  0x0B "set_attr"         None                          MinVer MaxVer
        twoOp  0x0C "clear_attr"       None                          MinVer MaxVer
        twoOp  0x0D "store"            FirstOpByRef                  MinVer MaxVer
        twoOp  0x0E "insert_obj"       None                          MinVer MaxVer
        twoOp  0x0F "loadw"            Store                         MinVer MaxVer
        twoOp  0x10 "loadb"            Store                         MinVer MaxVer
        twoOp  0x11 "get_prop"         Store                         MinVer MaxVer
        twoOp  0x12 "get_prop_addr"    Store                         MinVer MaxVer
        twoOp  0x13 "get_next_prop"    Store                         MinVer MaxVer
        twoOp  0x14 "add"              Store                         MinVer MaxVer
        twoOp  0x15 "sub"              Store                         MinVer MaxVer
        twoOp  0x16 "mul"              Store                         MinVer MaxVer
        twoOp  0x17 "div"              Store                         MinVer MaxVer
        twoOp  0x18 "mod"              Store                         MinVer MaxVer
        twoOp  0x19 "call_2s"         (Call ||| Store)               4      MaxVer
        twoOp  0x1A "call_2n"          Call                          5      MaxVer
        twoOp  0x1B "set_color"        None                          5      5
        twoOp  0x1B "set_color"        None                          6      6
        twoOp  0x1B "set_color"        None                          7      MaxVer
        twoOp  0x1C "throw"            None                          5      MaxVer

        oneOp  0x00 "jz"               Branch                        MinVer MaxVer
        oneOp  0x01 "get_sibling"     (Branch ||| Store)             MinVer MaxVer
        oneOp  0x02 "get_child"       (Branch ||| Store)             MinVer MaxVer
        oneOp  0x03 "get_parent"       Store                         MinVer MaxVer
        oneOp  0x04 "get_prop_len"     Store                         MinVer MaxVer
        oneOp  0x05 "inc"              FirstOpByRef                  MinVer MaxVer
        oneOp  0x06 "dec"              FirstOpByRef                  MinVer MaxVer
        oneOp  0x07 "print_addr"       None                          MinVer MaxVer
        oneOp  0x08 "call_1s"         (Call ||| Store)               4      MaxVer
        oneOp  0x09 "remove_obj"       None                          MinVer MaxVer
        oneOp  0x0A "print_obj"        None                          MinVer MaxVer
        oneOp  0x0B "ret"              Return                        MinVer MaxVer
        oneOp  0x0C "jump"             None                          MinVer MaxVer
        oneOp  0x0D "print_paddr"      None                          MinVer MaxVer
        oneOp  0x0E "load"            (Store ||| FirstOpByRef)       MinVer MaxVer
        oneOp  0x0F "not"              Store                         MinVer 4
        oneOp  0x0F "call_1n"          Call                          5      MaxVer

        zeroOp 0x00 "rtrue"            Return                        MinVer MaxVer
        zeroOp 0x01 "rfalse"           Return                        MinVer MaxVer
        zeroOp 0x02 "print"            Text                          MinVer MaxVer
        zeroOp 0x03 "print_ret"       (Return ||| Text)              MinVer MaxVer
        zeroOp 0x04 "nop"              None                          MinVer MaxVer
        zeroOp 0x05 "save"             Branch                        MinVer 3
        zeroOp 0x05 "save"             Store                         4      4
        zeroOp 0x06 "restore"          Branch                        MinVer 3
        zeroOp 0x06 "restore"          Store                         4      4
        zeroOp 0x07 "restart"          None                          MinVer MaxVer
        zeroOp 0x08 "ret_popped"       Return                        MinVer MaxVer
        zeroOp 0x09 "pop"              None                          MinVer 4
        zeroOp 0x09 "catch"            Store                         5      MaxVer
        zeroOp 0x0A "quit"             None                          MinVer MaxVer
        zeroOp 0x0B "new_line"         None                          MinVer MaxVer
        zeroOp 0x0C "show_status"      None                          3      3
        zeroOp 0x0D "verify"           Branch                        3      MaxVer
        zeroOp 0x0F "piracy"           Branch                        5      MaxVer

        varOp  0x00 "call"            (Call ||| Store)               MinVer 4
        varOp  0x00 "call_vs"         (Call ||| Store)               5      MaxVer
        varOp  0x01 "storew"           None                          MinVer MaxVer
        varOp  0x02 "storeb"           None                          MinVer MaxVer
        varOp  0x03 "put_prop"         None                          MinVer MaxVer
        varOp  0x04 "sread"            None                          MinVer 3
        varOp  0x04 "sread"            None                          4      4
        varOp  0x04 "aread"            Store                         5      MaxVer
        varOp  0x05 "print_char"       None                          MinVer MaxVer
        varOp  0x06 "print_num"        None                          MinVer MaxVer
        varOp  0x07 "random"           Store                         MinVer MaxVer
        varOp  0x08 "push"             None                          MinVer MaxVer
        varOp  0x09 "pull"             FirstOpByRef                  MinVer 5
        varOp  0x09 "pull"             Store                         6      6
        varOp  0x09 "pull"             FirstOpByRef                  7      MaxVer
        varOp  0x0A "split_window"     None                          3      MaxVer
        varOp  0x0B "set_window"       None                          3      MaxVer
        varOp  0x0C "call_vs2"        (Call ||| Store ||| DoubleVar) 4      MaxVer
        varOp  0x0D "erase_window"     None                          4      MaxVer
        varOp  0x0E "erase_line"       None                          4      5
        varOp  0x0E "erase_line"       None                          6      MaxVer
        varOp  0x0F "set_cursor"       None                          4      5
        varOp  0x0F "set_cursor"       None                          6      6
        varOp  0x0F "set_cursor"       None                          7      MaxVer
        varOp  0x10 "get_cursor"       None                          4      MaxVer
        varOp  0x11 "set_text_style"   None                          4      MaxVer
        varOp  0x12 "buffer_mode"      None                          4      MaxVer
        varOp  0x13 "output_stream"    None                          3      4
        varOp  0x13 "output_stream"    None                          5      5
        varOp  0x13 "output_stream"    None                          6      6
        varOp  0x13 "output_stream"    None                          7      MaxVer
        varOp  0x14 "input_stream"     None                          3      MaxVer
        varOp  0x15 "sound_effect"     None                          3      MaxVer
        varOp  0x16 "read_char"        Store                         4      MaxVer
        varOp  0x17 "scan_table"      (Branch ||| Store)             4      MaxVer
        varOp  0x18 "not"              Store                         5      MaxVer
        varOp  0x19 "call_vn"          Call                          5      MaxVer
        varOp  0x1A "call_vn2"        (Call ||| DoubleVar)           5      MaxVer
        varOp  0x1B "tokenize"         None                          5      MaxVer
        varOp  0x1C "encode_text"      None                          5      MaxVer
        varOp  0x1D "copy_table"       None                          5      MaxVer
        varOp  0x1E "print_table"      None                          5      MaxVer
        varOp  0x1F "check_arg_count"  Branch                        5      MaxVer

        ext    0x00 "save"             Store                         5      MaxVer
        ext    0x01 "restore"          Store                         5      MaxVer
        ext    0x02 "log_shift"        Store                         5      MaxVer
        ext    0x03 "art_shift"        Store                         5      MaxVer
        ext    0x04 "set_font"         Store                         5      MaxVer
        ext    0x05 "draw_picture"     None                          6      MaxVer
        ext    0x06 "picture_data"     Branch                        6      MaxVer
        ext    0x07 "erase_picture"    None                          6      MaxVer
        ext    0x08 "set_margins"      None                          6      MaxVer
        ext    0x09 "save_undo"        Store                         5      MaxVer
        ext    0x0A "restore_undo"     Store                         5      MaxVer
        ext    0x0B "print_unicode"    None                          5      MaxVer
        ext    0x0C "check_unicode"    None                          5      MaxVer
        ext    0x10 "move_window"      None                          6      MaxVer
        ext    0x11 "window_size"      None                          6      MaxVer
        ext    0x12 "window_style"     None                          6      MaxVer
        ext    0x13 "get_wind_prop"    Store                         6      MaxVer
        ext    0x14 "scroll_window"    None                          6      MaxVer
        ext    0x15 "pop_stack"        None                          6      MaxVer
        ext    0x16 "read_mouse"       None                          6      MaxVer
        ext    0x17 "mouse_window"     None                          6      MaxVer
        ext    0x18 "push_stack"       Branch                        6      MaxVer
        ext    0x19 "put_wind_prop"    None                          6      MaxVer
        ext    0x1A "print_form"       None                          6      MaxVer
        ext    0x1B "make_menu"        Branch                        6      MaxVer
        ext    0x1C "picture_table"    None                          6      MaxVer

    let getTable (version : byte) =
        opcodeTables.[int version - 1]

[<Struct>]
type Variable = 
    | StackVariable
    | LocalVariable of lv: byte
    | GlobalVariable of gv: byte

    static member FromByte value =
        if value = 0uy then
            StackVariable
        elif value >= 1uy && value <= 15uy then
            LocalVariable(value - 1uy)
        else
            GlobalVariable(value - 16uy)

    member x.ToByte() =
        match x with
        | StackVariable    -> 0uy
        | LocalVariable v  -> v + 1uy
        | GlobalVariable v -> v + 16uy

    override x.ToString() =
        match x with
        | StackVariable    -> "SP"
        | LocalVariable v  -> sprintf "L%02x" v
        | GlobalVariable v -> sprintf "G%02x" v

    member x.WriteToBuilder(builder: StringBuilder) =
        match x with
        | StackVariable    -> builder |> StringBuilder.appendString "SP"
        | LocalVariable v  -> (builder |> StringBuilder.appendFormat "L%02x") v
        | GlobalVariable v -> (builder |> StringBuilder.appendFormat "G%02x") v

[<Struct>]
type Operand =
    | LargeConstantOperand of lco: uint16
    | SmallConstantOperand of sco: byte
    | VariableOperand of vo: Variable

    member x.Value =
        match x with
        | LargeConstantOperand v -> v
        | SmallConstantOperand v -> uint16 v
        | VariableOperand v      -> uint16 (v.ToByte())

    override x.ToString() =
        match x with
        | LargeConstantOperand v -> sprintf "%04x" v
        | SmallConstantOperand v -> sprintf "%02x" v
        | VariableOperand(v)     -> v.ToString()

    member x.WriteToBuilder(builder: StringBuilder) =
        match x with
        | LargeConstantOperand v -> (builder |> StringBuilder.appendFormat "%04x") v
        | SmallConstantOperand v -> (builder |> StringBuilder.appendFormat "%02x") v
        | VariableOperand v      -> v.WriteToBuilder builder

module OperandPatterns =

    let (|NoOps|_|) = function [||] -> Some() | _ -> None
    let (|OpAndList|_|) (x:'a[]) =
        if x.Length >= 1 then Some(x.[0], Array.tail x) else None
    let (|Op1|_|) (x:'a[]) =
        if x.Length = 1 then Some(x.[0]) else None
    let (|Op2|_|) (x:'a[]) =
        if x.Length = 2 then Some(x.[0], x.[1]) else None
    let (|Op3|_|) (x:'a[]) =
        if x.Length = 3 then Some(x.[0], x.[1], x.[2]) else None
    let (|Op4|_|) (x:'a[]) =
        if x.Length = 4 then Some(x.[0], x.[1], x.[2], x.[3]) else None

[<Struct>]
type Branch =
    | RTrueBranch of rtb: bool
    | RFalseBranch of rfb: bool
    | OffsetBranch of ofb: bool * ofs: int16

    member x.Condition =
        match x with
        | RTrueBranch c     -> c
        | RFalseBranch c    -> c
        | OffsetBranch(c,_) -> c

    override x.ToString() =
        match x with
        | RTrueBranch(c)    -> sprintf "[%b] rtrue" c
        | RFalseBranch(c)   -> sprintf "[%b] rfalse" c
        | OffsetBranch(c,o) -> sprintf "[%b] %x" c o

    member x.WriteToBuilder(builder: StringBuilder) =
        match x with
        | RTrueBranch(c)    -> (builder |> StringBuilder.appendFormat "[%b] rtrue") c
        | RFalseBranch(c)   -> (builder |> StringBuilder.appendFormat "[%b] rfalse") c
        | OffsetBranch(c,o) -> (builder |> StringBuilder.appendFormat "[%b] %x") c o

[<Struct>]
type Instruction(address: int, length: int, opcode: Opcode, operands: Operand[],
                 storeVariable: Variable voption, branch: Branch voption, text: string voption) =

    member x.Address = address
    member x.Length = length
    member x.Opcode = opcode
    member x.Operands = operands
    member x.StoreVariable = storeVariable
    member x.Branch = branch
    member x.Text = text

    member x.JumpAddress =
        if not opcode.IsJump then
            failcompile "Not a jump instruction"

        let jumpOffset =
            match operands with
            | [| LargeConstantOperand(v) |] -> int16 v
            | _ -> failcompile "Expected single large constant operand"

        address + (length + int jumpOffset - 2)

    member x.BranchAddress =
        match branch with
        | ValueSome(OffsetBranch(_,o)) -> 
            ValueSome(address + (length + int o - 2))
        | _ -> ValueNone

    override x.ToString() =
        let builder = StringBuilder.create()

        (builder |> StringBuilder.appendFormat "%x: ") address

        builder |> StringBuilder.appendString opcode.Name

        builder |> StringBuilder.appendJoinChar ' ' operands

        match storeVariable with
        | ValueSome v -> 
            builder |> StringBuilder.appendString " -> "
            v.WriteToBuilder builder
        | _  -> ()

        match branch with
        | ValueSome b -> 
            builder |> StringBuilder.appendChar ' '
            b.WriteToBuilder builder
        | _ -> ()

        match text with
        | ValueSome t ->
            builder |> StringBuilder.appendString " \""
            builder |> StringBuilder.appendString (t.Replace("\n", "\\n"))
            builder |> StringBuilder.appendChar '"'
        | _ -> ()

        builder.ToString()

type InstructionReader (memory: Memory) =

    let textReader = new ZTextReader(memory)
    let opcodeTable = OpcodeTables.getTable (byte memory.Version)

    [<Literal>]
    let LargeConstantOp = 0uy
    [<Literal>]
    let SmallConstantOp = 1uy
    [<Literal>]
    let VariableOp = 2uy
    [<Literal>]
    let OmittedOp = 3uy

    let longForm opByte =
        byte (opByte &&& 0x1F)

    let shortForm opByte =
        byte (opByte &&& 0x0F)

    let varForm opByte =
        byte (opByte &&& 0x1F)

    // Fast lookup for first instruction byte. In cases where
    // the opcode is not VarOp or Ext, the operand kinds are
    // precomputed.
    let lookupTable =
        let arr = Array.zeroCreate 256

        let operandKinds = [|SmallConstantOp; SmallConstantOp|]
        for i = 0x00 to 0x1F do
            arr.[i] <- struct(OpcodeKind.TwoOp, longForm i, operandKinds)

        let operandKinds = [|SmallConstantOp; VariableOp|]
        for i = 0x20 to 0x3F do
            arr.[i] <- struct(OpcodeKind.TwoOp, longForm i, operandKinds)

        let operandKinds = [|VariableOp; SmallConstantOp|]
        for i = 0x40 to 0x5F do
            arr.[i] <- struct(OpcodeKind.TwoOp, longForm i, operandKinds)

        let operandKinds = [|VariableOp; VariableOp|]
        for i = 0x60 to 0x7F do
            arr.[i] <- struct(OpcodeKind.TwoOp, longForm i, operandKinds)

        let operandKinds = [|LargeConstantOp|]
        for i = 0x80 to 0x8F do
            arr.[i] <- struct(OpcodeKind.OneOp, shortForm i, operandKinds)

        let operandKinds = [|SmallConstantOp|]
        for i = 0x90 to 0x9F do
            arr.[i] <- struct(OpcodeKind.OneOp, shortForm i, operandKinds)

        let operandKinds = [|VariableOp|]
        for i = 0xA0 to 0xAF do
            arr.[i] <- struct(OpcodeKind.OneOp, shortForm i, operandKinds)

        let operandKinds = Array.empty
        for i = 0xB0 to 0xBF do
            arr.[i] <- struct(OpcodeKind.ZeroOp, shortForm i, operandKinds)

        // Actually, 0xBE should be Ext
        arr.[0xBE] <- struct(OpcodeKind.Ext, 0uy, operandKinds)

        // For the rest, the operand kinds will be calculated by checking the
        // next bytes of the instruction
        for i = 0xC0 to 0xDF do
            arr.[i] <- struct(OpcodeKind.TwoOp, varForm i, operandKinds)

        for i = 0xE0 to 0xFF do
            arr.[i] <- struct(OpcodeKind.VarOp, varForm i, operandKinds)

        arr

    let readOperandKinds (reader: IMemoryReader) =
        [| 
            let b = reader.NextByte()
            let shift = ref 6
            let stop = ref false
            while !shift >= 0 && not (!stop) do
                let kind = (b >>> !shift) &&& 0x03uy
                if kind <> OmittedOp then
                    yield kind
                    shift := !shift - 2
                else
                    stop := true 
        |]

    let readDoubleOperandKinds (reader: IMemoryReader) =
        [| 
            let w = reader.NextWord()
            let shift = ref 14
            let stop = ref false
            while !shift >= 0 && not (!stop) do
                let kind = byte (w >>> !shift) &&& 0x03uy
                if kind <> OmittedOp then
                    yield kind
                    shift := !shift - 2
                else
                    stop := true 
        |]

    let readOperand kind (reader: IMemoryReader) =
        match kind with
        | LargeConstantOp -> LargeConstantOperand(reader.NextWord())
        | SmallConstantOp -> SmallConstantOperand(reader.NextByte())
        | VariableOp      -> VariableOperand(reader.NextByte() |> Variable.FromByte)
        | k               -> failcompile "Unexpected operand kind: %d" k

    let readStoreVariable (reader: IMemoryReader) =
        reader.NextByte() |> Variable.FromByte

    let readBranch (reader: IMemoryReader) =
        let b1 = reader.NextByte()

        let condition = (b1 &&& 0x80uy) = 0x80uy

        let offset =
            if (b1 &&& 0x40uy) = 0x40uy then // single byte
                int16 (b1 &&& 0x3fuy) // bottom 6 bits
            else
                // OR bottom 7 bits with the next byte
                let b1 = byte (b1 &&& 0x3fuy)
                let b2 = reader.NextByte()
                let temp = (uint16 b1 <<< 8) ||| uint16 b2

                // if bit 13, set bits 14 and 15 as well to produce a proper signed value
                let temp' =
                    if (temp &&& 0x2000us) = 0x2000us then
                        uint16 (temp ||| 0xc000us)
                    else
                        temp

                int16 temp'

        match offset with
        | 0s  -> RFalseBranch(condition)
        | 1s  -> RTrueBranch(condition)
        | ofs -> OffsetBranch(condition, ofs)

    let readText (reader: IMemoryReader) =
        reader |> textReader.ReadString

    member x.ReadInstruction (reader: IMemoryReader) =
        if reader.Memory <> memory then
            failcompile "Expected IMemoryReader from same memory"

        let address = reader.Address
        let opcodeByte1 = reader.NextByte()

        let struct(kind, number, operandKinds) = lookupTable.[int opcodeByte1]

        let opcode =
            if kind <> OpcodeKind.Ext then
                opcodeTable.[kind, number]
            else
                opcodeTable.[kind, reader.NextByte()]

        let operandKinds =
            if opcodeByte1 <= 0xBFuy && opcodeByte1 <> 0xBEuy then
                operandKinds
            elif not opcode.IsDoubleVar then
                reader |> readOperandKinds
            else
                reader |> readDoubleOperandKinds

        let operands = operandKinds |> Array.map (fun k -> reader |> readOperand k)

        let storeVariable =
            if opcode.HasStoreVariable then
                ValueSome(reader |> readStoreVariable)
            else
                ValueNone

        let branch =
            if opcode.HasBranch then
                ValueSome(reader |> readBranch)
            else
                ValueNone

        let text =
            if opcode.HasText then
                ValueSome(reader |> readText)
            else
                ValueNone

        let length = reader.Address - address

        Instruction(address, length, opcode, operands, storeVariable, branch, text)

    member x.ReadInstruction (address: int) =
        let reader = address |> memory.CreateMemoryReader
        x.ReadInstruction reader

type Routine(address: int, instructions: Instruction[], locals: uint16[]) =

    let jumpTargets =
        let targets = SortedSet.create()

        let mutable current = instructions.[0]
        let mutable rest = instructions.AsSpan(1)

        targets |> SortedSet.add current.Address

        let mutable stop = false
        while not stop do
            if current.Branch.IsSome then
                // conditional branch to address
                if current.BranchAddress.IsSome then
                    targets |> SortedSet.add current.BranchAddress.Value

                // add "else" portion of conditional (i.e. the next instruction).
                if rest.Length > 0 then
                    let next = rest.[0]
                    targets |> SortedSet.add next.Address

            elif current.Opcode.IsJump then
                // unconditional branch
                targets |> SortedSet.add current.JumpAddress

            if not rest.IsEmpty then
                current <- rest.[0]
                rest <- rest.Slice(1)
            else
                stop <- true

        targets :> seq<_>

    member x.Address = address
    member x.Instructions = instructions
    member x.Locals = locals
    member x.JumpTargets = jumpTargets

type RoutineReader(memory: Memory) =

    let readLocals (reader: IMemoryReader) =
        let localCount = int (reader.NextByte())
        if memory.Version <= 4 then
            reader.NextWords(localCount)
        else
            let output = Array.zeroCreate localCount
            output

    let readInstructions (reader: IMemoryReader) =
        let instructionReader = new InstructionReader(memory)
        let result = new ResizeArray<_>()

        let mutable lastKnownAddress = reader.Address
        let mutable stop = false
        while not stop do
            let i = reader |> instructionReader.ReadInstruction
            result.Add(i)

            if reader.Address > lastKnownAddress && (i.Opcode.IsReturn || i.Opcode.IsQuit) then
                stop <- true
            elif i.Opcode.IsJump then
                let jumpAddress = i.JumpAddress
                if jumpAddress > lastKnownAddress then
                    lastKnownAddress <- jumpAddress
                if reader.Address > lastKnownAddress then
                    stop <- true
            else
                match i.BranchAddress with
                | ValueSome a ->
                    if a > lastKnownAddress then
                        lastKnownAddress <- a
                | ValueNone -> ()

        result.ToArray()

    member x.ReadRoutine (reader: IMemoryReader) =
        if reader.Memory <> memory then
            failcompile "Expected IMemoryReader from same memory"

        let address = reader.Address
        let locals = readLocals reader
        let instructions = readInstructions reader

        new Routine(address, instructions, locals)

    member x.ReadRoutine (address: int) =
        let reader = address |> memory.CreateMemoryReader
        x.ReadRoutine reader
