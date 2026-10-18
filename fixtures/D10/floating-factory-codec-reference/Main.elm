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
float=T.Reference () (p "Morphir.SDK",p "Basics",n "Float") []
bool=T.Reference () (p "Morphir.SDK",p "Basics",n "Bool") []
integer=T.Reference () (p "Example",p "Compat.Integers",n "Integer") []
complex=T.Reference () (p "Example",p "Compat.Floating",n "Complex") []
whole value=V.Literal int (L.WholeNumberLiteral value)
real value=V.Literal float (L.FloatLiteral value)
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
constant digits=V.Record integer (Dict.fromList [(n "digits",V.List (T.Reference () (p "Morphir.SDK",p "List",n "List") [int]) (List.map whole digits)),(n "negative",boolean False)])
privateFunctions controlled=
    let definition=controlled.value in {controlled|value={definition|values=Dict.map (\_ entry->{entry|access=Private}) definition.values}}
reference helpers=
    case helpers of
        Library package dependencies original ->
            let
                modules=Dict.map (\_ controlled->privateFunctions controlled) original.modules
                i name arguments=call modules "Compat.Integers" name arguments
                f name arguments=call modules "Compat.Floating" name arguments
                subject=var float "value"
                nan=sdk "isNaN" [float] bool [subject]
                below=sdk "lessThanOrEqual" [float,float] bool [subject,real 0]
                above=sdk "greaterThanOrEqual" [float,float] bool [subject,real 256]
                bounded=V.IfThenElse integer nan (constant []) (V.IfThenElse integer below (constant []) (V.IfThenElse integer above (constant [255]) (i "fromFiniteFloat" [subject])))
                small=businessValue "Small" float int (i "toSmall" [i "wrap" [whole 8,boolean False,bounded]])
                wrapped=businessValue "Wrapped" float int (i "toSmall" [i "floatCast" [whole 32,boolean True,subject]])
                narrow=businessValue "Narrow" float float (f "roundBinary" [whole 11,whole -14,whole 15,subject])
                extended=V.Record complex (Dict.fromList [(n "real",i "toBinaryFloat" [whole 53,var integer "value"]),(n "imaginary",real 0)])
                extend=businessValue "Extend" integer complex extended
                rules={access=Public,value={doc=Nothing,types=Dict.empty,values=Dict.fromList [small,wrapped,narrow,extend]}}
            in Library package dependencies {modules=Dict.insert (p "D10.Rules") rules modules}
main : Program E.Value () Never
main=Platform.worker {init=\flags->((),output (case D.decodeValue decodeVersionedDistribution flags of
    Ok helpers -> E.object [("floating-factories",encodeVersionedDistribution (reference helpers))]
    Err error -> E.object [("error",E.string (D.errorToString error))])),update=\_ state->(state,Cmd.none),subscriptions=\_->Sub.none}
