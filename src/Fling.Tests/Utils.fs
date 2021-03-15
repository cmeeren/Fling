[<AutoOpen>]
module Utils



module List =


  let isDistinctBy f xs =
    xs |> List.distinctBy f |> List.length = xs.Length



module Map =


  let keys map =
    map
    |> Map.toSeq
    |> Seq.map fst
    |> set
