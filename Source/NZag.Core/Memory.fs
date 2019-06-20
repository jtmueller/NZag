namespace NZag.Core

open System
open System.IO
open NZag.Utilities

module private MemoryModule =
    let inline readWord (bytes: ReadOnlySpan<byte>) =
        (uint16 bytes.[0] <<< 8) ||| uint16 bytes.[1]

    let inline readDWord (bytes: ReadOnlySpan<byte>) =
        ((uint32 bytes.[0]) <<< 24) |||
        ((uint32 bytes.[1]) <<< 16) |||
        ((uint32 bytes.[2]) <<< 8) |||
         (uint32 bytes.[3])

open MemoryModule

type IMemoryReader =
    /// Read the next byte without incrementing the address
    abstract member PeekByte : unit -> byte
    /// Read the next word without incrementing the address
    abstract member PeekWord : unit -> uint16
    /// Read the next dword without incrementing the address
    abstract member PeekDWord : unit -> uint32

    /// Read the next byte
    abstract member NextByte : unit -> byte
    /// Read the next word
    abstract member NextWord : unit -> uint16
    /// Read the next dword
    abstract member NextDWord : unit -> uint32

    /// Read a byte array of the given count
    abstract member NextBytes : int -> ReadOnlySpan<byte>
    /// Read a word array of the given count
    abstract member NextWords : int -> uint16[]

    /// Increment the address by a given number of positive bytes
    abstract member SkipBytes : int -> unit

    /// The current address to read from
    abstract member Address : int
    /// Determines whether the address of this reader is at or past the end of the memory.
    abstract member AtEndOfMemory : bool

    /// The memory this reader was created from
    abstract member Memory : Memory

