module Example.Parity.Rules exposing (..)
use : Int -> Int
use value =
    let
        echo : t -> t
        echo item = item
    in
    echo value
forward : t -> t
forward value =
    let
        echo : u -> u
        echo item = item
    in
    echo value

through : Int -> Int
through value =
    let
        echo : u -> u
        echo item = item
        forward : v -> v
        forward item = echo item
    in
    forward value
