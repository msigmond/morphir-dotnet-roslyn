module Example.D2.Business.Rules exposing (eligible, generatedThreshold)

import Example.D2.Shared.AgeRule as AgeRule

adult : Int -> Bool
adult age =
    AgeRule.adult age

eligible : Int -> Bool
eligible age =
    adult age

generatedThreshold : Int
generatedThreshold =
    18
