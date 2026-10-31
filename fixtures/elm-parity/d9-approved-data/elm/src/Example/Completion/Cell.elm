module Example.Completion.Cell exposing (..)
import Example.Completion exposing (Cell)
new0NoneInt : Int -> Cell
new0NoneInt value = { value = value }
getDouble0 : Cell -> Int
getDouble0 this = this.value * 2
read : Cell -> Int
read this = getDouble0 this
