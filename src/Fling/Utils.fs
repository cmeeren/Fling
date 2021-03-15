namespace Fling

[<AutoOpen>]
module internal Utils =


  module Async =


    let map f asnc =
      async {
        let! x = asnc
        return f x
      }


    let bind f asnc =
      async {
        let! x = asnc
        return! f x
      }


  module Option =


    let traverseAsync f opt =
      async {
        match opt with
        | None -> return None
        | Some x -> return! f x |> Async.map Some
      }
