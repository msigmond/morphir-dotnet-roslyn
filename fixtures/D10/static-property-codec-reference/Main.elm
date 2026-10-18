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
int=T.Reference () (p "Morphir.SDK",p "Basics",n "Int") []
unit=T.Unit ()
provider=T.Reference () (fq "D10" "Provider") []
callback=T.Function () unit int
var kind name=V.Variable kind (n name)
input name kind=(n name,kind,kind)
value name inputs result body=(n name,{access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}})
moduleDefinition types values={access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
reference=
    let
        providerType=(n "Provider",{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () [])}})
        union=(n "IFactory",{access=Public,value={doc="",value=T.CustomTypeDefinition [] {access=Public,value=Dict.singleton (n "ProviderFactory") [(n "arg1",provider)]}}})
        getter="GetValue__0__"
        getValue=value getter [] int (V.Literal int (L.WholeNumberLiteral 7))
        contract=value getter [input "implementation" callback] int (V.Apply int (var callback "implementation") (V.Unit unit))
        witnessName="call_t_Example.D10.IFactory_GetValue__0__"
        read=value "Read" [input witnessName callback] int (V.Apply int (var callback witnessName) (V.Unit unit))
        adapter=V.Lambda callback (V.UnitPattern unit) (V.Reference int (fq "D10.Provider" getter))
        run=value "Run" [] int (V.Apply int (V.Reference (T.Function () callback int) (fq "D10.Rules" "Read")) adapter)
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition [providerType,union] []),(p "D10.Provider",moduleDefinition [] [getValue]),(p "D10.IFactory",moduleDefinition [] [contract]),(p "D10.Rules",moduleDefinition [] [read,run])]}
main : Program () () Never
main=Platform.worker {init=\_ -> ((),output (E.object [("static-properties",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
