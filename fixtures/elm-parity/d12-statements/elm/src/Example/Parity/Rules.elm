module Example.Parity.Rules exposing (..)
choose : Int -> Int
choose value = case value of
    0 -> 1
    _ -> 2
defaultFirst : Int -> Int
defaultFirst value = case value of
    _ -> 0
    1 -> 1
local : Int -> Int
local value = case value of
    0 ->
        let
            result : Int
            result = value + 1
        in result
    _ -> value
bind : Int -> Int
bind value = case value of
    result -> result
nullable : Maybe Int -> Int
nullable value = case value of
    Nothing -> 0
    Just 1 -> 1
    _ -> 2
