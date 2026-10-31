module Example.CurrentSupported.ClassicValue exposing (..)
import Example.CurrentSupported exposing (ClassicValue)
new0NoneInt : Int -> ClassicValue
new0NoneInt value = { value = value }
read : ClassicValue -> Int
read this = this.value
