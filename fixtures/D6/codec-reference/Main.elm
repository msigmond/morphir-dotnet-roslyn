port module Main exposing (main)

import Dict
import Json.Decode as Decode
import Json.Encode as E
import Morphir.IR.AccessControlled exposing (Access(..))
import Morphir.IR.Distribution exposing (Distribution(..))
import Morphir.IR.FormatVersion.Codec exposing (decodeVersionedDistribution, encodeVersionedDistribution)
import Morphir.IR.Literal as L
import Morphir.IR.Name as N
import Morphir.IR.Path as P
import Morphir.IR.Type as T
import Morphir.IR.Value as V
import Morphir.SDK.Decimal as D
import Platform

port output : E.Value -> Cmd msg

n = N.fromString
p = P.fromString
ref owner name = T.Reference () (p "Morphir.SDK",p owner,n name) []
int = ref "Basics" "Int"
bool = ref "Basics" "Bool"
string = ref "String" "String"
sdkDecimal = ref "Decimal" "Decimal"
integer = T.Reference () (p "Example",p "Compat.Integers",n "Integer") []
decimal = T.Reference () (p "Example",p "Compat.Decimal",n "Decimal") []
maybe t = T.Reference () (p "Morphir.SDK",p "Maybe",n "Maybe") [t]
whole value = V.Literal int (L.WholeNumberLiteral value)
boolean value = V.Literal bool (L.BoolLiteral value)
var t name = V.Variable t (n name)
input name t = (n name,t,t)
wrap inputs result body = {access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}}
apply arguments signature function =
    case (arguments,signature) of
        ([],_) -> function
        (argument::rest,T.Function _ _ result) -> apply rest result (V.Apply result function argument)
        _ -> Debug.todo "Invalid independently authored reference application"
sdk owner name types result arguments =
    let signature=List.foldr (T.Function ()) result types in
    apply arguments signature (V.Reference signature (p "Morphir.SDK",p owner,n name))
basic name t result a b = sdk "Basics" name [t,t] result [a,b]
call modules owner name arguments =
    case Dict.get (p owner) modules |> Maybe.andThen (\controlled -> Dict.get (n name) controlled.value.values) of
        Nothing -> Debug.todo "Missing independent helper"
        Just controlled ->
            let
                definition=controlled.value.value
                signature=List.foldr (\(_,_,t) result -> T.Function () t result) definition.outputType definition.inputTypes
            in apply arguments signature (V.Reference signature (p "Example",p owner,n name))
parsed name text =
    let
        source=sdk "Decimal" "fromString" [string] (maybe sdkDecimal) [V.Literal string (L.StringLiteral text)]
        binder=var sdkDecimal name
    in V.PatternMatch sdkDecimal source [(V.ConstructorPattern (maybe sdkDecimal) (p "Morphir.SDK",p "Maybe",n "Just") [V.AsPattern sdkDecimal (V.WildcardPattern sdkDecimal) (n name)],binder)]
limbs digits = V.Record integer (Dict.fromList [(n "digits",V.List (T.Reference () (p "Morphir.SDK",p "List",n "List") [int]) (List.map whole digits)),(n "negative",boolean False)])
constant name text scale digits = V.Record decimal (Dict.fromList [(n "coefficient",limbs digits),(n "negative",boolean False),(n "scale",whole scale),(n "value",parsed name text)])
epsilon name = parsed name "0.0000000000000000000000000001"

values modules prefix =
    let
        c name args=call modules "Compat.Decimal" name args
        i name args=call modules "Compat.Integers" name args
        units=var int "units"
        value=var decimal "value"
        product=c "multiply" [epsilon "parsedDecimal_4",c "fromInteger" [epsilon "parsedDecimal_2",i "fromSmall" [units]],constant "parsedDecimal_3" "0.1" 1 [1]]
        price=V.IfThenElse decimal (basic "lessThan" int bool units (whole 0)) (constant "parsedDecimal" "0" 0 [])
            (V.IfThenElse decimal (basic "greaterThan" int bool units (whole 1000)) (constant "parsedDecimal_1" "0" 0 []) product)
    in Dict.fromList
        [(n (prefix++"Price"),wrap [input "units" int] decimal price)
        ,(n (prefix++"Exact"),wrap [] decimal (constant "parsedDecimal" "0.1" 1 [1]))
        ,(n (prefix++"Scaled"),wrap [] decimal (constant "parsedDecimal" "1.2300" 4 [12300]))
        ,(n (prefix++"Round"),wrap [input "value" decimal] decimal (c "roundTo" [epsilon "parsedDecimal",whole 2,whole 0,value]))
        ,(n (prefix++"Scale"),wrap [input "value" decimal] int (V.Field int value (n "scale")))
        ,(n (prefix++"Negate"),wrap [input "value" decimal] decimal (c "negate" [value]))
        ,(n (prefix++"Less"),wrap [input "left" decimal,input "right" decimal] bool (basic "lessThan" int bool (c "compare" [var decimal "left",var decimal "right"]) (whole 0)))
        ,(n (prefix++"FromLong"),wrap [input "value" integer] decimal (c "fromInteger" [epsilon "parsedDecimal",var integer "value"]))]

privateFunctions controlled =
    let definition=controlled.value in {controlled|value={definition|values=Dict.map (\_ entry -> {entry|access=Private}) definition.values}}

references helpers previous =
    case (helpers,previous,D.fromString "0.1") of
        (Library package dependencies helperPackage,Library _ _ oldPackage,Just tenth) ->
            let
                helperModules=Dict.map (\_ controlled -> privateFunctions controlled) helperPackage.modules
                owner={access=Public,value={types=Dict.empty,values=values helperModules "",doc=Nothing}}
                business=Library package dependencies {modules=Dict.insert (p "D6.Rules") owner helperModules}
                extend controlled=let original=controlled.value in {controlled|value={original|values=Dict.union (values helperModules "Decimal") original.values}}
                sample=Library package dependencies {modules=Dict.union helperModules (Dict.update (p "CurrentSupported.CurrentSupportedExample") (Maybe.map extend) oldPackage.modules)}
                model=Library (p "Example") Dict.empty {modules=Dict.singleton (p "D6.Model") {access=Public,value={types=Dict.empty,values=Dict.singleton (n "exactTenth") {access=Public,value={doc="Exact decimal literal payload",value={inputTypes=[],outputType=sdkDecimal,body=V.Literal sdkDecimal (L.DecimalLiteral tenth)}}},doc=Nothing}}}
            in E.object [("decimal",encodeVersionedDistribution business),("sample",encodeVersionedDistribution sample),("model-literal",encodeVersionedDistribution model)]
        _ -> E.object [("error",E.string "Pinned SDK rejected exact fixture constant")]

main : Program E.Value () Never
main = Platform.worker
    {init=\flags -> ((),output (case Decode.decodeValue (Decode.map2 Tuple.pair (Decode.field "helpers" decodeVersionedDistribution) (Decode.field "previous" decodeVersionedDistribution)) flags of
        Ok (helpers,previous) -> references helpers previous
        Err error -> E.object [("error",E.string (Decode.errorToString error))]))
    ,update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
