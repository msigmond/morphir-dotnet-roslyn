module Example.Completion.Rules exposing (..)
import Morphir.SDK.Decimal as Decimal exposing (Decimal)
fromInt : Int -> Decimal
fromInt value = Decimal.fromInt value
fromFloat : Float -> Decimal
fromFloat value = Decimal.fromFloat value
round : Decimal -> Decimal
round value = Decimal.round value
truncate : Decimal -> Decimal
truncate value = Decimal.truncate value
abs : Decimal -> Decimal
abs value = Decimal.abs value
text : Decimal -> String
text value = Decimal.toString value
equal : Decimal -> Decimal -> Bool
equal a b = Decimal.eq a b
different : Decimal -> Decimal -> Bool
different a b = Decimal.neq a b
less : Decimal -> Decimal -> Bool
less a b = Decimal.lt a b
lessEqual : Decimal -> Decimal -> Bool
lessEqual a b = Decimal.lte a b
greater : Decimal -> Decimal -> Bool
greater a b = Decimal.gt a b
greaterEqual : Decimal -> Decimal -> Bool
greaterEqual a b = Decimal.gte a b

add : Decimal -> Decimal -> Decimal
add a b = Decimal.add a b
subtract : Decimal -> Decimal -> Decimal
subtract a b = Decimal.sub a b
multiply : Decimal -> Decimal -> Decimal
multiply a b = Decimal.mul a b
negate : Decimal -> Decimal
negate x = Decimal.negate x
staticAbs : Decimal -> Decimal
staticAbs x = Decimal.abs x
staticEquals : Decimal -> Decimal -> Bool
staticEquals a b = Decimal.eq a b
instanceEquals : Decimal -> Decimal -> Bool
instanceEquals a b = Decimal.eq a b
