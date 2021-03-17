namespace Fling.Interop.Facil

open Microsoft.Data.SqlClient
open Fling



module Fling =


  let withTransactionFromConnStr f connStr oldEntity newEntity =
    async {
      let! ct = Async.CancellationToken
      use conn = new SqlConnection(connStr)
      do! conn.OpenAsync(ct) |> Async.AwaitTask
      use tran = conn.BeginTransaction ()
      let! res = f (conn, tran) oldEntity newEntity
      do! tran.CommitAsync (ct) |> Async.AwaitTask
      return res
    }


  let inline saveRoot< ^insertScript, ^insertScript_executable, ^updateScript, ^updateScript_executable, ^rootDto, 'rootEntity, 'insertResult, 'updateResult when
                       ^insertScript : (static member WithConnection: SqlConnection -> ^insertScript)
                       and ^insertScript : (member ConfigureCommand: (SqlCommand -> unit) -> ^insertScript)
                       and ^insertScript : (member WithParameters: ^rootDto -> ^insertScript_executable)
                       and ^insertScript_executable : (member AsyncExecute: unit -> Async<'insertResult>)
                       and ^updateScript : (static member WithConnection: SqlConnection -> ^updateScript)
                       and ^updateScript : (member ConfigureCommand: (SqlCommand -> unit) -> ^updateScript)
                       and ^updateScript : (member WithParameters: ^rootDto -> ^updateScript_executable)
                       and ^updateScript_executable : (member AsyncExecute: unit -> Async<'updateResult>)
                       and ^rootDto : equality>
    (_insertScriptCtor: unit -> ^insertScript)
    (_updateScriptCtor: unit -> ^updateScript)
    (toDto: 'rootEntity -> 'rootDto) =

    let insert (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^rootDto) : Async<unit> =
      async {
        let withConn = (^insertScript: (static member WithConnection: SqlConnection -> ^insertScript) conn)
        let withConfiguredCmd = (^insertScript: (member ConfigureCommand: (SqlCommand -> unit) -> ^insertScript) withConn, (fun (cmd: SqlCommand) -> cmd.Transaction <- tran))
        let exexutable = (^insertScript: (member WithParameters: ^rootDto -> ^insertScript_executable) withConfiguredCmd, rootDto)
        do! (^insertScript_executable: (member AsyncExecute: unit -> Async<'insertResult>) exexutable) |> Async.Ignore<'insertResult>
      }

    let update (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^rootDto) : Async<unit> =
      async {
        let withConn = (^updateScript: (static member WithConnection: SqlConnection -> ^updateScript) conn)
        let withConfiguredCmd = (^updateScript: (member ConfigureCommand: (SqlCommand -> unit) -> ^updateScript) withConn, (fun (cmd: SqlCommand) -> cmd.Transaction <- tran))
        let exexutable = (^updateScript: (member WithParameters: ^rootDto -> ^updateScript_executable) withConfiguredCmd, rootDto)
        do! (^updateScript_executable: (member AsyncExecute: unit -> Async<'updateResult>) exexutable) |> Async.Ignore<'updateResult>
      }

    Fling.saveRoot
      toDto
      insert
      update


  let inline saveRootWithOutput< ^insertScript, ^insertScript_executable, ^updateScript, ^updateScript_executable, ^rootDto, 'rootEntity, 'result when
                                 ^insertScript : (static member WithConnection: SqlConnection -> ^insertScript)
                                 and ^insertScript : (member ConfigureCommand: (SqlCommand -> unit) -> ^insertScript)
                                 and ^insertScript : (member WithParameters: ^rootDto -> ^insertScript_executable)
                                 and ^insertScript_executable : (member AsyncExecute: unit -> Async<'result>)
                                 and ^updateScript : (static member WithConnection: SqlConnection -> ^updateScript)
                                 and ^updateScript : (member ConfigureCommand: (SqlCommand -> unit) -> ^updateScript)
                                 and ^updateScript : (member WithParameters: ^rootDto -> ^updateScript_executable)
                                 and ^updateScript_executable : (member AsyncExecute: unit -> Async<'result>)
                                 and ^rootDto : equality>
    (_insertScriptCtor: unit -> ^insertScript)
    (_updateScriptCtor: unit -> ^updateScript)
    (toDto: 'rootEntity -> 'rootDto) =

    let insert (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^rootDto) : Async<'result> =
      async {
        let withConn = (^insertScript: (static member WithConnection: SqlConnection -> ^insertScript) conn)
        let withConfiguredCmd = (^insertScript: (member ConfigureCommand: (SqlCommand -> unit) -> ^insertScript) withConn, (fun (cmd: SqlCommand) -> cmd.Transaction <- tran))
        let executable = (^insertScript: (member WithParameters: ^rootDto -> ^insertScript_executable) withConfiguredCmd, rootDto)
        return! (^insertScript_executable: (member AsyncExecute: unit -> Async<'result>) executable)
      }

    let update (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^rootDto) : Async<'result> =
      async {
        let withConn = (^updateScript: (static member WithConnection: SqlConnection -> ^updateScript) conn)
        let withConfiguredCmd = (^updateScript: (member ConfigureCommand: (SqlCommand -> unit) -> ^updateScript) withConn, (fun (cmd: SqlCommand) -> cmd.Transaction <- tran))
        let executable = (^updateScript: (member WithParameters: ^rootDto -> ^updateScript_executable) withConfiguredCmd, rootDto)
        return! (^updateScript_executable: (member AsyncExecute: unit -> Async<'result>) executable)
      }

    Fling.saveRootWithOutput
      toDto
      insert
      update


  let inline saveChild< ^insertScript, ^insertScript_executable, ^updateScript, ^updateScript_executable, ^childDto, 'rootEntity, 'insertResult, 'updateResult, 'saveResult when
                        ^insertScript : (static member WithConnection: SqlConnection -> ^insertScript)
                        and ^insertScript : (member ConfigureCommand: (SqlCommand -> unit) -> ^insertScript)
                        and ^insertScript : (member WithParameters: ^childDto -> ^insertScript_executable)
                        and ^insertScript_executable : (member AsyncExecute: unit -> Async<'insertResult>)
                        and ^updateScript : (static member WithConnection: SqlConnection -> ^updateScript)
                        and ^updateScript : (member ConfigureCommand: (SqlCommand -> unit) -> ^updateScript)
                        and ^updateScript : (member WithParameters: ^childDto -> ^updateScript_executable)
                        and ^updateScript_executable : (member AsyncExecute: unit -> Async<'updateResult>)
                        and ^childDto : equality>
    (_insertScriptCtor: unit -> ^insertScript)
    (_updateScriptCtor: unit -> ^updateScript)
    (toDto: 'rootEntity -> ^childDto)
    (existingSave: (SqlConnection * SqlTransaction) -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>) =

    let insert (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^insertScript: (static member WithConnection: SqlConnection -> ^insertScript) conn)
        let withConfiguredCmd = (^insertScript: (member ConfigureCommand: (SqlCommand -> unit) -> ^insertScript) withConn, (fun (cmd: SqlCommand) -> cmd.Transaction <- tran))
        let exexutable = (^insertScript: (member WithParameters: ^childDto -> ^insertScript_executable) withConfiguredCmd, rootDto)
        do! (^insertScript_executable: (member AsyncExecute: unit -> Async<'insertResult>) exexutable) |> Async.Ignore<'insertResult>
      }

    let update (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^updateScript: (static member WithConnection: SqlConnection -> ^updateScript) conn)
        let withConfiguredCmd = (^updateScript: (member ConfigureCommand: (SqlCommand -> unit) -> ^updateScript) withConn, (fun (cmd: SqlCommand) -> cmd.Transaction <- tran))
        let exexutable = (^updateScript: (member WithParameters: ^childDto -> ^updateScript_executable) withConfiguredCmd, rootDto)
        do! (^updateScript_executable: (member AsyncExecute: unit -> Async<'updateResult>) exexutable) |> Async.Ignore<'updateResult>
      }

    Fling.saveChild
      toDto
      insert
      update
      existingSave


  let inline saveOptChild< ^insertScript, ^insertScript_executable, ^updateScript, ^updateScript_executable, ^deleteScript, ^deleteScript_executable, ^childDto, 'childDtoId, 'rootEntity, 'insertResult, 'updateResult, 'deleteResult, 'saveResult when
                           ^insertScript : (static member WithConnection: SqlConnection -> ^insertScript)
                           and ^insertScript : (member ConfigureCommand: (SqlCommand -> unit) -> ^insertScript)
                           and ^insertScript : (member WithParameters: ^childDto -> ^insertScript_executable)
                           and ^insertScript_executable : (member AsyncExecute: unit -> Async<'insertResult>)
                           and ^updateScript : (static member WithConnection: SqlConnection -> ^updateScript)
                           and ^updateScript : (member ConfigureCommand: (SqlCommand -> unit) -> ^updateScript)
                           and ^updateScript : (member WithParameters: ^childDto -> ^updateScript_executable)
                           and ^updateScript_executable : (member AsyncExecute: unit -> Async<'updateResult>)
                           and ^deleteScript : (static member WithConnection: SqlConnection -> ^deleteScript)
                           and ^deleteScript : (member ConfigureCommand: (SqlCommand -> unit) -> ^deleteScript)
                           and ^deleteScript : (member WithParameters: 'childDtoId -> ^deleteScript_executable)
                           and ^deleteScript_executable : (member AsyncExecute: unit -> Async<'deleteResult>)
                           and ^childDto : equality
                           and ^childDtoId : equality>
    (_insertScriptCtor: unit -> ^insertScript)
    (_updateScriptCtor: unit -> ^updateScript)
    (_deleteScriptCtor: unit -> ^deleteScript)
    (toDto: 'rootEntity -> ^childDto option)
    (getId: ^childDto -> 'childDtoId)
    (existingSave: (SqlConnection * SqlTransaction) -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    =

    let insert (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^insertScript: (static member WithConnection: SqlConnection -> ^insertScript) conn)
        let withConfiguredCmd = (^insertScript: (member ConfigureCommand: (SqlCommand -> unit) -> ^insertScript) withConn, (fun (cmd: SqlCommand) -> cmd.Transaction <- tran))
        let exexutable = (^insertScript: (member WithParameters: ^childDto -> ^insertScript_executable) withConfiguredCmd, rootDto)
        do! (^insertScript_executable: (member AsyncExecute: unit -> Async<'insertResult>) exexutable) |> Async.Ignore<'insertResult>
      }

    let update (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^updateScript: (static member WithConnection: SqlConnection -> ^updateScript) conn)
        let withConfiguredCmd = (^updateScript: (member ConfigureCommand: (SqlCommand -> unit) -> ^updateScript) withConn, (fun (cmd: SqlCommand) -> cmd.Transaction <- tran))
        let exexutable = (^updateScript: (member WithParameters: ^childDto -> ^updateScript_executable) withConfiguredCmd, rootDto)
        do! (^updateScript_executable: (member AsyncExecute: unit -> Async<'updateResult>) exexutable) |> Async.Ignore<'updateResult>
      }

    let delete (conn: SqlConnection, tran: SqlTransaction) (childDtoId: 'childDtoId) : Async<unit> =
      async {
        let withConn = (^deleteScript: (static member WithConnection: SqlConnection -> ^deleteScript) conn)
        let withConfiguredCmd = (^deleteScript: (member ConfigureCommand: (SqlCommand -> unit) -> ^deleteScript) withConn, (fun (cmd: SqlCommand) -> cmd.Transaction <- tran))
        let exexutable = (^deleteScript: (member WithParameters: 'childDtoId -> ^deleteScript_executable) withConfiguredCmd, childDtoId)
        do! (^deleteScript_executable: (member AsyncExecute: unit -> Async<'deleteResult>) exexutable) |> Async.Ignore<'deleteResult>
      }

    Fling.saveOptChild
      toDto
      getId
      insert
      update
      delete
      existingSave


  let inline saveChildren< ^insertScript, ^insertScript_executable, ^updateScript, ^updateScript_executable, ^deleteScript, ^deleteScript_executable, ^childDto, 'childDtoId, 'rootEntity, 'insertResult, 'updateResult, 'deleteResult, 'saveResult when
                           ^insertScript : (static member WithConnection: SqlConnection -> ^insertScript)
                           and ^insertScript : (member ConfigureCommand: (SqlCommand -> unit) -> ^insertScript)
                           and ^insertScript : (member WithParameters: ^childDto -> ^insertScript_executable)
                           and ^insertScript_executable : (member AsyncExecute: unit -> Async<'insertResult>)
                           and ^updateScript : (static member WithConnection: SqlConnection -> ^updateScript)
                           and ^updateScript : (member ConfigureCommand: (SqlCommand -> unit) -> ^updateScript)
                           and ^updateScript : (member WithParameters: ^childDto -> ^updateScript_executable)
                           and ^updateScript_executable : (member AsyncExecute: unit -> Async<'updateResult>)
                           and ^deleteScript : (static member WithConnection: SqlConnection -> ^deleteScript)
                           and ^deleteScript : (member ConfigureCommand: (SqlCommand -> unit) -> ^deleteScript)
                           and ^deleteScript : (member WithParameters: 'childDtoId -> ^deleteScript_executable)
                           and ^deleteScript_executable : (member AsyncExecute: unit -> Async<'deleteResult>)
                           and ^childDto : equality
                           and ^childDtoId : equality>
    (_insertScriptCtor: unit -> ^insertScript)
    (_updateScriptCtor: unit -> ^updateScript)
    (_deleteScriptCtor: unit -> ^deleteScript)
    (toDtos: 'rootEntity -> ^childDto list)
    (getId: ^childDto -> 'childDtoId)
    (existingSave: (SqlConnection * SqlTransaction) -> 'rootEntity option -> 'rootEntity -> Async<'saveResult>)
    =

    let insert (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^insertScript: (static member WithConnection: SqlConnection -> ^insertScript) conn)
        let withConfiguredCmd = (^insertScript: (member ConfigureCommand: (SqlCommand -> unit) -> ^insertScript) withConn, (fun (cmd: SqlCommand) -> cmd.Transaction <- tran))
        let exexutable = (^insertScript: (member WithParameters: ^childDto -> ^insertScript_executable) withConfiguredCmd, rootDto)
        do! (^insertScript_executable: (member AsyncExecute: unit -> Async<'insertResult>) exexutable) |> Async.Ignore<'insertResult>
      }

    let update (conn: SqlConnection, tran: SqlTransaction) (rootDto: ^childDto) : Async<unit> =
      async {
        let withConn = (^updateScript: (static member WithConnection: SqlConnection -> ^updateScript) conn)
        let withConfiguredCmd = (^updateScript: (member ConfigureCommand: (SqlCommand -> unit) -> ^updateScript) withConn, (fun (cmd: SqlCommand) -> cmd.Transaction <- tran))
        let exexutable = (^updateScript: (member WithParameters: ^childDto -> ^updateScript_executable) withConfiguredCmd, rootDto)
        do! (^updateScript_executable: (member AsyncExecute: unit -> Async<'updateResult>) exexutable) |> Async.Ignore<'updateResult>
      }

    let delete (conn: SqlConnection, tran: SqlTransaction) (childDtoId: 'childDtoId) : Async<unit> =
      async {
        let withConn = (^deleteScript: (static member WithConnection: SqlConnection -> ^deleteScript) conn)
        let withConfiguredCmd = (^deleteScript: (member ConfigureCommand: (SqlCommand -> unit) -> ^deleteScript) withConn, (fun (cmd: SqlCommand) -> cmd.Transaction <- tran))
        let exexutable = (^deleteScript: (member WithParameters: 'childDtoId -> ^deleteScript_executable) withConfiguredCmd, childDtoId)
        do! (^deleteScript_executable: (member AsyncExecute: unit -> Async<'deleteResult>) exexutable) |> Async.Ignore<'deleteResult>
      }

    Fling.saveChildren
      toDtos
      getId
      insert
      update
      delete
      existingSave


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
