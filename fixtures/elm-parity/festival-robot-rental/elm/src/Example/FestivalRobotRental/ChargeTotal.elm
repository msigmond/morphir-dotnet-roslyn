module Example.FestivalRobotRental.ChargeTotal exposing (..)
import Example.FestivalRobotRental exposing (ChargeTotal)
import Morphir.SDK.Decimal as Decimal
opAddition0NoneGlobalExampleFestivalRobotRentalChargeTotalNoneGlobalExampleFestivalRobotRentalChargeTotal : ChargeTotal -> ChargeTotal -> ChargeTotal
opAddition0NoneGlobalExampleFestivalRobotRentalChargeTotalNoneGlobalExampleFestivalRobotRentalChargeTotal left right =
    ChargeTotal (Decimal.add left.amount right.amount)
