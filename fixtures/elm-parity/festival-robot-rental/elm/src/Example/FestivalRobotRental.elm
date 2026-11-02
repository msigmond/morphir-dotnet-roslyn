module Example.FestivalRobotRental exposing (..)
import Dict exposing (Dict)
import Set exposing (Set)
import Morphir.SDK.Decimal exposing (Decimal)

type alias RentalState = Int
type alias RentalOrder =
    { orderId : String, robotModel : String, quantity : Int, days : Int
    , dailyRate : Decimal, discountPercent : Maybe Decimal, state : RentalState, zone : Char }
type alias RentalQuote =
    { orderId : String, rental : Decimal, discount : Decimal, delivery : Decimal, dispatchDays : Int }
type alias Review t = { item : t, accepted : Bool }
type alias RobotCatalog = { models : Set String, promotions : Dict String Decimal }
type alias ChargeTotal = { amount : Decimal }
type alias BatchPlan =
    { reviews : List (Review RentalOrder), quotes : List RentalQuote, total : ChargeTotal
    , robotCount : Int, cartonCount : Int, firstOrder : Maybe String, capacityUse : Float, rewardCredit : Decimal }
type alias RentalPolicy = { minimumQuantity : Int, capacity : Int, deliveryFee : Decimal }
type alias DeliveryRule = { localDays : Int, remoteDays : Int }
