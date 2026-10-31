module Example.Completion.Rules exposing (..)
import Example.Completion exposing (Data)
equal : Data -> Data -> Bool
equal a b = a == b
different : Data -> Data -> Bool
different a b = a /= b
tupleEqual : (Int,Bool) -> (Int,Bool) -> Bool
tupleEqual a b = a == b
update : Data -> Int -> Data
update data value = { data | x = value }
multiple : Data -> Int -> Int -> Data
multiple data x y = { data | y = y, x = x }
