namespace Fling

[<AutoOpen>]
module internal Utils =


    module Async =


        let map f comp =
            async {
                let! x = comp
                return f x
            }


        let bind f comp =
            async {
                let! x = comp
                return! f x
            }


    module Option =


        let traverseAsync f opt =
            async {
                match opt with
                | None -> return None
                | Some x -> return! f x |> Async.map Some
            }
