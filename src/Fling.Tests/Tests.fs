module Tests


open System.Diagnostics
open Expecto
open Hedgehog
open Fling.Fling



[<AutoOpen>]
module Domain =

    type ChildToOne = { Data: string }

    type ChildToOneOpt = { Data: string }

    type ChildToMany = { Id: int; Data: string }

    type Root = {
        Id: int
        Data: string
        ToOne: ChildToOne
        ToOneOpt: ChildToOneOpt option
        ToMany: ChildToMany list
    }



[<AutoOpen>]
module Dtos =

    type ChildToOneDto = { RootId: int; Data: string }

    type ChildToOneOptDto = { RootId: int; Data: string }

    type ChildToManyDto = { RootId: int; Id: int; Data: string }

    type RootDto = { Id: int; Data: string }

    let rootToDto (r: Root) : RootDto = { Id = r.Id; Data = r.Data }

    let rootToToOneDto (r: Root) : ChildToOneDto = { RootId = r.Id; Data = r.ToOne.Data }

    let rootToToOneOptDto (r: Root) : ChildToOneOptDto option =
        r.ToOneOpt |> Option.map (fun c -> { RootId = r.Id; Data = c.Data })

    let rootToToManyDtos (r: Root) : ChildToManyDto list =
        r.ToMany
        |> List.map (fun c -> {
            RootId = r.Id
            Id = c.Id
            Data = c.Data
        })

    let dtosToRoot
        (rootDto: RootDto)
        (toOneDto: ChildToOneDto)
        (toOneOptDto: ChildToOneOptDto option)
        (toManyDtos: ChildToManyDto list)
        : Root =
        {
            Id = rootDto.Id
            Data = rootDto.Data
            ToOne = { Data = toOneDto.Data }
            ToOneOpt = toOneOptDto |> Option.map (fun d -> { Data = d.Data })
            ToMany = toManyDtos |> List.map (fun d -> { Id = d.Id; Data = d.Data })
        }



module Gen =


    let rootDtoWithChildDtos =
        gen {

            let! rootDto = GenX.auto<RootDto>

            let! toOneDto = GenX.auto<ChildToOneDto>
            let toOneDto = { toOneDto with RootId = rootDto.Id }

            let! toOneOptDto = GenX.auto<ChildToOneOptDto option>

            let toOneOptDto =
                toOneOptDto |> Option.map (fun x -> { x with RootId = rootDto.Id })

            let! toManyDtos = GenX.auto<ChildToManyDto list> |> Gen.filter (List.isDistinctBy (fun x -> x.Id))
            let toManyDtos = toManyDtos |> List.map (fun x -> { x with RootId = rootDto.Id })

            return rootDto, toOneDto, toOneOptDto, toManyDtos
        }


    let rootDtoWithChildDtosWithUpdates =
        gen {
            let config =
                {
                    GenX.defaults with
                        SeqRange = Range.linear 1 7
                }
                |> AutoGenConfig.addGenerator (Gen.int32 (Range.linear 1 20))
                |> AutoGenConfig.addGenerator (Gen.alphaNum |> Gen.string (Range.linear 1 10))

            let! rootDto = GenX.autoWith<RootDto> config
            let! rootDto' = GenX.autoWith<RootDto> config
            let rootDto' = { rootDto' with Id = rootDto.Id }

            let! toOneDto = GenX.autoWith<ChildToOneDto> config
            let toOneDto = { toOneDto with RootId = rootDto.Id }

            let! toOneDto' = GenX.autoWith<ChildToOneDto> config
            let toOneDto' = { toOneDto' with RootId = rootDto'.Id }

            let! toOneOptDto = GenX.autoWith<ChildToOneOptDto option> config

            let toOneOptDto =
                toOneOptDto |> Option.map (fun x -> { x with RootId = rootDto.Id })

            let! toOneOptDto' = GenX.autoWith<ChildToOneOptDto option> config

            let toOneOptDto' =
                toOneOptDto' |> Option.map (fun x -> { x with RootId = rootDto.Id })

            let! toManyDtos =
                GenX.autoWith<ChildToManyDto list> config
                |> Gen.filter (List.isDistinctBy (fun x -> x.Id))

            let toManyDtos = toManyDtos |> List.map (fun x -> { x with RootId = rootDto.Id })

            let! toManyDtos' =
                GenX.autoWith<ChildToManyDto list> config
                |> Gen.filter (List.isDistinctBy (fun x -> x.Id))

            let toManyDtos' = toManyDtos' |> List.map (fun x -> { x with RootId = rootDto.Id })

            return rootDto, toOneDto, toOneOptDto, toManyDtos, rootDto', toOneDto', toOneOptDto', toManyDtos'
        }



