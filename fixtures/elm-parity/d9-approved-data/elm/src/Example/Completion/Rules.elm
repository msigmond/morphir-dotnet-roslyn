module Example.Completion.Rules exposing (..)
import Example.Completion exposing (Primary,Cell,Amount)
import Example.Completion.Primary as Primary
import Example.Completion.Cell as Cell
import Example.Completion.Amount as Amount
make : Int -> Primary
make x = Primary.new0NoneInt x
build : Int -> Cell
build x = Cell.new0NoneInt x
get : Cell -> Int
get cell = Cell.getDouble0 cell
add : Amount -> Amount -> Amount
add a b = Amount.opAddition0NoneGlobalExampleCompletionAmountNoneGlobalExampleCompletionAmount a b
