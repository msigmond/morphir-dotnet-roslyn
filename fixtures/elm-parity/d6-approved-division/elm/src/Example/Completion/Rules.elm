module Example.Completion.Rules exposing (..)
import Morphir.SDK.Decimal as Decimal exposing (Decimal)
divide : Decimal -> Decimal -> Maybe Decimal
divide amount count = Decimal.div amount count
block : Decimal -> Decimal -> Maybe Decimal
block amount count = Decimal.div amount count
checked : Decimal -> Decimal -> Maybe Decimal
checked amount count = Decimal.div amount count
cast : Decimal -> Decimal -> Maybe Decimal
cast amount count = Decimal.div amount count

staticCall : Decimal -> Decimal -> Maybe Decimal
staticCall a b = Decimal.div a b
