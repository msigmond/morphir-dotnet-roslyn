module Sigmond.Buslog.Algo.LoanCalculations exposing (marketValue, dailyFee, calculate)

import Sigmond.Buslog.Model exposing (Loan, LoanFee)

marketValue : Loan -> Int
marketValue loan =
    loan.quantity * loan.price

dailyFee : Loan -> Int
dailyFee loan =
    let
        marketVal =
            (marketValue loan)

        rate =
            loan.annualFeeRate
    in
    marketVal * rate // 100 // 360

calculate : Loan -> LoanFee
calculate loan =
    let
        marketVal =
            (marketValue loan)

        dailyF =
            (dailyFee loan)
    in
    LoanFee loan.securityId marketVal dailyF
