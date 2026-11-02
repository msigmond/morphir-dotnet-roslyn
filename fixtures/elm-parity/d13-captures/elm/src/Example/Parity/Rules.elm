module Example.Parity.Rules exposing (..)
atLeast : Int -> Int -> Bool
atLeast minimum = \age -> age >= minimum
nested : Int -> Int -> Int -> Int
nested offset = \left -> \right -> left + right + offset
local : Int -> Int -> Int
local value offset =
    let
        add : Int -> Int
        add = \item -> item + offset
    in
    add value
block : Int -> Int -> Int
block offset = \value ->
    let
        result : Int
        result = value + offset
    in
    result * 2
pass : Int -> Int
pass value = use (\item -> item + 1) value
use : (Int -> Int) -> Int -> Int
use callback value = callback value
localFunction : Int -> Int -> Int
localFunction offset =
    let
        add : Int -> Int
        add = \item -> item + offset
    in
    \value -> add value
branch : Int -> Int -> Int
branch minimum = \value -> if value > minimum then value else minimum
natural : Int -> Int
natural value =
    let
        add : Int -> Int
        add = \item -> item + 1
    in
    add value
choice : Bool -> (Int -> Int)
choice flag = if flag then (\value -> value + 1) else (\value -> value + 2)
