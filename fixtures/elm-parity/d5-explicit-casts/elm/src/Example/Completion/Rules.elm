module Example.Completion.Rules exposing (..)
import Morphir.SDK.Decimal as Decimal exposing (Decimal)
widen : Int -> Int
widen value = value
narrow : Int -> Int
narrow value = value
float : Int -> Float
float value = toFloat value
integer : Float -> Int
integer value = truncate value
decimal : Int -> Decimal
decimal value = Decimal.fromInt value
floatDecimal : Float -> Decimal
floatDecimal value = Decimal.fromFloat value
decimalFloat : Decimal -> Float
decimalFloat value = Decimal.toFloat value
