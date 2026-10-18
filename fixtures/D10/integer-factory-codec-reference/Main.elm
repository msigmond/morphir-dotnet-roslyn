port module Main exposing (main)

import Dict
import Json.Decode as D
import Json.Encode as E
import Morphir.IR.AccessControlled exposing (Access(..))
import Morphir.IR.Distribution exposing (Distribution(..))
import Morphir.IR.FormatVersion.Codec exposing (decodeVersionedDistribution, encodeVersionedDistribution)
import Morphir.IR.Literal as L
import Morphir.IR.Name as N
import Morphir.IR.Path as P
import Morphir.IR.Type as T
import Morphir.IR.Value as V
import Platform

port output : E.Value -> Cmd msg
n=N.fromString
p=P.fromString
int=T.Reference () (p "Morphir.SDK",p "Basics",n "Int") []
bool=T.Reference () (p "Morphir.SDK",p "Basics",n "Bool") []
integer=T.Reference () (p "Example",p "Compat.Integers",n "Integer") []
whole value=V.Literal int (L.WholeNumberLiteral value)
boolean value=V.Literal bool (L.BoolLiteral value)
var kind name=V.Variable kind (n name)
input name kind=(n name,kind,kind)
businessValue name kind result body=(n name,{access=Public,value={doc="",value={inputTypes=[input "value" kind],outputType=result,body=body}}})
apply arguments signature function=
    case arguments of
        [] -> function
        argument::rest ->
            case signature of
                T.Function _ _ result -> apply rest result (V.Apply result function argument)
                _ -> Debug.todo "Independent reference application requires a function"
sdk name types result arguments=
    let signature=List.foldr (T.Function ()) result types in apply arguments signature (V.Reference signature (p "Morphir.SDK",p "Basics",n name))
call modules name arguments=
    case Dict.get (p "Compat.Integers") modules |> Maybe.andThen (\controlled->Dict.get (n name) controlled.value.values) of
        Nothing -> Debug.todo "Missing frozen original helper"
        Just controlled ->
            let definition=controlled.value.value
                signature=List.foldr (\(_,_,kind) result->T.Function () kind result) definition.outputType definition.inputTypes
            in apply arguments signature (V.Reference signature (p "Example",p "Compat.Integers",n name))
constant digits=V.Record integer (Dict.fromList [(n "digits",V.List (T.Reference () (p "Morphir.SDK",p "List",n "List") [int]) (List.map whole digits)),(n "negative",boolean False)])
privateFunctions controlled=
    let definition=controlled.value in {controlled|value={definition|values=Dict.map (\_ entry->{entry|access=Private}) definition.values}}
reference helpers=
    case helpers of
        Library package dependencies original ->
            let
                modules=Dict.map (\_ controlled->privateFunctions controlled) original.modules
                i name arguments=call modules name arguments
                subject=var integer "value"
                minimum=constant []
                maximum=constant [255]
                below=sdk "lessThan" [int,int] bool [i "compare" [subject,minimum],whole 0]
                above=sdk "greaterThan" [int,int] bool [i "compare" [subject,maximum],whole 0]
                clamped=V.IfThenElse integer below minimum (V.IfThenElse integer above maximum subject)
                small=businessValue "Small" integer int (i "toSmall" [i "wrap" [whole 8,boolean False,clamped]])
                wrapped=businessValue "Wrapped" integer int (i "toSmall" [i "wrap" [whole 32,boolean True,subject]])
                widen=businessValue "Widen" int integer (i "wrap" [whole 64,boolean True,i "fromSmall" [var int "value"]])
                rules={access=Public,value={doc=Nothing,types=Dict.empty,values=Dict.fromList [small,wrapped,widen]}}
            in Library package dependencies {modules=Dict.insert (p "D10.Rules") rules modules}
main : Program E.Value () Never
main=Platform.worker {init=\flags->((),output (case D.decodeValue decodeVersionedDistribution flags of
    Ok helpers -> E.object [("integer-factories",encodeVersionedDistribution (reference helpers))]
    Err error -> E.object [("error",E.string (D.errorToString error))])),update=\_ state->(state,Cmd.none),subscriptions=\_->Sub.none}
