module Example.Parity.Rules exposing (..)
import Example.Parity exposing (..)
import Morphir.SDK.Decimal as Decimal exposing (Decimal)
marketValue : Loan -> Decimal
marketValue loan = Decimal.mul loan.quantity loan.price
someMethod : SomeObj -> Int
someMethod obj = obj.intOne * obj.intTwo
