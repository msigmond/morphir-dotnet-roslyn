port module Main exposing (main)

import D4Sample
import Dict
import Json.Decode as D
import Json.Encode as E
import Morphir.IR.AccessControlled exposing (Access(..))
import Morphir.IR.Distribution exposing (Distribution(..))
import Morphir.IR.FormatVersion.Codec exposing (decodeVersionedDistribution, encodeVersionedDistribution)
import Morphir.IR.Name as N
import Morphir.IR.Path as P
import Morphir.IR.Type as T
import Morphir.IR.Value as V
import Morphir.IR.Literal as L
import Platform

port output : E.Value -> Cmd msg
n = N.fromString
p = P.fromString
int = T.Reference () (p "Morphir.SDK",p "Basics",n "Int") []
bool = T.Reference () (p "Morphir.SDK",p "Basics",n "Bool") []
float = T.Reference () (p "Morphir.SDK",p "Basics",n "Float") []
integer = T.Reference () (p "Example",p "Compat.Integers",n "Integer") []
complex = T.Reference () (p "Example",p "Compat.Floating",n "Complex") []
maybe t = T.Reference () (p "Morphir.SDK",p "Maybe",n "Maybe") [t]
var t name = V.Variable t (n name)
whole value = V.Literal int (L.WholeNumberLiteral value)
real value = V.Literal float (L.FloatLiteral value)
boolean value = V.Literal bool (L.BoolLiteral value)
input name t = (n name,t,t)
wrap inputs result body = {access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}}
apply arguments functionType function =
    case arguments of
        [] -> function
        argument :: rest ->
            case functionType of
                T.Function _ _ result -> apply rest result (V.Apply result function argument)
                _ -> Debug.todo "Reference application has an unexpected type"
sdk name types result arguments =
    let signature = List.foldr (T.Function ()) result types in
    apply arguments signature (V.Reference signature (p "Morphir.SDK",p "Basics",n name))
call modules owner name arguments =
    case Dict.get (p owner) modules |> Maybe.andThen (\controlled -> Dict.get (n name) controlled.value.values) of
        Nothing -> Debug.todo ("Independent helper definition missing: " ++ owner ++ "." ++ name)
        Just controlled ->
            let
                definition = controlled.value.value
                signature = List.foldr (\(_,_,t) result -> T.Function () t result) definition.outputType definition.inputTypes
            in apply arguments signature (V.Reference signature (p "Example",p owner,n name))
nothing t = V.Constructor (maybe t) (p "Morphir.SDK",p "Maybe",n "Nothing")
just t value = V.Apply (maybe t) (V.Constructor (T.Function () t (maybe t)) (p "Morphir.SDK",p "Maybe",n "Just")) value
caseMaybe t result subject name absent present = V.PatternMatch result subject
    [(V.ConstructorPattern (maybe t) (p "Morphir.SDK",p "Maybe",n "Nothing") [],absent)
    ,(V.ConstructorPattern (maybe t) (p "Morphir.SDK",p "Maybe",n "Just") [V.AsPattern t (V.WildcardPattern t) (n name)],present)]
limbs negative digits = V.Record integer (Dict.fromList [(n "digits",V.List (T.Reference () (p "Morphir.SDK",p "List",n "List") [int]) (List.map whole digits)),(n "negative",boolean negative)])

