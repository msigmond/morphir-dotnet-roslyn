module Example.FestivalRobotRental.RentalPolicy exposing (..)
import Example.FestivalRobotRental exposing (RentalPolicy, RentalOrder)
import Morphir.SDK.Decimal exposing (Decimal)
new0NoneIntNoneIntNoneDecimal : Int -> Int -> Decimal -> RentalPolicy
new0NoneIntNoneIntNoneDecimal minimumQuantity capacity deliveryFee =
    { minimumQuantity = minimumQuantity, capacity = capacity, deliveryFee = deliveryFee }
accepts : RentalPolicy -> RentalOrder -> Bool
accepts this order =
    (order.quantity >= this.minimumQuantity && order.quantity <= this.capacity) && order.days > 0
