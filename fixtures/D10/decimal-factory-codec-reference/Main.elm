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
decimal=T.Reference () (p "Example",p "Compat.Decimal",n "Decimal") []
sdkDecimal=T.Reference () (p "Morphir.SDK",p "Decimal",n "Decimal") []
string=T.Reference () (p "Morphir.SDK",p "String",n "String") []
maybe kind=T.Reference () (p "Morphir.SDK",p "Maybe",n "Maybe") [kind]
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
call modules owner name arguments=
    case Dict.get (p owner) modules |> Maybe.andThen (\controlled->Dict.get (n name) controlled.value.values) of
        Nothing -> Debug.todo "Missing frozen original helper"
        Just controlled ->
            let definition=controlled.value.value
                signature=List.foldr (\(_,_,kind) result->T.Function () kind result) definition.outputType definition.inputTypes
            in apply arguments signature (V.Reference signature (p "Example",p owner,n name))
constant negative digits=V.Record integer (Dict.fromList [(n "digits",V.List (T.Reference () (p "Morphir.SDK",p "List",n "List") [int]) (List.map whole digits)),(n "negative",boolean negative)])
privateFunctions controlled=
    let definition=controlled.value in {controlled|value={definition|values=Dict.map (\_ entry->{entry|access=Private}) definition.values}}
reference helpers=
    case helpers of
        Library package dependencies original ->
            let
                modules=Dict.map (\_ controlled->privateFunctions controlled) original.modules
                i name arguments=call modules "Compat.Integers" name arguments
                c name arguments=call modules "Compat.Decimal" name arguments
                clamp subject minimum maximum=
                    let
                        below=sdk "lessThan" [int,int] bool [i "compare" [subject,minimum],whole 0]
                        above=sdk "greaterThan" [int,int] bool [i "compare" [subject,maximum],whole 0]
                    in V.IfThenElse integer below minimum (V.IfThenElse integer above maximum subject)
                epsilonSource=apply [V.Literal string (L.StringLiteral "0.0000000000000000000000000001")] (T.Function () string (maybe sdkDecimal)) (V.Reference (T.Function () string (maybe sdkDecimal)) (p "Morphir.SDK",p "Decimal",n "fromString"))
                epsilon=V.PatternMatch sdkDecimal epsilonSource [(V.ConstructorPattern (maybe sdkDecimal) (p "Morphir.SDK",p "Maybe",n "Just") [V.AsPattern sdkDecimal (V.WildcardPattern sdkDecimal) (n "parsedDecimal")],var sdkDecimal "parsedDecimal")]
                limit=List.repeat 6 65535
                saturated=clamp (var integer "value") (constant True limit) (constant False limit)
                converted=c "toInteger" [var decimal "value"]
                saturate=businessValue "Saturate" integer decimal (c "fromInteger" [epsilon,saturated])
                toByte=businessValue "ToByte" decimal int (i "toSmall" [i "wrap" [whole 8,boolean False,clamp converted (constant False []) (constant False [255])]])
                toNative=businessValue "ToNative" decimal integer (i "wrap" [whole 64,boolean True,clamp converted (constant True [0,0,0,32768]) (constant False [65535,65535,65535,32767])])
                rules={access=Public,value={doc=Nothing,types=Dict.empty,values=Dict.fromList [saturate,toByte,toNative]}}
            in Library package dependencies {modules=Dict.insert (p "D10.Rules") rules modules}
main : Program E.Value () Never
main=Platform.worker {init=\flags->((),output (case D.decodeValue decodeVersionedDistribution flags of
    Ok helpers -> E.object [("decimal-factories",encodeVersionedDistribution (reference helpers))]
    Err error -> E.object [("error",E.string (D.errorToString error))])),update=\_ state->(state,Cmd.none),subscriptions=\_->Sub.none}
