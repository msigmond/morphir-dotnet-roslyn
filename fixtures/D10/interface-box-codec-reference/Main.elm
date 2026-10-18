port module Main exposing (main)

import Dict
import Json.Encode as E
import Morphir.IR.AccessControlled exposing (Access(..))
import Morphir.IR.Distribution exposing (Distribution(..))
import Morphir.IR.FormatVersion.Codec exposing (encodeVersionedDistribution)
import Morphir.IR.Literal as L
import Morphir.IR.Name as N
import Morphir.IR.Path as P
import Morphir.IR.Type as T
import Morphir.IR.Value as V
import Platform

port output : E.Value -> Cmd msg
n=N.fromString
p=P.fromString
fq owner name=(p "Example",p owner,n name)
ref name=T.Reference () (fq "D10" name) []
int=T.Reference () (p "Morphir.SDK",p "Basics",n "Int") []
maybe kind=T.Reference () (p "Morphir.SDK",p "Maybe",n "Maybe") [kind]
var kind name=V.Variable kind (n name)
whole number=V.Literal int (L.WholeNumberLiteral number)
input name kind=(n name,kind,kind)
value name inputs result body=(n name,{access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}})
moduleDefinition types values={access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
call owner kind receiver=V.Apply int (V.Reference (T.Function () kind int) (fq owner "GetValue__0__")) receiver
reference=
    let
        item=ref "Counter"
        contract=ref "IValue"
        itemType=(n "Counter",{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () [{name=n "value",tpe=int}])}})
        interfaceType=(n "IValue",{access=Public,value={doc="",value=T.CustomTypeDefinition [] {access=Public,value=Dict.singleton (n "CounterValue") [(n "arg1",item)]}}})
        getter=value "GetValue__0__" [input "receiver" item] int (V.Field int (var item "receiver") (n "value"))
        dispatch=value "GetValue__0__" [input "receiver" contract] int (V.PatternMatch int (var contract "receiver")
            [(V.ConstructorPattern contract (fq "D10" "CounterValue") [V.AsPattern item (V.WildcardPattern item) (n "rule")],call "D10.Counter" item (var item "rule"))])
        setterName="SetValue__0__None_global::System.Int32"
        setter=value setterName [input "receiver" item,input "value" int] item (V.UpdateRecord item (var item "receiver") (Dict.singleton (n "value") (var int "value")))
        changed=V.UpdateRecord item (var item "typeValue") (Dict.singleton (n "value") (var int "parameter_value"))
        tagged=V.Apply contract (V.Constructor (T.Function () item contract) (fq "D10" "CounterValue")) changed
        setterDispatch=value setterName [input "receiver" contract,input "value" int] contract (V.PatternMatch contract (var contract "receiver")
            [(V.ConstructorPattern contract (fq "D10" "CounterValue") [V.AsPattern item (V.WildcardPattern item) (n "typeValue")],
              V.LetDefinition contract (n "parameter_value") {inputTypes=[],outputType=int,body=var int "value"} tagged)])
        initial=V.Record item (Dict.singleton (n "value") (whole 0))
        box payload=V.Apply contract (V.Constructor (T.Function () item contract) (fq "D10" "CounterValue")) payload
        boxed=V.Apply (maybe contract) (V.Constructor (T.Function () contract (maybe contract)) (p "Morphir.SDK",p "Maybe",n "Just")) (box initial)
        updated=V.UpdateRecord item initial (Dict.singleton (n "value") (var int "parameter_value"))
        result=call "D10.IValue" contract (box updated)
        example=value "Example" [input "value" int] int (V.LetDefinition int (n "local_box") {inputTypes=[],outputType=maybe contract,body=boxed}
            (V.LetDefinition int (n "parameter_value") {inputTypes=[],outputType=int,body=var int "value"} result))
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition [itemType,interfaceType] []),(p "D10.Counter",moduleDefinition [] [getter,setter]),(p "D10.IValue",moduleDefinition [] [dispatch,setterDispatch]),(p "D10.Rules",moduleDefinition [] [example])]}
main : Program () () Never
main=Platform.worker {init=\_ -> ((),output (E.object [("interface-boxes",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
