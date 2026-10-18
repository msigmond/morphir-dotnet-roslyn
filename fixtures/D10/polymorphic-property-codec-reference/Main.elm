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
        item=ref "Item"
        contract=ref "IValue"
        itemType=(n "Item",{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () [{name=n "value",tpe=int}])}})
        interfaceType=(n "IValue",{access=Public,value={doc="",value=T.CustomTypeDefinition [] {access=Public,value=Dict.singleton (n "ItemValue") [(n "arg1",item)]}}})
        getter=value "GetValue__0__" [input "receiver" item] int (V.Field int (var item "receiver") (n "value"))
        dispatch=value "GetValue__0__" [input "receiver" contract] int (V.PatternMatch int (var contract "receiver")
            [(V.ConstructorPattern contract (fq "D10" "ItemValue") [V.AsPattern item (V.WildcardPattern item) (n "rule")],call "D10.Item" item (var item "rule"))])
        read=value "Read" [input "item" (maybe contract)] int (V.PatternMatch int (var (maybe contract) "item")
            [(V.ConstructorPattern (maybe contract) (p "Morphir.SDK",p "Maybe",n "Nothing") [],whole 0),
             (V.ConstructorPattern (maybe contract) (p "Morphir.SDK",p "Maybe",n "Just") [V.AsPattern contract (V.WildcardPattern contract) (n "presentValue")],call "D10.IValue" contract (var contract "presentValue"))])
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition [itemType,interfaceType] []),(p "D10.Item",moduleDefinition [] [getter]),(p "D10.IValue",moduleDefinition [] [dispatch]),(p "D10.Rules",moduleDefinition [] [read])]}
main : Program () () Never
main=Platform.worker {init=\_ -> ((),output (E.object [("polymorphic-properties",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
