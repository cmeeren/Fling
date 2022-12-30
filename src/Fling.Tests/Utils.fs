[<AutoOpen>]
module Utils



module List =


    let isDistinctBy f xs =
        xs |> List.distinctBy f |> List.length = xs.Length
