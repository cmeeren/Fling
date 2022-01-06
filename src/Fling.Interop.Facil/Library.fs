namespace Fling.Interop.Facil

open System.ComponentModel
open Microsoft.Data.SqlClient
open Fling



[<EditorBrowsable(EditorBrowsableState.Never)>]
type FacilIgnore_Executable internal () =
  member _.AsyncExecute() = async.Return 0


/// A mock Facil "script" that can be used as an insert/update/delete script and does
/// nothing if called.
type FacilIgnore () =
  member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this
  static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) = FacilIgnore()
  static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = FacilIgnore()
  member _.WithParameters(_dto: 'a) = FacilIgnore_Executable()


[<EditorBrowsable(EditorBrowsableState.Never)>]
type FacilThrow_Executable internal (argStringRepresentation) =
  member _.AsyncExecute() =
    async {
      invalidOp $"FacilThrow called with %s{argStringRepresentation}"
      return 0
    }


/// A mock Facil "script" that can be used as an insert/update/delete script and throws if
/// called.
type FacilThrow () =
  member this.ConfigureCommand(_configureCommand: SqlCommand -> unit) = this
  static member WithConnection(_connStr: string, ?_configureConn: SqlConnection -> unit) = FacilThrow()
  static member WithConnection(_conn: SqlConnection, ?_tran: SqlTransaction) = FacilThrow()
  member _.WithParameters(dto: 'a) = FacilThrow_Executable(sprintf "%A" dto)



module Fling =


  let withTransactionFromConnStr f connStr oldEntity newEntity =
    async {
      let! ct = Async.CancellationToken
      use conn = new SqlConnection(connStr)
      do! conn.OpenAsync(ct) |> Async.AwaitTask
      use tran = conn.BeginTransaction ()
      let! res = f (conn, tran) oldEntity newEntity
      do! tran.CommitAsync ct |> Async.AwaitTask
      return res
    }


  let inline saveRoot< ^insertScript, ^insertScript_executable, ^updateScript, ^updateScript_executable, ^rootDto, 'rootEntity, 'insertResult, 'updateResult when
                       ^insertScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript)
                       and ^insertScript : (member WithParameters: ^rootDto -> ^insertScript_executable)
                       and ^insertScript_executable : (member AsyncExecute: unit -> Async<'insertResult>)
                       and ^updateScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^updateScript)
                       and ^updateScript : (member WithParameters: ^rootDto -> ^updateScript_executable)
                       and ^updateScript_executable : (member AsyncExecute: unit -> Async<'updateResult>)
                       and ^rootDto : equality>
    (toDto: 'rootEntity -> 'rootDto)
    (_insertScriptCtor: unit -> ^insertScript)
    (_updateScriptCtor: unit -> ^updateScript)
    =

    let insert (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^rootDto) : Async<unit> =
      async {
        let withConn = (^insertScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript) (conn, Some tran))
        let executable = (^insertScript: (member WithParameters: ^rootDto -> ^insertScript_executable) withConn, rootDto)
        do! (^insertScript_executable: (member AsyncExecute: unit -> Async<'insertResult>) executable) |> Async.Ignore<'insertResult>
      }

    let update (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^rootDto) : Async<unit> =
      async {
        let withConn = (^updateScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^updateScript) (conn, Some tran))
        let executable = (^updateScript: (member WithParameters: ^rootDto -> ^updateScript_executable) withConn, rootDto)
        do! (^updateScript_executable: (member AsyncExecute: unit -> Async<'updateResult>) executable) |> Async.Ignore<'updateResult>
      }

    Fling.saveRoot
      toDto
      insert
      update


  let inline saveRootWithOutput< ^insertScript, ^insertScript_executable, ^updateScript, ^updateScript_executable, ^rootDto, 'rootEntity, 'result when
                                 ^insertScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript)
                                 and ^insertScript : (member WithParameters: ^rootDto -> ^insertScript_executable)
                                 and ^insertScript_executable : (member AsyncExecute: unit -> Async<'result>)
                                 and ^updateScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^updateScript)
                                 and ^updateScript : (member WithParameters: ^rootDto -> ^updateScript_executable)
                                 and ^updateScript_executable : (member AsyncExecute: unit -> Async<'result>)
                                 and ^rootDto : equality>
    (toDto: 'rootEntity -> 'rootDto)
    (_insertScriptCtor: unit -> ^insertScript)
    (_updateScriptCtor: unit -> ^updateScript)
    =

    let insert (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^rootDto) : Async<'result> =
      async {
        let withConn = (^insertScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript) (conn, Some tran))
        let executable = (^insertScript: (member WithParameters: ^rootDto -> ^insertScript_executable) withConn, rootDto)
        return! (^insertScript_executable: (member AsyncExecute: unit -> Async<'result>) executable)
      }

    let update (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^rootDto) : Async<'result> =
      async {
        let withConn = (^updateScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^updateScript) (conn, Some tran))
        let executable = (^updateScript: (member WithParameters: ^rootDto -> ^updateScript_executable) withConn, rootDto)
        return! (^updateScript_executable: (member AsyncExecute: unit -> Async<'result>) executable)
      }

    Fling.saveRootWithOutput
      toDto
      insert
      update


  let inline saveChildWithDifferentOldNew< ^insertScript, ^insertScript_executable, ^updateScript, ^updateScript_executable, ^childDto, 'rootEntity, 'insertResult, 'updateResult, 'saveResult when
                                           ^insertScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript)
                                           and ^insertScript : (member WithParameters: ^childDto -> ^insertScript_executable)
                                           and ^insertScript_executable : (member AsyncExecute: unit -> Async<'insertResult>)
                                           and ^updateScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^updateScript)
                                           and ^updateScript : (member WithParameters: ^childDto -> ^updateScript_executable)
                                           and ^updateScript_executable : (member AsyncExecute: unit -> Async<'updateResult>)
                                           and ^childDto : equality>
    (oldToDto: 'rootEntity -> ^childDto)
    (newToDto: 'rootEntity -> ^childDto)
    (_insertScriptCtor: unit -> ^insertScript)
    (_updateScriptCtor: unit -> ^updateScript)
    (existingSave: SqlConnection * SqlTransaction -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>) =

    let insert (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^insertScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript) (conn, Some tran))
        let executable = (^insertScript: (member WithParameters: ^childDto -> ^insertScript_executable) withConn, rootDto)
        do! (^insertScript_executable: (member AsyncExecute: unit -> Async<'insertResult>) executable) |> Async.Ignore<'insertResult>
      }

    let update (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^updateScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^updateScript) (conn, Some tran))
        let executable = (^updateScript: (member WithParameters: ^childDto -> ^updateScript_executable) withConn, rootDto)
        do! (^updateScript_executable: (member AsyncExecute: unit -> Async<'updateResult>) executable) |> Async.Ignore<'updateResult>
      }

    Fling.saveChildWithDifferentOldNew
      oldToDto
      newToDto
      insert
      update
      existingSave


  let inline saveChild
    (toDto: 'rootEntity -> ^childDto)
    (_insertScriptCtor: unit -> ^insertScript)
    (_updateScriptCtor: unit -> ^updateScript)
    (existingSave: SqlConnection * SqlTransaction -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>) =
    saveChildWithDifferentOldNew toDto toDto _insertScriptCtor _updateScriptCtor existingSave


  let inline saveChildWithoutUpdateWithDifferentOldNew< ^insertScript, ^insertScript_executable, ^updateScript, ^updateScript_executable, ^childDto, 'rootEntity, 'insertResult, 'updateResult, 'saveResult when
                                                        ^insertScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript)
                                                        and ^insertScript : (member WithParameters: ^childDto -> ^insertScript_executable)
                                                        and ^insertScript_executable : (member AsyncExecute: unit -> Async<'insertResult>)
                                                        and ^childDto : equality>
    (oldToDto: 'rootEntity -> ^childDto)
    (newToDto: 'rootEntity -> ^childDto)
    (_insertScriptCtor: unit -> ^insertScript)
    (existingSave: SqlConnection * SqlTransaction -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>) =

    let insert (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^insertScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript) (conn, Some tran))
        let executable = (^insertScript: (member WithParameters: ^childDto -> ^insertScript_executable) withConn, rootDto)
        do! (^insertScript_executable: (member AsyncExecute: unit -> Async<'insertResult>) executable) |> Async.Ignore<'insertResult>
      }

    Fling.saveChildWithoutUpdateWithDifferentOldNew
      oldToDto
      newToDto
      insert
      existingSave


  let inline saveChildWithoutUpdate
    (toDto: 'rootEntity -> ^childDto)
    (_insertScriptCtor: unit -> ^insertScript)
    (existingSave: SqlConnection * SqlTransaction -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>) =
    saveChildWithoutUpdateWithDifferentOldNew toDto toDto _insertScriptCtor existingSave


  let inline saveOptChildWithDifferentOldNew< ^insertScript, ^insertScript_executable, ^updateScript, ^updateScript_executable, ^deleteScript, ^deleteScript_executable, ^childDto, 'childDtoId, 'rootEntity, 'insertResult, 'updateResult, 'deleteResult, 'saveResult when
                                              ^insertScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript)
                                              and ^insertScript : (member WithParameters: ^childDto -> ^insertScript_executable)
                                              and ^insertScript_executable : (member AsyncExecute: unit -> Async<'insertResult>)
                                              and ^updateScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^updateScript)
                                              and ^updateScript : (member WithParameters: ^childDto -> ^updateScript_executable)
                                              and ^updateScript_executable : (member AsyncExecute: unit -> Async<'updateResult>)
                                              and ^deleteScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^deleteScript)
                                              and ^deleteScript : (member WithParameters: 'childDtoId -> ^deleteScript_executable)
                                              and ^deleteScript_executable : (member AsyncExecute: unit -> Async<'deleteResult>)
                                              and ^childDto : equality
                                              and ^childDtoId : equality>
    (oldToDto: 'rootEntity -> ^childDto option)
    (newToDto: 'rootEntity -> ^childDto option)
    (getId: ^childDto -> 'childDtoId)
    (_insertScriptCtor: unit -> ^insertScript)
    (_updateScriptCtor: unit -> ^updateScript)
    (_deleteScriptCtor: unit -> ^deleteScript)
    (existingSave: SqlConnection * SqlTransaction -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    =

    let insert (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^insertScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript) (conn, Some tran))
        let executable = (^insertScript: (member WithParameters: ^childDto -> ^insertScript_executable) withConn, rootDto)
        do! (^insertScript_executable: (member AsyncExecute: unit -> Async<'insertResult>) executable) |> Async.Ignore<'insertResult>
      }

    let update (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^updateScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^updateScript) (conn, Some tran))
        let executable = (^updateScript: (member WithParameters: ^childDto -> ^updateScript_executable) withConn, rootDto)
        do! (^updateScript_executable: (member AsyncExecute: unit -> Async<'updateResult>) executable) |> Async.Ignore<'updateResult>
      }

    let delete (conn: SqlConnection, tran: SqlTransaction) (childDtoId: 'childDtoId) : Async<unit> =
      async {
        let withConn = (^deleteScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^deleteScript) (conn, Some tran))
        let executable = (^deleteScript: (member WithParameters: 'childDtoId -> ^deleteScript_executable) withConn, childDtoId)
        do! (^deleteScript_executable: (member AsyncExecute: unit -> Async<'deleteResult>) executable) |> Async.Ignore<'deleteResult>
      }

    Fling.saveOptChildWithDifferentOldNew
      oldToDto
      newToDto
      getId
      insert
      update
      delete
      existingSave


  let inline saveOptChild
    (toDto: 'rootEntity -> ^childDto option)
    (getId: ^childDto -> 'childDtoId)
    (_insertScriptCtor: unit -> ^insertScript)
    (_updateScriptCtor: unit -> ^updateScript)
    (_deleteScriptCtor: unit -> ^deleteScript)
    (existingSave: SqlConnection * SqlTransaction -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    =
    saveOptChildWithDifferentOldNew toDto toDto getId _insertScriptCtor _updateScriptCtor _deleteScriptCtor existingSave


  let inline saveOptChildWithoutUpdateWithDifferentOldNew< ^insertScript, ^insertScript_executable, ^updateScript, ^updateScript_executable, ^deleteScript, ^deleteScript_executable, ^childDto, 'childDtoId, 'rootEntity, 'insertResult, 'updateResult, 'deleteResult, 'saveResult when
                                                           ^insertScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript)
                                                           and ^insertScript : (member WithParameters: ^childDto -> ^insertScript_executable)
                                                           and ^insertScript_executable : (member AsyncExecute: unit -> Async<'insertResult>)
                                                           and ^deleteScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^deleteScript)
                                                           and ^deleteScript : (member WithParameters: 'childDtoId -> ^deleteScript_executable)
                                                           and ^deleteScript_executable : (member AsyncExecute: unit -> Async<'deleteResult>)
                                                           and ^childDto : equality
                                                           and ^childDtoId : equality>
    (oldToDto: 'rootEntity -> ^childDto option)
    (newToDto: 'rootEntity -> ^childDto option)
    (getId: ^childDto -> 'childDtoId)
    (_insertScriptCtor: unit -> ^insertScript)
    (_deleteScriptCtor: unit -> ^deleteScript)
    (existingSave: SqlConnection * SqlTransaction -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    =

    let insert (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^insertScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript) (conn, Some tran))
        let executable = (^insertScript: (member WithParameters: ^childDto -> ^insertScript_executable) withConn, rootDto)
        do! (^insertScript_executable: (member AsyncExecute: unit -> Async<'insertResult>) executable) |> Async.Ignore<'insertResult>
      }

    let delete (conn: SqlConnection, tran: SqlTransaction) (childDtoId: 'childDtoId) : Async<unit> =
      async {
        let withConn = (^deleteScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^deleteScript) (conn, Some tran))
        let executable = (^deleteScript: (member WithParameters: 'childDtoId -> ^deleteScript_executable) withConn, childDtoId)
        do! (^deleteScript_executable: (member AsyncExecute: unit -> Async<'deleteResult>) executable) |> Async.Ignore<'deleteResult>
      }
      
    Fling.saveOptChildWithoutUpdateWithDifferentOldNew
      oldToDto
      newToDto
      getId
      insert
      delete
      existingSave


  let inline saveOptChildWithoutUpdate
    (toDto: 'rootEntity -> ^childDto option)
    (getId: ^childDto -> 'childDtoId)
    (_insertScriptCtor: unit -> ^insertScript)
    (_deleteScriptCtor: unit -> ^deleteScript)
    (existingSave: SqlConnection * SqlTransaction -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    =
    saveOptChildWithoutUpdateWithDifferentOldNew toDto toDto getId _insertScriptCtor _deleteScriptCtor existingSave


  let inline saveChildrenWithDifferentOldNew< ^insertScript, ^insertScript_executable, ^updateScript, ^updateScript_executable, ^deleteScript, ^deleteScript_executable, ^childDto, 'childDtoId, 'rootEntity, 'insertResult, 'updateResult, 'deleteResult, 'saveResult when
                                              ^insertScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript)
                                              and ^insertScript : (member WithParameters: ^childDto -> ^insertScript_executable)
                                              and ^insertScript_executable : (member AsyncExecute: unit -> Async<'insertResult>)
                                              and ^updateScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^updateScript)
                                              and ^updateScript : (member WithParameters: ^childDto -> ^updateScript_executable)
                                              and ^updateScript_executable : (member AsyncExecute: unit -> Async<'updateResult>)
                                              and ^deleteScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^deleteScript)
                                              and ^deleteScript : (member WithParameters: 'childDtoId -> ^deleteScript_executable)
                                              and ^deleteScript_executable : (member AsyncExecute: unit -> Async<'deleteResult>)
                                              and ^childDto : equality
                                              and ^childDtoId : equality>
    (oldToDtos: 'rootEntity -> ^childDto list)
    (newToDtos: 'rootEntity -> ^childDto list)
    (getId: ^childDto -> 'childDtoId)
    (_insertScriptCtor: unit -> ^insertScript)
    (_updateScriptCtor: unit -> ^updateScript)
    (_deleteScriptCtor: unit -> ^deleteScript)
    (existingSave: SqlConnection * SqlTransaction -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    =

    let insert (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^insertScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript) (conn, Some tran))
        let executable = (^insertScript: (member WithParameters: ^childDto -> ^insertScript_executable) withConn, rootDto)
        do! (^insertScript_executable: (member AsyncExecute: unit -> Async<'insertResult>) executable) |> Async.Ignore<'insertResult>
      }

    let update (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^updateScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^updateScript) (conn, Some tran))
        let executable = (^updateScript: (member WithParameters: ^childDto -> ^updateScript_executable) withConn, rootDto)
        do! (^updateScript_executable: (member AsyncExecute: unit -> Async<'updateResult>) executable) |> Async.Ignore<'updateResult>
      }

    let delete (conn: SqlConnection, tran: SqlTransaction) (childDtoId: 'childDtoId) : Async<unit> =
      async {
        let withConn = (^deleteScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^deleteScript) (conn, Some tran))
        let executable = (^deleteScript: (member WithParameters: 'childDtoId -> ^deleteScript_executable) withConn, childDtoId)
        do! (^deleteScript_executable: (member AsyncExecute: unit -> Async<'deleteResult>) executable) |> Async.Ignore<'deleteResult>
      }

    Fling.saveChildrenWithDifferentOldNew
      oldToDtos
      newToDtos
      getId
      insert
      update
      delete
      existingSave


  let inline saveChildren
    (toDtos: 'rootEntity -> ^childDto list)
    (getId: ^childDto -> 'childDtoId)
    (_insertScriptCtor: unit -> ^insertScript)
    (_updateScriptCtor: unit -> ^updateScript)
    (_deleteScriptCtor: unit -> ^deleteScript)
    (existingSave: SqlConnection * SqlTransaction -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    =
    saveChildrenWithDifferentOldNew toDtos toDtos getId _insertScriptCtor _updateScriptCtor _deleteScriptCtor existingSave


  let inline saveChildrenWithoutUpdateWithDifferentOldNew< ^insertScript, ^insertScript_executable, ^updateScript, ^updateScript_executable, ^deleteScript, ^deleteScript_executable, ^childDto, 'childDtoId, 'rootEntity, 'insertResult, 'updateResult, 'deleteResult, 'saveResult when
                                                           ^insertScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript)
                                                           and ^insertScript : (member WithParameters: ^childDto -> ^insertScript_executable)
                                                           and ^insertScript_executable : (member AsyncExecute: unit -> Async<'insertResult>)
                                                           and ^deleteScript : (static member WithConnection: SqlConnection -> SqlTransaction option -> ^deleteScript)
                                                           and ^deleteScript : (member WithParameters: 'childDtoId -> ^deleteScript_executable)
                                                           and ^deleteScript_executable : (member AsyncExecute: unit -> Async<'deleteResult>)
                                                           and ^childDto : equality
                                                           and ^childDtoId : equality>
    (oldToDtos: 'rootEntity -> ^childDto list)
    (newToDtos: 'rootEntity -> ^childDto list)
    (getId: ^childDto -> 'childDtoId)
    (_insertScriptCtor: unit -> ^insertScript)
    (_deleteScriptCtor: unit -> ^deleteScript)
    (existingSave: SqlConnection * SqlTransaction -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    =

    let insert (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^insertScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^insertScript) (conn, Some tran))
        let executable = (^insertScript: (member WithParameters: ^childDto -> ^insertScript_executable) withConn, rootDto)
        do! (^insertScript_executable: (member AsyncExecute: unit -> Async<'insertResult>) executable) |> Async.Ignore<'insertResult>
      }

    let delete (conn: SqlConnection, tran: SqlTransaction) (childDtoId: 'childDtoId) : Async<unit> =
      async {
        let withConn = (^deleteScript: (static member WithConnection: SqlConnection -> SqlTransaction option -> ^deleteScript) (conn, Some tran))
        let executable = (^deleteScript: (member WithParameters: 'childDtoId -> ^deleteScript_executable) withConn, childDtoId)
        do! (^deleteScript_executable: (member AsyncExecute: unit -> Async<'deleteResult>) executable) |> Async.Ignore<'deleteResult>
      }

    Fling.saveChildrenWithoutUpdateWithDifferentOldNew
      oldToDtos
      newToDtos
      getId
      insert
      delete
      existingSave


  let inline saveChildrenWithoutUpdate
    (toDtos: 'rootEntity -> ^childDto list)
    (getId: ^childDto -> 'childDtoId)
    (_insertScriptCtor: unit -> ^insertScript)
    (_deleteScriptCtor: unit -> ^deleteScript)
    (existingSave: SqlConnection * SqlTransaction -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    =
    saveChildrenWithoutUpdateWithDifferentOldNew toDtos toDtos getId _insertScriptCtor _deleteScriptCtor existingSave


  let inline loadChild< ^script, ^script_executable, 'rootDto, 'rootDtoId, 'childDto, 'loadResult when
                        ^script : (static member WithConnection: string * (SqlConnection -> unit) option -> ^script)
                        and ^script : (member WithParameters: 'rootDtoId -> ^script_executable)
                        and ^script_executable : (member AsyncExecuteSingle: unit -> Async<'childDto option>)
                        and 'rootDtoId : equality>
    (_scriptCtor: unit -> ^script)
    (loader: Fling.Loader<'rootDto, 'rootDtoId, 'childDto -> 'loadResult, string>)
    =
    let loadChild (connString: string) (rootId: 'rootDtoId) : Async<'childDto> =
      async {
        let withConn = (^script: (static member WithConnection: string * (SqlConnection -> unit) option -> ^script) (connString, None))
        let executable = (^script: (member WithParameters: 'rootDtoId -> ^script_executable) withConn, rootId)
        let! res = (^script_executable: (member AsyncExecuteSingle: unit -> Async<'childDto option>) executable)
        return res |> Option.defaultWith (fun () -> failwith $"Query %s{typeof< ^script>.Name} returned no result for parameter %A{rootId}")
      }
    Fling.loadChild loadChild loader



  let inline loadOptChild< ^script, ^script_executable, 'rootDto, 'rootDtoId, 'childDto, 'loadResult when
                           ^script : (static member WithConnection: string * (SqlConnection -> unit) option -> ^script)
                           and ^script : (member WithParameters: 'rootDtoId -> ^script_executable)
                           and ^script_executable : (member AsyncExecuteSingle: unit -> Async<'childDto option>)
                           and 'rootDtoId : equality>
    (_scriptCtor: unit -> ^script)
    (loader: Fling.Loader<'rootDto, 'rootDtoId, 'childDto option -> 'loadResult, string>)
    =
    let loadChild (connString: string) (param: 'rootDtoId) : Async<'childDto option> =
      async {
        let withConn = (^script: (static member WithConnection: string * (SqlConnection -> unit) option -> ^script) (connString, None))
        let executable = (^script: (member WithParameters: 'rootDtoId -> ^script_executable) withConn, param)
        let! res = (^script_executable: (member AsyncExecuteSingle: unit -> Async<'childDto option>) executable)
        return res
      }
    Fling.loadChild loadChild loader



  let inline loadChildren< ^script, ^script_executable, 'rootDto, 'rootDtoId, 'childDto, 'loadResult when
                           ^script : (static member WithConnection: string * (SqlConnection -> unit) option -> ^script)
                           and ^script : (member WithParameters: 'rootDtoId -> ^script_executable)
                           and ^script_executable : (member AsyncExecute: unit -> Async<ResizeArray<'childDto>>)
                           and 'rootDtoId : equality>
    (_scriptCtor: unit -> ^script)
    (loader: Fling.Loader<'rootDto, 'rootDtoId, 'childDto list -> 'loadResult, string>)
    =
    let loadChild (connString: string) (param: 'rootDtoId) : Async<'childDto list> =
      async {
        let withConn = (^script: (static member WithConnection: string * (SqlConnection -> unit) option -> ^script) (connString, None))
        let executable = (^script: (member WithParameters: 'rootDtoId -> ^script_executable) withConn, param)
        let! res = (^script_executable: (member AsyncExecute: unit -> Async<ResizeArray<'childDto>>) executable)
        return Seq.toList res
      }
    Fling.loadChild loadChild loader



  let inline batchLoadChild< ^script, ^script_executable, ^tableType, 'rootDto, 'rootDtoId, 'childDto, 'loadResult when
                             ^script : (static member WithConnection: string * (SqlConnection -> unit) option -> ^script)
                             and ^script : (member WithParameters: seq< ^tableType> -> ^script_executable)
                             and ^tableType : (static member create: ^rootDtoId -> ^tableType)
                             and ^script_executable : (member AsyncExecute: unit -> Async<ResizeArray<'childDto>>)
                             and 'rootDtoId : equality>
    (_scriptCtor: unit -> ^script)
    (getRootId: 'childDto -> 'rootDtoId)
    (loader: Fling.BatchLoader<'rootDto, 'rootDtoId, 'childDto -> 'loadResult, string>)
    =
    let loadChild (connString: string) (rootIds: 'rootDtoId list) : Async<'childDto list> =
      async {
        let withConn = (^script: (static member WithConnection: string * (SqlConnection -> unit) option -> ^script) (connString, None))
        let tableTypeParams = rootIds |> List.map (fun param -> (^tableType: (static member create: ^rootDtoId -> ^tableType) param))
        let executable = (^script: (member WithParameters: seq< ^tableType> -> ^script_executable) withConn, tableTypeParams)
        let! res = (^script_executable: (member AsyncExecute: unit -> Async<ResizeArray<'childDto>>) executable)
        return Seq.toList res
      }
    Fling.batchLoadChild loadChild getRootId loader



  let inline batchLoadOptChild< ^script, ^script_executable, ^tableType, 'rootDto, 'rootDtoId, 'childDto, 'loadResult when
                                ^script : (static member WithConnection: string * (SqlConnection -> unit) option -> ^script)
                                and ^script : (member WithParameters: seq< ^tableType> -> ^script_executable)
                                and ^tableType : (static member create: ^rootDtoId -> ^tableType)
                                and ^script_executable : (member AsyncExecute: unit -> Async<ResizeArray<'childDto>>)
                                and 'rootDtoId : equality>
    (_scriptCtor: unit -> ^script)
    (getRootId: 'childDto -> 'rootDtoId)
    (loader: Fling.BatchLoader<'rootDto, 'rootDtoId, 'childDto option -> 'loadResult, string>)
    =
    let loadChild (connString: string) (rootIds: 'rootDtoId list) : Async<'childDto list> =
      async {
        let withConn = (^script: (static member WithConnection: string * (SqlConnection -> unit) option -> ^script) (connString, None))
        let tableTypeParams = rootIds |> List.map (fun param -> (^tableType: (static member create: ^rootDtoId -> ^tableType) param))
        let executable = (^script: (member WithParameters: seq< ^tableType> -> ^script_executable) withConn, tableTypeParams)
        let! res = (^script_executable: (member AsyncExecute: unit -> Async<ResizeArray<'childDto>>) executable)
        return Seq.toList res
      }
    Fling.batchLoadOptChild loadChild getRootId loader



  let inline batchLoadChildren< ^script, ^script_executable, ^tableType, 'rootDto, 'rootDtoId, 'childDto, 'loadResult when
                                ^script : (static member WithConnection: string * (SqlConnection -> unit) option -> ^script)
                                and ^script : (member WithParameters: seq< ^tableType> -> ^script_executable)
                                and ^tableType : (static member create: ^rootDtoId -> ^tableType)
                                and ^script_executable : (member AsyncExecute: unit -> Async<ResizeArray<'childDto>>)
                                and 'rootDtoId : equality>
    (_scriptCtor: unit -> ^script)
    (getRootId: 'childDto -> 'rootDtoId)
    (loader: Fling.BatchLoader<'rootDto, 'rootDtoId, 'childDto list -> 'loadResult, string>)
    =
    let loadChild (connString: string) (rootIds: 'rootDtoId list) : Async<'childDto list> =
      async {
        let withConn = (^script: (static member WithConnection: string * (SqlConnection -> unit) option -> ^script) (connString, None))
        let tableTypeParams = rootIds |> List.map (fun param -> (^tableType: (static member create: ^rootDtoId -> ^tableType) param))
        let executable = (^script: (member WithParameters: seq< ^tableType> -> ^script_executable) withConn, tableTypeParams)
        let! res = (^script_executable: (member AsyncExecute: unit -> Async<ResizeArray<'childDto>>) executable)
        return Seq.toList res
      }
    Fling.batchLoadChildren loadChild getRootId loader
