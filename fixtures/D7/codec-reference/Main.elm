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
import Platform

port output : E.Value -> Cmd msg
n = N.fromString
p = P.fromString
sdkType owner name args = T.Reference () (p "Morphir.SDK",p owner,n name) args
int = sdkType "Basics" "Int" []
bool = sdkType "Basics" "Bool" []
unit = T.Unit ()
list t = sdkType "List" "List" [t]
maybe t = sdkType "Maybe" "Maybe" [t]
var t name = V.Variable t (n name)
whole value = V.Literal int (L.WholeNumberLiteral value)
input name t = (n name,t,t)
wrap access inputs result body = {access=access,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}}
public = wrap Public
private = wrap Private
apply arguments signature function =
    case (arguments,signature) of
        ([],_) -> function
        (argument::rest,T.Function _ _ result) -> apply rest result (V.Apply result function argument)
        _ -> Debug.todo "Independent application signature mismatch"
reference owner name types result arguments =
    let signature=List.foldr (T.Function ()) result types in
    apply arguments signature (V.Reference signature (p "Example",owner,name))
sdk owner name types result arguments =
    let signature=List.foldr (T.Function ()) result types in
    apply arguments signature (V.Reference signature (p "Morphir.SDK",p owner,n name))
just t value = V.Apply (maybe t) (V.Constructor (T.Function () t (maybe t)) (p "Morphir.SDK",p "Maybe",n "Just")) value
nothing t = V.Constructor (maybe t) (p "Morphir.SDK",p "Maybe",n "Nothing")
caseMaybe t result subject name absent present = V.PatternMatch result subject
    [(V.ConstructorPattern (maybe t) (p "Morphir.SDK",p "Maybe",n "Nothing") [],absent)
    ,(V.ConstructorPattern (maybe t) (p "Morphir.SDK",p "Maybe",n "Just") [V.AsPattern t (V.WildcardPattern t) (n name)],present)]
letValue name value body = V.LetDefinition (V.valueAttribute body) (n name) {inputTypes=[],outputType=V.valueAttribute value,body=value} body
lambda t name body = V.Lambda (T.Function () t (V.valueAttribute body)) (V.AsPattern t (V.WildcardPattern t) (n name)) body
lambdaUnit body = V.Lambda (T.Function () unit (V.valueAttribute body)) (V.UnitPattern unit) body

-- Renaming is at the source owner/definition level, before the unmodified codec.
renameName prefix name = if prefix=="" then name else n prefix ++ name

additions owner dataOwner customerName prefix =
    let
        name local = n (prefix++local)
        customer=T.Reference () (p "Example",dataOwner,n customerName) []
        customerOwner=dataOwner++[n customerName]
        call local types result args=reference owner (name local) types result args
        customerCall local types result args=reference customerOwner (n local) types result args
        age=var int "age"
        value=var int "value"
        array=maybe (list int)
        length subject=sdk "List" "length" [list int] int [subject]
        present=var (list int) "presentValue"
        count=caseMaybe (list int) int (var array "values") "presentValue" (whole -1) (length present)
        dropped=sdk "List" "drop" [int,list int] (list int) [whole 0,present]
        head=sdk "List" "head" [list int] (maybe int) [dropped]
        extracted=V.PatternMatch int head [(V.ConstructorPattern (maybe int) (p "Morphir.SDK",p "Maybe",n "Just") [V.AsPattern int (V.WildcardPattern int) (n "paramsElement")],var int "paramsElement")]
        first=caseMaybe (list int) int (var array "values") "presentValue" (whole -1)
            (V.IfThenElse int (sdk "Basics" "equal" [int,int] bool [length present,whole 0]) (whole -1) extracted)
        record current=V.Record customer (Dict.singleton (n "age") current)
        namedBody=letValue "parameter_value" (whole 2) (call "Select" [int,int] int [var int "parameter_value",value])
        packedBody=letValue "parameter_value" (whole 2) (call "First" [array] int [just (list int) (V.List (list int) [value,var int "parameter_value"])])
        localOwner=owner++[name "Local"]
        localName=n "Allows__0__None_global::System.Int32__None_global::System.Int32"
        function=T.Function () int bool
        functionBody=lambda int "methodArgument0" (call "AdultAge" [int] bool [var int "methodArgument0"])
        referenceBody=letValue "local_check" (just function functionBody) (V.Apply bool functionBody age)
        snapshotFunction=letValue "methodReceiver" (var customer "local_value") (lambdaUnit (customerCall "Adult" [customer] bool [var customer "methodReceiver"]))
        snapshot=letValue "local_value" (record age)
            (letValue "local_check" (just (T.Function () unit bool) snapshotFunction)
                (letValue "local_value_1" (record (whole 100)) (V.Apply bool snapshotFunction (V.Unit unit))))
        values=Dict.fromList
            [(name "Select",private [input "first" int,input "second" int] int (var int "first"))
            ,(name "Count",private [input "values" array] int count)
            ,(name "First",private [input "values" array] int first)
            ,(name "AdultAge",private [input "value" int] bool (sdk "Basics" "greaterThanOrEqual" [int,int] bool [value,whole 18]))
            ,(name "NamedOrder",public [input "value" int] int namedBody)
            ,(name "Empty",public [] int (call "Count" [array] int [just (list int) (V.List (list int) [])]))
            ,(name "Missing",public [] int (call "Count" [array] int [nothing (list int)]))
            ,(name "Packed",public [input "value" int] int packedBody)
            ,(name "Instance",public [input "value" customer] bool (customerCall "Adult" [customer] bool [var customer "value"]))
            ,(name "Local",public [input "age" int] bool (reference localOwner localName [int,int] bool [age,whole 18]))
            ,(name "Reference",public [input "age" int] bool referenceBody)
            ,(name "Snapshot",public [input "age" int] bool snapshot)]
        memberValues=Dict.fromList
            [(n "Minimum",public [input "receiver" customer,input "minimum" int] bool (sdk "Basics" "greaterThanOrEqual" [int,int] bool [V.Field int (var customer "receiver") (n "age"),var int "minimum"]))
            ,(n "Adult",public [input "receiver" customer] bool (customerCall "Minimum" [customer,int] bool [var customer "receiver",whole 18]))]
        localValues=Dict.singleton localName (private [input "value" int,input "minimum" int] bool (sdk "Basics" "greaterThanOrEqual" [int,int] bool [value,var int "minimum"]))
        ownerModule={access=Public,value={types=Dict.empty,values=values,doc=Nothing}}
        memberModule={access=Public,value={types=Dict.empty,values=memberValues,doc=Nothing}}
        localModule={access=Public,value={types=Dict.empty,values=localValues,doc=Nothing}}
        alias=Dict.singleton (n customerName) {access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () [{name=n "age",tpe=int}])}}
    in (Dict.fromList [(owner,ownerModule),(customerOwner,memberModule),(localOwner,localModule)],alias)

