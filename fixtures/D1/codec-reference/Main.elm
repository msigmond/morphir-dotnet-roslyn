port module Main exposing (main)

import Dict
import Json.Encode as E
import Morphir.IR.AccessControlled exposing (Access(..))
import Morphir.IR.Distribution exposing (Distribution(..))
import Morphir.IR.FormatVersion.Codec exposing (encodeVersionedDistribution)
import Morphir.IR.Name as N
import Morphir.IR.Path as P
import Morphir.IR.Type as T
import Morphir.IR.Value as V
import Morphir.IR.Literal as L
import Platform

port output : E.Value -> Cmd msg

n = N.fromString
p = P.fromString
sdk name = T.Reference () (p "Morphir.SDK", p "Basics", n name) []
int = sdk "Int"
bool = sdk "Bool"
ref local = T.Reference () (p "Sample", p "Business", n local) []
documented item = { doc = "", value = item }
controlled access item = { access = access, value = documented item }
customer = T.TypeAliasDefinition [] (T.Record () [{name=n "Age",tpe=int},{name=n "Active",tpe=bool}])
empty = { types=Dict.empty, values=Dict.empty, doc=Nothing }
privateValue = { inputTypes=[], outputType=int, body=V.Literal int (L.WholeNumberLiteral 7) }
publicValue = { inputTypes=[(n "customer",ref "Customer",ref "Customer")], outputType=int, body=V.Field int (V.Variable (ref "Customer") (n "customer")) (n "Age") }

-- Ordinary Elm cannot reference an unexposed C# record across modules. Build the
-- complete distribution using the pinned upstream algebra/codec, without JSON edits.
distribution = Library (p "Sample") Dict.empty
    { modules=Dict.fromList
        [ (p "Business", {access=Public,value={empty|types=Dict.fromList [(n "Customer",controlled Private customer),(n "Unused",controlled Private (T.TypeAliasDefinition [] (T.Record () [{name=n "Flag",tpe=bool}])))]}})
        , (p "Business.Rules", {access=Public,value={empty|values=Dict.fromList [(n "Hidden",controlled Private privateValue),(n "Read",controlled Private publicValue)]}})
        ]
    }

main : Program () () Never
main = Platform.worker {init=\_ -> ((),output (E.object [("private",encodeVersionedDistribution distribution),("sample",encodeVersionedDistribution sample),("selection",encodeVersionedDistribution selection),("outside",encodeVersionedDistribution outside),("model",encodeVersionedDistribution model)])), update=\_ state -> (state,Cmd.none), subscriptions=\_ -> Sub.none}


sampleCustomer = T.Reference () (p "Example",p "CurrentSupported",n "Customer") []
var t name = V.Variable t (n name)
whole x = V.Literal int (L.WholeNumberLiteral x)
boolean x = V.Literal bool (L.BoolLiteral x)
field name t = V.Field t (var sampleCustomer "customer") (n name)
apply2 name result left right =
    V.Apply result (V.Apply (T.Function () int result) (V.Reference (T.Function () int (T.Function () int result)) (p "Morphir.SDK",p "Basics",n name)) left) right