[<Tests>]
let tests =
    testList (nameof Fling.Fling) [


        testList "load" [


            testCase "Returns correct result and calls DB functions with correct args"
            <| fun () ->
                Property.check
                <| property {

                    let! arg = GenX.auto<char>

                    let! rootDto, toOneDto, toOneOptDto, toManyDtos = Gen.rootDtoWithChildDtos

                    let getRootDto = fun (_arg: char) -> async.Return(Some rootDto)

                    let mutable getToOneForRootCalledWith = None

                    let getToOneForRoot arg (rootId: int) =
                        async {
                            getToOneForRootCalledWith <- Some(arg, rootId)
                            return toOneDto
                        }

                    let mutable getToOneOptForRootCalledWith = None

                    let getToOneOptForRoot arg (rootId: int) =
                        async {
                            getToOneOptForRootCalledWith <- Some(arg, rootId)
                            return toOneOptDto
                        }

                    let mutable getToManyForRootCalledWith = None

                    let getToManyForRoot arg (rootId: int) =
                        async {
                            getToManyForRootCalledWith <- Some(arg, rootId)
                            return toManyDtos
                        }

                    let load =
                        createLoader dtosToRoot (fun dto -> dto.Id)
                        |> loadChild getToOneForRoot
                        |> loadChild getToOneOptForRoot
                        |> loadChild getToManyForRoot
                        |> loadParallel

                    let expected = dtosToRoot rootDto toOneDto toOneOptDto toManyDtos

                    let actual = load arg getRootDto |> Async.RunSynchronously

                    Expect.equal actual (Some expected) ""
                    Expect.equal getToOneForRootCalledWith (Some(arg, rootDto.Id)) ""
                    Expect.equal getToOneOptForRootCalledWith (Some(arg, rootDto.Id)) ""
                    Expect.equal getToManyForRootCalledWith (Some(arg, rootDto.Id)) ""
                }


            testSequenced (
                testCase "Runs the loaders in parallel if using the parallel loader"
                <| fun () ->
                    let getChild _ _ = async { do! Async.Sleep 1000 }

                    let load =
                        createLoader (fun () () () () -> ()) (fun () -> ())
                        |> loadChild getChild
                        |> loadChild getChild
                        |> loadChild getChild
                        |> loadParallel

                    let sw = Stopwatch.StartNew()

                    load () (fun () -> async.Return(Some()))
                    |> Async.RunSynchronously
                    |> ignore<unit option>

                    sw.Stop()

                    Expect.isLessThan sw.ElapsedMilliseconds 2500L ""
            )


            testSequenced (
                testCase "Does not run the loaders in parallel if using the serial loader"
                <| fun () ->
                    let getChild _ _ = async { do! Async.Sleep 1000 }

                    let load =
                        createLoader (fun () () () () -> ()) (fun () -> ())
                        |> loadChild getChild
                        |> loadChild getChild
                        |> loadChild getChild
                        |> loadSerial

                    let sw = Stopwatch.StartNew()

                    load () (fun () -> async.Return(Some()))
                    |> Async.RunSynchronously
                    |> ignore<unit option>

                    sw.Stop()

                    // Should be > 3000, but often fails on CI with a few ms less. In any case, parallel will be
                    // ~1000ms, so 2500 seems OK.
                    Expect.isGreaterThan sw.ElapsedMilliseconds 2500L ""
            )


        ]


        testList "loadBatch" [


            testCase "Returns correct result and calls DB functions with correct args"
            <| fun () ->
                Property.check
                <| property {

                    let! arg = Gen.unicode

                    let! dtos = Gen.rootDtoWithChildDtos |> GenX.eList 1 10

                    let mutable getToOneForRootCalledWith = None

                    let getToOneForRoots arg (rootIds: int list) =
                        async {
                            getToOneForRootCalledWith <- Some(arg, rootIds)
                            return dtos |> List.map (fun (_, xs, _, _) -> xs)
                        }

                    let mutable getToOneOptForRootsCalledWith = None

                    let getToOneOptForRoots arg (rootIds: int list) =
                        async {
                            getToOneOptForRootsCalledWith <- Some(arg, rootIds)
                            return dtos |> List.choose (fun (_, _, xs, _) -> xs)
                        }

                    let mutable getToManyForRootsCalledWith = None

                    let getToManyForRoots arg (rootIds: int list) =
                        async {
                            getToManyForRootsCalledWith <- Some(arg, rootIds)
                            return dtos |> List.collect (fun (_, _, _, xs) -> xs)
                        }

                    let load =
                        createBatchLoader dtosToRoot (fun dto -> dto.Id)
                        |> batchLoadChild getToOneForRoots (fun dto -> dto.RootId)
                        |> batchLoadOptChild getToOneOptForRoots (fun dto -> dto.RootId)
                        |> batchLoadChildren getToManyForRoots (fun dto -> dto.RootId)
                        |> loadBatchParallel

                    let rootDtos = dtos |> List.map (fun (x, _, _, _) -> x)

                    let getRootDtos =
                        fun (_arg: char) -> dtos |> List.map (fun (x, _, _, _) -> x) |> async.Return

                    let rootDtoIds = rootDtos |> List.map (fun x -> x.Id)

                    let expected =
                        dtos
                        |> List.map (fun (rootDto, toOneDto, toOneOptDto, toManyDtos) ->
                            dtosToRoot rootDto toOneDto toOneOptDto toManyDtos
                        )

                    let actual = load arg getRootDtos |> Async.RunSynchronously

                    Expect.equal actual expected ""
                    Expect.equal getToOneForRootCalledWith (Some(arg, rootDtoIds)) ""
                    Expect.equal getToOneOptForRootsCalledWith (Some(arg, rootDtoIds)) ""
                    Expect.equal getToManyForRootsCalledWith (Some(arg, rootDtoIds)) ""
                }


            testSequenced (
                testCase "Runs the loaders in parallel if using the parallel loader"
                <| fun () ->
                    let getChild _ _ =
                        async {
                            do! Async.Sleep 1000
                            return [ () ]
                        }

                    let load =
                        createBatchLoader (fun _ _ _ _ -> ()) (fun () -> 0)
                        |> batchLoadChild getChild (fun () -> 0)
                        |> batchLoadOptChild getChild (fun () -> 0)
                        |> batchLoadChildren getChild (fun () -> 0)
                        |> loadBatchParallel

                    let sw = Stopwatch.StartNew()
                    load () (fun () -> async.Return [ () ]) |> Async.RunSynchronously |> ignore
                    sw.Stop()

                    Expect.isLessThan sw.ElapsedMilliseconds 2500L ""
            )


            testSequenced (
                testCase "Does not run the loaders in parallel if using the serial loader"
                <| fun () ->
                    let getChild _ _ =
                        async {
                            do! Async.Sleep 1000
                            return [ () ]
                        }

                    let load =
                        createBatchLoader (fun _ _ _ _ -> ()) (fun () -> 0)
                        |> batchLoadChild getChild (fun () -> 0)
                        |> batchLoadOptChild getChild (fun () -> 0)
                        |> batchLoadChildren getChild (fun () -> 0)
                        |> loadBatchSerial

                    let sw = Stopwatch.StartNew()

                    load () (fun () -> async.Return [ () ])
                    |> Async.RunSynchronously
                    |> ignore<unit list>

                    sw.Stop()

                    // Should be > 3000, but often fails on CI with a few ms less. In any case, parallel will be
                    // ~1000ms, so 2500 seems OK.
                    Expect.isGreaterThan sw.ElapsedMilliseconds 2500L ""
            )


        ]


        testList "save" [


            testCase "Returns correct result and calls DB functions in sequence with correct args when old is None"
            <| fun () ->
                Property.check
                <| property {

                    let! arg = GenX.auto<int>
                    let! result = GenX.auto<int>

                    let! rootDto, toOneDto, toOneOptDto, toManyDtos = Gen.rootDtoWithChildDtos

                    let mutable i = 1

                    let mutable insertRootCalledWith = []

                    let insertRoot arg dto =
                        async {
                            insertRootCalledWith <- insertRootCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                            return result
                        }

                    let mutable updateRootCalledWith = []

                    let updateRoot arg dto =
                        async {
                            updateRootCalledWith <- updateRootCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                            return result
                        }

                    let mutable insertToOneCalledWith = []

                    let insertToOne arg dto =
                        async {
                            insertToOneCalledWith <- insertToOneCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable updateToOneCalledWith = []

                    let updateToOne arg dto =
                        async {
                            updateToOneCalledWith <- updateToOneCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable insertToOneOptCalledWith = []

                    let insertToOneOpt arg dto =
                        async {
                            insertToOneOptCalledWith <- insertToOneOptCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable updateToOneOptCalledWith = []

                    let updateToOneOpt arg dto =
                        async {
                            updateToOneOptCalledWith <- updateToOneOptCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable deleteToOneOptCalledWith = []

                    let deleteToOneOpt arg dto =
                        async {
                            deleteToOneOptCalledWith <- deleteToOneOptCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable insertToManyCalledWith = []

                    let insertToMany arg dto =
                        async {
                            insertToManyCalledWith <- insertToManyCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable updateToManyCalledWith = []

                    let updateToMany arg dto =
                        async {
                            updateToManyCalledWith <- updateToManyCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable deleteToManyCalledWith = []

                    let deleteToMany arg dto =
                        async {
                            deleteToManyCalledWith <- deleteToManyCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }


                    let rootToDto () = rootDto
                    let rootToToOneDto () = toOneDto
                    let rootToToOneOptDto () = toOneOptDto
                    let rootToToManyDtos () = toManyDtos

                    let save =
                        saveRootWithOutput rootToDto insertRoot updateRoot
                        |> saveChild rootToToOneDto insertToOne updateToOne
                        |> saveOptChild
                            rootToToOneOptDto
                            (fun dto -> dto.RootId)
                            insertToOneOpt
                            updateToOneOpt
                            deleteToOneOpt
                        |> saveChildren rootToToManyDtos (fun dto -> dto.Id) insertToMany updateToMany deleteToMany

                    let actual = save arg None () |> Async.RunSynchronously

                    let mutable expectedId = 0

                    let nextExpectedId () =
                        expectedId <- expectedId + 1
                        expectedId

                    Expect.equal actual (Some result) "Incorrect result"
                    Expect.sequenceEqual insertRootCalledWith [ nextExpectedId (), arg, rootDto ] "insertRootCalledWith"
                    Expect.sequenceEqual updateRootCalledWith [] "updateRoot should not be called"

                    Expect.sequenceEqual
                        insertToOneCalledWith
                        [ nextExpectedId (), arg, toOneDto ]
                        "insertToOne called in wrong order or with wrong args"

                    Expect.sequenceEqual updateToOneCalledWith [] "updateToOne should not be called"

                    Expect.sequenceEqual
                        insertToOneOptCalledWith
                        [
                            for x in Option.toList toOneOptDto do
                                (nextExpectedId (), arg, x)
                        ]
                        "insertToOneOpt called in wrong order or with wrong args"

                    Expect.sequenceEqual updateToOneOptCalledWith [] "updateToOneOpt should not be called"
                    Expect.sequenceEqual deleteToOneOptCalledWith [] "deleteToOneOpt should not be called"

                    Expect.sequenceEqual
                        insertToManyCalledWith
                        [
                            for x in toManyDtos do
                                (nextExpectedId (), arg, x)
                        ]
                        "insertToMany called in wrong order or with wrong args"

                    Expect.sequenceEqual updateToManyCalledWith [] "updateToMany should not be called"
                    Expect.sequenceEqual deleteToManyCalledWith [] "deleteToMany should not be called"
                }


            testCase "Returns correct result and calls DB functions in sequence with correct args when old is Some"
            <| fun () ->
                Property.checkWith (PropertyConfig.defaultConfig |> PropertyConfig.withTests 1000<tests>)
                <| property {

                    let! arg = GenX.auto<int>
                    let! result = GenX.auto<int>

                    let! rootDto, toOneDto, toOneOptDto, toManyDtos, rootDto', toOneDto', toOneOptDto', toManyDtos' =
                        Gen.rootDtoWithChildDtosWithUpdates

                    let mutable i = 1

                    let mutable insertRootCalledWith = []

                    let insertRoot arg dto =
                        async {
                            insertRootCalledWith <- insertRootCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                            return result
                        }

                    let mutable updateRootCalledWith = []

                    let updateRoot arg dto =
                        async {
                            updateRootCalledWith <- updateRootCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                            return result
                        }

                    let mutable insertToOneCalledWith = []

                    let insertToOne arg dto =
                        async {
                            insertToOneCalledWith <- insertToOneCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable updateToOneCalledWith = []

                    let updateToOne arg dto =
                        async {
                            updateToOneCalledWith <- updateToOneCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable insertToOneOptCalledWith = []

                    let insertToOneOpt arg dto =
                        async {
                            insertToOneOptCalledWith <- insertToOneOptCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable updateToOneOptCalledWith = []

                    let updateToOneOpt arg dto =
                        async {
                            updateToOneOptCalledWith <- updateToOneOptCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable deleteToOneOptCalledWith = []

                    let deleteToOneOpt arg dto =
                        async {
                            deleteToOneOptCalledWith <- deleteToOneOptCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable insertToManyCalledWith = []

                    let insertToMany arg dto =
                        async {
                            insertToManyCalledWith <- insertToManyCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable updateToManyCalledWith = []

                    let updateToMany arg dto =
                        async {
                            updateToManyCalledWith <- updateToManyCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable deleteToManyCalledWith = []

                    let deleteToMany arg dto =
                        async {
                            deleteToManyCalledWith <- deleteToManyCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }


                    let rootToDto =
                        function
                        | 1 -> rootDto
                        | 2 -> rootDto'
                        | _ -> failwith "Invalid arg"

                    let rootToToOneDto =
                        function
                        | 1 -> toOneDto
                        | 2 -> toOneDto'
                        | _ -> failwith "Invalid arg"

                    let rootToToOneOptDto =
                        function
                        | 1 -> toOneOptDto
                        | 2 -> toOneOptDto'
                        | _ -> failwith "Invalid arg"

                    let rootToToManyDtos =
                        function
                        | 1 -> toManyDtos
                        | 2 -> toManyDtos'
                        | _ -> failwith "Invalid arg"

                    let save =
                        saveRootWithOutput rootToDto insertRoot updateRoot
                        |> saveChild rootToToOneDto insertToOne updateToOne
                        |> saveOptChild
                            rootToToOneOptDto
                            (fun dto -> dto.RootId)
                            insertToOneOpt
                            updateToOneOpt
                            deleteToOneOpt
                        |> saveChildren rootToToManyDtos (fun dto -> dto.Id) insertToMany updateToMany deleteToMany

                    let actual = save arg (Some 1) 2 |> Async.RunSynchronously

                    let mutable expectedId = 0

                    let nextExpectedId () =
                        expectedId <- expectedId + 1
                        expectedId

                    let expectedResult = if rootDto = rootDto' then None else Some result

                    Expect.equal actual expectedResult "Incorrect result"
                    Expect.sequenceEqual insertRootCalledWith [] "insertRootCalledWith"

                    Expect.sequenceEqual
                        updateRootCalledWith
                        [
                            if rootDto <> rootDto' then
                                nextExpectedId (), arg, rootDto'
                        ]
                        "updateRoot"

                    Expect.sequenceEqual insertToOneCalledWith [] "insertToOne"

                    Expect.sequenceEqual
                        updateToOneCalledWith
                        [
                            if toOneDto <> toOneDto' then
                                nextExpectedId (), arg, toOneDto'
                        ]
                        "updateToOne"

                    Expect.sequenceEqual
                        insertToOneOptCalledWith
                        [
                            if toOneOptDto.IsNone && toOneOptDto'.IsSome then
                                nextExpectedId (), arg, toOneOptDto'.Value
                        ]
                        "insertToOneOpt"

                    Expect.sequenceEqual
                        updateToOneOptCalledWith
                        [
                            if toOneOptDto.IsSome && toOneOptDto'.IsSome && toOneOptDto <> toOneOptDto' then
                                nextExpectedId (), arg, toOneOptDto'.Value
                        ]
                        "updateToOneOpt"

                    Expect.sequenceEqual
                        deleteToOneOptCalledWith
                        [
                            if toOneOptDto.IsSome && toOneOptDto'.IsNone then
                                nextExpectedId (), arg, toOneOptDto.Value.RootId
                        ]
                        "deleteToOneOpt"

                    let oldToManyById = toManyDtos |> List.map (fun x -> x.Id, x) |> Map.ofList
                    let newToManyById = toManyDtos' |> List.map (fun x -> x.Id, x) |> Map.ofList

                    let deletedIds =
                        toManyDtos
                        |> List.map (fun x -> x.Id)
                        |> List.filter (not << newToManyById.ContainsKey)

                    Expect.sequenceEqual
                        deleteToManyCalledWith
                        [
                            for id in deletedIds do
                                nextExpectedId (), arg, id
                        ]
                        "deleteToMany"

                    Expect.sequenceEqual
                        updateToManyCalledWith
                        [
                            for newToManyDto in toManyDtos' do
                                match oldToManyById.TryFind newToManyDto.Id with
                                | Some oldToManyDto when oldToManyDto <> newToManyDto ->
                                    nextExpectedId (), arg, newToManyDto
                                | _ -> ()
                        ]
                        "updateToMany"

                    Expect.sequenceEqual
                        insertToManyCalledWith
                        [
                            for newToManyDto in toManyDtos' do
                                match oldToManyById.TryFind newToManyDto.Id with
                                | None -> nextExpectedId (), arg, newToManyDto
                                | Some _ -> ()
                        ]
                        "insertToMany"
                }


        ]


        testList "saveWithDifferentOldNew" [


            testCase "Returns correct result and calls DB functions in sequence with correct args when old is None"
            <| fun () ->
                Property.check
                <| property {

                    let! arg = GenX.auto<int>
                    let! result = GenX.auto<int>

                    let! rootDto, toOneDto, toOneOptDto, toManyDtos = Gen.rootDtoWithChildDtos

                    let mutable i = 1

                    let mutable insertRootCalledWith = []

                    let insertRoot arg dto =
                        async {
                            insertRootCalledWith <- insertRootCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                            return result
                        }

                    let mutable updateRootCalledWith = []

                    let updateRoot arg dto =
                        async {
                            updateRootCalledWith <- updateRootCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                            return result
                        }

                    let mutable insertToOneCalledWith = []

                    let insertToOne arg dto =
                        async {
                            insertToOneCalledWith <- insertToOneCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable updateToOneCalledWith = []

                    let updateToOne arg dto =
                        async {
                            updateToOneCalledWith <- updateToOneCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable insertToOneOptCalledWith = []

                    let insertToOneOpt arg dto =
                        async {
                            insertToOneOptCalledWith <- insertToOneOptCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable updateToOneOptCalledWith = []

                    let updateToOneOpt arg dto =
                        async {
                            updateToOneOptCalledWith <- updateToOneOptCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable deleteToOneOptCalledWith = []

                    let deleteToOneOpt arg dto =
                        async {
                            deleteToOneOptCalledWith <- deleteToOneOptCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable insertToManyCalledWith = []

                    let insertToMany arg dto =
                        async {
                            insertToManyCalledWith <- insertToManyCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable updateToManyCalledWith = []

                    let updateToMany arg dto =
                        async {
                            updateToManyCalledWith <- updateToManyCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable deleteToManyCalledWith = []

                    let deleteToMany arg dto =
                        async {
                            deleteToManyCalledWith <- deleteToManyCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }


                    let rootToDto () = rootDto
                    let rootToToOneDto () = toOneDto
                    let rootToToOneOptDto () = toOneOptDto
                    let rootToToManyDtos () = toManyDtos

                    let save =
                        saveRootWithOutput rootToDto insertRoot updateRoot
                        |> saveChildWithDifferentOldNew
                            (fun _ -> failwith "Should not be called")
                            rootToToOneDto
                            insertToOne
                            updateToOne
                        |> saveOptChildWithDifferentOldNew
                            (fun _ -> failwith "Should not be called")
                            rootToToOneOptDto
                            (fun dto -> dto.RootId)
                            insertToOneOpt
                            updateToOneOpt
                            deleteToOneOpt
                        |> saveChildrenWithDifferentOldNew
                            (fun _ -> failwith "Should not be called")
                            rootToToManyDtos
                            (fun dto -> dto.Id)
                            insertToMany
                            updateToMany
                            deleteToMany

                    let actual = save arg None () |> Async.RunSynchronously

                    let mutable expectedId = 0

                    let nextExpectedId () =
                        expectedId <- expectedId + 1
                        expectedId

                    Expect.equal actual (Some result) "Incorrect result"
                    Expect.sequenceEqual insertRootCalledWith [ nextExpectedId (), arg, rootDto ] "insertRootCalledWith"
                    Expect.sequenceEqual updateRootCalledWith [] "updateRoot should not be called"

                    Expect.sequenceEqual
                        insertToOneCalledWith
                        [ nextExpectedId (), arg, toOneDto ]
                        "insertToOne called in wrong order or with wrong args"

                    Expect.sequenceEqual updateToOneCalledWith [] "updateToOne should not be called"

                    Expect.sequenceEqual
                        insertToOneOptCalledWith
                        [
                            for x in Option.toList toOneOptDto do
                                (nextExpectedId (), arg, x)
                        ]
                        "insertToOneOpt called in wrong order or with wrong args"

                    Expect.sequenceEqual updateToOneOptCalledWith [] "updateToOneOpt should not be called"
                    Expect.sequenceEqual deleteToOneOptCalledWith [] "deleteToOneOpt should not be called"

                    Expect.sequenceEqual
                        insertToManyCalledWith
                        [
                            for x in toManyDtos do
                                (nextExpectedId (), arg, x)
                        ]
                        "insertToMany called in wrong order or with wrong args"

                    Expect.sequenceEqual updateToManyCalledWith [] "updateToMany should not be called"
                    Expect.sequenceEqual deleteToManyCalledWith [] "deleteToMany should not be called"
                }


            testCase "Returns correct result and calls DB functions in sequence with correct args when old is Some"
            <| fun () ->
                Property.checkWith (PropertyConfig.defaultConfig |> PropertyConfig.withTests 1000<tests>)
                <| property {

                    let! arg = GenX.auto<int>
                    let! result = GenX.auto<int>

                    let! rootDto, toOneDto, toOneOptDto, toManyDtos, _, toOneDto', toOneOptDto', toManyDtos' =
                        Gen.rootDtoWithChildDtosWithUpdates

                    let mutable i = 1

                    let mutable insertRootCalledWith = []

                    let insertRoot arg dto =
                        async {
                            insertRootCalledWith <- insertRootCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                            return result
                        }

                    let mutable updateRootCalledWith = []

                    let updateRoot arg dto =
                        async {
                            updateRootCalledWith <- updateRootCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                            return result
                        }

                    let mutable insertToOneCalledWith = []

                    let insertToOne arg dto =
                        async {
                            insertToOneCalledWith <- insertToOneCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable updateToOneCalledWith = []

                    let updateToOne arg dto =
                        async {
                            updateToOneCalledWith <- updateToOneCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable insertToOneOptCalledWith = []

                    let insertToOneOpt arg dto =
                        async {
                            insertToOneOptCalledWith <- insertToOneOptCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable updateToOneOptCalledWith = []

                    let updateToOneOpt arg dto =
                        async {
                            updateToOneOptCalledWith <- updateToOneOptCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable deleteToOneOptCalledWith = []

                    let deleteToOneOpt arg dto =
                        async {
                            deleteToOneOptCalledWith <- deleteToOneOptCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable insertToManyCalledWith = []

                    let insertToMany arg dto =
                        async {
                            insertToManyCalledWith <- insertToManyCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable updateToManyCalledWith = []

                    let updateToMany arg dto =
                        async {
                            updateToManyCalledWith <- updateToManyCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }

                    let mutable deleteToManyCalledWith = []

                    let deleteToMany arg dto =
                        async {
                            deleteToManyCalledWith <- deleteToManyCalledWith @ [ i, arg, dto ]
                            i <- i + 1
                        }


                    let save =
                        saveRootWithOutput (fun _ -> rootDto) insertRoot updateRoot
                        |> saveChildWithDifferentOldNew (fun _ -> toOneDto) (fun _ -> toOneDto') insertToOne updateToOne
                        |> saveOptChildWithDifferentOldNew
                            (fun _ -> toOneOptDto)
                            (fun _ -> toOneOptDto')
                            (fun dto -> dto.RootId)
                            insertToOneOpt
                            updateToOneOpt
                            deleteToOneOpt
                        |> saveChildrenWithDifferentOldNew
                            (fun _ -> toManyDtos)
                            (fun _ -> toManyDtos')
                            (fun dto -> dto.Id)
                            insertToMany
                            updateToMany
                            deleteToMany

                    let actual = save arg (Some()) () |> Async.RunSynchronously

                    let mutable expectedId = 0

                    let nextExpectedId () =
                        expectedId <- expectedId + 1
                        expectedId

                    Expect.equal actual None "Incorrect result"
                    Expect.sequenceEqual insertRootCalledWith [] "insertRootCalledWith"
                    Expect.sequenceEqual updateRootCalledWith [] "updateRoot"
                    Expect.sequenceEqual insertToOneCalledWith [] "insertToOne"

                    Expect.sequenceEqual
                        updateToOneCalledWith
                        [
                            if toOneDto <> toOneDto' then
                                nextExpectedId (), arg, toOneDto'
                        ]
                        "updateToOne"

                    Expect.sequenceEqual
                        insertToOneOptCalledWith
                        [
                            if toOneOptDto.IsNone && toOneOptDto'.IsSome then
                                nextExpectedId (), arg, toOneOptDto'.Value
                        ]
                        "insertToOneOpt"

                    Expect.sequenceEqual
                        updateToOneOptCalledWith
                        [
                            if toOneOptDto.IsSome && toOneOptDto'.IsSome && toOneOptDto <> toOneOptDto' then
                                nextExpectedId (), arg, toOneOptDto'.Value
                        ]
                        "updateToOneOpt"

                    Expect.sequenceEqual
                        deleteToOneOptCalledWith
                        [
                            if toOneOptDto.IsSome && toOneOptDto'.IsNone then
                                nextExpectedId (), arg, toOneOptDto.Value.RootId
                        ]
                        "deleteToOneOpt"

                    let oldToManyById = toManyDtos |> List.map (fun x -> x.Id, x) |> Map.ofList
                    let newToManyById = toManyDtos' |> List.map (fun x -> x.Id, x) |> Map.ofList

                    let deletedIds =
                        toManyDtos
                        |> List.map (fun x -> x.Id)
                        |> List.filter (not << newToManyById.ContainsKey)

                    Expect.sequenceEqual
                        deleteToManyCalledWith
                        [
                            for id in deletedIds do
                                nextExpectedId (), arg, id
                        ]
                        "deleteToMany"

                    Expect.sequenceEqual
                        updateToManyCalledWith
                        [
                            for newToManyDto in toManyDtos' do
                                match oldToManyById.TryFind newToManyDto.Id with
                                | Some oldToManyDto when oldToManyDto <> newToManyDto ->
                                    nextExpectedId (), arg, newToManyDto
                                | _ -> ()
                        ]
                        "updateToMany"

                    Expect.sequenceEqual
                        insertToManyCalledWith
                        [
                            for newToManyDto in toManyDtos' do
                                match oldToManyById.TryFind newToManyDto.Id with
                                | None -> nextExpectedId (), arg, newToManyDto
                                | Some _ -> ()
                        ]
                        "insertToMany"
                }


        ]


        testList "saveBatch" [


            testCase "Returns correct result and calls DB functions in sequence with correct args"
            <| fun () ->
                Property.checkWith (PropertyConfig.defaultConfig |> PropertyConfig.withTests 1000<tests>)
                <| property {

                    let! arg = GenX.auto<char>

                    let! rootDto1, toOneDto1, toOneOptDto1, toManyDtos1 = Gen.rootDtoWithChildDtos

                    let! rootDto2,
                         toOneDto2,
                         toOneOptDto2,
                         toManyDtos2,
                         rootDto2',
                         toOneDto2',
                         toOneOptDto2',
                         toManyDtos2' =
                        Gen.rootDtoWithChildDtosWithUpdates
                        |> Gen.filter (fun (r2, _, _, tm2, _, _, _, tm2') ->
                            r2.Id <> rootDto1.Id
                            && Set.isEmpty (
                                Set.intersect
                                    (toManyDtos1 |> List.map _.Id |> set)
                                    (Set.union (tm2 |> List.map _.Id |> set) (tm2' |> List.map _.Id |> set))
                            )
                        )

                    let mutable i = 1

                    let mutable batchInsertRootCalledWith = []

                    let batchInsertRoot arg dtos =
                        async {
                            batchInsertRootCalledWith <- batchInsertRootCalledWith @ [ i, arg, Seq.toList dtos ]
                            i <- i + 1
                        }

                    let mutable batchUpdateRootCalledWith = []

                    let batchUpdateRoot arg dtos =
                        async {
                            batchUpdateRootCalledWith <- batchUpdateRootCalledWith @ [ i, arg, Seq.toList dtos ]
                            i <- i + 1
                        }

                    let mutable batchInsertToOneCalledWith = []

                    let batchInsertToOne arg dtos =
                        async {
                            batchInsertToOneCalledWith <- batchInsertToOneCalledWith @ [ i, arg, Seq.toList dtos ]
                            i <- i + 1
                        }

                    let mutable batchUpdateToOneCalledWith = []

                    let batchUpdateToOne arg dtos =
                        async {
                            batchUpdateToOneCalledWith <- batchUpdateToOneCalledWith @ [ i, arg, Seq.toList dtos ]
                            i <- i + 1
                        }

                    let mutable batchInsertToOneOptCalledWith = []

                    let batchInsertToOneOpt arg dtos =
                        async {
                            batchInsertToOneOptCalledWith <- batchInsertToOneOptCalledWith @ [ i, arg, Seq.toList dtos ]
                            i <- i + 1
                        }

                    let mutable batchUpdateToOneOptCalledWith = []

                    let batchUpdateToOneOpt arg dtos =
                        async {
                            batchUpdateToOneOptCalledWith <- batchUpdateToOneOptCalledWith @ [ i, arg, Seq.toList dtos ]
                            i <- i + 1
                        }

                    let mutable batchDeleteToOneOptCalledWith = []

                    let batchDeleteToOneOpt arg dtos =
                        async {
                            batchDeleteToOneOptCalledWith <- batchDeleteToOneOptCalledWith @ [ i, arg, Seq.toList dtos ]
                            i <- i + 1
                        }

                    let mutable batchInsertToManyCalledWith = []

                    let batchInsertToMany arg dtos =
                        async {
                            batchInsertToManyCalledWith <- batchInsertToManyCalledWith @ [ i, arg, Seq.toList dtos ]
                            i <- i + 1
                        }

                    let mutable batchUpdateToManyCalledWith = []

                    let batchUpdateToMany arg dtos =
                        async {
                            batchUpdateToManyCalledWith <- batchUpdateToManyCalledWith @ [ i, arg, Seq.toList dtos ]
                            i <- i + 1
                        }

                    let mutable batchDeleteToManyCalledWith = []

                    let batchDeleteToMany arg dtos =
                        async {
                            batchDeleteToManyCalledWith <- batchDeleteToManyCalledWith @ [ i, arg, Seq.toList dtos ]
                            i <- i + 1
                        }


                    let rootToDto =
                        function
                        | 1 -> rootDto1
                        | 2 -> rootDto2
                        | 3 -> rootDto2'
                        | _ -> failwith "Invalid arg"

                    let rootToToOneDto =
                        function
                        | 1 -> toOneDto1
                        | 2 -> toOneDto2
                        | 3 -> toOneDto2'
                        | _ -> failwith "Invalid arg"

                    let rootToToOneOptDto =
                        function
                        | 1 -> toOneOptDto1
                        | 2 -> toOneOptDto2
                        | 3 -> toOneOptDto2'
                        | _ -> failwith "Invalid arg"

                    let rootToToManyDtos =
                        function
                        | 1 -> toManyDtos1
                        | 2 -> toManyDtos2
                        | 3 -> toManyDtos2'
                        | _ -> failwith "Invalid arg"

                    let save =
                        Batch.saveRoot rootToDto batchInsertRoot batchUpdateRoot
                        |> Batch.saveChild rootToToOneDto batchInsertToOne batchUpdateToOne
                        |> Batch.saveOptChild
                            rootToToOneOptDto
                            _.RootId
                            batchInsertToOneOpt
                            batchUpdateToOneOpt
                            batchDeleteToOneOpt
                        |> Batch.saveChildren
                            rootToToManyDtos
                            _.Id
                            batchInsertToMany
                            batchUpdateToMany
                            batchDeleteToMany

                    save arg [ (None, 1); (Some 2, 3) ] |> Async.RunSynchronously

                    let mutable expectedId = 0

                    let nextExpectedId () =
                        expectedId <- expectedId + 1
                        expectedId

                    Expect.sequenceEqual
                        batchUpdateRootCalledWith
                        [
                            if rootDto2 <> rootDto2' then
                                nextExpectedId (), arg, [ rootDto2' ]
                        ]
                        "batchUpdateRoot"

                    Expect.sequenceEqual
                        batchInsertRootCalledWith
                        [ nextExpectedId (), arg, [ rootDto1 ] ]
                        "batchInsertRoot"

                    Expect.sequenceEqual
                        batchUpdateToOneCalledWith
                        [
                            if toOneDto2 <> toOneDto2' then
                                nextExpectedId (), arg, [ toOneDto2' ]
                        ]
                        "batchUpdateToOne"

                    Expect.sequenceEqual
                        batchInsertToOneCalledWith
                        [ nextExpectedId (), arg, [ toOneDto1 ] ]
                        "batchInsertToOne"

                    Expect.sequenceEqual
                        batchDeleteToOneOptCalledWith
                        [
                            if toOneOptDto2.IsSome && toOneOptDto2'.IsNone then
                                nextExpectedId (), arg, [ toOneOptDto2.Value.RootId ]
                        ]
                        "batchDeleteToOneOpt"

                    Expect.sequenceEqual
                        batchUpdateToOneOptCalledWith
                        [
                            if toOneOptDto2.IsSome && toOneOptDto2'.IsSome && toOneOptDto2 <> toOneOptDto2' then
                                nextExpectedId (), arg, [ toOneOptDto2'.Value ]
                        ]
                        "batchUpdateToOneOpt"

                    Expect.sequenceEqual
                        batchInsertToOneOptCalledWith
                        [
                            let dtos = [
                                if toOneOptDto1.IsSome then
                                    toOneOptDto1.Value

                                if toOneOptDto2.IsNone && toOneOptDto2'.IsSome then
                                    toOneOptDto2'.Value
                            ]

                            if not dtos.IsEmpty then
                                nextExpectedId (), arg, dtos
                        ]
                        "batchInsertToOneOpt"

                    let oldToManyById = toManyDtos2 |> List.map (fun x -> x.Id, x) |> Map.ofList

                    let newToManyById =
                        toManyDtos1 @ toManyDtos2' |> List.map (fun x -> x.Id, x) |> Map.ofList

                    let deletedIds =
                        toManyDtos2
                        |> List.map (fun x -> x.Id)
                        |> List.filter (not << newToManyById.ContainsKey)

                    Expect.sequenceEqual
                        batchDeleteToManyCalledWith
                        [
                            if not deletedIds.IsEmpty then
                                nextExpectedId (), arg, deletedIds
                        ]
                        "batchDeleteToMany"

                    Expect.sequenceEqual
                        batchUpdateToManyCalledWith
                        [
                            let dtos = [
                                for newToManyDto in toManyDtos1 @ toManyDtos2' do
                                    match oldToManyById.TryFind newToManyDto.Id with
                                    | Some oldToManyDto when oldToManyDto <> newToManyDto -> newToManyDto
                                    | _ -> ()
                            ]

                            if not dtos.IsEmpty then
                                nextExpectedId (), arg, dtos
                        ]
                        "batchUpdateToMany"

                    Expect.sequenceEqual
                        batchInsertToManyCalledWith
                        [

                            let dtos = [
                                for newToManyDto in toManyDtos1 @ toManyDtos2' do
                                    match oldToManyById.TryFind newToManyDto.Id with
                                    | None -> newToManyDto
                                    | Some _ -> ()
                            ]

                            if not dtos.IsEmpty then
                                nextExpectedId (), arg, dtos
                        ]
                        "batchInsertToMany"
                }


        ]


    ]
