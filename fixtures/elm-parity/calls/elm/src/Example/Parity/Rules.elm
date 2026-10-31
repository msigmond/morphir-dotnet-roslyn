module Example.Parity.Rules exposing (..)


double : Int -> Int
double x = x * 2
call : Int -> Int
call x = double x
optional : Int -> Int
optional x = x
explicit : Int
explicit = optional 4
recursive : Int -> Int
recursive x = recursive x
