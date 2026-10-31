module Example.Completion.Rules exposing (..)
import Example.Completion exposing (State)
import Example.Completion.State as State
identity : State -> State
identity value = value
get : State
get = State.active
code : State -> Int
code value = value
from : Int -> State
from value = value
equal : State -> State -> Bool
equal a b = a == b
