port module Main exposing (main)

import Dict
import Json.Decode as D
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
sdk owner local args = T.Reference () (p "Morphir.SDK",p owner,n local) args
int = sdk "Basics" "Int" []
bool = sdk "Basics" "Bool" []
maybe t = sdk "Maybe" "Maybe" [t]
pair = T.Tuple () [int,bool]
var t name = V.Variable t (n name)
whole value = V.Literal int (L.WholeNumberLiteral value)
truth = V.Literal bool (L.BoolLiteral True)
input name t = (n name,t,t)
public inputs result body = {access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}}
moduleDefinition types values = {access=Public,value={types=types,values=values,doc=Nothing}}
letValue name value body = V.LetDefinition (V.valueAttribute body) (n name) {inputTypes=[],outputType=V.valueAttribute value,body=value} body
just t value = V.Apply (maybe t) (V.Constructor (T.Function () t (maybe t)) (p "Morphir.SDK",p "Maybe",n "Just")) value
call owner local arguments outputType values =
    let
        signature=List.foldr (T.Function ()) outputType arguments
        apply rest t value=case (rest,t) of
            ([],_) -> value
            (arg::tail,T.Function _ _ result) -> apply tail result (V.Apply result value arg)
            _ -> Debug.todo "Original reference application mismatch"
    in apply values signature (V.Reference signature (p "Example",p owner,n local))
first subject = V.PatternMatch int subject
    [(V.TuplePattern pair [V.AsPattern int (V.WildcardPattern int) (n "tupleElement"),V.WildcardPattern bool],var int "tupleElement")]
tupleValues prefix =
    let
        name local=n (prefix++local)
        literal=V.Tuple pair [var int "code",truth]
        deconstruction=letValue "deconstructionValue" literal
            (letValue "local_value" (first (var pair "deconstructionValue")) (var int "local_value"))
        guard=V.PatternMatch int (var (maybe pair) "value")
            [(V.ConstructorPattern (maybe pair) (p "Morphir.SDK",p "Maybe",n "Nothing") [],whole -1)
            ,(V.ConstructorPattern (maybe pair) (p "Morphir.SDK",p "Maybe",n "Just") [V.AsPattern pair (V.WildcardPattern pair) (n "presentValue")],first (var pair "presentValue"))]
    in Dict.fromList
        [(name "Code",public [input "result" pair] int (first (var pair "result")))
        ,(name "Deconstruct",public [input "code" int] int deconstruction)
        ,(name "Reference",public [input "code" int] (maybe pair) (just pair literal))
        ,(name "Guard",public [input "value" (maybe pair)] int guard)]
enumType owner name = T.Reference () (p "Example",p owner,n name) []
unpack owner name variable subject =
    let enum=enumType owner name in
    V.PatternMatch int subject [(V.ConstructorPattern enum (p "Example",p owner,n name) [V.AsPattern int (V.WildcardPattern int) (n variable)],var int variable)]
pack owner name value =
    let enum=enumType owner name in
    V.Apply enum (V.Constructor (T.Function () int enum) (p "Example",p owner,n name)) value
flagBody owner name =
    let
        enum=enumType owner name
        integer=T.Reference () (p "Example",p "Compat.Integers",n "Integer") []
        fromSmall value=call "Compat.Integers" "fromSmall" [int] integer [value]
        a=fromSmall (unpack owner name "enumValue" (var enum "a"))
        b=fromSmall (unpack owner name "enumValue_1" (var enum "b"))
        combined=call "Compat.Integers" "bitOr" [integer,integer] integer [a,b]
        wrapped=call "Compat.Integers" "wrap" [int,bool,integer] integer [whole 32,truth,combined]
        small=call "Compat.Integers" "toSmall" [integer] int [wrapped]
    in unpack owner name "enumValue_2" (pack owner name small)
requireModule owner modules =
    case Dict.get (p owner) modules of
        Just controlled -> controlled.value
        Nothing -> Debug.todo "Original prerequisite module missing"
describeBody owner name =
    let
        enum=enumType owner name
        signature=T.Function () enum (T.Function () enum bool)
        equal=V.Reference signature (p "Morphir.SDK",p "Basics",n "equal")
        access=var enum "access"
        read=V.Reference enum (p "Example",p (owner++"."++name),n "Read")
        comparison=V.Apply bool (V.Apply (T.Function () enum bool) equal access) read
    in V.Tuple pair [unpack owner name "enumValue" access,comparison]
