﻿namespace NZag.Core

open System
open System.Buffers
open System.Threading.Tasks
open NZag.Utilities

type IInputStream =
    abstract member ReadCharAsync : unit -> Task<char>
    abstract member ReadTextAsync : maxChars:int -> Task<string>

type IOutputStream =
    abstract member WriteCharAsync : ch:char -> Task
    abstract member WriteTextAsync : text:string -> Task

[<Flags>]
type ZTextStyle =
    | Roman = 0x00
    | Reverse = 0x01
    | Bold = 0x02
    | Italic = 0x04
    | FixedPitch = 0x08

[<Flags>]
type ZColor =
    | None = 0
    | Default = 1
    | Black = 2
    | Red = 3
    | Green = 4
    | Yellow = 5
    | Blue = 6
    | Magenta = 7
    | Cyan = 8
    | White = 9
    | Gray = 10

type IScreen =
    inherit IInputStream
    inherit IOutputStream

    abstract member ClearAsync : window:int -> Task
    abstract member ClearAllAsync : unsplit:bool -> Task

    abstract member SplitAsync : lines:int -> Task
    abstract member UnsplitAsync : unit -> Task

    abstract member SetWindowAsync : window:int -> Task

    abstract member ShowStatusAsync : unit -> Task

    abstract member GetCursorLineAsync : unit -> Task<int>
    abstract member GetCursorColumnAsync : unit -> Task<int>
    abstract member SetCursorAsync : line:int * column:int -> Task

    abstract SetTextStyleAsync : style:ZTextStyle -> Task
    abstract SetForegroundColorAsync : color:ZColor -> Task
    abstract SetBackgroundColorAsync : color:ZColor -> Task

    abstract member ScreenHeightInLines : byte
    abstract member ScreenWidthInColumns : byte
    abstract member ScreenHeightInUnits : uint16
    abstract member ScreenWidthInUnits : uint16
    abstract member FontHeightInUnits : byte
    abstract member FontWidthInUnits : byte

module NullInstances =

    let private emptyCharTask = Task.FromResult(char 0)
    let private emptyIntTask = Task.FromResult(0)
    let private emptyStringTask = Task.FromResult(String.Empty)
    let private emptyTask = Task.CompletedTask

    let InputStream =
        { new IInputStream with
            member x.ReadCharAsync() = emptyCharTask
            member x.ReadTextAsync _ = emptyStringTask }

    let OutputStream =
        { new IOutputStream with
            member x.WriteCharAsync _ = emptyTask
            member x.WriteTextAsync _ = emptyTask }

    let Screen =
        { new IScreen with
            member x.ReadCharAsync() = emptyCharTask
            member x.ReadTextAsync _ = emptyStringTask

            member x.WriteCharAsync _ = emptyTask
            member x.WriteTextAsync _ = emptyTask

            member x.ClearAsync _ = emptyTask
            member x.ClearAllAsync _ = emptyTask

            member x.SplitAsync _ = emptyTask
            member x.UnsplitAsync() = emptyTask

            member x.SetWindowAsync _ = emptyTask

            member x.ShowStatusAsync() = emptyTask

            member x.GetCursorLineAsync() = emptyIntTask
            member x.GetCursorColumnAsync() = emptyIntTask
            member x.SetCursorAsync(line, column) = emptyTask

            member x.SetTextStyleAsync _ = emptyTask
            member x.SetForegroundColorAsync _ = emptyTask
            member x.SetBackgroundColorAsync _ = emptyTask

            member x.ScreenHeightInLines = 0uy
            member x.ScreenWidthInColumns = 0uy
            member x.ScreenHeightInUnits = 0us
            member x.ScreenWidthInUnits = 0us
            member x.FontHeightInUnits = 0uy
            member x.FontWidthInUnits = 0uy }

type MemoryOutputStream(memory: Memory, address: int) =

    let mutable count = 0us

    let writeAddress() = address + (2 + int count)
    let writeCount() = memory.WriteWord(address, count)

    do writeCount()

    let charToByte ch =
        if ch = '\n' then 13uy else byte ch

    interface IOutputStream with
        member x.WriteCharAsync ch =
            memory.WriteByte(writeAddress(), charToByte ch)
            count <- count + 1us
            writeCount()
            Task.CompletedTask

        member x.WriteTextAsync(s: string) =
            Task.Run(fun () ->
                let chars = s.AsSpan()
                let buffer = ArrayPool<byte>.Shared.Rent(chars.Length)
                try
                    for i = 0 to chars.Length - 1 do
                        buffer.[i] <- charToByte chars.[i]
                    memory.WriteBytes(writeAddress(), ReadOnlySpan(buffer, 0, chars.Length))
                    count <- count + (uint16 chars.Length)
                    writeCount()
                finally
                    ArrayPool<byte>.Shared.Return(buffer)
            )

type OutputStreamCollection(memory: Memory) =
    let memoryStreams = Stack.create()

    let mutable screenStream = NullInstances.OutputStream
    let mutable screenActive = true

    let mutable transcriptStream = NullInstances.OutputStream
    let mutable transcriptActive = true

    member x.RegisterScreenStream(stream: IOutputStream) =
        screenStream <- stream
    member x.RegisterTranscriptStream(stream: IOutputStream) =
        transcriptStream <- stream

    member x.SelectScreenStream() =
        screenActive <- true
    member x.DeselectScreenStream() =
        screenActive <- false

    member x.SelectTranscriptStream() =
        transcriptActive <- true

        // set transcript flag
        let flags2 = memory.ReadWord(0x10)
        let flags2' = flags2 ||| 0x01us
        memory.WriteWord(0x10, flags2')

    member x.DeselectTranscriptStream() =
        transcriptActive <- false

        // set transcript flag
        let flags2 = memory.ReadWord(0x10)
        let flags2' = flags2 &&& ~~~0x01us
        memory.WriteWord(0x10, flags2')

    member x.SelectMemoryStream(address: int) =
        if memoryStreams.Count = 16 then
            failruntime "Cannot create more than 16 memory output streams."

        memoryStreams |> Stack.push (new MemoryOutputStream(memory, address) :> IOutputStream)

    member x.DeselectMemoryStream() =
        memoryStreams |> Stack.pop |> ignore

    interface IOutputStream with

        // If a member stream is active, we don't output to other streams

        member x.WriteCharAsync ch =
            if memoryStreams |> Stack.isEmpty then
                if screenActive then
                    screenStream.WriteCharAsync(ch)
                elif transcriptActive then
                    transcriptStream.WriteCharAsync(ch)
                else
                    Task.CompletedTask
            else
                memoryStreams |> Stack.peek |> (fun stream -> stream.WriteCharAsync(ch))

        member x.WriteTextAsync s =
            if memoryStreams |> Stack.isEmpty then
                if screenActive then
                    screenStream.WriteTextAsync(s)
                elif transcriptActive then
                    transcriptStream.WriteTextAsync(s)
                else
                    Task.CompletedTask
            else
                memoryStreams |> Stack.peek |> (fun stream -> stream.WriteTextAsync(s))
