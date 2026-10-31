module Example.Completion.Rules exposing (..)
import Example.Completion exposing (Cell)
import Example.Completion.Cell as Cell
bind : Cell -> (Int -> Int)
bind cell = Cell.add cell
zero : Cell -> Int
zero cell = Cell.read cell
call : Cell -> Int -> Int
call cell value = Cell.add cell value
apply : Cell -> Int -> Int
apply cell value =
    let
        callback : Int -> Int
        callback = Cell.add cell
    in callback value
