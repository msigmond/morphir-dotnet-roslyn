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
bool=T.Reference () (p "Morphir.SDK",p "Basics",n "Bool") []
value name result body=(n name,{access=Public,value={doc="",value={inputTypes=[],outputType=result,body=body}}})
moduleDefinition types values={access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
reference=
    let
        own=n "Example.D10.Child.Value"
        root=n "Example.D10.Parent.Value"
        fields=[{name=own,tpe=bool},{name=root,tpe=int}]
        child=(n "Child",{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () fields)}})
        parent=(n "Parent",{access=Public,value={doc="",value=T.CustomTypeDefinition [] {access=Public,value=Dict.singleton (n "ChildParent") [(n "arg1",ref "Child")]}}})
        initial=V.Record (ref "Child") (Dict.fromList [(own,V.Literal bool (L.BoolLiteral False)),(root,V.Literal int (L.WholeNumberLiteral 0))])
        values=[value "Own" bool (V.Field bool initial own),value "Root" int (V.Field int initial root)]
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition [child,parent] []),(p "D10.Rules",moduleDefinition [] values)]}
main : Program () () Never
main=Platform.worker {init=\_ -> ((),output (E.object [("hidden-storage",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
