module Example.Parity.Rules exposing (..)
filter : List Int -> Int -> List Int
filter values minimum = List.filter (\value -> value > minimum) values
map : List Int -> List Int
map values = List.map (\value -> value * 2) values
chain : List Int -> List Int
chain values = List.map (\value -> value + 1) (List.filter (\value -> value > 0) values)
copy : List Int -> List Int
copy values = values
callback : List Int -> (Int -> String) -> List String
callback values callback = List.map callback values
generic : List t -> List t
generic values = List.map (\value -> value) values
find : List Int -> List Int
find values = List.filter (\value -> value > 0) values
convert : List Int -> List Int
convert values = List.map (\value -> value + 1) values
named : List Int -> List Int
named values = List.map (\value -> value + 1) values
nullable : List Int -> List (Maybe Int)
nullable values = List.map (\value -> Just value) values
method : List Int -> List Int
method values = List.map increment values
increment : Int -> Int
increment value = value + 1
convertCallback : List Int -> (Int -> String) -> List String
convertCallback values callback = List.map callback values
findCallback : List Int -> (Int -> Bool) -> List Int
findCallback values callback = List.filter callback values
