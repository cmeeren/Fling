module ExampleUsage

open System



module Domain =

  type UserId =
    | UserId of int
    member this.value = let (UserId x) = this in x

  type OrderLineId =
    | OrderLineId of int
    member this.value = let (OrderLineId x) = this in x

  type OrderId =
    | OrderId of int
    member this.value = let (OrderId x) = this in x

  type OrderLine =
    { Id: OrderLineId
      ProductName: string }

  type Coupon =
    { Code: string
      Expiration: DateTimeOffset }

  type PriceData = { NetPrice: decimal }

  type Order =
    { Id: OrderId
      OrderNumber: string
      Lines: OrderLine list
      AssociatedUsers: Set<UserId>
      Coupon: Coupon option
      PriceData: PriceData }



module Dtos =

  open Domain


  type OrderLineDto =
    { OrderId: int
      OrderLineId: int
      ProductName: string }

  type OrderAssociatedUserDto = { OrderId: int; UserId: int }

  type OrderCouponDto =
    { OrderId: int
      Code: string
      Expiration: DateTimeOffset }

  type OrderPriceDataDto = { OrderId: int; NetPrice: decimal }

  type OrderDto = { OrderId: int; OrderNumber: string }


  let orderToLineDtos (o: Order) =
    o.Lines
    |> List.map
         (fun ol ->
           { OrderId = o.Id.value
             OrderLineId = ol.Id.value
             ProductName = ol.ProductName })


  let orderToAssociatedUserDtos (o: Order) =
    o.AssociatedUsers
    |> Set.toList
    |> List.map
         (fun u ->
           { OrderId = o.Id.value
             UserId = u.value })


  let orderToCouponDto (o: Order) =
    o.Coupon
    |> Option.map
         (fun c ->
           { OrderId = o.Id.value
             Code = c.Code
             Expiration = c.Expiration })


  let orderToPriceDataDto (o: Order) =
    { OrderId = o.Id.value
      NetPrice = o.PriceData.NetPrice }


  let orderToDbDto (o: Order) =
    { OrderId = o.Id.value
      OrderNumber = o.OrderNumber }


  let orderFromDbDto
    (dto: OrderDto)
    (lines: OrderLineDto list)
    (users: OrderAssociatedUserDto list)
    (coupon: OrderCouponDto option)
    (price: OrderPriceDataDto)
    : Order =
    { Id = OrderId dto.OrderId
      OrderNumber = dto.OrderNumber
      Lines =
        lines
        |> List.map
             (fun lineDto ->
               { Id = OrderLineId lineDto.OrderLineId
                 ProductName = lineDto.ProductName })
      AssociatedUsers =
        users
        |> List.map (fun x -> UserId x.UserId)
        |> set
      Coupon =
        coupon
        |> Option.map
             (fun c ->
               { Code = c.Code
                 Expiration = c.Expiration })
      PriceData = { NetPrice = price.NetPrice } }



