module Example.FestivalRobotRental.RentalQuote exposing (..)
import Example.FestivalRobotRental exposing (RentalQuote)
import Morphir.SDK.Decimal as Decimal exposing (Decimal)
getAmount0 : RentalQuote -> Decimal
getAmount0 this = Decimal.add (Decimal.sub this.rental this.discount) this.delivery
