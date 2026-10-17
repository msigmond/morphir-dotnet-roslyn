module Example.D4.Rules exposing (adult, regionOrDefault)

import Example.D4 exposing (Customer)

adult : Customer -> Bool
adult customer =
    case customer.age of
        Nothing ->
            False

        Just presentValue ->
            presentValue >= 18

regionOrDefault : Customer -> String
regionOrDefault customer =
    case customer.region of
        Nothing ->
            "unknown"

        Just presentValue ->
            presentValue