value access name inputs result body = (n name,controlled access {inputTypes=inputs,outputType=result,body=body})
input name t = (n name,t,t)
age = field "Age" int
active = field "Active" bool
customerInput = [input "customer" sampleCustomer]
createBody = V.Record sampleCustomer (Dict.fromList [(n "age",var int "age"),(n "active",var bool "active")])
sample = Library (p "Example") Dict.empty
    {modules=Dict.fromList
        [ (p "CurrentSupported", {access=Public,value={empty|types=Dict.fromList [(n "Customer",controlled Public customer),(n "Unused",controlled Private (T.TypeAliasDefinition [] (T.Record () [{name=n "Flag",tpe=bool}])))]}})
        , (p "CurrentSupported.CurrentSupportedExample", {access=Public,value={empty|types=Dict.fromList [(n "Nested",controlled Public (T.TypeAliasDefinition [] (T.Record () [{name=n "Value",tpe=int}])))],values=Dict.fromList
            [ value Public "Eligible" customerInput bool (V.IfThenElse bool active (apply2 "greaterThanOrEqual" bool age (whole 18)) (boolean False))
            , value Public "ActiveOrAdult" customerInput bool (V.IfThenElse bool active (boolean True) (apply2 "greaterThan" bool age (whole 17)))
            , value Public "Below" customerInput bool (apply2 "lessThan" bool age (whole 18))
            , value Public "AtMost" customerInput bool (apply2 "lessThanOrEqual" bool age (whole 18))
            , value Public "Exactly" customerInput bool (apply2 "equal" bool age (whole 18))
            , value Public "Different" customerInput bool (apply2 "notEqual" bool age (whole 18))
            , docValue Public "EligibleLabel" [] (T.Reference () (p "Morphir.SDK",p "String",n "String") []) (V.Literal (T.Reference () (p "Morphir.SDK",p "String",n "String") []) (L.StringLiteral "eligible"))
            , value Public "Create" [input "age" int,input "active" bool] sampleCustomer createBody
            , value Public "TargetTyped" [input "age" int,input "active" bool] sampleCustomer createBody
            , value Public "Addition" [] int (apply2 "add" int (whole 20) (whole 22))
            , value Public "IdentityAddition" [input "value" int] int (apply2 "add" int (var int "value") (whole 0))
            , value Public "IdentityCast" [input "checked" int] int (apply2 "add" int (var int "checked") (whole 0))
            , value Public "Always" [] bool (boolean True)
            , value Public "Named" [] sampleCustomer (V.Record sampleCustomer (Dict.fromList [(n "active",boolean True),(n "age",whole 18)]))
            , value Public "Negative" [] int (whole -18)
            , value Public "Echo0NoneGlobalSystemInt32" [input "value" int] int (var int "value")
            , value Public "Echo0NoneGlobalSystemBoolean" [input "value" bool] bool (var bool "value")
            , value Private "HiddenFlag" [] bool (boolean False)
            , value Private "Hidden" [] int (whole 7)
            ]}})
        , (p "CurrentSupported.CurrentSupportedExampleTools",{access=Public,value={empty|values=Dict.fromList [value Public "Number" [] int (whole 2)]}})
        , (p "CurrentSupported.Customer",{access=Public,value={empty|values=Dict.fromList [value Public "DataConstant" [] int (whole 1)]}})
        , (p "CurrentSupported.CurrentSupportedExample.Tools",{access=Public,value={empty|values=Dict.fromList [value Public "Echo" [input "flag" bool] bool (var bool "flag")]}})
        ]}


readValue t = {inputTypes=[(n "c",t,t)],outputType=int,body=V.Field int (V.Variable t (n "c")) (n "Age")}
selection = Library (p "Sample") Dict.empty
    {modules=Dict.fromList
        [ (p "Business",{access=Public,value={empty|types=Dict.fromList [(n "Customer",controlled Public customer)]}})
        , (p "Business.Rules",{access=Public,value={empty|values=Dict.fromList [(n "Read",controlled Public (readValue (ref "Customer")))]}})
        , (p "Business.Tax",{access=Public,value={empty|types=Dict.fromList [(n "Rate",controlled Private (T.TypeAliasDefinition [] (T.Record () [{name=n "Value",tpe=int}])))]}})
        ]}
sharedCustomer = T.Reference () (p "Sample",p "Shared",n "Customer") []
outside = Library (p "Sample") Dict.empty
    {modules=Dict.fromList
        [ (p "Business.Rules",{access=Public,value={empty|values=Dict.fromList [(n "Read",controlled Public (readValue sharedCustomer))]}})
        , (p "Shared",{access=Public,value={empty|types=Dict.fromList [(n "Customer",controlled Public customer),(n "Unused",controlled Private (T.TypeAliasDefinition [] (T.Record () [{name=n "Flag",tpe=bool}])))]}})
        ]}

model = Library (p "Model")
    (Dict.fromList [(p "External",{modules=Dict.fromList [(p "Rules",{types=Dict.fromList [(n "Age",{doc="dependency alias",value=T.TypeAliasSpecification [] int})],values=Dict.fromList [(n "Answer",{doc="external constant",value={inputs=[],output=int}})],doc=Just "external module"})]})])
    {modules=Dict.fromList [(p "Rules",{access=Public,value={types=Dict.empty,values=Dict.fromList [(n "Answer",{access=Public,value={doc="qualified reference",value={inputTypes=[],outputType=int,body=V.Reference int (p "External",p "Rules",n "Answer")}}})],doc=Just "model codec coverage"}})]}


docValue access name inputs result body = (n name,{access=access,value={doc="The eligibility label.",value={inputTypes=inputs,outputType=result,body=body}}})
