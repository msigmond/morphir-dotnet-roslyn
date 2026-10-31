module Example.CurrentSupported.ClassicLoanCalculations exposing (marketValue, dailyFee, calculate)

import Example.CurrentSupported exposing (ClassicLoan, ClassicLoanFee)

marketValue : ClassicLoan -> Int
marketValue loan =
    loan.quantity * loan.price

dailyFee : ClassicLoan -> Int
dailyFee loan =
    let
        marketVal : Int
        marketVal =
            (marketValue loan)

        rate : Int
        rate =
            loan.annualFeeRate
    in
    marketVal * rate // 100 // 360

calculate : ClassicLoan -> ClassicLoanFee
calculate loan =
    let
        marketVal : Int
        marketVal =
            (marketValue loan)

        dailyF : Int
        dailyF =
            (dailyFee loan)
    in
    ClassicLoanFee loan.securityId marketVal dailyF
