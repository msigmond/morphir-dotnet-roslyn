module Example.CurrentSupported exposing (..)
type alias ClassicLoan = { securityId : String, quantity : Int, price : Int, annualFeeRate : Int }
type alias ClassicLoanFee = { securityId : String, marketValue : Int, dailyFee : Int }
type alias ClassicState = Int
type alias ClassicValue = { value : Int }
type alias ClassicBox t = { value : t }
