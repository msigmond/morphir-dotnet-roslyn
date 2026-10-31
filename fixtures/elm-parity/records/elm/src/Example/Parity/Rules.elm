module Example.Parity.Rules exposing (..)

import Example.Parity exposing (..)

create : Loan -> LoanFee
create loan = LoanFee loan.securityId (loan.quantity * loan.price)
read : Loan -> Int
read loan = loan.quantity
