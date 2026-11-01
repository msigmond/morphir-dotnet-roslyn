module Example.Probe exposing (..)
import Array exposing (Array)
read : Int -> Array Int -> Maybe Int
read index values = Array.get index values
