module Example.Completion.Rules exposing (..)
remainder : Int -> Int -> Int
remainder value divisor = remainderBy divisor value
code : Char -> Int
code value = Char.toCode value
character : Int -> Char
character value = Char.fromCode value
textEqual : String -> String -> Bool
textEqual a b = a == b
textDifferent : String -> String -> Bool
textDifferent a b = a /= b
