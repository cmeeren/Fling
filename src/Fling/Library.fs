namespace Fling


module Fling =


    open System.Collections.Generic


    let saveRootWithOutput
        (toDto: 'rootEntity -> 'rootDto)
        (insert: 'arg -> 'rootDto -> Async<'saveResult>)
        (update: 'arg -> 'rootDto -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult option> =
        fun (arg: 'arg) (oldRoot: 'rootEntity option) (newRoot: 'rootEntity) ->
            async {
                match oldRoot with
                | None -> return! newRoot |> toDto |> insert arg |> Async.map Some

                | Some oldRoot ->
                    let oldDto = toDto oldRoot
                    let newDto = toDto newRoot

                    if oldDto <> newDto then
                        return! update arg newDto |> Async.map Some
                    else
                        return None
            }


    let saveRoot
        (toDto: 'rootEntity -> 'rootDto)
        (insert: 'arg -> 'rootDto -> Async<unit>)
        (update: 'arg -> 'rootDto -> Async<unit>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<unit> =
        fun (arg: 'arg) (oldRoot: 'rootEntity option) (newRoot: 'rootEntity) ->
            saveRootWithOutput toDto insert update arg oldRoot newRoot
            |> Async.Ignore<unit option>


    let batchSaveChildrenWithDifferentOldNewWithFullDeleteDto
        (oldToDtos: 'rootEntity -> 'childDto list)
        (newToDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
        (batchUpdate: 'arg -> 'childDto seq -> Async<unit>)
        (batchDelete: 'arg -> 'childDto seq -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        fun (arg: 'arg) (oldRoot: 'rootEntity option) (newRoot: 'rootEntity) ->
            async {
                let! result = existingSave arg oldRoot newRoot

                match oldRoot with
                | None -> do! batchInsert arg (newToDtos newRoot)

                | Some oldRoot ->
                    let oldChildren = oldToDtos oldRoot
                    let newChildren = newToDtos newRoot

                    let oldChildrenById = Dictionary<'childDtoId, 'childDto>()
                    let newChildrenById = Dictionary<'childDtoId, 'childDto>()

                    for dto in oldChildren do
                        oldChildrenById[getId dto] <- dto

                    for dto in newChildren do
                        newChildrenById[getId dto] <- dto

                    let toInsert = ResizeArray()
                    let toUpdate = ResizeArray()
                    let toDelete = ResizeArray()

                    for oldChild in oldChildren do
                        let oldChildId = getId oldChild

                        if not (newChildrenById.ContainsKey oldChildId) then
                            toDelete.Add oldChild

                    for newChild in newChildren do
                        match oldChildrenById.TryGetValue(getId newChild) with
                        | false, _ -> toInsert.Add newChild
                        | true, oldChild when newChild <> oldChild -> toUpdate.Add newChild
                        | true, _ -> ()

                    if toDelete.Count > 0 then
                        do! batchDelete arg toDelete

                    if toUpdate.Count > 0 then
                        do! batchUpdate arg toUpdate

                    if toInsert.Count > 0 then
                        do! batchInsert arg toInsert

                return result
            }


    let batchSaveChildrenWithDifferentOldNew
        (oldToDtos: 'rootEntity -> 'childDto list)
        (newToDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
        (batchUpdate: 'arg -> 'childDto seq -> Async<unit>)
        (batchDelete: 'arg -> 'childDtoId seq -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        batchSaveChildrenWithDifferentOldNewWithFullDeleteDto
            oldToDtos
            newToDtos
            getId
            batchInsert
            batchUpdate
            (fun arg xs -> batchDelete arg (xs |> Seq.map getId))
            existingSave


    let private asBatched (f: 'arg -> 'childDto -> Async<unit>) : 'arg -> 'childDto seq -> Async<unit> =
        fun a xs ->
            async {
                for x in xs do
                    do! f a x
            }


    let saveChildrenWithDifferentOldNewWithFullDeleteDto
        (oldToDtos: 'rootEntity -> 'childDto list)
        (newToDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (update: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDto -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        batchSaveChildrenWithDifferentOldNewWithFullDeleteDto
            oldToDtos
            newToDtos
            getId
            (asBatched insert)
            (asBatched update)
            (asBatched delete)
            existingSave


    let saveChildrenWithDifferentOldNew
        (oldToDtos: 'rootEntity -> 'childDto list)
        (newToDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (update: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDtoId -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveChildrenWithDifferentOldNewWithFullDeleteDto
            oldToDtos
            newToDtos
            getId
            insert
            update
            (fun arg x -> delete arg (getId x))
            existingSave


    let batchSaveChildrenWithFullDeleteDto
        (toDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto seq -> Async<unit>)
        (update: 'arg -> 'childDto seq -> Async<unit>)
        (delete: 'arg -> 'childDto seq -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        batchSaveChildrenWithDifferentOldNewWithFullDeleteDto toDtos toDtos getId insert update delete existingSave


    let batchSaveChildren
        (toDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto seq -> Async<unit>)
        (update: 'arg -> 'childDto seq -> Async<unit>)
        (delete: 'arg -> 'childDtoId seq -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        batchSaveChildrenWithFullDeleteDto
            toDtos
            getId
            insert
            update
            (fun arg xs -> delete arg (xs |> Seq.map getId))
            existingSave


    let saveChildrenWithFullDeleteDto
        (toDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (update: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDto -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveChildrenWithDifferentOldNewWithFullDeleteDto toDtos toDtos getId insert update delete existingSave


    let saveChildren
        (toDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (update: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDtoId -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveChildrenWithFullDeleteDto toDtos getId insert update (fun arg x -> delete arg (getId x)) existingSave


    let batchSaveChildrenWithoutUpdateWithDifferentOldNewWithFullDeleteDto
        (oldToDtos: 'rootEntity -> 'childDto list)
        (newToDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto seq -> Async<unit>)
        (delete: 'arg -> 'childDto seq -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        batchSaveChildrenWithDifferentOldNewWithFullDeleteDto
            oldToDtos
            newToDtos
            getId
            insert
            (fun _ dtos ->
                let dto = Seq.head dtos

                failwith
                    $"Update needed in Fling ...WithoutUpdate function due to changed child DTO of type %s{typeof<'childDto>.FullName} with ID %A{getId dto}. Updated child DTO: %A{dto}"
            )
            delete
            existingSave


    let batchSaveChildrenWithoutUpdateWithDifferentOldNew
        (oldToDtos: 'rootEntity -> 'childDto list)
        (newToDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto seq -> Async<unit>)
        (delete: 'arg -> 'childDtoId seq -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        batchSaveChildrenWithoutUpdateWithDifferentOldNewWithFullDeleteDto
            oldToDtos
            newToDtos
            getId
            insert
            (fun arg xs -> delete arg (xs |> Seq.map getId))
            existingSave


    let saveChildrenWithoutUpdateWithDifferentOldNewWithFullDeleteDto
        (oldToDtos: 'rootEntity -> 'childDto list)
        (newToDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDto -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveChildrenWithDifferentOldNewWithFullDeleteDto
            oldToDtos
            newToDtos
            getId
            insert
            (fun _ dto ->
                failwith
                    $"Update needed in Fling ...WithoutUpdate function due to changed child DTO of type %s{typeof<'childDto>.FullName} with ID %A{getId dto}. Updated child DTO: %A{dto}"
            )
            delete
            existingSave


    let saveChildrenWithoutUpdateWithDifferentOldNew
        (oldToDtos: 'rootEntity -> 'childDto list)
        (newToDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDtoId -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveChildrenWithoutUpdateWithDifferentOldNewWithFullDeleteDto
            oldToDtos
            newToDtos
            getId
            insert
            (fun arg x -> delete arg (getId x))
            existingSave


    let batchSaveChildrenWithoutUpdateWithFullDeleteDto
        (toDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto seq -> Async<unit>)
        (delete: 'arg -> 'childDto seq -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        batchSaveChildrenWithoutUpdateWithDifferentOldNewWithFullDeleteDto
            toDtos
            toDtos
            getId
            insert
            delete
            existingSave


    let batchSaveChildrenWithoutUpdate
        (toDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto seq -> Async<unit>)
        (delete: 'arg -> 'childDtoId seq -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        batchSaveChildrenWithoutUpdateWithFullDeleteDto
            toDtos
            getId
            insert
            (fun arg xs -> delete arg (xs |> Seq.map getId))
            existingSave


    let saveChildrenWithoutUpdateWithFullDeleteDto
        (toDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDto -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveChildrenWithoutUpdateWithDifferentOldNewWithFullDeleteDto toDtos toDtos getId insert delete existingSave


    let saveChildrenWithoutUpdate
        (toDtos: 'rootEntity -> 'childDto list)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDtoId -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveChildrenWithoutUpdateWithFullDeleteDto toDtos getId insert (fun arg x -> delete arg (getId x)) existingSave


    let saveOptChildWithDifferentOldNewWithFullDeleteDto
        (oldToDto: 'rootEntity -> 'childDto option)
        (newToDto: 'rootEntity -> 'childDto option)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (update: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDto -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveChildrenWithDifferentOldNewWithFullDeleteDto
            (oldToDto >> Option.toList)
            (newToDto >> Option.toList)
            getId
            insert
            update
            delete
            existingSave


    let saveOptChildWithDifferentOldNew
        (oldToDto: 'rootEntity -> 'childDto option)
        (newToDto: 'rootEntity -> 'childDto option)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (update: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDtoId -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveOptChildWithDifferentOldNewWithFullDeleteDto
            oldToDto
            newToDto
            getId
            insert
            update
            (fun arg x -> delete arg (getId x))
            existingSave


    let saveOptChildWithFullDeleteDto
        (toDto: 'rootEntity -> 'childDto option)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (update: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDto -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveOptChildWithDifferentOldNewWithFullDeleteDto toDto toDto getId insert update delete existingSave


    let saveOptChild
        (toDto: 'rootEntity -> 'childDto option)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (update: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDtoId -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveOptChildWithFullDeleteDto toDto getId insert update (fun arg x -> delete arg (getId x)) existingSave


    let saveOptChildWithoutUpdateWithDifferentOldNewWithFullDeleteDto
        (oldToDto: 'rootEntity -> 'childDto option)
        (newToDto: 'rootEntity -> 'childDto option)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDto -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveOptChildWithDifferentOldNewWithFullDeleteDto
            oldToDto
            newToDto
            getId
            insert
            (fun _ dto ->
                failwith
                    $"Update needed in Fling ...WithoutUpdate function due to changed child DTO of type %s{typeof<'childDto>.FullName} with ID %A{getId dto}. Updated child DTO: %A{dto}"
            )
            delete
            existingSave


    let saveOptChildWithoutUpdateWithDifferentOldNew
        (oldToDto: 'rootEntity -> 'childDto option)
        (newToDto: 'rootEntity -> 'childDto option)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDtoId -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveOptChildWithoutUpdateWithDifferentOldNewWithFullDeleteDto
            oldToDto
            newToDto
            getId
            insert
            (fun arg x -> delete arg (getId x))
            existingSave


    let saveOptChildWithoutUpdateWithFullDeleteDto
        (toDto: 'rootEntity -> 'childDto option)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDto -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveOptChildWithoutUpdateWithDifferentOldNewWithFullDeleteDto toDto toDto getId insert delete existingSave


    let saveOptChildWithoutUpdate
        (toDto: 'rootEntity -> 'childDto option)
        (getId: 'childDto -> 'childDtoId)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (delete: 'arg -> 'childDtoId -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveOptChildWithoutUpdateWithFullDeleteDto toDto getId insert (fun arg x -> delete arg (getId x)) existingSave


    let saveChildWithDifferentOldNew
        (oldToDto: 'rootEntity -> 'childDto)
        (newToDto: 'rootEntity -> 'childDto)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (update: 'arg -> 'childDto -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveChildrenWithDifferentOldNew
            (oldToDto >> List.singleton)
            (newToDto >> List.singleton)
            (fun _ -> 0) // We always have exactly one child
            insert
            update
            (fun _ -> failwith "saveChild should never delete")
            existingSave


    let saveChild
        (toDto: 'rootEntity -> 'childDto)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (update: 'arg -> 'childDto -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveChildWithDifferentOldNew toDto toDto insert update existingSave


    let saveChildWithoutUpdateWithDifferentOldNew
        (oldToDto: 'rootEntity -> 'childDto)
        (newToDto: 'rootEntity -> 'childDto)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveChildWithDifferentOldNew
            oldToDto
            newToDto
            insert
            (fun _ dto ->
                failwith
                    $"Update needed in Fling ...WithoutUpdate function due to changed child DTO of type %s{typeof<'childDto>.FullName}. Updated child DTO: %A{dto}"
            )
            existingSave


    let saveChildWithoutUpdate
        (toDto: 'rootEntity -> 'childDto)
        (insert: 'arg -> 'childDto -> Async<unit>)
        (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
        : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
        saveChildWithoutUpdateWithDifferentOldNew toDto toDto insert existingSave


    /// Contains similar functions to those outside this module, but for saving a batch of root entities.
    [<RequireQualifiedAccess>]
    module Batch =


        let saveRootWithOutput
            (toDto: 'rootEntity -> 'rootDto)
            (batchInsert: 'arg -> 'rootDto seq -> Async<'insertResult>)
            (batchUpdate: 'arg -> 'rootDto seq -> Async<'updateResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'insertResult option * 'updateResult option> =
            fun (arg: 'arg) (roots: #seq<'rootEntity option * 'rootEntity>) ->
                async {
                    let toInsert = ResizeArray()
                    let toUpdate = ResizeArray()

                    for oldRoot, newRoot in roots do
                        let newDto = toDto newRoot

                        match oldRoot |> Option.map toDto with
                        | None -> toInsert.Add newDto
                        | Some oldDto when oldDto = newDto -> ()
                        | Some _ -> toUpdate.Add newDto

                    let mutable insertResult = None
                    let mutable updateResult = None

                    if toUpdate.Count > 0 then
                        let! res = batchUpdate arg toUpdate
                        updateResult <- Some res

                    if toInsert.Count > 0 then
                        let! res = batchInsert arg toInsert
                        insertResult <- Some res

                    return insertResult, updateResult
                }

        let saveRoot
            (toDto: 'rootEntity -> 'rootDto)
            (batchInsert: 'arg -> 'rootDto seq -> Async<unit>)
            (batchUpdate: 'arg -> 'rootDto seq -> Async<unit>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<unit> =
            fun arg roots ->
                saveRootWithOutput toDto batchInsert batchUpdate arg roots
                |> Async.Ignore<unit option * unit option>


        let saveChildWithDifferentOldNew
            (oldToDto: 'rootEntity -> 'childDto)
            (newToDto: 'rootEntity -> 'childDto)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchUpdate: 'arg -> 'childDto seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            fun (arg: 'arg) (roots: #seq<'rootEntity option * 'rootEntity>) ->
                async {
                    let! result = existingSave arg roots

                    let toInsert = ResizeArray()
                    let toUpdate = ResizeArray()

                    for oldRoot, newRoot in roots do
                        let newChildDto = newToDto newRoot

                        match oldRoot |> Option.map oldToDto with
                        | None -> toInsert.Add newChildDto
                        | Some oldChildDto when oldChildDto = newChildDto -> ()
                        | Some _ -> toUpdate.Add newChildDto

                    if toUpdate.Count > 0 then
                        do! batchUpdate arg toUpdate

                    if toInsert.Count > 0 then
                        do! batchInsert arg toInsert

                    return result
                }


        let saveChild
            (toDto: 'rootEntity -> 'childDto)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchUpdate: 'arg -> 'childDto seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveChildWithDifferentOldNew toDto toDto batchInsert batchUpdate existingSave


        let saveChildWithoutUpdateWithDifferentOldNew
            (oldToDto: 'rootEntity -> 'childDto)
            (newToDto: 'rootEntity -> 'childDto)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveChildWithDifferentOldNew
                oldToDto
                newToDto
                batchInsert
                (fun _ dtos ->
                    let dto = Seq.head dtos

                    failwith
                        $"Update needed in Fling ...WithoutUpdate function due to changed child DTO of type %s{typeof<'childDto>.FullName}. Updated child DTO: %A{dto}"
                )
                existingSave


        let saveChildWithoutUpdate
            (toDto: 'rootEntity -> 'childDto)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveChildWithoutUpdateWithDifferentOldNew toDto toDto batchInsert existingSave


        let saveOptChildWithDifferentOldNewWithFullDeleteDto
            (oldToDto: 'rootEntity -> 'childDto option)
            (newToDto: 'rootEntity -> 'childDto option)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchUpdate: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDto seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            fun (arg: 'arg) (roots: #seq<'rootEntity option * 'rootEntity>) ->
                async {
                    let! result = existingSave arg roots

                    let toInsert = ResizeArray()
                    let toUpdate = ResizeArray()
                    let toDelete = ResizeArray()

                    for oldRoot, newRoot in roots do
                        match oldRoot |> Option.bind oldToDto, newToDto newRoot with
                        | None, None -> ()
                        | None, Some newChildDto -> toInsert.Add newChildDto
                        | Some oldChildDto, Some newChildDto when oldChildDto = newChildDto -> ()
                        | Some _, Some newChildDto -> toUpdate.Add newChildDto
                        | Some oldChildDto, None -> toDelete.Add(oldChildDto)

                    if toDelete.Count > 0 then
                        do! batchDelete arg toDelete

                    if toUpdate.Count > 0 then
                        do! batchUpdate arg toUpdate

                    if toInsert.Count > 0 then
                        do! batchInsert arg toInsert

                    return result
                }


        let saveOptChildWithDifferentOldNew
            (oldToDto: 'rootEntity -> 'childDto option)
            (newToDto: 'rootEntity -> 'childDto option)
            (getId: 'childDto -> 'childDtoId)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchUpdate: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDtoId seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveOptChildWithDifferentOldNewWithFullDeleteDto
                oldToDto
                newToDto
                batchInsert
                batchUpdate
                (fun arg xs -> batchDelete arg (xs |> Seq.map getId))
                existingSave


        let saveOptChildWithFullDeleteDto
            (toDto: 'rootEntity -> 'childDto option)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchUpdate: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDto seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveOptChildWithDifferentOldNewWithFullDeleteDto
                toDto
                toDto
                batchInsert
                batchUpdate
                batchDelete
                existingSave


        let saveOptChild
            (toDto: 'rootEntity -> 'childDto option)
            (getId: 'childDto -> 'childDtoId)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchUpdate: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDtoId seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveOptChildWithFullDeleteDto
                toDto
                batchInsert
                batchUpdate
                (fun arg xs -> batchDelete arg (xs |> Seq.map getId))
                existingSave


        let saveOptChildWithoutUpdateWithDifferentOldNewWithFullDeleteDto
            (oldToDto: 'rootEntity -> 'childDto option)
            (newToDto: 'rootEntity -> 'childDto option)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDto seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveOptChildWithDifferentOldNewWithFullDeleteDto
                oldToDto
                newToDto
                batchInsert
                (fun _ dtos ->
                    let dto = Seq.head dtos

                    failwith
                        $"Update needed in Fling ...WithoutUpdate function due to changed child DTO of type %s{typeof<'childDto>.FullName}. Updated child DTO: %A{dto}"
                )
                batchDelete
                existingSave


        let saveOptChildWithoutUpdateWithDifferentOldNew
            (oldToDto: 'rootEntity -> 'childDto option)
            (newToDto: 'rootEntity -> 'childDto option)
            (getId: 'childDto -> 'childDtoId)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDtoId seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveOptChildWithoutUpdateWithDifferentOldNewWithFullDeleteDto
                oldToDto
                newToDto
                batchInsert
                (fun arg xs -> batchDelete arg (xs |> Seq.map getId))
                existingSave


        let saveOptChildWithoutUpdateWithFullDeleteDto
            (toDto: 'rootEntity -> 'childDto option)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDto seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveOptChildWithoutUpdateWithDifferentOldNewWithFullDeleteDto
                toDto
                toDto
                batchInsert
                batchDelete
                existingSave


        let saveOptChildWithoutUpdate
            (toDto: 'rootEntity -> 'childDto option)
            (getId: 'childDto -> 'childDtoId)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDtoId seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveOptChildWithoutUpdateWithFullDeleteDto
                toDto
                batchInsert
                (fun arg xs -> batchDelete arg (xs |> Seq.map getId))
                existingSave


        let saveChildrenWithDifferentOldNewWithFullDeleteDto
            (oldToDto: 'rootEntity -> #seq<'childDto>)
            (newToDto: 'rootEntity -> #seq<'childDto>)
            (getId: 'childDto -> 'childDtoId)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchUpdate: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDto seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            fun (arg: 'arg) (roots: #seq<'rootEntity option * 'rootEntity>) ->
                async {
                    let! result = existingSave arg roots

                    let oldChildren = roots |> Seq.choose (fst >> Option.map oldToDto) |> Seq.collect id
                    let newChildren = roots |> Seq.map (snd >> newToDto) |> Seq.collect id

                    let oldChildrenById = Dictionary<'childDtoId, 'childDto>()
                    let newChildrenById = Dictionary<'childDtoId, 'childDto>()

                    for dto in oldChildren do
                        oldChildrenById[getId dto] <- dto

                    for dto in newChildren do
                        newChildrenById[getId dto] <- dto

                    let toInsert = ResizeArray()
                    let toUpdate = ResizeArray()
                    let toDelete = ResizeArray()

                    for oldChild in oldChildren do
                        let oldChildId = getId oldChild

                        if not (newChildrenById.ContainsKey oldChildId) then
                            toDelete.Add oldChild

                    for newChild in newChildren do
                        match oldChildrenById.TryGetValue(getId newChild) with
                        | false, _ -> toInsert.Add newChild
                        | true, oldChild when newChild <> oldChild -> toUpdate.Add newChild
                        | true, _ -> ()

                    if toDelete.Count > 0 then
                        do! batchDelete arg toDelete

                    if toUpdate.Count > 0 then
                        do! batchUpdate arg toUpdate

                    if toInsert.Count > 0 then
                        do! batchInsert arg toInsert

                    return result
                }


        let saveChildrenWithDifferentOldNew
            (oldToDto: 'rootEntity -> #seq<'childDto>)
            (newToDto: 'rootEntity -> #seq<'childDto>)
            (getId: 'childDto -> 'childDtoId)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchUpdate: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDtoId seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveChildrenWithDifferentOldNewWithFullDeleteDto
                oldToDto
                newToDto
                getId
                batchInsert
                batchUpdate
                (fun arg xs -> batchDelete arg (xs |> Seq.map getId))
                existingSave


        let saveChildrenWithFullDeleteDto
            (toDto: 'rootEntity -> #seq<'childDto>)
            (getId: 'childDto -> 'childDtoId)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchUpdate: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDto seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveChildrenWithDifferentOldNewWithFullDeleteDto
                toDto
                toDto
                getId
                batchInsert
                batchUpdate
                batchDelete
                existingSave


        let saveChildren
            (toDto: 'rootEntity -> #seq<'childDto>)
            (getId: 'childDto -> 'childDtoId)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchUpdate: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDtoId seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveChildrenWithFullDeleteDto
                toDto
                getId
                batchInsert
                batchUpdate
                (fun arg xs -> batchDelete arg (xs |> Seq.map getId))
                existingSave


        let saveChildrenWithoutUpdateWithDifferentOldNewWithFullDeleteDto
            (oldToDto: 'rootEntity -> #seq<'childDto>)
            (newToDto: 'rootEntity -> #seq<'childDto>)
            (getId: 'childDto -> 'childDtoId)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDto seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveChildrenWithDifferentOldNewWithFullDeleteDto
                oldToDto
                newToDto
                getId
                batchInsert
                (fun _ dtos ->
                    let dto = Seq.head dtos

                    failwith
                        $"Update needed in Fling ...WithoutUpdate function due to changed child DTO of type %s{typeof<'childDto>.FullName} with ID %A{getId dto}. Updated child DTO: %A{dto}"
                )
                batchDelete
                existingSave


        let saveChildrenWithoutUpdateWithDifferentOldNew
            (oldToDto: 'rootEntity -> #seq<'childDto>)
            (newToDto: 'rootEntity -> #seq<'childDto>)
            (getId: 'childDto -> 'childDtoId)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDtoId seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveChildrenWithoutUpdateWithDifferentOldNewWithFullDeleteDto
                oldToDto
                newToDto
                getId
                batchInsert
                (fun arg xs -> batchDelete arg (xs |> Seq.map getId))
                existingSave


        let saveChildrenWithoutUpdateWithFullDeleteDto
            (toDto: 'rootEntity -> #seq<'childDto>)
            (getId: 'childDto -> 'childDtoId)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDto seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveChildrenWithoutUpdateWithDifferentOldNewWithFullDeleteDto
                toDto
                toDto
                getId
                batchInsert
                batchDelete
                existingSave


        let saveChildrenWithoutUpdate
            (toDto: 'rootEntity -> #seq<'childDto>)
            (getId: 'childDto -> 'childDtoId)
            (batchInsert: 'arg -> 'childDto seq -> Async<unit>)
            (batchDelete: 'arg -> 'childDtoId seq -> Async<unit>)
            (existingSave: 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult>)
            : 'arg -> #seq<'rootEntity option * 'rootEntity> -> Async<'saveResult> =
            saveChildrenWithoutUpdateWithFullDeleteDto
                toDto
                getId
                batchInsert
                (fun arg xs -> batchDelete arg (xs |> Seq.map getId))
                existingSave



    type Loader<'rootDto, 'rootDtoId, 'loadResult, 'arg when 'rootDtoId: equality> = {
        GetId: 'rootDto -> 'rootDtoId
        Load: bool -> 'arg -> 'rootDto -> Async<'loadResult>
    }


    type BatchLoader<'rootDto, 'rootDtoId, 'loadResult, 'arg when 'rootDtoId: equality> = {
        GetId: 'rootDto -> 'rootDtoId
        Load: bool -> 'arg -> 'rootDto list -> Async<'loadResult list>
    }


    let createLoader
        (f: 'rootDto -> 'loadResult)
        (getId: 'rootDto -> 'rootDtoId)
        : Loader<'rootDto, 'rootDtoId, 'loadResult, 'arg> =
        {
            GetId = getId
            Load = fun _loadInParallel _arg dto -> f dto |> async.Return
        }


    let createBatchLoader
        (f: 'rootDto -> 'loadResult)
        (getId: 'rootDto -> 'rootDtoId)
        : BatchLoader<'rootDto, 'rootDtoId, 'loadResult, 'arg> =
        {
            GetId = getId
            Load = fun _loadInParallel _arg dtos -> dtos |> List.map f |> async.Return
        }


    let private load'
        loadInParallel
        (loader: Loader<'rootDto, 'rootDtoId, 'loadResult, 'arg>)
        arg
        (getRootDto: 'arg -> Async<'rootDto option>)
        : Async<'loadResult option> =
        async {
            match! getRootDto arg with
            | None -> return None
            | Some rootDto -> return! loader.Load loadInParallel arg rootDto |> Async.map Some
        }


    let private loadBatch'
        loadInParallel
        (loader: BatchLoader<'rootDto, 'rootDtoId, 'loadResult, 'arg>)
        arg
        (getRootDtos: 'arg -> Async<#seq<'rootDto>>)
        : Async<'loadResult list> =
        async {
            let! rootDtos = getRootDtos arg
            return! loader.Load loadInParallel arg (Seq.toList rootDtos)
        }


    /// Runs the loader, loading all child entities in parallel.
    let loadParallel
        (loader: Loader<'rootDto, 'rootDtoId, 'loadResult, 'arg>)
        (arg: 'arg)
        (getRootDto: 'arg -> Async<'rootDto option>)
        : Async<'loadResult option> =
        load' true loader arg getRootDto


    /// Runs the batch loader, loading all child entities in parallel.
    let loadBatchParallel
        (loader: BatchLoader<'rootDto, 'rootDtoId, 'loadResult, 'arg>)
        (arg: 'arg)
        (getRootDto: 'arg -> Async<#seq<'rootDto>>)
        : Async<'loadResult list> =
        loadBatch' true loader arg getRootDto


    /// Runs the loader, loading all child entities serially.
    let loadSerial
        (loader: Loader<'rootDto, 'rootDtoId, 'loadResult, 'arg>)
        (arg: 'arg)
        (getRootDto: 'arg -> Async<'rootDto option>)
        : Async<'loadResult option> =
        load' false loader arg getRootDto


    /// Runs the batch loader, loading all child entities serially.
    let loadBatchSerial
        (loader: BatchLoader<'rootDto, 'rootDtoId, 'loadResult, 'arg>)
        (arg: 'arg)
        (getRootDto: 'arg -> Async<#seq<'rootDto>>)
        : Async<'loadResult list> =
        loadBatch' false loader arg getRootDto


    let loadChild
        (loadChild: 'arg -> 'rootDtoId -> Async<'childDto>)
        (loader: Loader<'rootDto, 'rootDtoId, 'childDto -> 'loadResult, 'arg>)
        : Loader<'rootDto, 'rootDtoId, 'loadResult, 'arg> =

        let load loadInParallel arg rootDto =
            async {
                let! loadComp =
                    loader.Load loadInParallel arg rootDto
                    |> if loadInParallel then Async.StartChild else async.Return

                let! childComp =
                    rootDto
                    |> loader.GetId
                    |> loadChild arg
                    |> if loadInParallel then Async.StartChild else async.Return

                let! load = loadComp
                let! child = childComp
                return load child
            }

        { GetId = loader.GetId; Load = load }


    let batchLoadChildren
        (loadChildren: 'arg -> 'rootDtoId list -> Async<'childDto list>)
        (getRootId: 'childDto -> 'rootDtoId)
        (loader: BatchLoader<'rootDto, 'rootDtoId, 'childDto list -> 'loadResult, 'arg>)
        : BatchLoader<'rootDto, 'rootDtoId, 'loadResult, 'arg> =

        let load loadInParallel arg rootDtos =
            async {
                let! loadComp =
                    loader.Load loadInParallel arg rootDtos
                    |> if loadInParallel then Async.StartChild else async.Return

                let! childComp =
                    rootDtos
                    |> List.map loader.GetId
                    |> loadChildren arg
                    |> if loadInParallel then Async.StartChild else async.Return

                let! load = loadComp
                let! childBatch = childComp

                let childByRootId = Dictionary<'rootDtoId, ResizeArray<'childDto>>()

                for child in childBatch do
                    let rootId = getRootId child

                    match childByRootId.TryGetValue rootId with
                    | false, _ -> childByRootId[rootId] <- ResizeArray([ child ])
                    | true, existingChildren -> existingChildren.Add child

                return
                    List.zip load rootDtos
                    |> List.map (fun (f, rootDto) ->
                        let rootId = loader.GetId rootDto

                        match childByRootId.TryGetValue rootId with
                        | false, _ -> f []
                        | true, children -> f (Seq.toList children)
                    )
            }

        { GetId = loader.GetId; Load = load }


    let batchLoadChild
        (loadChildren: 'arg -> 'rootDtoId list -> Async<'childDto list>)
        (getRootId: 'childDto -> 'rootDtoId)
        (loader: BatchLoader<'rootDto, 'rootDtoId, 'childDto -> 'loadResult, 'arg>)
        : BatchLoader<'rootDto, 'rootDtoId, 'loadResult, 'arg> =

        let load: bool -> 'arg -> 'rootDto list -> Async<('childDto list -> 'loadResult) list> =
            fun loadInParallel arg rootDtos ->
                async {
                    let! fs = loader.Load loadInParallel arg rootDtos

                    return
                        List.zip rootDtos fs
                        |> List.map (fun (rootDto, f) ->
                            function
                            | [] -> invalidOp $"No child entity found for root entity '%A{loader.GetId rootDto}'"
                            | [ child ] -> f child
                            | children ->
                                invalidOp
                                <| "Multiple child entities found for root entity "
                                   + $"'%A{loader.GetId rootDto}': %A{children}"
                        )
                }

        let newLoader: BatchLoader<'rootDto, 'rootDtoId, 'childDto list -> 'loadResult, 'arg> = {
            GetId = loader.GetId
            Load = load
        }

        batchLoadChildren loadChildren getRootId newLoader


    let batchLoadOptChild
        (loadChildren: 'arg -> 'rootDtoId list -> Async<'childDto list>)
        (getRootId: 'childDto -> 'rootDtoId)
        (loader: BatchLoader<'rootDto, 'rootDtoId, 'childDto option -> 'loadResult, 'arg>)
        : BatchLoader<'rootDto, 'rootDtoId, 'loadResult, 'arg> =

        let load: bool -> 'arg -> 'rootDto list -> Async<('childDto list -> 'loadResult) list> =
            fun loadInParallel arg rootDtos ->
                async {
                    let! fs = loader.Load loadInParallel arg rootDtos

                    return
                        List.zip rootDtos fs
                        |> List.map (fun (rootDto, f) ->
                            function
                            | [] -> f None
                            | [ child ] -> f (Some child)
                            | children ->
                                invalidOp
                                <| "Multiple child entities found for root entity "
                                   + $"'%A{loader.GetId rootDto}': %A{children}"
                        )
                }

        let newLoader: BatchLoader<'rootDto, 'rootDtoId, 'childDto list -> 'loadResult, 'arg> = {
            GetId = loader.GetId
            Load = load
        }

        batchLoadChildren loadChildren getRootId newLoader
