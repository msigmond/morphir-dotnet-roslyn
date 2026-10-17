port module Main exposing (main)

import Example.Compat.Integers as I
import Platform

port output : List I.Integer -> Cmd msg

type alias Case = { operation : String, left : I.Integer, right : I.Integer, bits : Int, signed : Bool, count : Int }

run : Case -> I.Integer
run c =
    case c.operation of
        "add" -> I.add c.left c.right
        "subtract" -> I.subtract c.left c.right
        "multiply" -> I.multiply c.left c.right
        "divide" -> I.divide c.left c.right
        "remainder" -> I.remainder c.left c.right
        "wrap" -> I.wrap c.bits c.signed c.left
        "and" -> I.bitAnd c.left c.right
        "or" -> I.bitOr c.left c.right
        "xor" -> I.bitXor c.left c.right
        "complement" -> I.complement c.left
        "left" -> I.shiftLeft c.left c.count
        "right" -> I.shiftRight c.left c.count
        "logical" -> I.shiftBigUnsigned c.left c.count
        "compare" -> I.fromSmall (I.compare c.left c.right)
        _ -> c.left

main : Program (List Case) () Never
main = Platform.worker {init=\cases -> ((),output (List.map run cases)),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
