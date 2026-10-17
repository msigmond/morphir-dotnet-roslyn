module Example.D7.Rules exposing (eligible, approved, defaultEligible)

meets0NoneGlobalSystemInt32NoneGlobalSystemInt32 : Int -> Int -> Bool
meets0NoneGlobalSystemInt32NoneGlobalSystemInt32 age minimum = age >= minimum

meets0NoneGlobalSystemBoolean : Bool -> Bool
meets0NoneGlobalSystemBoolean approval = approval

eligible : Int -> Bool
eligible age = meets0NoneGlobalSystemInt32NoneGlobalSystemInt32 age 18

approved : Bool -> Bool
approved approval = meets0NoneGlobalSystemBoolean approval

defaultEligible : Int -> Bool
defaultEligible age = meets0NoneGlobalSystemInt32NoneGlobalSystemInt32 age 18
