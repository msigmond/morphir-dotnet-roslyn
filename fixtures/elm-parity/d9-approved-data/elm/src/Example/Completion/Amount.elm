module Example.Completion.Amount exposing (..)
import Example.Completion exposing (Amount)
new0NoneInt : Int -> Amount
new0NoneInt value = { value = value }
opAddition0NoneGlobalExampleCompletionAmountNoneGlobalExampleCompletionAmount : Amount -> Amount -> Amount
opAddition0NoneGlobalExampleCompletionAmountNoneGlobalExampleCompletionAmount a b = new0NoneInt (a.value + b.value)