smallDigits value = if value==0 then [] else modBy 65536 value :: smallDigits (value // 65536)
integerConstant value = limbs (value<0) (smallDigits (abs value))

values modules =
    let
        i name args = call modules "Compat.Integers" name args
        f name args = call modules "Compat.Floating" name args
        from value = i "fromSmall" [value]
        narrow bits signed value = i "toSmall" [i "wrap" [whole bits,boolean signed,value]]
        wide bits signed value = i "wrap" [whole bits,boolean signed,value]
        smallOperation name leftValue rightValue = narrow 32 True (i name [from leftValue,from rightValue])
        rounded precision value = f "roundBinary" [whole precision,whole (if precision==24 then -126 else -14),whole (if precision==24 then 127 else 15),value]
        left = var int "left"
        right = var int "right"
        wideLeft = var integer "left"
        wideRight = var integer "right"
        zero = whole 0
        divide = smallOperation "divide" left right
        compared name value constant = sdk name [int,int] bool [i "compare" [value,integerConstant constant],zero]
        divideBody = V.IfThenElse int (sdk "equal" [int,int] bool [right,zero]) zero
            (V.IfThenElse int (sdk "equal" [int,int] bool [left,whole -2147483648])
                (V.IfThenElse int (sdk "equal" [int,int] bool [right,whole -1]) zero divide)
                divide)
        narrowBody = V.IfThenElse int (compared "lessThan" (var integer "value") -2147483648) zero
            (V.IfThenElse int (compared "greaterThan" (var integer "value") 2147483647) zero (narrow 32 True (var integer "value")))
        finite = sdk "not" [bool] bool [sdk "or" [bool,bool] bool [sdk "isNaN" [float] bool [var float "value"],sdk "isInfinite" [float] bool [var float "value"]]]
        finiteBody = V.IfThenElse integer finite (i "fromFiniteFloat" [var float "value"]) (from zero)
        smallInputs = [input "left" int,input "right" int]
        wideInputs = [input "left" integer,input "right" integer]
        floatInputs = [input "left" float,input "right" float]
        realPart = V.Field float (var complex "value") (n "real")
        imaginaryPart = V.Field float (var complex "value") (n "imaginary")
        finiteReal = sdk "not" [bool] bool [sdk "or" [bool,bool] bool [sdk "isNaN" [float] bool [realPart],sdk "isInfinite" [float] bool [realPart]]]
    in Dict.fromList
        [(n "NumericSize",wrap [] int (whole 4))
        ,(n "NumericBigDouble",wrap [input "value" integer] float (i "toTruncatedFloat" [var integer "value"]))
        ,(n "NumericBigLogical",wrap [input "value" integer,input "count" int] integer (i "shiftBigUnsigned" [var integer "value",var int "count"]))
        ,(n "NumericComplexInteger",wrap [input "value" complex] integer (V.IfThenElse integer (sdk "equal" [float,float] bool [imaginaryPart,real 0]) (V.IfThenElse integer finiteReal (i "fromFiniteFloat" [realPart]) (from zero)) (from zero)))
        ,(n "NumericNext",wrap [input "value" int] int (smallOperation "add" (var int "value") (whole 1)))
        ,(n "NumericPromotion",wrap smallInputs int (smallOperation "add" left right))
        ,(n "NumericByte",wrap [input "value" int] int (narrow 8 False (from (var int "value"))))
        ,(n "NumericSByte",wrap [input "value" int] int (narrow 8 True (from (var int "value"))))
        ,(n "NumericShort",wrap [input "value" int] int (narrow 16 True (from (var int "value"))))
        ,(n "NumericUShort",wrap [input "value" int] int (narrow 16 False (from (var int "value"))))
        ,(n "NumericChar",wrap [input "value" int] int (narrow 16 False (from (var int "value"))))
        ,(n "NumericUInt",wrap smallInputs int (narrow 32 False (i "add" [from left,from right])))
        ,(n "NumericLong",wrap wideInputs integer (wide 64 True (i "multiply" [wideLeft,wideRight])))
        ,(n "NumericULong",wrap wideInputs integer (wide 64 False (i "add" [wideLeft,wideRight])))
        ,(n "NumericNative",wrap wideInputs integer (wide 64 True (i "add" [wideLeft,wideRight])))
        ,(n "NumericUNative",wrap wideInputs integer (wide 64 False (i "add" [wideLeft,wideRight])))
        ,(n "NumericInt128",wrap wideInputs integer (wide 128 True (i "add" [wideLeft,wideRight])))
        ,(n "NumericUInt128",wrap wideInputs integer (wide 128 False (i "multiply" [wideLeft,wideRight])))
        ,(n "NumericBigInteger",wrap wideInputs integer (i "multiply" [wideLeft,wideRight]))
        ,(n "NumericSingle",wrap floatInputs float (rounded 24 (sdk "multiply" [float,float] float [var float "left",var float "right"])))
        ,(n "NumericDouble",wrap floatInputs float (sdk "divide" [float,float] float [var float "left",var float "right"]))
        ,(n "NumericHalf",wrap floatInputs float (rounded 11 (rounded 24 (sdk "add" [float,float] float [var float "left",var float "right"]))))
        ,(n "NumericComplex",wrap [input "left" complex,input "right" complex] complex (f "divideComplex" [var complex "left",var complex "right"]))
        ,(n "NumericDivide",wrap smallInputs int divideBody)
        ,(n "NumericNarrow",wrap [input "value" integer] int narrowBody)
        ,(n "NumericFinite",wrap [input "value" float] integer finiteBody)
        ,(n "NumericLargeLong",wrap [] integer (limbs False [1,0,0,32]))
        ,(n "NumericMaximum128",wrap [] integer (limbs False [65535,65535,65535,65535,65535,65535,65535,65535]))
        ,(n "NumericConstruct128",wrap [input "upper" integer,input "lower" integer] integer (wide 128 True (i "add" [i "shiftLeft" [var integer "upper",whole 64],var integer "lower"])))
        ,(n "NumericHalfMinimum",wrap [] float (real 0.000000059604644775390625))
        ,(n "NumericNaN",wrap [] float (f "nan" []))
        ,(n "NumericInfinity",wrap [] float (f "infinity" []))
        ,(n "NumericNegativeZero",wrap [] float (sdk "negate" [float] float [real 0]))
        ,(n "NumericNullable",wrap [input "value" (maybe int)] (maybe integer)
            (caseMaybe int (maybe integer) (var (maybe int) "value") "presentValue" (nothing integer) (just integer (wide 64 True (from (var int "presentValue"))))))
        ,(n "NumericBits",wrap [input "value" int,input "shift" int] int (narrow 32 True (i "shiftUnsigned" [whole 32,from (smallOperation "bitXor" (var int "value") (whole -1)),sdk "modBy" [int,int] int [whole 32,var int "shift"]])))
        ,(n "NumericIntegerToDouble",wrap [input "value" integer] float (i "toBinaryFloat" [whole 53,var integer "value"]))
        ,(n "NumericDoubleToByte",wrap [input "value" float] int (i "toSmall" [i "floatCast" [whole 8,boolean False,var float "value"]]))]

privateFunctions controlled =
    let definition=controlled.value in
    {controlled|value={definition|values=Dict.map (\_ entry -> {entry|access=Private}) definition.values}}

references helpers =
    case helpers of
        Library package dependencies originalHelpers ->
            let
                helperDefinition = {modules=Dict.map (\_ controlled -> privateFunctions controlled) originalHelpers.modules}
                additions = values helperDefinition.modules
                wrapped = Library package dependencies {modules=Dict.insert (p "D5.Rules") {access=Public,value={types=Dict.empty,values=Dict.fromList (case Dict.get (n "NumericNext") additions of
                    Just definition -> [(n "Next",definition)]
                    Nothing -> Debug.todo "Missing wrap definition"),doc=Nothing}} helperDefinition.modules}
                numeric = Library package dependencies {modules=Dict.insert (p "D5.Rules") {access=Public,value={types=Dict.empty,values=additions,doc=Nothing}} helperDefinition.modules}
                sample = case D4Sample.sample of
                    Library samplePackage sampleDependencies definition ->
                        let
                            extend controlled = let original=controlled.value in {controlled|value={original|values=Dict.union additions original.values}}
                        in Library samplePackage sampleDependencies {modules=Dict.union helperDefinition.modules (Dict.update (p "CurrentSupported.CurrentSupportedExample") (Maybe.map extend) definition.modules)}
            in E.object [("numeric",encodeVersionedDistribution numeric),("sample",encodeVersionedDistribution sample),("wrap",encodeVersionedDistribution wrapped)]

main : Program E.Value () Never
main = Platform.worker
    {init=\flags -> ((),output (case D.decodeValue decodeVersionedDistribution flags of
        Ok helpers -> references helpers
        Err error -> E.object [("error",E.string (D.errorToString error))]))
    ,update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
