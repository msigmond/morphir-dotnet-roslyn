module Example.Parity.Rules exposing (f)
import Example.Parity exposing (Cell)
import Example.Parity.Cell as Cell
f : Cell -> Int
f c = Cell.read c
