module Example.FestivalRobotRental.DeliveryRule exposing (..)
import Example.FestivalRobotRental exposing (DeliveryRule)
new0NoneIntNoneInt : Int -> Int -> DeliveryRule
new0NoneIntNoneInt localDays remoteDays = { localDays = localDays, remoteDays = remoteDays }
days : DeliveryRule -> Char -> Int
days this zone = case zone of
    'L' -> this.localDays
    _ -> this.remoteDays
