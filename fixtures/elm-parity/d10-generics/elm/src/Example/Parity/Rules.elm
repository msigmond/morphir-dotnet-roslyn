module Example.Parity.Rules exposing (..)

import Example.Parity exposing (..)

identity : t -> t
identity value = value
selectFirst : a -> b -> a
selectFirst first second = first
wrap : t -> Box t
wrap value = Box value
unwrap : Box t -> t
unwrap box = box.value
explicit : Int -> Int
explicit value = identity value
inferred : String -> String
inferred value = identity value
intBox : Int -> Box Int
intBox value = wrap value
readBox : Box Int -> Int
readBox box = unwrap box
makePair : Int -> String -> Pair Int String
makePair first second = Pair first second
readSecond : Pair Int String -> String
readSecond pair = pair.second

asFunction : Int -> Int
asFunction = identity
apply : (t -> u) -> t -> u
apply callback value = callback value
optional : t -> Maybe t
optional value = Just value

optionalBox : t -> Box (Maybe t)
optionalBox value = Box (Just value)
