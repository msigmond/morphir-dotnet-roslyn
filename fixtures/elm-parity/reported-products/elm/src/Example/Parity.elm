module Example.Parity exposing (..)
import Morphir.SDK.Decimal exposing (Decimal)
type alias Loan = { securityId : String, quantity : Decimal, price : Decimal }
type alias SomeObj = { intOne : Int, intTwo : Int }
