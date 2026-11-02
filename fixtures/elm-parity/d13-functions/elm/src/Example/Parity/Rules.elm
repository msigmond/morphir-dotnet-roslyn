module Example.Parity.Rules exposing (..)
increment : Int -> Int
increment = \value -> value + 1
subtract : Int -> Int -> Int
subtract = \left right -> left - right
explicit : Int -> Int
explicit = \value -> value * 2
anonymous : Int -> Int
anonymous = \value -> value + 2
construct : Int -> Int
construct = \value -> value + 3
apply : (Int -> Int) -> Int -> Int
apply callback value = callback value
invoke : (Int -> Int) -> Int -> Int
invoke callback value = callback value
identity : t -> t
identity = \value -> value
nullable : Int -> Maybe Int
nullable = \value -> Just value
empty : Int -> ()
empty = \value -> ()
zero : Int -> Int
zero value = value
emptyZero : ()
emptyZero = ()
static : Int -> Int
static = \value -> value + 1
run : (Int -> ()) -> Int -> ()
run callback value = callback value
forward : (Int -> ()) -> Int -> ()
forward callback value = callback value
copy : (Int -> Int) -> Int -> Int
copy callback = callback
parenthesized : Int -> Int
parenthesized = \value -> value + 1
inferred : Int -> Int
inferred = \value -> value + 1
