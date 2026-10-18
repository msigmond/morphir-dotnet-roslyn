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
var kind name=V.Variable kind (n name)
input name kind=(n name,kind,kind)
value access name inputs result body=(n name,{access=access,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}})
moduleDefinition types values={access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
domain name constructor=(n name,{access=Public,value={doc="",value=T.CustomTypeDefinition [] {access=Public,value=Dict.singleton (n constructor) [(n "arg1",ref "ValueHolder")]}}})
dispatch access owner name constructor=
    let
        call=V.Apply int (V.Reference (T.Function () (ref "ValueHolder") int) (fq "D10.ValueHolder" "Read")) (var (ref "ValueHolder") "rule")
        pattern=V.ConstructorPattern (ref owner) (fq "D10" constructor) [V.AsPattern (ref "ValueHolder") (V.WildcardPattern (ref "ValueHolder")) (n "rule")]
    in value access name [input "receiver" (ref owner)] int (V.PatternMatch int (var (ref owner) "receiver") [(pattern,call)])
reference=
    let
        holder=(n "ValueHolder",{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () [{name=n "value",tpe=int}])}})
        read=value Public "Read" [input "receiver" (ref "ValueHolder")] int (V.Field int (var (ref "ValueHolder") "receiver") (n "value"))
        original=dispatch Public "IValue" "Read" "ValueHolderValue"
        default=value Private "ReadDefaultBody" [input "receiver" (ref "IValue")] int (V.Literal int (L.WholeNumberLiteral 3))
        required=dispatch Private "IRequired" "Example.D10.IValue.Read" "ValueHolderRequired"
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition [holder,domain "IValue" "ValueHolderValue",domain "IRequired" "ValueHolderRequired"] []),(p "D10.ValueHolder",moduleDefinition [] [read]),(p "D10.IValue",moduleDefinition [] [original,default]),(p "D10.IRequired",moduleDefinition [] [required])]}
main : Program () () Never
main=Platform.worker {init=\_ -> ((),output (E.object [("reabstraction",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
