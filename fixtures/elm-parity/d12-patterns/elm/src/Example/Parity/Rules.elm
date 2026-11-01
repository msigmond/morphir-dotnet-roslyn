module Example.Parity.Rules exposing (..)
bind : t -> t
bind value = case value of
    bound -> bound
pair : (Int,String) -> String
pair value = case value of
    (0,text) -> text
    (_,other) -> other
triple : (Int,Int,Int) -> Int
triple value = case value of
    (0,second,third) -> second + third
    _ -> 0
referencePair : (Int,Bool) -> Int
referencePair value = case value of
    (first,True) -> first
    _ -> 0
first : List Int -> Int
first value = case value of
    [] -> 0
    first :: rest -> first
two : List Int -> Int
two value = case value of
    first :: second :: [] -> first + second
    _ -> 0
rest : List Int -> Int
rest value = case value of
    _ :: rest -> List.length rest
    _ -> 0
prefix : List Int -> Int
prefix value = case value of
    0 :: _ -> 1
    _ -> 0
copy : List t -> List t
copy value = case value of
    rest -> rest

namedPair : (Int,Int) -> Int
namedPair value = case value of
    (0,_) as pair -> Tuple.second pair
    _ -> 0
namedList : List Int -> Int
namedList value = case value of
    (_ :: _) as all -> List.length all
    _ -> 0
