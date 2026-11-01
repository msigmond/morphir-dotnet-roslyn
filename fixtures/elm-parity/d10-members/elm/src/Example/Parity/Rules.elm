module Example.Parity.Rules exposing (..)
import Example.Parity exposing (..)
import Example.Parity.Box as Box
import Example.Parity.Cell as Cell
import Example.Parity.Crate as Crate
read : Box Int -> Int
read box = Box.read box
current : Box String -> String
current box = Box.getCurrent0 box
createCell : Int -> Cell Int
createCell value = Cell.new0NoneT value
readCell : Cell Int -> Int
readCell cell = Cell.read cell
createCrate : String -> Crate String
createCrate value = Crate.new0NoneT value
replace : Box t -> t -> Box t
replace box value = { box | value = value }

createNullableCrate : Int -> Crate (Maybe Int)
createNullableCrate value = Crate.new0NoneT (Just value)
