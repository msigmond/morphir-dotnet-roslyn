module Example.Completion.Rules exposing (..)
empty : ()
empty = ()
return : ()
return = ()
emptyReference : ()
emptyReference = empty
invoke : () -> ()
invoke callback = callback
forward : Int -> Int
forward x =
    let
        local : Int -> Int
        local y = y + 1
    in local x
mutual : Int -> Bool
mutual n =
    let
        even : Int -> Bool
        even x = x == 0 || odd (x - 1)
        odd : Int -> Bool
        odd x = x /= 0 && even (x - 1)
    in even n
chain : Int -> Int
chain n =
    let
        first : Int -> Int
        first x = second x + 1
        second : Int -> Int
        second x = x * 2
    in first n
independent : Int -> Int
independent n =
    let
        first : Int -> Int
        first x = x + 1
        second : Int -> Int
        second x = x * 2
    in first (second n)

interleaved : Int -> Int
interleaved n =
    let
        first : Int -> Int
        first x = second x + 1
        offset : Int
        offset = 2
        second : Int -> Int
        second x = x + offset
    in first n

keep : List Int -> Int
keep callbacks = 1
listRecursion : Int
listRecursion =
    let
        loop : Int
        loop = keep [loop]
    in loop

localParams : Int
localParams =
    let
        size : List Int -> Int
        size items = List.length items
    in size [1,2]
