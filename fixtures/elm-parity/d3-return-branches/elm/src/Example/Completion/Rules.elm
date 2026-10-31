module Example.Completion.Rules exposing (..)
explicit : Bool -> Int -> Int -> Int
explicit b a c = if b then a else c
early : Bool -> Int -> Int -> Int
early b a c = if b then a else c
nested : Bool -> Bool -> Int
nested a b = if a then (if b then 1 else 2) else 3
locals : Bool -> Int -> Int
locals b a =
    if b then
        let
            x : Int
            x = a + 1
        in x
    else
        let
            x : Int
            x = a * 2
        in x
constant : Int
constant = if False then 2 else 1 + 3
checked : Int -> Int
checked a = a + 1
