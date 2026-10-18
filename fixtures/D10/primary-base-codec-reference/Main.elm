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
pattern=V.ConstructorPattern (ref "Parent") (fq "D10" "ChildParent") [V.AsPattern (ref "Child") (V.WildcardPattern (ref "Child")) (n "typeValue")]
updated child number=V.UpdateRecord (ref "Child") child (Dict.singleton (n "value") number)
bind name expression body=V.LetDefinition (V.valueAttribute body) (n name) {inputTypes=[],outputType=int,body=expression} body
reference=
    let
        child=(n "Child",{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () [{name=n "value",tpe=int}])}})
        parent=(n "Parent",{access=Public,value={doc="",value=T.CustomTypeDefinition [] {access=Public,value=Dict.singleton (n "ChildParent") [(n "arg1",ref "Child")]}}})
        constructorName="New__0__None_global::System.Int32"
        parentConstructor=value Private constructorName [input "this" (ref "Parent"),input "value" int] (ref "Parent") (V.PatternMatch (ref "Parent") (var (ref "Parent") "this") [(pattern,tagged (updated (var (ref "Child") "typeValue") (var int "value")))])
        initial=V.Record (ref "Child") (Dict.singleton (n "value") (V.Literal int (L.WholeNumberLiteral 0)))
        childConstructor=value Public constructorName [input "value" int] (ref "Child") (bind "parameter_value" (var int "value") (updated initial (var int "parameter_value")))
        expression=V.Field int (updated initial (var int "parameter_value_1")) (n "value")
        example=value Public "Example" [input "value" int] int (bind "parameter_value" (var int "value") (bind "parameter_value_1" (var int "parameter_value") expression))
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition [child,parent] []),(p "D10.Child",moduleDefinition [] [childConstructor]),(p "D10.Parent",moduleDefinition [] [parentConstructor]),(p "D10.Rules",moduleDefinition [] [example])]}
main : Program () () Never
main=Platform.worker {init=\_ -> ((),output (E.object [("primary-base",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
