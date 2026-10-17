module Example.Compat.Floating exposing (Complex, roundBinary, nan, infinity, negativeInfinity, negativeZero, remainder, addComplex, subtractComplex, multiplyComplex, divideComplex, multiplyScalar, divideScalar, scalarDivide)

type alias Complex =
    { real : Float, imaginary : Float }

nan : Float
nan = 0 / 0

infinity : Float
infinity = 1 / 0

negativeInfinity : Float
negativeInfinity = Basics.negate infinity

negativeZero : Float
negativeZero = 0 / -1

exponent : Float -> Int -> Int
exponent value power =
    if value >= 2 then exponent (value / 2) (power + 1)
    else if value < 1 then exponent (value * 2) (power - 1)
    else power

roundBinary : Int -> Int -> Int -> Float -> Float
roundBinary precision minimum maximum value =
    if value == 0 || isNaN value || isInfinite value then value
    else if abs value < (2 ^ toFloat (minimum - precision)) then
        if value < 0 then negativeZero else 0
    else
        let
            magnitude : Float
            magnitude = abs value
            overflow : Float
            overflow = 2 ^ toFloat (maximum + 1)
            power : Int
            power = if magnitude >= overflow then maximum + 1 else exponent magnitude 0
            step : Float
            step = 2 ^ toFloat (max minimum power - precision + 1)
            scaled : Float
            scaled = if magnitude >= overflow then 0 else magnitude / step
            lower : Int
            lower = floor scaled
            fraction : Float
            fraction = scaled - toFloat lower
            nearest : Int
            nearest = if fraction > 0.5 || fraction == 0.5 && modBy 2 lower /= 0 then lower + 1 else lower
            rounded : Float
            rounded = if magnitude >= overflow then infinity else toFloat nearest * step
            result : Float
            result = if rounded >= overflow then infinity else rounded
        in
        if value < 0 then Basics.negate result else result

largestScale : Float -> Float -> Float
largestScale numerator denominator =
    if denominator * 2 > numerator || isInfinite (denominator * 2) then denominator
    else largestScale numerator (denominator * 2)

positiveRemainder : Float -> Float -> Float
positiveRemainder numerator denominator =
    if numerator < denominator then numerator
    else positiveRemainder (numerator - largestScale numerator denominator) denominator

remainder : Float -> Float -> Float
remainder left right =
    if isNaN left || isNaN right || isInfinite left || right == 0 then nan
    else if isInfinite right || left == 0 then left
    else
        let
            magnitude : Float
            magnitude = positiveRemainder (abs left) (abs right)
        in
        if left < 0 then Basics.negate magnitude else magnitude

addComplex : Complex -> Complex -> Complex
addComplex left right = {real=left.real+right.real,imaginary=left.imaginary+right.imaginary}

subtractComplex : Complex -> Complex -> Complex
subtractComplex left right = {real=left.real - right.real,imaginary=left.imaginary - right.imaginary}

multiplyComplex : Complex -> Complex -> Complex
multiplyComplex left right =
    { real=left.real * right.real - left.imaginary * right.imaginary
    , imaginary=left.imaginary*right.real+left.real*right.imaginary }

divideComplex : Complex -> Complex -> Complex
divideComplex left right =
    if abs right.imaginary < abs right.real then
        let
            ratio : Float
            ratio = right.imaginary / right.real
            denominator : Float
            denominator = right.real + right.imaginary * ratio
        in
        {real=(left.real+left.imaginary*ratio)/denominator,imaginary=(left.imaginary - left.real * ratio)/denominator}
    else
        let
            ratio : Float
            ratio = right.real / right.imaginary
            denominator : Float
            denominator = right.imaginary + right.real * ratio
        in
        {real=(left.imaginary+left.real*ratio)/denominator,imaginary=((Basics.negate left.real)+left.imaginary*ratio)/denominator}

finite : Float -> Bool
finite value = not (isNaN value || isInfinite value)

multiplyScalar : Complex -> Float -> Complex
multiplyScalar value scalar =
    if not (finite value.real) && not (finite value.imaginary) then {real=nan,imaginary=nan}
    else if not (finite value.real) then {real=value.real*scalar,imaginary=nan}
    else if not (finite value.imaginary) then {real=nan,imaginary=value.imaginary*scalar}
    else {real=value.real*scalar,imaginary=value.imaginary*scalar}

divideScalar : Complex -> Float -> Complex
divideScalar value scalar =
    if scalar == 0 || not (finite value.real) && not (finite value.imaginary) then {real=nan,imaginary=nan}
    else if not (finite value.real) then {real=value.real/scalar,imaginary=nan}
    else if not (finite value.imaginary) then {real=nan,imaginary=value.imaginary/scalar}
    else {real=value.real/scalar,imaginary=value.imaginary/scalar}

scalarDivide : Float -> Complex -> Complex
scalarDivide scalar value =
    if abs value.imaginary < abs value.real then
        let
            ratio : Float
            ratio = value.imaginary / value.real
            denominator : Float
            denominator = value.real + value.imaginary * ratio
        in
        {real=scalar/denominator,imaginary=((Basics.negate scalar)*ratio)/denominator}
    else
        let
            ratio : Float
            ratio = value.real / value.imaginary
            denominator : Float
            denominator = value.imaginary + value.real * ratio
        in
        {real=scalar*ratio/denominator,imaginary=(Basics.negate scalar)/denominator}
