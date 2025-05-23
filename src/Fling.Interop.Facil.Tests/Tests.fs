module Tests

open Microsoft.Data.SqlClient



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


[<AutoOpen>]
module FacilMock =



    type rootIdList() =

        static member create(_Value: int) = rootIdList ()

        static member inline create(dto: ^a) =
            ignore (^a: (member Value: int) dto)
            rootIdList ()


    type Root_Insert_Executable() =
        member _.AsyncExecute() = async.Return 1

    type Root_Insert() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this
        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) = Root_Insert()
        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = Root_Insert()
        member _.WithParameters(_id: int, _data: string) = Root_Insert_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member Id: int) dto)
            ignore (^a: (member Data: string) dto)
            Root_Insert_Executable()


    type Root_InsertBatch_args(_fields: obj[]) =

        static member inline create(dto: ^a) : Root_InsertBatch_args =
            [|
                (^a: (member ``Id``: int) dto) |> box
                (^a: (member ``Data``: string) dto) |> box
            |]
            |> Root_InsertBatch_args


    type Root_InsertBatch_Executable() =
        member _.AsyncExecute() = async.Return 1

    type Root_InsertBatch() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this
        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) = Root_InsertBatch()
        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = Root_InsertBatch()
        member _.WithParameters(_args: seq<Root_InsertBatch_args>) = Root_InsertBatch_Executable()

    type Root_Update_Executable() =
        member _.AsyncExecute() = async.Return 1

    type Root_Update() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this
        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) = Root_Update()
        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = Root_Update()
        member _.WithParameters(_id: int, _data: string) = Root_Update_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member Id: int) dto)
            ignore (^a: (member Data: string) dto)
            Root_Update_Executable()



    type Root_UpdateBatch_args(_fields: obj[]) =

        static member inline create(dto: ^a) : Root_UpdateBatch_args =
            [|
                (^a: (member ``Id``: int) dto) |> box
                (^a: (member ``Data``: string) dto) |> box
            |]
            |> Root_UpdateBatch_args


    type Root_UpdateBatch_Executable() =
        member _.AsyncExecute() = async.Return 1

    type Root_UpdateBatch() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this
        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) = Root_UpdateBatch()
        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = Root_UpdateBatch()
        member _.WithParameters(_args: seq<Root_UpdateBatch_args>) = Root_UpdateBatch_Executable()


    type ChildToOne_Insert_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToOne_Insert() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this
        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) = ChildToOne_Insert()
        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToOne_Insert()
        member _.WithParameters(_rootIt: int, _data: string) = ChildToOne_Insert_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member RootId: int) dto)
            ignore (^a: (member Data: string) dto)
            ChildToOne_Insert_Executable()


    type ChildToOne_InsertBatch_args(_fields: obj[]) =

        static member inline create(dto: ^a) : ChildToOne_InsertBatch_args =
            [|
                (^a: (member ``RootId``: int) dto) |> box
                (^a: (member ``Data``: string) dto) |> box
            |]
            |> ChildToOne_InsertBatch_args


    type ChildToOne_InsertBatch_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToOne_InsertBatch() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this

        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) =
            ChildToOne_InsertBatch()

        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToOne_InsertBatch()
        member _.WithParameters(_args: seq<ChildToOne_InsertBatch_args>) = ChildToOne_InsertBatch_Executable()


    type ChildToOne_Update_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToOne_Update() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this
        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) = ChildToOne_Update()
        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToOne_Update()
        member _.WithParameters(_rootId: int, _data: string) = ChildToOne_Update_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member RootId: int) dto)
            ignore (^a: (member Data: string) dto)
            ChildToOne_Update_Executable()


    type ChildToOne_UpdateBatch_args(_fields: obj[]) =

        static member inline create(dto: ^a) : ChildToOne_UpdateBatch_args =
            [|
                (^a: (member ``RootId``: int) dto) |> box
                (^a: (member ``Data``: string) dto) |> box
            |]
            |> ChildToOne_UpdateBatch_args


    type ChildToOne_UpdateBatch_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToOne_UpdateBatch() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this

        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) =
            ChildToOne_UpdateBatch()

        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToOne_UpdateBatch()
        member _.WithParameters(_args: seq<ChildToOne_UpdateBatch_args>) = ChildToOne_UpdateBatch_Executable()


    type ChildToOne_GetByRootId_Executable() =
        member _.AsyncExecuteSingle() =
            async.Return Unchecked.defaultof<ChildToOneDto option>

    type ChildToOne_GetByRootId() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this

        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) =
            ChildToOne_GetByRootId()

        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToOne_GetByRootId()
        member _.WithParameters(_rootId: int) = ChildToOne_GetByRootId_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member RootId: int) dto)
            ChildToOne_GetByRootId_Executable()


    type ChildToOne_GetByRootIds_Executable() =
        member _.AsyncExecute() =
            async.Return Unchecked.defaultof<ResizeArray<ChildToOneDto>>

    type ChildToOne_GetByRootIds() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this

        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) =
            ChildToOne_GetByRootIds()

        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToOne_GetByRootIds()
        member _.WithParameters(_ids: seq<rootIdList>) = ChildToOne_GetByRootIds_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member Ids: #seq<rootIdList>) dto)
            ChildToOne_GetByRootIds_Executable()


    type ChildToOneOpt_Insert_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToOneOpt_Insert() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this
        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) = ChildToOneOpt_Insert()
        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToOneOpt_Insert()
        member _.WithParameters(_rootId: int, _data: string) = ChildToOneOpt_Insert_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member RootId: int) dto)
            ignore (^a: (member Data: string) dto)
            ChildToOneOpt_Insert_Executable()


    type ChildToOneOpt_InsertBatch_args(_fields: obj[]) =

        static member inline create(dto: ^a) : ChildToOneOpt_InsertBatch_args =
            [|
                (^a: (member ``RootId``: int) dto) |> box
                (^a: (member ``Data``: string) dto) |> box
            |]
            |> ChildToOneOpt_InsertBatch_args


    type ChildToOneOpt_InsertBatch_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToOneOpt_InsertBatch() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this

        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) =
            ChildToOneOpt_InsertBatch()

        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToOneOpt_InsertBatch()
        member _.WithParameters(_args: seq<ChildToOneOpt_InsertBatch_args>) = ChildToOneOpt_InsertBatch_Executable()


    type ChildToOneOpt_Update_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToOneOpt_Update() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this
        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) = ChildToOneOpt_Update()
        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToOneOpt_Update()
        member _.WithParameters(_rootId: int, _data: string) = ChildToOneOpt_Update_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member RootId: int) dto)
            ignore (^a: (member Data: string) dto)
            ChildToOneOpt_Update_Executable()


    type ChildToOneOpt_UpdateBatch_args(_fields: obj[]) =

        static member inline create(dto: ^a) : ChildToOneOpt_UpdateBatch_args =
            [|
                (^a: (member ``RootId``: int) dto) |> box
                (^a: (member ``Data``: string) dto) |> box
            |]
            |> ChildToOneOpt_UpdateBatch_args


    type ChildToOneOpt_UpdateBatch_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToOneOpt_UpdateBatch() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this

        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) =
            ChildToOneOpt_UpdateBatch()

        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToOneOpt_UpdateBatch()
        member _.WithParameters(_args: seq<ChildToOneOpt_UpdateBatch_args>) = ChildToOneOpt_UpdateBatch_Executable()


    type ChildToOneOpt_Delete_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToOneOpt_Delete() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this
        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) = ChildToOneOpt_Delete()
        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToOneOpt_Delete()
        member _.WithParameters(_rootId: int) = ChildToOneOpt_Delete_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member RootId: int) dto)
            ChildToOneOpt_Delete_Executable()


    type ChildToOneOpt_DeleteBatch_args(_fields: obj[]) =

        static member create(id: int) : ChildToOneOpt_DeleteBatch_args =
            [| id |> box |] |> ChildToOneOpt_DeleteBatch_args

        static member inline create(dto: ^a) : ChildToOneOpt_DeleteBatch_args =
            [| (^a: (member ``Id``: int) dto) |> box |] |> ChildToOneOpt_DeleteBatch_args


    type ChildToOneOpt_DeleteBatch_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToOneOpt_DeleteBatch() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this

        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) =
            ChildToOneOpt_DeleteBatch()

        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToOneOpt_DeleteBatch()
        member _.WithParameters(_args: seq<ChildToOneOpt_DeleteBatch_args>) = ChildToOneOpt_DeleteBatch_Executable()


    type ChildToOneOpt_GetByRootId_Executable() =
        member _.AsyncExecuteSingle() =
            async.Return Unchecked.defaultof<ChildToOneOptDto option>

    type ChildToOneOpt_GetByRootId() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this

        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) =
            ChildToOneOpt_GetByRootId()

        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToOneOpt_GetByRootId()
        member _.WithParameters(_rootId: int) = ChildToOneOpt_GetByRootId_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member RootId: int) dto)
            ChildToOneOpt_GetByRootId_Executable()


    type ChildToOneOpt_GetByRootIds_Executable() =
        member _.AsyncExecute() =
            async.Return Unchecked.defaultof<ResizeArray<ChildToOneOptDto>>

    type ChildToOneOpt_GetByRootIds() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this

        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) =
            ChildToOneOpt_GetByRootIds()

        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToOneOpt_GetByRootIds()
        member _.WithParameters(_ids: seq<rootIdList>) = ChildToOneOpt_GetByRootIds_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member Ids: #seq<rootIdList>) dto)
            ChildToOneOpt_GetByRootIds_Executable()


    type ChildToMany_Insert_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToMany_Insert() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this
        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) = ChildToMany_Insert()
        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToMany_Insert()
        member _.WithParameters(_rootId: int, _id: int, _data: string) = ChildToMany_Insert_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member RootId: int) dto)
            ignore (^a: (member Id: int) dto)
            ignore (^a: (member Data: string) dto)
            ChildToMany_Insert_Executable()


    type ChildToMany_InsertBatch_args(_fields: obj[]) =

        static member inline create(dto: ^a) : ChildToMany_InsertBatch_args =
            [|
                (^a: (member ``RootId``: int) dto) |> box
                (^a: (member ``Id``: int) dto) |> box
                (^a: (member ``Data``: string) dto) |> box
            |]
            |> ChildToMany_InsertBatch_args


    type ChildToMany_InsertBatch_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToMany_InsertBatch() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this

        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) =
            ChildToMany_InsertBatch()

        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToMany_InsertBatch()
        member _.WithParameters(_args: seq<ChildToMany_InsertBatch_args>) = ChildToMany_InsertBatch_Executable()


    type ChildToMany_Update_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToMany_Update() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this
        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) = ChildToMany_Update()
        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToMany_Update()
        member _.WithParameters(_rootId: int, _id: int, _data: string) = ChildToMany_Update_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member RootId: int) dto)
            ignore (^a: (member Id: int) dto)
            ignore (^a: (member Data: string) dto)
            ChildToMany_Update_Executable()


    type ChildToMany_UpdateBatch_args(_fields: obj[]) =

        static member inline create(dto: ^a) : ChildToMany_UpdateBatch_args =
            [|
                (^a: (member ``RootId``: int) dto) |> box
                (^a: (member ``Id``: int) dto) |> box
                (^a: (member ``Data``: string) dto) |> box
            |]
            |> ChildToMany_UpdateBatch_args


    type ChildToMany_UpdateBatch_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToMany_UpdateBatch() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this

        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) =
            ChildToMany_UpdateBatch()

        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToMany_UpdateBatch()
        member _.WithParameters(_args: seq<ChildToMany_UpdateBatch_args>) = ChildToMany_UpdateBatch_Executable()


    type ChildToMany_Delete_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToMany_Delete() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this
        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) = ChildToMany_Delete()
        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToMany_Delete()
        member _.WithParameters(_id: int) = ChildToMany_Delete_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member Id: int) dto)
            ChildToMany_Delete_Executable()


    type ChildToMany_DeleteBatch_args(_fields: obj[]) =

        static member create(id: int) : ChildToMany_DeleteBatch_args =
            [| id |> box |] |> ChildToMany_DeleteBatch_args

        static member inline create(dto: ^a) : ChildToMany_DeleteBatch_args =
            [| (^a: (member ``Id``: int) dto) |> box |] |> ChildToMany_DeleteBatch_args


    type ChildToMany_DeleteBatch_Executable() =
        member _.AsyncExecute() = async.Return 1

    type ChildToMany_DeleteBatch() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this

        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) =
            ChildToMany_DeleteBatch()

        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToMany_DeleteBatch()
        member _.WithParameters(_args: seq<ChildToMany_DeleteBatch_args>) = ChildToMany_DeleteBatch_Executable()


    type ChildToMany_GetByRootId_Executable() =
        member _.AsyncExecute() =
            async.Return Unchecked.defaultof<ResizeArray<ChildToManyDto>>

    type ChildToMany_GetByRootId() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this

        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) =
            ChildToMany_GetByRootId()

        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToMany_GetByRootId()
        member _.WithParameters(_rootId: int) = ChildToMany_GetByRootId_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member RootId: int) dto)
            ignore (^a: (member Id: int) dto)
            ChildToMany_GetByRootId_Executable()


    type ChildToMany_GetByRootIds_Executable() =
        member _.AsyncExecute() =
            async.Return Unchecked.defaultof<ResizeArray<ChildToManyDto>>

    type ChildToMany_GetByRootIds() =
        member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this

        static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) =
            ChildToMany_GetByRootIds()

        static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = ChildToMany_GetByRootIds()
        member _.WithParameters(_ids: seq<rootIdList>) = ChildToMany_GetByRootIds_Executable()

        member inline _.WithParameters(dto: ^a) =
            ignore (^a: (member Ids: #seq<rootIdList>) dto)
            ChildToMany_GetByRootIds_Executable()



module UsageCompileTimeTests =


    open Fling.Fling
    open Fling.Interop.Facil.Fling


    let load: (SqlConnection * SqlTransaction -> Async<RootDto option>) -> Async<Root option> =
        createLoader dtosToRoot (fun dto -> dto.Id)
        |> loadChild ChildToOne_GetByRootId
        |> loadOptChild ChildToOneOpt_GetByRootId
        |> loadChildren ChildToMany_GetByRootId
        |> loadWithTransactionFromConnStr ""


    let loadBatch: (SqlConnection * SqlTransaction -> Async<RootDto seq>) -> Async<Root list> =
        createBatchLoader dtosToRoot (fun dto -> dto.Id)
        |> batchLoadChild ChildToOne_GetByRootIds (fun dto -> dto.RootId)
        |> batchLoadOptChild ChildToOneOpt_GetByRootIds (fun dto -> dto.RootId)
        |> batchLoadChildren ChildToMany_GetByRootIds (fun dto -> dto.RootId)
        |> loadBatchWithTransactionFromConnStr ""


    let save: string -> Root option -> Root -> Async<unit> =
        saveRoot rootToDto Root_Insert Root_Update
        |> saveChild rootToToOneDto ChildToOne_Insert ChildToOne_Update
        |> saveOptChild
            rootToToOneOptDto
            (fun dto -> dto.RootId)
            ChildToOneOpt_Insert
            ChildToOneOpt_Update
            ChildToOneOpt_Delete
        |> saveChildren rootToToManyDtos (fun dto -> dto.Id) ChildToMany_Insert ChildToMany_Update ChildToMany_Delete
        |> saveWithTransactionFromConnStr


    let saveBatched: string -> Root option -> Root -> Async<unit> =
        saveRoot rootToDto Root_Insert Root_Update
        |> saveChild rootToToOneDto ChildToOne_Insert ChildToOne_Update
        |> saveOptChild
            rootToToOneOptDto
            (fun dto -> dto.RootId)
            ChildToOneOpt_Insert
            ChildToOneOpt_Update
            ChildToOneOpt_Delete
        |> batchSaveChildren
            rootToToManyDtos
            (fun dto -> dto.Id)
            ChildToMany_InsertBatch
            ChildToMany_UpdateBatch
            ChildToMany_DeleteBatch
        |> saveWithTransactionFromConnStr


    let saveBatchedRoot: string -> (Root option * Root) seq -> Async<unit> =
        Batch.saveRoot rootToDto Root_InsertBatch Root_UpdateBatch
        |> Batch.saveChild rootToToOneDto ChildToOne_InsertBatch ChildToOne_UpdateBatch
        |> Batch.saveChildWithDifferentOldNew
            rootToToOneDto
            rootToToOneDto
            ChildToOne_InsertBatch
            ChildToOne_UpdateBatch
        |> Batch.saveChildWithoutUpdate rootToToOneDto ChildToOne_InsertBatch
        |> Batch.saveChildWithoutUpdateWithDifferentOldNew rootToToOneDto rootToToOneDto ChildToOne_InsertBatch
        |> Batch.saveOptChild
            rootToToOneOptDto
            (fun dto -> dto.RootId)
            ChildToOneOpt_InsertBatch
            ChildToOneOpt_UpdateBatch
            ChildToOneOpt_DeleteBatch
        |> Batch.saveOptChildWithDifferentOldNew
            rootToToOneOptDto
            rootToToOneOptDto
            (fun dto -> dto.RootId)
            ChildToOneOpt_InsertBatch
            ChildToOneOpt_UpdateBatch
            ChildToOneOpt_DeleteBatch
        |> Batch.saveOptChildWithoutUpdate
            rootToToOneOptDto
            (fun dto -> dto.RootId)
            ChildToOneOpt_InsertBatch
            ChildToOneOpt_DeleteBatch
        |> Batch.saveOptChildWithoutUpdateWithDifferentOldNew
            rootToToOneOptDto
            rootToToOneOptDto
            (fun dto -> dto.RootId)
            ChildToOneOpt_InsertBatch
            ChildToOneOpt_DeleteBatch
        |> Batch.saveChildren
            rootToToManyDtos
            (fun dto -> dto.Id)
            ChildToMany_InsertBatch
            ChildToMany_UpdateBatch
            ChildToMany_DeleteBatch
        |> Batch.saveChildrenWithDifferentOldNew
            rootToToManyDtos
            rootToToManyDtos
            (fun dto -> dto.Id)
            ChildToMany_InsertBatch
            ChildToMany_UpdateBatch
            ChildToMany_DeleteBatch
        |> Batch.saveChildrenWithoutUpdate
            rootToToManyDtos
            (fun dto -> dto.Id)
            ChildToMany_InsertBatch
            ChildToMany_DeleteBatch
        |> Batch.saveChildrenWithoutUpdateWithDifferentOldNew
            rootToToManyDtos
            rootToToManyDtos
            (fun dto -> dto.Id)
            ChildToMany_InsertBatch
            ChildToMany_DeleteBatch
        |> Batch.saveWithTransactionFromConnStr


    let saveWithOutput: string -> Root option -> Root -> Async<int option> =
        saveRootWithOutput rootToDto Root_Insert Root_Update
        |> saveChild rootToToOneDto ChildToOne_Insert ChildToOne_Update
        |> saveOptChild
            rootToToOneOptDto
            (fun dto -> dto.RootId)
            ChildToOneOpt_Insert
            ChildToOneOpt_Update
            ChildToOneOpt_Delete
        |> saveChildren rootToToManyDtos (fun dto -> dto.Id) ChildToMany_Insert ChildToMany_Update ChildToMany_Delete
        |> saveWithTransactionFromConnStr


    let saveWithOutputBatched: string -> Root option -> Root -> Async<int option> =
        saveRootWithOutput rootToDto Root_Insert Root_Update
        |> saveChild rootToToOneDto ChildToOne_Insert ChildToOne_Update
        |> saveOptChild
            rootToToOneOptDto
            (fun dto -> dto.RootId)
            ChildToOneOpt_Insert
            ChildToOneOpt_Update
            ChildToOneOpt_Delete
        |> batchSaveChildren
            rootToToManyDtos
            (fun dto -> dto.Id)
            ChildToMany_InsertBatch
            ChildToMany_UpdateBatch
            ChildToMany_DeleteBatch
        |> saveWithTransactionFromConnStr


    let saveWithoutUpdate: string -> Root option -> Root -> Async<unit> =
        saveRoot rootToDto Root_Insert Root_Update
        |> saveChildWithoutUpdate rootToToOneDto ChildToOne_Insert
        |> saveOptChildWithoutUpdate rootToToOneOptDto (fun dto -> dto.RootId) ChildToOneOpt_Insert ChildToOneOpt_Delete
        |> saveChildrenWithoutUpdate rootToToManyDtos (fun dto -> dto.Id) ChildToMany_Insert ChildToMany_Delete
        |> saveWithTransactionFromConnStr


    let saveWithoutUpdateBatched: string -> Root option -> Root -> Async<unit> =
        saveRoot rootToDto Root_Insert Root_Update
        |> saveChildWithoutUpdate rootToToOneDto ChildToOne_Insert
        |> saveOptChildWithoutUpdate rootToToOneOptDto (fun dto -> dto.RootId) ChildToOneOpt_Insert ChildToOneOpt_Delete
        |> batchSaveChildrenWithoutUpdate
            rootToToManyDtos
            (fun dto -> dto.Id)
            ChildToMany_InsertBatch
            ChildToMany_DeleteBatch
        |> saveWithTransactionFromConnStr



module IgnoreCompileTimeTests =


    open Fling.Interop.Facil
    open Fling.Interop.Facil.Fling


    let save: string -> Root option -> Root -> Async<unit> =
        saveRoot rootToDto Root_Insert Root_Update
        |> saveChild rootToToOneDto FacilIgnore FacilIgnore
        |> saveOptChild rootToToOneOptDto (fun dto -> dto.RootId) FacilIgnore FacilIgnore FacilIgnore
        |> saveChildren rootToToManyDtos (fun dto -> dto.Id) FacilIgnore FacilIgnore FacilIgnore
        |> saveWithTransactionFromConnStr


    let saveWithOutput: string -> Root option -> Root -> Async<int option> =
        saveRootWithOutput rootToDto Root_Insert Root_Update
        |> saveChild rootToToOneDto FacilIgnore FacilIgnore
        |> saveOptChild rootToToOneOptDto (fun dto -> dto.RootId) FacilIgnore FacilIgnore FacilIgnore
        |> saveChildren rootToToManyDtos (fun dto -> dto.Id) FacilIgnore FacilIgnore FacilIgnore
        |> saveWithTransactionFromConnStr


    let saveWithoutUpdate: string -> Root option -> Root -> Async<unit> =
        saveRoot rootToDto Root_Insert Root_Update
        |> saveChildWithoutUpdate rootToToOneDto FacilIgnore
        |> saveOptChildWithoutUpdate rootToToOneOptDto (fun dto -> dto.RootId) FacilIgnore FacilIgnore
        |> saveChildrenWithoutUpdate rootToToManyDtos (fun dto -> dto.Id) FacilIgnore FacilIgnore
        |> saveWithTransactionFromConnStr



module ThrowCompileTimeTests =


    open Fling.Interop.Facil
    open Fling.Interop.Facil.Fling


    let save: string -> Root option -> Root -> Async<unit> =
        saveRoot rootToDto Root_Insert Root_Update
        |> saveChild rootToToOneDto FacilThrow FacilThrow
        |> saveOptChild rootToToOneOptDto (fun dto -> dto.RootId) FacilThrow FacilThrow FacilThrow
        |> saveChildren rootToToManyDtos (fun dto -> dto.Id) FacilThrow FacilThrow FacilThrow
        |> saveWithTransactionFromConnStr


    let saveWithOutput: string -> Root option -> Root -> Async<int option> =
        saveRootWithOutput rootToDto Root_Insert Root_Update
        |> saveChild rootToToOneDto FacilThrow FacilThrow
        |> saveOptChild rootToToOneOptDto (fun dto -> dto.RootId) FacilThrow FacilThrow FacilThrow
        |> saveChildren rootToToManyDtos (fun dto -> dto.Id) FacilThrow FacilThrow FacilThrow
        |> saveWithTransactionFromConnStr


    let saveWithoutUpdate: string -> Root option -> Root -> Async<unit> =
        saveRoot rootToDto Root_Insert Root_Update
        |> saveChildWithoutUpdate rootToToOneDto FacilThrow
        |> saveOptChildWithoutUpdate rootToToOneOptDto (fun dto -> dto.RootId) FacilThrow FacilThrow
        |> saveChildrenWithoutUpdate rootToToManyDtos (fun dto -> dto.Id) FacilThrow FacilThrow
        |> saveWithTransactionFromConnStr
