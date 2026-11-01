module Example.Probe exposing (..)
read : Int -> Int
read value = case value of
    (-1) -> 1
    _ -> 0
