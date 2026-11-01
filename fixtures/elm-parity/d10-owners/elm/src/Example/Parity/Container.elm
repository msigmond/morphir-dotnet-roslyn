module Example.Parity.Container exposing (..)
type alias Nested t u = { first : t, second : u }
identity : t -> t
identity value = value