and Memory private (stream : Stream) =

    // We split memory into 64k chunks to avoid creating very large arrays.
    [<Literal>]
    let ChunkSize = 0x10000

    let version =
        do stream.Seek(0L, SeekOrigin.Begin) |> ignore

        match stream.NextByte() with
        | ValueSome b -> 
            if b >= 1uy && b <= 8uy then int b
            else failwithf "Invalid version number: %d" b

        | ValueNone    -> failwith "Could not read version"

    let size =
        do stream.Seek(0x1AL, SeekOrigin.Begin) |> ignore

        let packedSize =
            match stream.NextWord() with
            | ValueSome w -> int w
            | ValueNone   -> failwith "Could not read size"

        match version with
        | 1 | 2 | 3 -> packedSize * 2
        | 4 | 5     -> packedSize * 4
        | 6 | 7 | 8 -> packedSize * 8
        | v -> failwithf "Invalid version number: %d" v

    let packedMultiplier =
        match version with
        | 1 | 2 | 3 -> 2
        | 4 | 5 | 6 | 7 -> 4
        | 8 -> 8
        | v -> failwithf "Invalid version number: %d" v

    let routinesOffset =
        do stream.Seek(0x28L, SeekOrigin.Begin) |> ignore

        match stream.NextWord() with
        | ValueSome w -> int w * 8
        | ValueNone   -> failwith "Could not read routines offset"

    let stringsOffset =
        do stream.Seek(0x2AL, SeekOrigin.Begin) |> ignore

        match stream.NextWord() with
        | ValueSome w -> int w * 8
        | ValueNone   -> failwith "Could not read static strings offset"

    let chunks =
        do stream.Seek(0L, SeekOrigin.Begin) |> ignore

        let count =
            let count' = size / ChunkSize
            if size % ChunkSize > 0 then count' + 1
            else count'

        let readChunk() =
            let chunk = Array.zeroCreate ChunkSize

            let mutable bytesReadSoFar = 0
            let mutable stop = false

            while not stop do
                let read = stream.Read(chunk, bytesReadSoFar, ChunkSize - bytesReadSoFar)

                if read <= 0 then
                    stop <- true
                else
                    bytesReadSoFar <- bytesReadSoFar + read
                    if bytesReadSoFar = ChunkSize then
                        stop <- true
                    else
                        // We got less than expected, are we at the end of the stream?
                        match stream.NextByte() with
                        | ValueSome b -> 
                            // Nope, it's not the end of the stream. Set the byte we
                            // just read, increment, and carry on.
                            chunk.[bytesReadSoFar] <- b
                            bytesReadSoFar <- bytesReadSoFar + 1

                        | ValueNone -> 
                            // Yup, this is the end of the stream, so we're done.
                            stop <- true

            chunk

        Array.init count (fun _ -> readChunk())

    let mutable currentChunk = chunks.[0]
    let mutable currentChunkStart = 0
    let mutable currentChunkEnd = ChunkSize

    let selectChunk address =
        let chunkIndex = address / ChunkSize
        currentChunk <- chunks.[chunkIndex]
        currentChunkStart <- chunkIndex * ChunkSize
        currentChunkEnd <- currentChunkStart + ChunkSize

    let readByte address =
        if address < currentChunkStart || address >= currentChunkEnd then
            selectChunk address

        currentChunk.[address - currentChunkStart]

    let writeByte address value =
        if address < currentChunkStart || address >= currentChunkEnd then
            selectChunk address

        currentChunk.[address - currentChunkStart] <- value

    member x.Read(buffer: Span<byte>, offset, count, address) =
        if offset < 0 then
            argOutOfRange "offset" "offset is less than zero"
        if count < 0 then
            argOutOfRange "count" "count is less than zero"
        if offset + count > buffer.Length then
            argOutOfRange "count" "count is larger than buffer size"
        if count > size then
            argOutOfRange "count" "count is larger than the Memory size"
        if address > size - count then
            argOutOfRange "address" "Expected address to be in range 0 to %d" (size - count)

        let chunkIndex = (address + offset) / ChunkSize
        let chunkOffset =
            let chunkStart = chunkIndex * ChunkSize
            (address + offset) - chunkStart

        if chunkOffset <= ChunkSize - count then
            // We take a faster path if all the bytes can be read from the current chunk
            chunks.[chunkIndex].AsSpan(chunkOffset, count).CopyTo(buffer)
        else
            // Otherwise we have to copy the bytes from more than
            // one chunk

            let mutable readSoFar = offset

            while readSoFar < count do
                let chunkIndex = (address + readSoFar) / ChunkSize
                let chunk = chunks.[chunkIndex]
                let chunkStart = chunkIndex * ChunkSize
                let chunkEnd = chunkStart + ChunkSize
                let chunkOffset = (address + readSoFar) - chunkStart
                let amountToRead = min (count - readSoFar) (chunkEnd - (chunkStart + chunkOffset))

                chunk.AsSpan(chunkOffset, amountToRead).CopyTo(buffer.Slice(readSoFar))
                readSoFar <- readSoFar + amountToRead

    member x.ReadByte address =
        if address > size - 1 then
            argOutOfRange "address" "Expected address to be in range 0 to %d" (size - 1)

        readByte address

    member x.ReadBytes(address, count) =
        if count > size then
            argOutOfRange "count" "count is larger than the Memory size"
        if address > size - count then
            argOutOfRange "address" "Expected address to be in range 0 to %d" (size - count)

        let chunkIndex = address / ChunkSize
        let offset =
            let chunkStart = chunkIndex * ChunkSize
            address - chunkStart
        
        if offset <= ChunkSize - count then
            // We take a faster path if all the bytes can be read from the current chunk
            Span.asReadonly (chunks.[chunkIndex].AsSpan(offset, count))
        else
            // Otherwise we have to copy the bytes from more than
            // one chunk into a single array

            let buffer = Array.zeroCreate count
            let mutable readSoFar = 0

            while readSoFar < count do
                let chunkIndex = (address + readSoFar) / ChunkSize
                let chunk = chunks.[chunkIndex]
                let chunkStart = chunkIndex * ChunkSize
                let chunkEnd = chunkStart + ChunkSize
                let offset = (address + readSoFar) - chunkStart
                let amountToRead = min (count - readSoFar) (chunkEnd - (chunkStart + offset))

                Array.blit chunk offset buffer readSoFar amountToRead
                readSoFar <- readSoFar + amountToRead

            ReadOnlySpan(buffer)

    member x.ReadWord address =
        if address > size - 2 then
            argOutOfRange "address" "Expected address to be in range 0 to %d" (size - 2)

        let bytes = x.ReadBytes(address, 2)
        readWord bytes

    member x.ReadWords(address, count) =
        if (count * 2) > size then
            argOutOfRange "count" "count is larger than the Memory size"
        if address > size - (count * 2) then
            argOutOfRange "address" "Expected address to be in range 0 to % d" (size - count)

        let bytes = x.ReadBytes(address, count * 2)
        let words = Array.zeroCreate<uint16> count
        for i = 0 to count - 1 do
            words.[i] <- readWord (bytes.Slice(i * 2, 2))
        words

    member x.ReadDWord address =
        if address > size - 4 then
            argOutOfRange "address" "Expected address to be in range 0 to %d" (size - 4)

        let bytes = x.ReadBytes(address, 4)
        readDWord bytes

    member x.WriteByte(address, value) =
        if address > size - 1 then
            argOutOfRange "address" "Expected address to be in range 0 to %d" (size - 1)

        writeByte address value

    member x.WriteBytes(address, value: ReadOnlySpan<Byte>) =
        let count = value.Length

        if address > (size - count) then
            argOutOfRange "address" "Expected address to be in range 0 to %d" (size - count)

        let mutable index = 0

        while index < count do
            let chunkIndex = (address + index) / ChunkSize
            let chunk = chunks.[chunkIndex]
            let chunkStart = chunkIndex * ChunkSize
            let chunkEnd = chunkStart + ChunkSize
            let chunkOffset = (address + index) - chunkStart
            let amountToWrite = min (count - index) (chunkEnd - (chunkStart + chunkOffset))
            value.Slice(index, amountToWrite).CopyTo(chunk.AsSpan(chunkOffset))
            index <- index + amountToWrite

    member x.WriteWord(address, value: uint16) =
        if address > size - 2 then
            argOutOfRange "address" "Expected address to be in range 0 to %d" (size - 2)

        // We take a faster path if the entire word can be written to the current chunk
        if address >= currentChunkStart && address < currentChunkEnd - 2 then
            let chunk = currentChunk
            let chunkAddress = address - currentChunkStart

            chunk.[chunkAddress]    <- byte (value >>> 8)
            chunk.[chunkAddress+1]  <- byte (value &&& 0xffus)
        else
            byte (value >>> 8)      |> writeByte  address
            byte (value &&& 0xffus) |> writeByte (address+1)

    member x.WriteDWord(address, value: uint32) =
        if address > size - 4 then
            argOutOfRange "address" "Expected argument to be in range 0 to %d" (size - 4)

        // We take a faster path if the entire dword can be written to the current chunk
        if address >= currentChunkStart && address < currentChunkEnd - 4 then
            let chunk = currentChunk
            let chunkAddress = address - currentChunkStart

            chunk.[chunkAddress]   <- byte (value >>> 24)
            chunk.[chunkAddress+1] <- byte (value >>> 16)
            chunk.[chunkAddress+2] <- byte (value >>> 8)
            chunk.[chunkAddress+3] <- byte (value &&& 0xffu)
        else
            byte (value >>> 24)    |> writeByte  address
            byte (value >>> 16)    |> writeByte (address+1)
            byte (value >>> 8)     |> writeByte (address+2)
            byte (value &&& 0xffu) |> writeByte (address+3) 

    member x.Size = size
    member x.Version = version

    member x.CreateMemoryReader address =
        let readerAddress = ref address
        let mutable readerChunk = ref None
        let readerChunkOffset = ref 0

        let readPastEndOfMemory() =
            failwith "Attempted to read past end of memory."

        let getChunk() =
            match !readerChunk with
            | Some chunk -> chunk
            | None -> readPastEndOfMemory()

        let selectChunk() =
            let readerAddress' = !readerAddress
            let chunkIndex = readerAddress' / ChunkSize
            readerChunkOffset := readerAddress' % ChunkSize
            readerChunk := if chunkIndex < chunks.Length then Some(chunks.[chunkIndex]) else None

        let increment count =
            readerAddress := !readerAddress + count
            let readerChunkOffset' = !readerChunkOffset + count
            if readerChunkOffset' < ChunkSize then
                readerChunkOffset := readerChunkOffset'
            else
                selectChunk()

        let readNextByte() =
            match !readerChunk with
            | Some(chunk) ->
                let result = chunk.[!readerChunkOffset]
                increment 1
                result
            | None ->
                readPastEndOfMemory()

        let peek f =
            let oldReaderAddress = !readerAddress
            let oldReaderChunk = !readerChunk
            let oldReaderChunkOffset = !readerChunkOffset

            let result = f()

            readerAddress := oldReaderAddress
            readerChunk := oldReaderChunk
            readerChunkOffset := oldReaderChunkOffset

            result

        selectChunk()

        { new IMemoryReader with
            member y.PeekByte() =
                peek (fun () -> y.NextByte())
            member y.PeekWord() =
                peek (fun () -> y.NextWord())
            member y.PeekDWord() =
                peek (fun () -> y.NextDWord())

            member y.NextByte() =
                if !readerAddress > size - 1 then
                    readPastEndOfMemory()

                readNextByte()

            member y.NextWord() =
                let bytes = y.NextBytes(2)
                readWord bytes

            member y.NextDWord() =
                let bytes = y.NextBytes(4)
                readDWord bytes

            member y.NextBytes count =
                if !readerAddress > size - count then
                    readPastEndOfMemory()

                let mutable curChunk = getChunk()
                let mutable offset = !readerChunkOffset

                if offset <= ChunkSize - count then
                    // We take a faster path if all the bytes can be read from the current chunk
                    let result = curChunk.AsSpan(offset, count)
                    increment count
                    Span.asReadonly result
                else
                    // Otherwise we have to copy the bytes from more than
                    // one chunk into a single array
                    let bytes = Array.zeroCreate<byte> count
                    let mutable buffer = bytes.AsSpan()
                    let mutable remaining = count
                    while remaining > 0 do
                        let chunk = curChunk.AsSpan(offset)
                        let read = Math.Min(chunk.Length, buffer.Length)
                        chunk.Slice(0, read).CopyTo(buffer)
                        increment read
                        curChunk <- getChunk()
                        offset <- !readerChunkOffset
                        remaining <- remaining - read
                        buffer <- buffer.Slice(read)

                    ReadOnlySpan(bytes)

            member y.NextWords count =
                let bytes = y.NextBytes(count * 2)
                let words = Array.zeroCreate<uint16> count
                for i = 0 to count - 1 do
                    words.[i] <- readWord (bytes.Slice(i * 2, 2))
                words

            member y.SkipBytes count =
                if count < 0 then
                    argOutOfRange "count" "count was less than 0."
                if count > 0 then
                    increment count

            member y.Address = !readerAddress
            member y.AtEndOfMemory = !readerAddress >= size

            member y.Memory = x
        }

    static member CreateFrom(stream: Stream) =
        let position = stream.Position
        let memory = new Memory(stream)
        stream.Position <- position

        memory

