module Example.Parity.Rules exposing (..)


identity : ( Int, Bool ) -> ( Int, Bool )
identity pair = pair
create : Int -> Bool -> ( Int, Bool )
create code flag = ( code, flag )
