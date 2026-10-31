module Example.Parity.Rules exposing (..)

import Morphir.SDK.Decimal as Decimal exposing (Decimal)

add : Decimal -> Decimal -> Decimal
add a b = Decimal.add a b
subtract : Decimal -> Decimal -> Decimal
subtract a b = Decimal.sub a b
multiply : Decimal -> Decimal -> Decimal
multiply a b = Decimal.mul a b
