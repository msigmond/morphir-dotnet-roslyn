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
tagged child=V.Apply (ref "Parent") (V.Constructor (T.Function () (ref "Child") (ref "Parent")) (fq "D10" "ChildParent")) child
pattern name=V.ConstructorPattern (ref "Parent") (fq "D10" "ChildParent") [V.AsPattern (ref "Child") (V.WildcardPattern (ref "Child")) (n name)]
reference=
    let
        child=(n "Child",{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () [{name=n "value",tpe=int}])}})
        parent=(n "Parent",{access=Public,value={doc="",value=T.CustomTypeDefinition [] {access=Public,value=Dict.singleton (n "ChildParent") [(n "arg1",ref "Child")]}}})
        branch=(pattern "rule",V.Apply int (V.Reference (T.Function () (ref "Parent") int) (fq "D10.Parent" "ReadDefaultBody")) (tagged (var (ref "Child") "rule")))
        dispatch=value Public "Read" [input "receiver" (ref "Parent")] int (V.PatternMatch int (var (ref "Parent") "receiver") [branch])
        body=value Private "ReadDefaultBody" [input "receiver" (ref "Parent")] int (V.PatternMatch int (var (ref "Parent") "receiver") [(pattern "typeValue",V.Field int (var (ref "Child") "typeValue") (n "value"))])
        initial=V.Record (ref "Child") (Dict.singleton (n "value") (V.Literal int (L.WholeNumberLiteral 0)))
        example=value Public "Example" [] int (V.Apply int (V.Reference (T.Function () (ref "Parent") int) (fq "D10.Parent" "Read")) (tagged initial))
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition [child,parent] []),(p "D10.Parent",moduleDefinition [] [dispatch,body]),(p "D10.Rules",moduleDefinition [] [example])]}
main : Program () () Never
main=Platform.worker {init=\_ -> ((),output (E.object [("inherited-fields",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
