module Example.D3.Rules exposing (category)

category : Int -> Bool -> Int
category age active =
    let
        localCategory : Int
        localCategory =
            0
    in
    if active then
        let
            localCategory1 : Int
            localCategory1 =
                1
        in
        if age >= 18 then
            let
                localCategory2 : Int
                localCategory2 =
                    2
            in
            localCategory2
        else
            localCategory1
    else
        localCategory
