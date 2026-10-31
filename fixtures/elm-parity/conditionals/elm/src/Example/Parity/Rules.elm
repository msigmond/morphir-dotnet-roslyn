module Example.Parity.Rules exposing (..)


choose : Bool -> Int -> Int -> Int
choose condition a b = if condition then a else b
constant : Int -> Int -> Int
constant a b = if True then a else b
