module Example.Parity.Rules exposing (..)
import Morphir.SDK.Decimal as Decimal exposing (Decimal)
fraction : Decimal
fraction = Decimal.fromFloat 0.1
whole : Decimal
whole = Decimal.fromFloat 2.0
scaled : Decimal
scaled = Decimal.fromFloat 0.100
maximum : Decimal
maximum = Decimal.fromFloat 79228162514264337593543950335.0
negative : Decimal
negative = Decimal.negate (Decimal.fromFloat 0.1)
negate : Decimal -> Decimal
negate a = Decimal.negate a
positive : Decimal -> Decimal
positive a = a
