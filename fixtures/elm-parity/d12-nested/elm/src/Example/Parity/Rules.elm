module Example.Parity.Rules exposing (..)
nullableList : Maybe (List Int) -> Int
nullableList value = case value of
    Nothing -> 0
    Just [] -> 1
    Just (first :: _) -> first
nested : List (Int,String) -> Int
nested value = case value of
    (0,text) :: _ -> String.length text
    _ -> 0
nestedSwitch : Int -> Int -> Int
nestedSwitch value other = case value of
    0 ->
        case other of
            1 -> 2
            _ -> 3
    _ -> 4
maybeValue : Maybe Int -> Maybe Int
maybeValue value = case value of
    Nothing -> Nothing
    Just 0 -> Just 1
    other -> other