module DbScripts =


  // Mock types/functions
  type SqlConnection = SqlConnection of _connStr: string

  type SqlTransaction = IDisposable

  let createTransaction (_conn: SqlConnection) =
    { new IDisposable with
        member _.Dispose() = () }

  let commit (_tran: SqlTransaction) = ()



  let getAllOrders (_connStr: string) : Async<Dtos.OrderDto list> = failwith ""

  let getOrderById (_connStr: string) (_orderId: int) : Async<Dtos.OrderDto option> =
    failwith ""

  let getOrderLinesForOrder
    (_connStr: string)
    (_orderId: int)
    : Async<Dtos.OrderLineDto list> =
    failwith ""

  let getOrderLinesForOrders
    (_connStr: string)
    (_orderIds: int list)
    : Async<Dtos.OrderLineDto list> =
    failwith ""

  let getAssociatedUsersForOrder
    (_connStr: string)
    (_orderId: int)
    : Async<Dtos.OrderAssociatedUserDto list> =
    failwith ""

  let getAssociatedUsersForOrders
    (_connStr: string)
    (_orderIds: int list)
    : Async<Dtos.OrderAssociatedUserDto list> =
    failwith ""

  let getCouponForOrder
    (_connStr: string)
    (_orderId: int)
    : Async<Dtos.OrderCouponDto option> =
    failwith ""

  let getCouponForOrders
    (_connStr: string)
    (_orderIds: int list)
    : Async<Dtos.OrderCouponDto list> =
    failwith ""

  let getPriceDataForOrder
    (_connStr: string)
    (_orderId: int)
    : Async<Dtos.OrderPriceDataDto> =
    failwith ""

  let getPriceDataForOrders
    (_connStr: string)
    (_orderIds: int list)
    : Async<Dtos.OrderPriceDataDto list> =
    failwith ""

  let insertOrder
    (_conn: SqlConnection, _tran: SqlTransaction)
    (_dto: Dtos.OrderDto)
    : Async<unit> =
    async.Return()

  let updateOrder
    (_conn: SqlConnection, _tran: SqlTransaction)
    (_dto: Dtos.OrderDto)
    : Async<unit> =
    async.Return()

  let insertOrderLine
    (_conn: SqlConnection, _tran: SqlTransaction)
    (_dto: Dtos.OrderLineDto)
    : Async<unit> =
    async.Return()

  let updateOrderLine
    (_conn: SqlConnection, _tran: SqlTransaction)
    (_dto: Dtos.OrderLineDto)
    : Async<unit> =
    async.Return()

  let deleteOrderLine
    (_conn: SqlConnection, _tran: SqlTransaction)
    (_orderLineId: int)
    : Async<unit> =
    async.Return()

  let insertOrderAssociatedUser
    (_conn: SqlConnection, _tran: SqlTransaction)
    (_dto: Dtos.OrderAssociatedUserDto)
    : Async<unit> =
    async.Return()

  let updateOrderAssociatedUser
    (_conn: SqlConnection, _tran: SqlTransaction)
    (_dto: Dtos.OrderAssociatedUserDto)
    : Async<unit> =
    async.Return()

  let deleteOrderAssociatedUser
    (_conn: SqlConnection, _tran: SqlTransaction)
    (_orderId: int, _userId: int)
    : Async<unit> =
    async.Return()

  /// This is for checking that the Fling API works with SRTP matching the expected DTO
  /// shape
  let inline insertCoupon
    (_conn: SqlConnection, _tran: SqlTransaction)
    (dto: ^a)
    : Async<unit> =
    async {
      ignore (^a: (member OrderId : int) dto)
      ignore (^a: (member Code : string) dto)
      ignore (^a: (member Expiration : DateTimeOffset) dto)
    }

  let updateCoupon
    (_conn: SqlConnection, _tran: SqlTransaction)
    (_dto: Dtos.OrderCouponDto)
    : Async<unit> =
    async.Return()

  let deleteCoupon
    (_conn: SqlConnection, _tran: SqlTransaction)
    (_orderId: int)
    : Async<unit> =
    async.Return()

  let insertPriceData
    (_conn: SqlConnection, _tran: SqlTransaction)
    (_dto: Dtos.OrderPriceDataDto)
    : Async<unit> =
    async.Return()

  let updatePriceData
    (_conn: SqlConnection, _tran: SqlTransaction)
    (_dto: Dtos.OrderPriceDataDto)
    : Async<unit> =
    async.Return()



module Db =


  open Fling.Fling
  open Dtos
  open DbScripts
  open Domain


  module Order =


    let private load =
      createLoader orderFromDbDto (fun x -> x.OrderId)
      |> loadChild getOrderLinesForOrder
      |> loadChild getAssociatedUsersForOrder
      |> loadChild getCouponForOrder
      |> loadChild getPriceDataForOrder
      |> runLoader


    let private loadBatch =
      createBatchLoader orderFromDbDto (fun x -> x.OrderId)
      |> batchLoadChildren getOrderLinesForOrders (fun x -> x.OrderId)
      |> batchLoadChildren getAssociatedUsersForOrders (fun x -> x.OrderId)
      |> batchLoadOptChild getCouponForOrders (fun x -> x.OrderId)
      |> batchLoadChild getPriceDataForOrders (fun x -> x.OrderId)
      |> runBatchLoader


    let private save =
      saveRoot orderToDbDto insertOrder updateOrder
      |> saveChildren
           orderToLineDtos
           (fun dto -> dto.OrderLineId)
           insertOrderLine
           updateOrderLine
           deleteOrderLine
      |> saveChildren
           orderToAssociatedUserDtos
           (fun dto -> dto.OrderId, dto.UserId)
           insertOrderAssociatedUser
           updateOrderAssociatedUser
           deleteOrderAssociatedUser
      |> saveOptChild
           orderToCouponDto
           (fun dto -> dto.OrderId)
           insertCoupon
           updateCoupon
           deleteCoupon
      |> saveChild
           orderToPriceDataDto
           insertPriceData
           updatePriceData


    let saveChanges connStr (oldOrder: Order option) (newOrder: Order) =
      async {
        let conn = SqlConnection connStr
        use tran = createTransaction conn
        do! save (conn, tran) oldOrder newOrder
        commit tran
      }


    let byId connStr (OrderId oid) =
      async {
        match! getOrderById connStr oid with
        | None -> return None
        | Some orderDto ->
            let! order = load connStr orderDto
            return Some order
      }


    let all connStr =
      async {
        let! orderDtos = getAllOrders connStr
        return! loadBatch connStr orderDtos
      }