module Header =

    let private offset_InitialPC = 0x06
    let private offset_DictionaryAddress = 0x08
    let private offset_ObjectTableAddress = 0x0A
    let private offset_GlobalVariableTableAddress = 0x0C
    let private offset_AbbreviationTableAddress = 0x18
    let private offset_FileSize = 0x1A
    let private offset_Checksum = 0x1C
    let private offset_RoutinesOffset = 0x28
    let private offset_StringsOffset = 0x2A
    let private offset_AlphabetTableAddress = 0x34

    let readMainRoutineAddress (memory: Memory) =
        let initialPC = memory.ReadWord(offset_InitialPC)
        if memory.Version <> 6 then
            initialPC - 1us
        else
            initialPC

    let readDictionaryAddress (memory: Memory) =
        memory.ReadWord(offset_DictionaryAddress)

    let readObjectTableAddress (memory: Memory) =
        memory.ReadWord(offset_ObjectTableAddress)

    let readGlobalVariableTableAddress (memory: Memory) =
        memory.ReadWord(offset_GlobalVariableTableAddress)

    let readAbbreviationTableAddress (memory: Memory) =
        memory.ReadWord(offset_AbbreviationTableAddress)

    let readFileSize (memory: Memory) =
        let fileSize = memory.ReadWord(offset_FileSize)

        if memory.Version <= 3 then (int fileSize) * 2
        elif memory.Version <= 5 then (int fileSize) * 4
        else (int fileSize) * 8

    let readChecksum (memory: Memory) =
        memory.ReadWord(offset_Checksum)

    let readRoutinesOffset (memory: Memory) =
        memory.ReadWord(offset_RoutinesOffset)

    let readStringOffset (memory: Memory) =
        memory.ReadWord(offset_StringsOffset)

    let readAlphabetTableAddress (memory: Memory) =
        memory.ReadWord(offset_AlphabetTableAddress)

    let writeScreenHeightInLines value (memory: Memory) =
        memory.WriteByte(0x20, value)

    let writeScreenWidthInColumns value (memory: Memory) =
        memory.WriteByte(0x21, value)

    let writeScreenHeightInUnits value (memory: Memory) =
        memory.WriteWord(0x24, value)

    let writeScreenWidthInUnits value (memory: Memory) =
        memory.WriteWord(0x22, value)

    let writeFontHeightInUnits value (memory: Memory) =
        if memory.Version = 6 then
            memory.WriteByte(0x26, value)
        else
            memory.WriteByte(0x27, value)

    let writeFontWidthInUnits value (memory: Memory) =
        if memory.Version = 6 then
            memory.WriteByte(0x27, value)
        else
            memory.WriteByte(0x26, value)
