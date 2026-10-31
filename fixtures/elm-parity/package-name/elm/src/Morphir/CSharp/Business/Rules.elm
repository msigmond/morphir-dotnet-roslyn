module Morphir.CSharp.Business.Rules exposing (f)
import Morphir.CSharp.Business exposing (Loan)
f : Loan -> Int
f loan = loan.value
