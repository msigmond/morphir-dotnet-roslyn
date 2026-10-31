module Example.Completion.Rules exposing (compute)
type alias Secret = { code : Int }
read : Secret -> Int
read data = data.code
compute : Int -> Int
compute event = read (Secret event)
