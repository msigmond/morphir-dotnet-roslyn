module Example.Parity.Rules exposing (many)
many : Int -> String -> (Int, String)
many value text =
    let
        echo : t -> t
        echo item = item
    in
    (echo value, echo text)
