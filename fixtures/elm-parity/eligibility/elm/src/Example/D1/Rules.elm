module Example.D1.Rules exposing (..)
import Example.D1 exposing (Customer)
eligible : Customer -> Bool
eligible customer = customer.active && customer.age >= 18
eligibleLabel : String
eligibleLabel = "eligible"
create : Int -> Bool -> Customer
create age active = Customer age active
addition : Int
addition = 20 + 22
identityAddition : Int -> Int
identityAddition value = value + 0
