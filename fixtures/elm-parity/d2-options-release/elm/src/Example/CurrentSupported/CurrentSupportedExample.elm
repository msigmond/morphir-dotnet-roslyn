module Example.CurrentSupported.CurrentSupportedExample exposing (..)
import Example.Parity.SharedRules as SharedRules
generatedThreshold : Int
generatedThreshold = 18
sharedValue : Int -> Int
sharedValue value = SharedRules.double value

configurationValue : Int
configurationValue = 2
