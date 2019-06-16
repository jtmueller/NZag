﻿namespace NZag.Utilities

open System

type IBitSet =
    inherit IEquatable<IBitSet>

    abstract member Add : bit:int -> unit
    abstract member Clear : unit -> unit
    abstract member Contains : bit:int -> bool
    abstract member Remove : bit:int -> unit
    abstract member RemoveWhere : predicate:Func<int,bool> -> unit
    abstract member UnionWith : other:IBitSet -> unit

    abstract member Item : index:int -> bool with get, set
    abstract member Length : int

    abstract member AllSet : seq<int>
    abstract member Clone : unit -> IBitSet

module BitSet =

    let private bitMustBeInRange = argOutOfRange "bit" "bit must be in the range 0 to %i."
    let private setsMustHaveSameLength = argOutOfRange "other" "Bit sets must have the same length. Expected %i but was %i"

    let inline private validateBit bit length =
        if uint32 bit >= uint32 length then
            bitMustBeInRange length

    let inline private validateBitSetLength (bitSet: IBitSet) length =
        if length <> bitSet.Length then
            setsMustHaveSameLength length bitSet.Length

    let inline private mask32 bit = 1ul <<< bit
    let inline private mask64 bit = 1UL <<< bit

    type private IBitSet32 =
        abstract member UnderlyingValue : uint32

    type private BitSet32(length: int) =
        [<Literal>]
        let zero = 0ul

        let mutable value = zero

        let add bit =
            validateBit bit length
            value <- value ||| (mask32 bit)

        let remove bit =
            validateBit bit length
            value <- value &&& ~~~(mask32 bit)

        let clear() =
            value <- zero

        let contains bit =
            validateBit bit length
            (value &&& (mask32 bit)) <> zero

        let equals other =
            validateBitSetLength other length
            value = (box other :?> IBitSet32).UnderlyingValue

        let removeWhere (predicate: Func<int,bool>) =
            for i = 0 to length - 1 do
                if contains i && predicate.Invoke(i) then
                    remove i

        let unionWith other =
            validateBitSetLength other length
            value <- value ||| (box other :?> IBitSet32).UnderlyingValue

        interface IEquatable<IBitSet> with
            member x.Equals other = equals other

        interface IBitSet32 with
            member x.UnderlyingValue = value

        interface IBitSet with
            member x.Add bit = add bit
            member x.Clear() = clear()
            member x.Contains bit = contains bit
            member x.Remove bit = remove bit
            member x.RemoveWhere predicate = removeWhere predicate
            member x.UnionWith other = unionWith other

            member x.Item
                with get index = contains index
                 and set index value =
                    if value then add index
                    else remove index

            member x.Length = length

            member x.AllSet =
                seq {
                    for i = 0 to length - 1 do
                        if contains i then
                            yield i
                }

            member x.Clone() =
                let res = new BitSet32(length) :> IBitSet
                res.UnionWith(x)
                res

    type private IBitSet64 =
        abstract member UnderlyingValue : uint64

    type private BitSet64(length: int) =
        [<Literal>]
        let zero = 0UL

        let mutable value = zero

        let add bit =
            validateBit bit length
            value <- value ||| (mask64 bit)

        let remove bit =
            validateBit bit length
            value <- value &&& ~~~(mask64 bit)

        let clear() =
            value <- zero

        let contains bit =
            validateBit bit length
            (value &&& (mask64 bit)) <> zero

        let equals other =
            validateBitSetLength other length
            value = (box other :?> IBitSet64).UnderlyingValue

        let removeWhere (predicate: Func<int,bool>) =
            for i = 0 to length - 1 do
                if contains i && predicate.Invoke(i) then
                    remove i

        let unionWith other =
            validateBitSetLength other length
            value <- value ||| (box other :?> IBitSet64).UnderlyingValue

        interface IEquatable<IBitSet> with
            member x.Equals other = equals other

        interface IBitSet64 with
            member x.UnderlyingValue = value

        interface IBitSet with
            member x.Add bit = add bit
            member x.Clear() = clear()
            member x.Contains bit = contains bit
            member x.Remove bit = remove bit
            member x.RemoveWhere predicate = removeWhere predicate
            member x.UnionWith other = unionWith other

            member x.Item
                with get index = contains index
                 and set index value =
                    if value then add index
                    else remove index

            member x.Length = length

            member x.AllSet =
                seq {
                    for i = 0 to length - 1 do
                        if contains i then
                            yield i
                }

            member x.Clone() =
                let res = new BitSet64(length) :> IBitSet
                res.UnionWith(x)
                res

    type private IBitSetN =
        abstract member UnderlyingValue : uint64[]

    type private BitSetN(length: int) =
        [<Literal>]
        let zero = 0UL
        [<Literal>]
        let resolution = 64

        let byteCount =
            let res = length / resolution
            if length % resolution > 0 then res + 1 else res

        let value = Array.zeroCreate byteCount

        let add bit =
            validateBit bit length
            let index = bit / resolution
            let bit = bit % resolution
            value.[index] <- value.[index] ||| (mask64 bit)

        let remove bit =
            validateBit bit length
            let index = bit / resolution
            let bit = bit % resolution
            value.[index] <- value.[index] &&& ~~~(mask64 bit)

        let clear() =
            Array.clear value

        let contains bit =
            validateBit bit length
            let index = bit / resolution
            let bit = bit % resolution
            (value.[index] &&& (mask64 bit)) <> zero

        let equals other =
            validateBitSetLength other length
            let otherValue = (box other :?> IBitSetN).UnderlyingValue
            Array.forall2 (fun v1 v2 -> v1 = v2) value otherValue

        let removeWhere (predicate: Func<int,bool>) =
            for i = 0 to length - 1 do
                if contains i && predicate.Invoke(i) then
                    remove i

        let unionWith other =
            validateBitSetLength other length
            let otherValue = (box other :?> IBitSetN).UnderlyingValue
            for i = 0 to byteCount - 1 do
                value.[i] <- value.[i] ||| otherValue.[i]

        interface IEquatable<IBitSet> with
            member x.Equals other = equals other

        interface IBitSetN with
            member x.UnderlyingValue = value

        interface IBitSet with
            member x.Add bit = add bit
            member x.Clear() = clear()
            member x.Contains bit = contains bit
            member x.Remove bit = remove bit
            member x.RemoveWhere predicate = removeWhere predicate
            member x.UnionWith other = unionWith other

            member x.Item
                with get index = contains index
                 and set index value =
                    if value then add index
                    else remove index

            member x.Length = length

            member x.AllSet =
                seq {
                    for i = 0 to length - 1 do
                        if contains i then
                            yield i
                }

            member x.Clone() =
                let res = new BitSetN(length) :> IBitSet
                res.UnionWith(x)
                res

    [<CompiledName("Create")>]
    let create length =
        if length < 0 then argOutOfRange "length" "length must be greater than or equal to zero."

        if length <= 32 then
            new BitSet32(length) :> IBitSet
        elif length <= 64 then
            new BitSet64(length) :> IBitSet
        else
            new BitSetN(length) :> IBitSet