-- Canonical names are independently derived from arity/ref-kind/full type names.
baseValues prefix owner =
    let
        name local=renameName prefix (n local)
        meetsInt=name "Meets__0__None_global::System.Int32__None_global::System.Int32"
        meetsBool=name "Meets__0__None_global::System.Boolean"
        age=var int "age"
        approval=var bool "approval"
        meets=reference owner meetsInt [int,int] bool [age,whole 18]
    in Dict.fromList
        [(meetsInt,private [input "age" int,input "minimum" int] bool (sdk "Basics" "greaterThanOrEqual" [int,int] bool [age,var int "minimum"]))
        ,(meetsBool,private [input "approval" bool] bool approval)
        ,(name "Eligible",public [input "age" int] bool meets)
        ,(name "Approved",public [input "approval" bool] bool (reference owner meetsBool [bool] bool [approval]))
        ,(name "DefaultEligible",public [input "age" int] bool meets)]

externalReference =
    let
        signature=T.Function () int (T.Function () int bool)
        target=V.Reference signature (p "Reviewed",p "Rules",n "meets")
        call age minimum=apply [age,minimum] signature target
        function=lambda int "methodArgument0" (lambda int "methodArgument1" (call (var int "methodArgument0") (var int "methodArgument1")))
        values=Dict.fromList
            [(n "Eligible",public [input "age" int] bool (call (var int "age") (whole 18)))
            ,(n "DefaultEligible",public [input "age" int] bool (call (var int "age") (whole 18)))
            ,(n "Order",public [input "value" int] bool (letValue "parameter_value" (whole 2) (call (var int "parameter_value") (var int "value"))))
            ,(n "Reference",public [input "age" int] bool (letValue "local_f" (just signature function) (apply [var int "age",whole 18] signature function)))]
        specification={modules=Dict.singleton (p "Rules") {types=Dict.empty,values=Dict.singleton (n "meets") {doc="Pure total threshold comparison",value={inputs=[(n "age",int),(n "minimum",int)],output=bool}},doc=Just "Reviewed external values"}}
        owner={access=Public,value={types=Dict.empty,values=values,doc=Nothing}}
    in Library (p "Example") (Dict.singleton (p "Reviewed") specification) {modules=Dict.singleton (p "D7.Rules") owner}

references optional previous =
    case (optional,previous) of
        (Library package dependencies original,Library _ baselineDependencies baseline) ->
            let
                owner=p "D7.Rules"
                (added,alias)=additions owner (p "D7") "Customer" ""
                mergeValues values controlled=let moduleDefinition=controlled.value in {controlled|value={moduleDefinition|values=Dict.union values moduleDefinition.values}}
                merged=Dict.update owner (Maybe.map (mergeValues (baseValues "" owner))) added
                business=Library package dependencies {modules=Dict.insert (p "D7") {access=Public,value={types=alias,values=Dict.empty,doc=Nothing}} merged}
                sampleOwner=p "CurrentSupported.CurrentSupportedExample"
                (sampleAdded,sampleAlias)=additions sampleOwner (p "CurrentSupported") "CallCustomer" "Call"
                sampleValues=case Dict.get sampleOwner sampleAdded of
                    Just controlled -> Dict.union (baseValues "Call" sampleOwner) controlled.value.values
                    Nothing -> Debug.todo "Missing sample owner"
                appendTypes controlled=let definition=controlled.value in {controlled|value={definition|types=Dict.union sampleAlias definition.types}}
                sampleModules=baseline.modules |> Dict.update sampleOwner (Maybe.map (mergeValues sampleValues)) |> Dict.update (p "CurrentSupported") (Maybe.map appendTypes)
                otherModules=Dict.remove sampleOwner sampleAdded
                sample=Library package baselineDependencies {modules=Dict.union otherModules sampleModules}
            in E.object [("calls",encodeVersionedDistribution business),("sample",encodeVersionedDistribution sample),("external",encodeVersionedDistribution externalReference)]

main : Program E.Value () Never
main=Platform.worker
    {init=\flags -> ((),output (case Decode.decodeValue (Decode.map2 Tuple.pair (Decode.field "optional" decodeVersionedDistribution) (Decode.field "previous" decodeVersionedDistribution)) flags of
        Ok (optional,previous)->references optional previous
        Err error->E.object [("error",E.string (Decode.errorToString error))]))
    ,update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
