module Example.Completion.Cell exposing (..)
import Example.Completion exposing (Cell)
add : Cell -> Int -> Int
add this x = this.value + x
read : Cell -> Int
read this = this.value
