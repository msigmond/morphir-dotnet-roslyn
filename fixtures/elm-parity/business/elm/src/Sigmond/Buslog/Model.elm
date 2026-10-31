module Sigmond.Buslog.Model exposing (Loan, LoanFee)

type alias Loan =
    { securityId: String
    , quantity: Int
    , price: Int
    , annualFeeRate: Int
    }

type alias LoanFee =
    { securityId: String
    , marketValue: Int
    , dailyFee: Int
    }