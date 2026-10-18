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
call owner name kind receiver=V.Apply int (V.Reference (T.Function () kind int) (fq owner name)) receiver
reference=
    let
        parent=ref "Parent"
        leaf=ref "Leaf"
        storage=T.Record () []
        leafType=(n "Leaf",{access=Public,value={doc="",value=T.TypeAliasDefinition [] storage}})
        parentType=(n "Parent",{access=Public,value={doc="",value=T.CustomTypeDefinition [] {access=Public,value=Dict.fromList [(n "ParentParent",[(n "arg1",storage)]),(n "LeafParent",[(n "arg1",leaf)])]}}})
        parentBody=(n "ReadDefaultBody",{access=Private,value={doc="",value={inputTypes=[input "receiver" parent],outputType=int,body=whole 3}}})
        leafBody=value "Read" [input "receiver" leaf] int (whole 7)
        parentPayload=V.Apply parent (V.Constructor (T.Function () storage parent) (fq "D10" "ParentParent")) (var storage "rule")
        dispatch=value "Read" [input "receiver" parent] int (V.PatternMatch int (var parent "receiver")
            [(V.ConstructorPattern parent (fq "D10" "ParentParent") [V.AsPattern storage (V.WildcardPattern storage) (n "rule")],call "D10.Parent" "ReadDefaultBody" parent parentPayload),
             (V.ConstructorPattern parent (fq "D10" "LeafParent") [V.AsPattern leaf (V.WildcardPattern leaf) (n "rule")],call "D10.Leaf" "Read" leaf (var leaf "rule"))])
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition [parentType,leafType] []),(p "D10.Parent",moduleDefinition [] [dispatch,parentBody]),(p "D10.Leaf",moduleDefinition [] [leafBody])]}
main : Program () () Never
main=Platform.worker {init=\_ -> ((),output (E.object [("concrete-base",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