-- A complete, independently constructed dependency specification tests public and
-- opaque custom types separately from source business lowering.
model =
    let
        constructors=Dict.singleton (n "Token") [(n "arg1",int)]
        custom=T.CustomTypeDefinition [] {access=Public,value=constructors}
        hidden=T.CustomTypeDefinition [] {access=Private,value=constructors}
        defs=Dict.fromList [(n "Token",{access=Public,value={doc="public constructors",value=custom}}),(n "Hidden",{access=Public,value={doc="private constructors",value=hidden}})]
        specs=Dict.fromList [(n "Token",{doc="public constructors",value=T.CustomTypeSpecification [] constructors}),(n "Hidden",{doc="private constructors",value=T.OpaqueTypeSpecification []})]
        packageSpec={modules=Dict.singleton (p "Types") {types=specs,values=Dict.empty,doc=Nothing}}
    in Library (p "Model") (Dict.singleton (p "External") packageSpec) {modules=Dict.singleton (p "Types") (moduleDefinition defs Dict.empty)}
references enums previous =
    case (enums,previous) of
        (Library package dependencies seed,Library _ oldDependencies baseline) ->
            let
                tuples=Library package dependencies {modules=Dict.singleton (p "D8.Results") (moduleDefinition Dict.empty (tupleValues ""))}
                seedData=requireModule "D8" seed.modules
                seedConstants=requireModule "D8.Access" seed.modules
                enum=enumType "D8" "Access"
                flagValues=Dict.singleton (n "Flags") (public [input "a" enum,input "b" enum] int (flagBody "D8" "Access"))
                helpers=Dict.filter (\key _ -> key==p "Compat.Integers" || key==p "Compat.Floating") baseline.modules
                flags=Library package dependencies {modules=Dict.union helpers (Dict.fromList [(p "D8",moduleDefinition seedData.types Dict.empty),(p "D8.Access",moduleDefinition Dict.empty seedConstants.values),(p "D8.Rules",moduleDefinition Dict.empty flagValues)])}
                sampleEnum=enumType "CurrentSupported" "SampleAccess"
                sampleValues=Dict.insert (n "EnumDescribe") (public [input "access" sampleEnum] pair (describeBody "CurrentSupported" "SampleAccess")) (tupleValues "Tuple")
                    |> Dict.insert (n "EnumFlags") (public [input "a" sampleEnum,input "b" sampleEnum] int (flagBody "CurrentSupported" "SampleAccess"))
                dataType=T.CustomTypeDefinition [] {access=Public,value=Dict.singleton (n "SampleAccess") [(n "arg1",int)]}
                constants=Dict.fromList (List.map (\(name,value)->(n name,public [] sampleEnum (pack "CurrentSupported" "SampleAccess" (whole value)))) [("None",0),("Read",1),("Alias",1),("Write",2),("All",3)])
                mergeValues controlled=let definition=controlled.value in {controlled|value={definition|values=Dict.union sampleValues definition.values}}
                mergeData controlled=let definition=controlled.value in {controlled|value={definition|types=Dict.insert (n "SampleAccess") {access=Public,value={doc="",value=dataType}} definition.types}}
                sampleModules=baseline.modules |> Dict.update (p "CurrentSupported.CurrentSupportedExample") (Maybe.map mergeValues) |> Dict.update (p "CurrentSupported") (Maybe.map mergeData) |> Dict.insert (p "CurrentSupported.SampleAccess") (moduleDefinition Dict.empty constants)
                sample=Library package oldDependencies {modules=sampleModules}
            in E.object [("tuples",encodeVersionedDistribution tuples),("flags",encodeVersionedDistribution flags),("sample",encodeVersionedDistribution sample),("model",encodeVersionedDistribution model)]
main : Program E.Value () Never
main=Platform.worker
    {init=\flags -> ((),output (case D.decodeValue (D.map2 Tuple.pair (D.field "enums" decodeVersionedDistribution) (D.field "previous" decodeVersionedDistribution)) flags of
        Ok (enums,previous)->references enums previous
        Err error->E.object [("error",E.string (D.errorToString error))]))
    ,update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
