module Sample.Business.Rules exposing (eligible, eligibleLabel, create, addition, identityAddition)

import Sample.Business exposing (Customer)

eligible : Customer -> Bool
eligible customer =
    if customer.active then
        customer.age >= 18
    else
        False

eligibleLabel : String
eligibleLabel =
    "eligible"

create : Int -> Bool -> Customer
create age active =
    { age = age, active = active }

addition : Int
addition =
    20 + 22

identityAddition : Int -> Int
identityAddition value =
    value + 0
