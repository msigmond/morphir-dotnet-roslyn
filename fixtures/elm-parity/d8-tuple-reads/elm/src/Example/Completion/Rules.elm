module Example.Completion.Rules exposing (..)
first : (Int,Bool) -> Int
first pair = Tuple.first pair
second : (Int,Bool) -> Bool
second pair = Tuple.second pair
deconstruct : (Int,Bool) -> Int
deconstruct pair =
    let
        (code,flag) = pair
    in code
discard : (Int,Bool) -> Int
discard pair =
    let
        (code,_) = pair
    in code

triple : (Int,Bool,Int) -> Int
triple pair =
    let
        (a,_,b) = pair
    in a+b
nested : (Int,(Bool,Int)) -> Int
nested pair =
    let
        (a,(_,b)) = pair
    in a+b
