module Example.Parity.Rules exposing (create, f1NoneT, f2NoneTNoneU, one, read, two, use)
import Example.Parity.Container as Container exposing (Nested)
use : Int -> Int
use value = Container.identity value
create : Int -> String -> Nested Int String
create first second = Nested first second
read : Nested Int String -> String
read pair = pair.second
f1NoneT : t -> t
f1NoneT value = value
f2NoneTNoneU : t -> u -> t
f2NoneTNoneU value unused = value
one : Int -> Int
one value = f1NoneT value
two : Int -> Int
two value = f2NoneTNoneU value True
hidden : t -> t
hidden value = value
