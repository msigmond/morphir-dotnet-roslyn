module Example.Parity.Cell exposing (..)
import Example.Parity exposing (Cell)
new0NoneT : t -> Cell t
new0NoneT value = { value = value }
read : Cell t -> t
read this = this.value
