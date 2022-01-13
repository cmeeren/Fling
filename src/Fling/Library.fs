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


  let saveChildrenWithDifferentOldNew
    (oldToDtos: 'rootEntity -> 'childDto list)
    (newToDtos: 'rootEntity -> 'childDto list)
    (getId: 'childDto -> 'childDtoId)
    (insert: 'arg -> 'childDto -> Async<unit>)
    (update: 'arg -> 'childDto -> Async<unit>)
    (delete: 'arg -> 'childDtoId -> Async<unit>)
    (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
    fun (arg: 'arg) (oldRoot: 'rootEntity option) (newRoot: 'rootEntity) ->
      async {
        let! result = existingSave arg oldRoot newRoot

        match oldRoot with
        | None ->
            for dto in newToDtos newRoot do
              do! insert arg dto

        | Some oldRoot ->
            let oldChildren = oldToDtos oldRoot
            let newChildren = newToDtos newRoot

            let oldChildrenById = Dictionary<'childDtoId, 'childDto>()
            let newChildrenById = Dictionary<'childDtoId, 'childDto>()

            for dto in oldChildren do
              oldChildrenById[getId dto] <- dto

            for dto in newChildren do
              newChildrenById[getId dto] <- dto

            for oldChild in oldChildren do
              let oldChildId = getId oldChild

              if not (newChildrenById.ContainsKey oldChildId) then
                do! delete arg oldChildId

            for newChild in newChildren do
              match oldChildrenById.TryGetValue(getId newChild) with
              | false, _ -> do! insert arg newChild
              | true, oldChild when newChild <> oldChild -> do! update arg newChild
              | true, _ -> ()

        return result
      }


  let saveChildren
    (toDtos: 'rootEntity -> 'childDto list)
    (getId: 'childDto -> 'childDtoId)
    (insert: 'arg -> 'childDto -> Async<unit>)
    (update: 'arg -> 'childDto -> Async<unit>)
    (delete: 'arg -> 'childDtoId -> Async<unit>)
    (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
    saveChildrenWithDifferentOldNew
      toDtos
      toDtos
      getId
      insert
      update
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
    saveChildrenWithDifferentOldNew
      oldToDtos
      newToDtos
      getId
      insert
      (fun _ dto -> failwith $"Update needed in saveChildrenWithoutUpdate due to changed child DTO of type %s{typeof<'childDto>.FullName} with ID %A{getId dto}. Updated child DTO: %A{dto}")
      delete
      existingSave


  let saveChildrenWithoutUpdate
    (toDtos: 'rootEntity -> 'childDto list)
    (getId: 'childDto -> 'childDtoId)
    (insert: 'arg -> 'childDto -> Async<unit>)
    (delete: 'arg -> 'childDtoId -> Async<unit>)
    (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
    saveChildrenWithoutUpdateWithDifferentOldNew
      toDtos
      toDtos
      getId
      insert
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
    saveChildrenWithDifferentOldNew
      (oldToDto >> Option.toList)
      (newToDto >> Option.toList)
      getId
      insert
      update
      delete
      existingSave


  let saveOptChild
    (toDto: 'rootEntity -> 'childDto option)
    (getId: 'childDto -> 'childDtoId)
    (insert: 'arg -> 'childDto -> Async<unit>)
    (update: 'arg -> 'childDto -> Async<unit>)
    (delete: 'arg -> 'childDtoId -> Async<unit>)
    (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
    saveOptChildWithDifferentOldNew toDto toDto getId insert update delete existingSave


  let saveOptChildWithoutUpdateWithDifferentOldNew
    (oldToDto: 'rootEntity -> 'childDto option)
    (newToDto: 'rootEntity -> 'childDto option)
    (getId: 'childDto -> 'childDtoId)
    (insert: 'arg -> 'childDto -> Async<unit>)
    (delete: 'arg -> 'childDtoId -> Async<unit>)
    (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
    saveOptChildWithDifferentOldNew
      oldToDto
      newToDto
      getId
      insert
      (fun _ dto -> failwith $"Update needed in saveOptChildWithoutUpdate due to changed child DTO of type %s{typeof<'childDto>.FullName} with ID %A{getId dto}. Updated child DTO: %A{dto}")
      delete
      existingSave


  let saveOptChildWithoutUpdate
    (toDto: 'rootEntity -> 'childDto option)
    (getId: 'childDto -> 'childDtoId)
    (insert: 'arg -> 'childDto -> Async<unit>)
    (delete: 'arg -> 'childDtoId -> Async<unit>)
    (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
    saveOptChildWithoutUpdateWithDifferentOldNew
      toDto
      toDto
      getId
      insert
      delete
      existingSave


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
      (fun _ -> 0)  // We always have exactly one child
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
    saveChildWithDifferentOldNew
      toDto
      toDto
      insert
      update
      existingSave
      

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
      (fun _ dto -> failwith $"Update needed in saveChildWithoutUpdate due to changed child DTO of type %s{typeof<'childDto>.FullName}. Updated child DTO: %A{dto}")
      existingSave
      

  let saveChildWithoutUpdate
    (toDto: 'rootEntity -> 'childDto)
    (insert: 'arg -> 'childDto -> Async<unit>)
    (existingSave: 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    : 'arg -> 'rootEntity option -> 'rootEntity -> Async<'saveResult> =
    saveChildWithoutUpdateWithDifferentOldNew
      toDto
      toDto
      insert
      existingSave


  type Loader<'rootDto, 'rootDtoId, 'loadResult, 'arg when 'rootDtoId: equality> =
    { GetId: 'rootDto -> 'rootDtoId
      Load: 'arg -> 'rootDto -> Async<'loadResult> }


  type BatchLoader<'rootDto, 'rootDtoId, 'loadResult, 'arg when 'rootDtoId: equality> =
    { GetId: 'rootDto -> 'rootDtoId
      Load: 'arg -> 'rootDto list -> Async<'loadResult list> }


  let createLoader
    (f: 'rootDto -> 'loadResult)
    (getId: 'rootDto -> 'rootDtoId)
    : Loader<'rootDto, 'rootDtoId, 'loadResult, 'arg> =
    { GetId = getId
      Load = fun _arg dto -> f dto |> async.Return }


  let createBatchLoader
    (f: 'rootDto -> 'loadResult)
    (getId: 'rootDto -> 'rootDtoId)
    : BatchLoader<'rootDto, 'rootDtoId, 'loadResult, 'arg> =
    { GetId = getId
      Load = fun _arg dtos -> dtos |> List.map f |> async.Return }


  let runLoader (loader: Loader<'rootDto, 'rootDtoId, 'loadResult, 'arg>) arg =
    loader.Load arg


  let runBatchLoader (loader: BatchLoader<'rootDto, 'rootDtoId, 'loadResult, 'arg>) arg =
    loader.Load arg


  let loadChild
    (loadChild: 'arg -> 'rootDtoId -> Async<'childDto>)
    (loader: Loader<'rootDto, 'rootDtoId, 'childDto -> 'loadResult, 'arg>)
    : Loader<'rootDto, 'rootDtoId, 'loadResult, 'arg> =

    let load arg rootDto =
      async {
        let! loadComp = loader.Load arg rootDto |> Async.StartChild

        let! childComp =
          rootDto
          |> loader.GetId
          |> loadChild arg
          |> Async.StartChild

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

    let load arg rootDtos =
      async {
        let! loadComp = loader.Load arg rootDtos |> Async.StartChild

        let! childComp =
          rootDtos
          |> List.map loader.GetId
          |> loadChildren arg
          |> Async.StartChild

        let! load = loadComp
        let! childBatch = childComp

        let childByRootId =
          Dictionary<'rootDtoId, ResizeArray<'childDto>>()

        for child in childBatch do
          let rootId = getRootId child

          match childByRootId.TryGetValue rootId with
          | false, _ -> childByRootId[rootId] <- ResizeArray([ child ])
          | true, existingChildren -> existingChildren.Add child

        return
          List.zip load rootDtos
          |> List.map
               (fun (f, rootDto) ->
                 let rootId = loader.GetId rootDto

                 match childByRootId.TryGetValue rootId with
                 | false, _ -> f []
                 | true, children -> f (Seq.toList children))
      }

    { GetId = loader.GetId; Load = load }


  let batchLoadChild
    (loadChildren: 'arg -> 'rootDtoId list -> Async<'childDto list>)
    (getRootId: 'childDto -> 'rootDtoId)
    (loader: BatchLoader<'rootDto, 'rootDtoId, 'childDto -> 'loadResult, 'arg>)
    : BatchLoader<'rootDto, 'rootDtoId, 'loadResult, 'arg> =

    let load : 'arg -> 'rootDto list -> Async<('childDto list -> 'loadResult) list> =
      fun arg rootDtos ->
        async {
          let! fs = loader.Load arg rootDtos

          return
            List.zip rootDtos fs
            |> List.map
                 (fun (rootDto, f) ->
                   function
                   | [] ->
                       invalidOp
                         $"No child entity found for root entity '%A{loader.GetId rootDto}'"
                   | [ child ] -> f child
                   | children ->
                       invalidOp
                       <| "Multiple child entities found for root entity "
                          + $"'%A{loader.GetId rootDto}': %A{children}")
        }

    let newLoader : BatchLoader<'rootDto, 'rootDtoId, 'childDto list -> 'loadResult, 'arg> =
      { GetId = loader.GetId; Load = load }

    batchLoadChildren loadChildren getRootId newLoader


  let batchLoadOptChild
    (loadChildren: 'arg -> 'rootDtoId list -> Async<'childDto list>)
    (getRootId: 'childDto -> 'rootDtoId)
    (loader: BatchLoader<'rootDto, 'rootDtoId, 'childDto option -> 'loadResult, 'arg>)
    : BatchLoader<'rootDto, 'rootDtoId, 'loadResult, 'arg> =

    let load : 'arg -> 'rootDto list -> Async<('childDto list -> 'loadResult) list> =
      fun arg rootDtos ->
        async {
          let! fs = loader.Load arg rootDtos

          return
            List.zip rootDtos fs
            |> List.map
                 (fun (rootDto, f) ->
                   function
                   | [] -> f None
                   | [ child ] -> f (Some child)
                   | children ->
                       invalidOp
                       <| "Multiple child entities found for root entity "
                          + $"'%A{loader.GetId rootDto}': %A{children}")
        }

    let newLoader : BatchLoader<'rootDto, 'rootDtoId, 'childDto list -> 'loadResult, 'arg> =
      { GetId = loader.GetId; Load = load }

    batchLoadChildren loadChildren getRootId newLoader
