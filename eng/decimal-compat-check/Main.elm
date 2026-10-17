port module Main exposing (main)

import Example.Compat.Decimal as C
import Example.Compat.Integers as I
import Morphir.SDK.Decimal as D
import Platform

port output : List Result -> Cmd msg
port failure : String -> Cmd msg

type alias Bits = {coefficient:I.Integer,scale:Int,negative:Bool}
type alias Case = {operation:String,left:Bits,right:Bits,digits:Int,mode:Int}
type alias Result = {coefficient:I.Integer,scale:Int,negative:Bool,value:String}

run : D.Decimal -> Case -> Result
run epsilon c =
    let
        left = C.make epsilon c.left.negative c.left.scale c.left.coefficient
        right = C.make epsilon c.right.negative c.right.scale c.right.coefficient
        result =
            case c.operation of
                "add" -> C.add epsilon left right
                "subtract" -> C.subtract epsilon left right
                "multiply" -> C.multiply epsilon left right
                "divide" -> C.divide epsilon left right
                "remainder" -> C.remainder epsilon left right
                "round" -> C.roundTo epsilon c.digits c.mode left
                _ -> left
    in
    {coefficient=result.coefficient,scale=result.scale,negative=result.negative,value=D.toString result.value}

main : Program (List Case) () Never
main =
    Platform.worker
        {init=\cases -> ((),case D.fromString "0.0000000000000000000000000001" of
            Just epsilon -> output (List.map (run epsilon) cases)
            Nothing -> failure "Pinned SDK rejected exact epsilon")
        ,update=\_ state -> (state,Cmd.none)
        ,subscriptions=\_ -> Sub.none}
